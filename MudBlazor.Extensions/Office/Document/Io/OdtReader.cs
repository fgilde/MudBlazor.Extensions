using System.Text;
using System.Xml;
using System.Xml.Linq;
using MudBlazor.Extensions.Helper.Internal;
using static MudBlazor.Extensions.Office.Document.OdtStyles;

namespace MudBlazor.Extensions.Office.Document;

internal static class OdtReader
{
    internal static TextDocument Read(byte[] bytes, PackageLimits limits = null)
    {
        try
        {
            var current = DocumentLoadLimits.Current;
            using var package = OpenDocumentFile.OpenPackage(bytes, limits ?? PackageLimits.Default with { MaxTotalLength = current.PackageBytes, Budget = current.TextCharacters });
            return new Reader(package).Read();
        }
        catch (Exception e) when (e is not (InvalidDataException or OutOfMemoryException))
        {
            throw new InvalidDataException("The file is not a readable OpenDocument text document.", e);
        }
    }

    private sealed class Reader(OpenDocumentFile package)
    {
        private const int MaxColumns = 63;
        private const int MaxRepeat = 1000;
        private const int MaxRows = 32767;
        private const int MaxSpaces = 1000;
        private const int MaxDepth = 100;
        private const int CellCost = 32;
        private const int ParagraphCost = 8;
        private const int PictureBytesPerUnit = 16;
        private const long EmuPerTwip = 635;
        private const long MaxEmu = 20116800;

        private static readonly (string Element, HeaderFooterKind Kind, HeaderFooterType Type)[] Stories =
        [
            ("header", HeaderFooterKind.Header, HeaderFooterType.Default), ("footer", HeaderFooterKind.Footer, HeaderFooterType.Default),
            ("header-left", HeaderFooterKind.Header, HeaderFooterType.Even), ("footer-left", HeaderFooterKind.Footer, HeaderFooterType.Even),
            ("header-first", HeaderFooterKind.Header, HeaderFooterType.First), ("footer-first", HeaderFooterKind.Footer, HeaderFooterType.First)
        ];

        private static readonly HashSet<string> Containers =
        [
            "section", "table-of-content", "index-body", "index-title", "illustration-index", "alphabetical-index", "bibliography", "user-index", "object-index",
            "table-index"
        ];

        private static readonly HashSet<string> SkippedInline =
            ["tracked-changes", "change", "change-start", "change-end", "ruby-text", "bookmark", "bookmark-start", "bookmark-end", "soft-page-break"];

        private readonly TextDocument _document = TextDocument.Create();
        private readonly List<AbstractNumbering> _abstracts = [];
        private readonly List<NumberingInstance> _instances = [];
        private readonly Dictionary<(bool, string), int> _abstractByStyle = new();
        private readonly Dictionary<(bool, string), int> _lastList = new();
        private readonly Dictionary<string, int> _listIds = new();
        private OdtStyles _styles;
        private DocumentStyles _definitions;
        private List<Block> _floating = [];
        private int _nextAbstract;
        private int _nextInstance;
        private int _width;
        private int _depth;
        private readonly Dictionary<string, (string Part, byte[] Data, string Type)> _pictures = new();
        private bool _header;
        private bool _pageBreak;

        public TextDocument Read()
        {
            if (package.ReadXml("META-INF/manifest.xml")?.Descendants().Any(e => e.Name.LocalName == "encryption-data") == true)
                throw new InvalidDataException("The document is encrypted; encrypted OpenDocument files are not supported.");
            var content = package.ReadXml("content.xml") ?? throw new InvalidDataException("The file contains no content.xml.");
            var body = content.Root?.Element(OfficeNs + "body")?.Element(OfficeNs + "text") ?? throw new InvalidDataException("The file is not an OpenDocument text document.");
            var styles = package.ReadXml("styles.xml");
            _styles = new OdtStyles(content, styles);
            _definitions = _document.Styles;
            _nextAbstract = _document.Numbering.Abstracts.Select(a => a.Id + 1).DefaultIfEmpty(0).Max();
            _nextInstance = Lists.NextId(_document.Numbering);
            var master = styles?.Root?.Element(OfficeNs + "master-styles")?.Elements(StyleNs + "master-page").FirstOrDefault();
            var section = _document.Sections[0] with { Page = Page(master, _document.Sections[0].Page) };
            _width = section.Page.TextWidth;

            var blocks = Blocks(body.Elements());
            if (blocks.Count == 0 || blocks[^1] is not Paragraph) blocks.Add(new Paragraph());
            if (blocks[0] is Paragraph { Format.PageBreakBefore: true } first) blocks[0] = first with { Format = first.Format with { PageBreakBefore = null } };
            _document.BlockList.Clear();
            _document.BlockList.AddRange(blocks.Select(_document.Identify));
            _document.SectionList[0] = HeadersFooters(master, section);
            _document.Styles = _definitions;
            if (_instances.Count > 0) _document.Numbering = new Numbering([.. _document.Numbering.Abstracts, .. _abstracts], [.. _document.Numbering.Instances, .. _instances]);
            if (package.ReadXml("meta.xml")?.Root?.Element(OfficeNs + "meta")?.Element(DcNs + "title")?.Value is { Length: > 0 } title)
                _document.Properties = _document.Properties with { Title = title };
            _document.InvalidateIndex();
            return _document;
        }

        private PageSetup Page(XElement master, PageSetup basis)
        {
            var layout = _styles.PageLayout(Attr(master, StyleNs + "page-layout-name"));
            var properties = layout?.Element(StyleNs + "page-layout-properties");
            if (properties == null || Twips(Attr(properties, FoNs + "page-width")) is not { } width || Twips(Attr(properties, FoNs + "page-height")) is not { } height) return basis;
            double Margin(string name) => Math.Max(0, Twips(Attr(properties, FoNs + name)) ?? 0);
            double Extent(string kind, string spacing) => Shown(master, kind) && layout.Element(StyleNs + kind + "-style")?.Element(StyleNs + "header-footer-properties") is { } story
                ? Math.Max(0, Twips(Attr(story, FoNs + "min-height")) ?? 0) + Math.Max(0, Twips(Attr(story, FoNs + spacing)) ?? 0)
                : 0;
            var (top, bottom) = (Margin("margin-top"), Margin("margin-bottom"));
            var page = new PageSetup((int)Math.Round(width), (int)Math.Round(height),
                Attr(properties, StyleNs + "print-orientation") == "landscape" || width > height ? PageOrientation.Landscape : PageOrientation.Portrait,
                (int)Math.Round(top + Extent("header", "margin-bottom")), (int)Math.Round(Margin("margin-right")), (int)Math.Round(bottom + Extent("footer", "margin-top")),
                (int)Math.Round(Margin("margin-left")), Shown(master, "header") ? (int)Math.Round(top) : basis.HeaderDistance,
                Shown(master, "footer") ? (int)Math.Round(bottom) : basis.FooterDistance, 0);
            return page.Width is >= 720 and <= 31680 && page.Height is >= 720 and <= 31680 && page.TextWidth > 0 && page.Height - page.MarginTop - page.MarginBottom > 0
                ? page
                : basis;
        }

        private static bool Shown(XElement master, string story) => master?.Element(StyleNs + story) is { } element && Attr(element, StyleNs + "display") != "false";

        private Section HeadersFooters(XElement master, Section section)
        {
            var created = new List<OpcRelationship>(_document.Package?.MainRelationships ?? []);
            var headers = new Dictionary<HeaderFooterType, string>();
            var footers = new Dictionary<HeaderFooterType, string>();
            (_header, _pageBreak) = (true, false);
            foreach (var (element, kind, type) in Stories.Where(s => Shown(master, s.Element)))
            {
                var blocks = Blocks(master.Element(StyleNs + element)!.Elements());
                if (blocks.Count == 0 || blocks[^1] is not Paragraph) blocks.Add(new Paragraph());
                var id = OpcPackageWriter.NextId(created);
                created.Add(new OpcRelationship(id, "", "", null));
                _document.HeaderFooterList.Add(new HeaderFooter { Id = id, Kind = kind, Blocks = blocks.Select(_document.Identify).ToList() });
                (kind == HeaderFooterKind.Header ? headers : footers)[type] = id;
            }

            _header = false;
            _document.EvenAndOddHeaders = headers.ContainsKey(HeaderFooterType.Even) || footers.ContainsKey(HeaderFooterType.Even);
            var titlePage = headers.ContainsKey(HeaderFooterType.First) || footers.ContainsKey(HeaderFooterType.First);
            foreach (var references in new[] { headers, footers })
                if (references.TryGetValue(HeaderFooterType.Default, out var id))
                {
                    if (_document.EvenAndOddHeaders) references.TryAdd(HeaderFooterType.Even, id);
                    if (titlePage) references.TryAdd(HeaderFooterType.First, id);
                }

            return section with { Headers = headers, Footers = footers, TitlePage = titlePage };
        }

        private List<Block> Blocks(IEnumerable<XElement> elements)
        {
            var blocks = new List<Block>();
            foreach (var element in elements) Nested(() => BlockOf(element, blocks));
            return blocks;
        }

        private void BlockOf(XElement element, List<Block> blocks)
        {
            if (element.Name == TextNs + "p" || element.Name == TextNs + "h")
                Add(blocks, ParagraphOf(element, element.Nodes(), null, null));
            else if (element.Name == TextNs + "list")
                List(element, blocks, 0, null);
            else if (element.Name == TableNs + "table")
                blocks.Add(TableOf(element));
            else if (element.Name == DrawNs + "frame" || element.Name == DrawNs + "a")
                Frame(element, blocks);
            else if (element.Name.Namespace == TextNs && Containers.Contains(element.Name.LocalName))
                foreach (var child in element.Elements())
                    Nested(() => BlockOf(child, blocks));
        }

        private void Frame(XElement element, List<Block> blocks)
        {
            var pending = _pageBreak;
            var paragraph = ParagraphOf(null, [element], null, null);
            if (paragraph.Inlines.Count > 0)
            {
                Add(blocks, paragraph);
                return;
            }

            _pageBreak = pending;
            foreach (var block in _floating)
                if (_pageBreak && block is Paragraph first)
                {
                    blocks.Add(first with { Format = first.Format with { PageBreakBefore = true } });
                    _pageBreak = false;
                }
                else
                    blocks.Add(block);

            _floating = [];
        }

        private void Add(List<Block> blocks, Paragraph paragraph)
        {
            blocks.Add(paragraph);
            blocks.AddRange(_floating);
            _floating = [];
        }

        private void List(XElement list, List<Block> blocks, int level, int? numId)
        {
            numId ??= Instance(list);
            foreach (var item in list.Elements())
            {
                var numbered = item.Name == TextNs + "list-item";
                if (!numbered && item.Name != TextNs + "list-header") continue;
                foreach (var child in item.Elements())
                {
                    var reference = new NumberingRef(numId.Value, Math.Min(level, 8));
                    if (child.Name == TextNs + "list")
                        Nested(() => List(child, blocks, level + 1, numId));
                    else if (child.Name == TextNs + "p" || child.Name == TextNs + "h")
                        Add(blocks, numbered ? ParagraphOf(child, child.Nodes(), reference, null) : ParagraphOf(child, child.Nodes(), null, reference));
                    else
                        Nested(() => BlockOf(child, blocks));
                    numbered = false;
                }
            }
        }

        private int Instance(XElement list)
        {
            var style = (_header, Attr(list, TextNs + "style-name") ?? "");
            var continued = Attr(list, TextNs + "continue-list");
            int id;
            if (continued != null && _listIds.TryGetValue(continued, out var target))
                id = target;
            else if ((continued != null || Attr(list, TextNs + "continue-numbering") == "true") && _lastList.TryGetValue(style, out var last))
                id = last;
            else
            {
                var abstractId = Abstract(style);
                var restart = _instances.Any(i => i.AbstractId == abstractId);
                id = _nextInstance++;
                _instances.Add(new NumberingInstance(id, abstractId, restart ? [new LevelOverride(0, _abstracts.First(a => a.Id == abstractId).Levels[0].Start, null)] : []));
            }

            _lastList[style] = id;
            if (Attr(list, XNamespace.Xml + "id") is { } xmlId) _listIds[xmlId] = id;
            return id;
        }

        private int Abstract((bool Header, string Name) style)
        {
            if (_abstractByStyle.TryGetValue(style, out var id)) return id;
            var definition = _styles.ListStyle(style.Header, style.Name);
            var levels = definition?.Elements().Where(e => Int(e, TextNs + "level") is >= 1 and <= 9).GroupBy(e => Int(e, TextNs + "level")!.Value - 1).ToDictionary(g => g.Key, g => g.First());
            var bullet = levels?.GetValueOrDefault(0) is not { } first || first.Name.LocalName != "list-level-style-number";
            var template = ListTemplates.Create(bullet ? ListTemplate.Bullet : ListTemplate.Decimal);
            var abstractNumbering = new AbstractNumbering(_nextAbstract++, template.Levels.Select(l => Level(l, levels?.GetValueOrDefault(l.Level))).ToList()) { Multilevel = true };
            _abstracts.Add(abstractNumbering);
            return _abstractByStyle[style] = abstractNumbering.Id;
        }

        private static NumberingLevel Level(NumberingLevel basis, XElement odf)
        {
            if (odf == null) return basis;
            var indent = Indent(odf) ?? basis.Indent;
            if (odf.Name.LocalName != "list-level-style-number")
            {
                var bullet = Attr(odf, TextNs + "bullet-char");
                return basis with
                {
                    Format = NumberFormat.Bullet, Text = string.IsNullOrEmpty(bullet) || bullet[0] is >= '\uE000' and <= '\uF8FF' ? "\u2022" :
                        bullet[..(char.IsSurrogatePair(bullet, 0) ? 2 : 1)], Indent = indent,
                    LabelFormat = new RunFormat()
                };
            }

            var format = Attr(odf, StyleNs + "num-format") switch
            {
                "a" => NumberFormat.LowerLetter,
                "A" => NumberFormat.UpperLetter,
                "i" => NumberFormat.LowerRoman,
                "I" => NumberFormat.UpperRoman,
                "" => NumberFormat.None,
                _ => NumberFormat.Decimal
            };
            var shown = Math.Clamp(Int(odf, TextNs + "display-levels") ?? 1, 1, basis.Level + 1);
            var counters = format == NumberFormat.None ? "" : string.Join(".", Enumerable.Range(basis.Level - shown + 2, shown).Select(n => "%" + n));
            return basis with
            {
                Format = format, Start = Math.Max(0, Int(odf, TextNs + "start-value") ?? 1), Indent = indent, LabelFormat = new RunFormat(),
                Text = Attr(odf, StyleNs + "num-prefix") + counters + Attr(odf, StyleNs + "num-suffix")
            };
        }

        private static ParagraphFormat Indent(XElement odf)
        {
            var properties = odf.Element(StyleNs + "list-level-properties");
            if (properties?.Element(StyleNs + "list-level-label-alignment") is { } alignment && RoundTwips(Attr(alignment, FoNs + "margin-left")) is { } left)
                return new ParagraphFormat { IndentLeft = left, IndentFirstLine = RoundTwips(Attr(alignment, FoNs + "text-indent")) ?? 0 };
            var before = RoundTwips(Attr(properties, TextNs + "space-before"));
            var width = RoundTwips(Attr(properties, TextNs + "min-label-width"));
            return before == null && width == null ? null : new ParagraphFormat { IndentLeft = (before ?? 0) + (width ?? 0), IndentFirstLine = -(width ?? 0) };
        }

        private Paragraph ParagraphOf(XElement element, IEnumerable<XNode> content, NumberingRef? numbering, NumberingRef? continued)
        {
            package.Spend(ParagraphCost);
            var style = _styles.ParagraphStyle(_header, Attr(element, TextNs + "style-name"));
            var styleId = element?.Name == TextNs + "h" ? "Heading" + Math.Clamp(Int(element, TextNs + "outline-level") ?? 1, 1, 6) : style.BuiltIn;
            if (styleId != null) _definitions = StyleDefinitions.With(_definitions, styleId, StyleType.Paragraph);
            var format = style.Format with { StyleId = styleId };
            if (numbering != null) format = format with { Numbering = numbering, IndentLeft = null, IndentFirstLine = null };
            else if (continued is { } item) format = format with { IndentLeft = IndentOf(item), IndentFirstLine = null };
            if (_pageBreak) format = format with { PageBreakBefore = true };
            _pageBreak = style.BreakAfter;
            var runs = new Runs();
            Inlines(content, style.Text, null, runs);
            return new Paragraph { Format = format, Inlines = runs.Done() };
        }

        private int? IndentOf(NumberingRef item)
        {
            var abstractId = _instances.First(i => i.Id == item.NumId).AbstractId;
            return _abstracts.First(a => a.Id == abstractId).Levels.FirstOrDefault(l => l.Level == item.Level)?.Indent?.IndentLeft;
        }

        private void Inlines(IEnumerable<XNode> nodes, IReadOnlyDictionary<XName, string> text, Hyperlink link, Runs runs)
        {
            foreach (var node in nodes)
            {
                if (node is XText value)
                {
                    package.Spend(value.Value.Length);
                    runs.Text(value.Value, Format(text, link), link);
                    continue;
                }

                if (node is not XElement element) continue;
                package.Spend(1);
                if (element.Name.Namespace == TextNs)
                    switch (element.Name.LocalName)
                    {
                        case "span":
                            Nested(() => Inlines(element.Nodes(), _styles.Span(_header, Attr(element, TextNs + "style-name"), text), link, runs));
                            break;
                        case "a":
                            Nested(() => Inlines(element.Nodes(), text, Link(Attr(element, XLinkNs + "href")) ?? link, runs));
                            break;
                        case "s":
                            var spaces = Math.Clamp(Int(element, TextNs + "c") ?? 1, 1, MaxSpaces);
                            package.Spend(spaces);
                            runs.Spaces(spaces, Format(text, link), link);
                            break;
                        case "tab":
                            runs.Add(new Tab { Format = Format(text, link), Link = link });
                            break;
                        case "line-break":
                            runs.Add(new Break(BreakKind.Line) { Format = Format(text, link), Link = link });
                            break;
                        case "note":
                            var citation = element.Element(TextNs + "note-citation");
                            runs.Add(new OpaqueInline(Attr(element, TextNs + "note-class") == "endnote" ? "endnote" : "footnote", Attr(citation, TextNs + "label") ?? citation?.Value ?? "")
                                { Format = Format(text, null) });
                            break;
                        default:
                            if (!SkippedInline.Contains(element.Name.LocalName)) Nested(() => Inlines(element.Nodes(), text, link, runs));
                            break;
                    }
                else if (element.Name == DrawNs + "frame")
                    Frame(element, text, link, runs);
                else if (element.Name == DrawNs + "a")
                    Nested(() => Inlines(element.Nodes(), text, Link(Attr(element, XLinkNs + "href")) ?? link, runs));
            }
        }

        private void Frame(XElement frame, IReadOnlyDictionary<XName, string> text, Hyperlink link, Runs runs)
        {
            foreach (var image in frame.Elements(DrawNs + "image"))
            {
                if (Picture(Attr(image, XLinkNs + "href")) is not ({ } part, { } data, { } type)) continue;
                var (width, height) = Twips(Attr(frame, SvgNs + "width")) is { } w and > 0 && Twips(Attr(frame, SvgNs + "height")) is { } h and > 0
                    ? (Math.Clamp((long)Math.Round(w * EmuPerTwip), 1, MaxEmu), Math.Clamp((long)Math.Round(h * EmuPerTwip), 1, MaxEmu))
                    : InsertImageCommand.Size(data, type, _width * EmuPerTwip);
                var alt = frame.Element(SvgNs + "title")?.Value ?? frame.Element(SvgNs + "desc")?.Value;
                runs.Add(new InlineImage(part, width, height)
                {
                    AltText = string.IsNullOrWhiteSpace(alt) ? null : alt,
                    Placement = Attr(frame, TextNs + "anchor-type") == "as-char" ? ImagePlacement.Inline : ImagePlacement.Left,
                    Format = Format(text, link) with { StyleId = null }, Link = link
                });
                return;
            }

            foreach (var box in frame.Elements(DrawNs + "text-box"))
            {
                var outer = (_floating, _pageBreak);
                _floating = [];
                var blocks = Blocks(box.Elements());
                (_floating, _pageBreak) = outer;
                _floating.AddRange(blocks);
            }
        }

        private (string Part, byte[] Data, string Type) Picture(string href)
        {
            if (string.IsNullOrEmpty(href) || href.Contains(':') || href.Contains("..", StringComparison.Ordinal)) return default;
            var path = href.StartsWith("./", StringComparison.Ordinal) ? href[2..] : href.TrimStart('/');
            if (_pictures.TryGetValue(path, out var cached)) return cached;
            var data = package.ReadEntry(path);
            package.Spend(1 + (data?.Length ?? 0) / PictureBytesPerUnit);
            return _pictures[path] = data is { Length: > 0 } && HtmlPasteReader.Sniff(data) is { } type
                ? (InsertImageCommand.AddMedia(_document, data, type), data, type)
                : default;
        }

        private Hyperlink Link(string href)
        {
            if (string.IsNullOrEmpty(href)) return null;
            var link = href[0] == '#' ? href.Length > 1 ? new Hyperlink(null, href[1..], null) : null : new Hyperlink(href, null, null);
            if (link?.SafeTarget == null) return null;
            _definitions = StyleDefinitions.With(_definitions, InlineEdit.HyperlinkStyle, StyleType.Character);
            return link;
        }

        private RunFormat Format(IReadOnlyDictionary<XName, string> text, Hyperlink link) =>
            link == null ? _styles.Run(text) : _styles.Run(text) with { StyleId = InlineEdit.HyperlinkStyle };

        private Table TableOf(XElement table)
        {
            var widths = new List<int>();
            foreach (var column in Descend(table, TableNs + "table-column", ["table-columns", "table-header-columns", "table-column-group"]))
            {
                var width = RoundTwips(Attr(_styles.Find(_header, "table-column", Attr(column, TableNs + "style-name"))?.Element(StyleNs + "table-column-properties"), StyleNs + "column-width"));
                for (var i = Math.Clamp(Int(column, TableNs + "number-columns-repeated") ?? 1, 1, MaxColumns); i > 0 && widths.Count < MaxColumns; i--) widths.Add(width ?? 0);
            }

            var rows = new List<TableRow>();
            var below = new Dictionary<int, (int Rows, int Span)>();
            foreach (var row in Descend(table, TableNs + "table-row", ["table-header-rows", "table-rows", "table-row-group"]))
            {
                var header = row.Parent?.Name == TableNs + "table-header-rows";
                var repeat = Math.Min(Math.Clamp(Int(row, TableNs + "number-rows-repeated") ?? 1, 1, MaxRepeat), MaxRows - rows.Count);
                for (var i = 0; i < repeat; i++)
                {
                    var before = package.Budget;
                    var (cells, next) = Row(row, below);
                    if (i == 0) package.Reserve((before - package.Budget) * (repeat - 1));
                    rows.Add(new TableRow(cells) { IsHeader = header });
                    below = next;
                }
            }

            var columns = Math.Max(1, rows.Select(r => r.Cells.Sum(c => c.GridSpan)).DefaultIfEmpty(0).Max());
            var grid = Enumerable.Range(0, columns).Select(c => c < widths.Count && widths[c] > 0 ? widths[c] : Math.Max(1, _width / columns)).ToList();
            if (rows.Count == 0) rows.Add(new TableRow([new TableCell([new Paragraph()])]));
            _definitions = StyleDefinitions.With(_definitions, Tables.GridStyle, StyleType.Table);
            return new Table
            {
                Format = new TableFormat { StyleId = Tables.GridStyle, Look = TableLook.FirstRow | TableLook.FirstColumn | TableLook.BandedRows },
                Grid = grid,
                Rows = rows.Select(row => Sized(row, grid)).ToList()
            };
        }

        private static TableRow Sized(TableRow row, List<int> grid)
        {
            var cells = new List<TableCell>();
            var column = 0;
            foreach (var cell in row.Cells)
            {
                cells.Add(cell with { Width = grid.Skip(column).Take(cell.GridSpan).Sum() });
                column += cell.GridSpan;
            }

            return row with { Cells = cells, GridAfter = grid.Count - column };
        }

        private (List<TableCell> Cells, Dictionary<int, (int Rows, int Span)> Below) Row(XElement row, Dictionary<int, (int Rows, int Span)> below)
        {
            var cells = new List<TableCell>();
            var next = new Dictionary<int, (int Rows, int Span)>();
            var column = 0;
            var skip = 0;
            foreach (var cell in row.Elements())
            {
                var covered = cell.Name == TableNs + "covered-table-cell";
                if (!covered && cell.Name != TableNs + "table-cell") continue;
                var repeat = Math.Clamp(Int(cell, TableNs + "number-columns-repeated") ?? 1, 1, MaxColumns);
                for (var i = 0; i < repeat && column < MaxColumns; i++)
                {
                    if (covered && skip > 0)
                    {
                        skip--;
                        continue;
                    }

                    package.Spend(CellCost);
                    if (covered)
                    {
                        var merge = below.Remove(column, out var m) ? m : (Rows: 0, Span: 1);
                        cells.Add(new TableCell([new Paragraph()]) { GridSpan = merge.Span, VMerge = merge.Rows > 0 ? VerticalMerge.Continue : VerticalMerge.None });
                        if (merge.Rows > 1) next[column] = (merge.Rows - 1, merge.Span);
                        (skip, column) = (merge.Span - 1, column + merge.Span);
                        continue;
                    }

                    var span = Math.Clamp(Int(cell, TableNs + "number-columns-spanned") ?? 1, 1, MaxColumns - column);
                    var spannedRows = Math.Clamp(Int(cell, TableNs + "number-rows-spanned") ?? 1, 1, MaxRows);
                    var before = package.Budget;
                    var blocks = CellBlocks(cell);
                    if (i == 0) package.Reserve((before - package.Budget) * (Math.Min(repeat, MaxColumns - column) - 1));
                    cells.Add(new TableCell(blocks)
                    {
                        GridSpan = span, VMerge = spannedRows > 1 ? VerticalMerge.Restart : VerticalMerge.None,
                        Shading = Color(Attr(_styles.Find(_header, "table-cell", Attr(cell, TableNs + "style-name"))?.Element(StyleNs + "table-cell-properties"), FoNs + "background-color"))
                    });
                    if (spannedRows > 1) next[column] = (spannedRows - 1, span);
                    (skip, column) = (span - 1, column + span);
                }
            }

            return (cells, next);
        }

        private List<Block> CellBlocks(XElement cell)
        {
            var blocks = Blocks(cell.Elements());
            if (blocks.Count == 0 || blocks[^1] is not Paragraph) blocks.Add(new Paragraph());
            return blocks;
        }

        private static IEnumerable<XElement> Descend(XElement parent, XName name, string[] groups, int depth = 0)
        {
            if (depth > MaxDepth) throw LoadLimit.Exceeded("The document is nested too deeply.");
            foreach (var child in parent.Elements())
                if (child.Name == name)
                    yield return child;
                else if (child.Name.Namespace == TableNs && groups.Contains(child.Name.LocalName))
                    foreach (var nested in Descend(child, name, groups, depth + 1))
                        yield return nested;
        }

        private T Nest<T>(Func<T> action)
        {
            if (++_depth > MaxDepth) throw LoadLimit.Exceeded("The document is nested too deeply.");
            try
            {
                return action();
            }
            finally
            {
                _depth--;
            }
        }

        private void Nested(Action action) => Nest(() =>
        {
            action();
            return 0;
        });
    }

    private sealed class Runs
    {
        private readonly List<Inline> _inlines = [];
        private readonly StringBuilder _text = new();
        private RunFormat _format;
        private Hyperlink _link;
        private bool _space = true;

        public void Text(string value, RunFormat format, Hyperlink link)
        {
            Use(format, link);
            foreach (var c in value)
            {
                var space = c is ' ' or '\t' or '\r' or '\n';
                if (!space || !_space) _text.Append(space ? ' ' : c);
                _space = space;
            }
        }

        public void Spaces(int count, RunFormat format, Hyperlink link)
        {
            Use(format, link);
            _text.Append(' ', count);
            _space = false;
        }

        public void Add(Inline inline)
        {
            Flush();
            _inlines.Add(inline);
            _space = false;
        }

        public List<Inline> Done()
        {
            Flush();
            return _inlines;
        }

        private void Use(RunFormat format, Hyperlink link)
        {
            if (_text.Length > 0 && (!ReferenceEquals(link, _link) || format != _format)) Flush();
            (_format, _link) = (format, link);
        }

        private void Flush()
        {
            if (_text.Length > 0) _inlines.Add(new TextRun(_text.ToString()) { Format = _format, Link = _link });
            _text.Clear();
        }
    }
}
