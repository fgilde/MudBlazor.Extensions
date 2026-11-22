
namespace MudBlazor.Extensions.Office.Document;

internal static class HtmlReader
{
    internal const string Monospace = "Consolas";
    internal const int MaxLength = 32 * 1024 * 1024;
    private const int MaxColumns = 63;
    private const long EmuPerTwip = 635;

    internal static readonly IReadOnlyDictionary<string, string> Highlights = new Dictionary<string, string>
    {
        ["black"] = "#000000", ["blue"] = "#0000FF", ["cyan"] = "#00FFFF", ["green"] = "#00FF00", ["magenta"] = "#FF00FF", ["red"] = "#FF0000",
        ["yellow"] = "#FFFF00", ["white"] = "#FFFFFF", ["darkBlue"] = "#000080", ["darkCyan"] = "#008080", ["darkGreen"] = "#008000",
        ["darkMagenta"] = "#800080", ["darkRed"] = "#800000", ["darkYellow"] = "#808000", ["darkGray"] = "#808080", ["lightGray"] = "#C0C0C0"
    };

    internal static TextDocument Read(Stream stream)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        for (int read; (read = stream.Read(chunk, 0, chunk.Length)) > 0;)
        {
            if (buffer.Length + read > 4L * MaxLength) throw new InvalidDataException("The HTML is too large to read.");
            buffer.Write(chunk, 0, read);
        }

        var bytes = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);
        var (encoding, preamble) = HtmlEncoding.Detect(bytes);
        return Read(encoding.GetString(bytes[preamble..]));
    }

    internal static TextDocument Read(string html)
    {
        if (html?.Length > MaxLength) throw new InvalidDataException("The HTML is too large to read.");
        var pasted = HtmlPasteReader.ReadBlocks(html, out var title);
        var document = TextDocument.Create();
        var builder = new Builder(document);
        var blocks = builder.Blocks(pasted, document.Sections[^1].Page.TextWidth);
        if (blocks.LastOrDefault() is not Paragraph) blocks.Add(new Paragraph());
        document.BlockList.Clear();
        document.BlockList.AddRange(blocks.Select(document.Identify));
        document.Styles = builder.Styles;
        if (builder.Instances.Count > 0) document.Numbering = new Numbering([.. document.Numbering.Abstracts, .. builder.Abstracts], [.. document.Numbering.Instances, .. builder.Instances]);
        if (title != null) document.Properties = document.Properties with { Title = title };
        document.InvalidateIndex();
        return document;
    }

    private sealed class Builder(TextDocument document)
    {
        private int _nextAbstract = document.Numbering.Abstracts.Select(a => a.Id + 1).DefaultIfEmpty(0).Max();
        private int _nextInstance = Lists.NextId(document.Numbering);

        public DocumentStyles Styles { get; private set; } = document.Styles;

        public List<AbstractNumbering> Abstracts { get; } = [];

        public List<NumberingInstance> Instances { get; } = [];

        public List<Block> Blocks(IEnumerable<PastedBlock> pasted, int width)
        {
            var result = new List<Block>();
            var lists = new Dictionary<(PastedList, int), int>();
            foreach (var block in pasted)
            {
                if (block is PastedTable table)
                {
                    result.Add(TableOf(table, width));
                    continue;
                }

                var paragraph = (PastedParagraph)block;
                NumberingRef? numbering = paragraph.List == PastedList.None ? null : new NumberingRef(List((paragraph.List, paragraph.ListRun), lists), Math.Clamp(paragraph.Depth, 0, 8));
                result.Add(ParagraphOf(paragraph, numbering, width));
            }

            return result;
        }

        private int List((PastedList Kind, int Run) kind, Dictionary<(PastedList, int), int> lists)
        {
            if (lists.TryGetValue(kind, out var id)) return id;
            var definition = ListTemplates.Create(kind.Kind == PastedList.Bullet ? ListTemplate.Bullet : ListTemplate.Decimal) with { Id = _nextAbstract++ };
            Abstracts.Add(definition);
            Instances.Add(new NumberingInstance(_nextInstance, definition.Id, []));
            return lists[kind] = _nextInstance++;
        }

        private string Style(string id, StyleType type)
        {
            Styles = StyleDefinitions.With(Styles, id, type);
            return id;
        }

        private Paragraph ParagraphOf(PastedParagraph source, NumberingRef? numbering, int width)
        {
            var format = new ParagraphFormat
            {
                StyleId = source.Heading is >= 1 and <= 6 ? Style("Heading" + source.Heading, StyleType.Paragraph) : null,
                Alignment = source.Align switch
                {
                    "center" => ParagraphAlignment.Center,
                    "right" => ParagraphAlignment.Right,
                    "justify" => ParagraphAlignment.Justify,
                    _ => null
                },
                IndentLeft = numbering == null ? source.IndentLeft : null,
                IndentFirstLine = numbering == null ? source.IndentFirstLine : null,
                PageBreakBefore = source.PageBreakBefore ? true : null,
                Numbering = numbering
            };
            var inlines = new List<Inline>();
            string href = null;
            Hyperlink link = null;
            foreach (var run in source.Runs)
            {
                if (run.Format.Link != href) (href, link) = (run.Format.Link, Link(run.Format.Link));
                var runFormat = Format(run.Format, source.Preformatted, link != null);
                if (run.ImageData != null)
                    inlines.Add(Image(run, runFormat with { StyleId = null }, link, width));
                else
                    inlines.AddRange(InlineEdit.Lines(run.Text.Replace('\n', '\v'), runFormat, link, null)[0]);
            }

            return new Paragraph { Format = format, Inlines = InlineEdit.Normalize(inlines) };
        }

        private Hyperlink Link(string href)
        {
            if (string.IsNullOrEmpty(href)) return null;
            var link = href[0] == '#' ? href.Length > 1 ? new Hyperlink(null, href[1..], null) : null : new Hyperlink(href, null, null);
            if (link?.SafeTarget == null) return null;
            Style(InlineEdit.HyperlinkStyle, StyleType.Character);
            return link;
        }

        private RunFormat Format(PastedFormat format, bool preformatted, bool linked)
        {
            var highlight = format.Highlight == null ? null : Highlights.FirstOrDefault(h => h.Value == format.Highlight).Key;
            return new RunFormat
            {
                Bold = format.Bold ? true : null,
                Italic = format.Italic ? true : null,
                Underline = format.Underline ? "single" : null,
                Strike = format.Strike ? true : null,
                SizePt = format.SizePt,
                Font = preformatted ? Monospace : format.Font,
                Color = format.Color,
                Highlight = highlight,
                Shading = highlight == null ? format.Highlight : null,
                VerticalAlign = format.VerticalAlign switch { "super" => VerticalAlign.Superscript, "sub" => VerticalAlign.Subscript, _ => null },
                StyleId = linked ? InlineEdit.HyperlinkStyle : null
            };
        }

        private InlineImage Image(PastedRun run, RunFormat format, Hyperlink link, int width)
        {
            var (w, h) = InsertImageCommand.Size(run.ImageData, run.ImageType, width * EmuPerTwip, run.Width, run.Height);
            return new InlineImage(InsertImageCommand.AddMedia(document, run.ImageData, run.ImageType), w, h)
            {
                AltText = string.IsNullOrEmpty(run.Alt) ? null : run.Alt, Format = format, Link = link
            };
        }

        private Table TableOf(PastedTable source, int width)
        {
            var layout = Layout(source);
            var columns = Math.Max(1, layout.Max(r => r.Sum(c => c.Span)));
            var column = Math.Max(1, width / columns);
            var rows = layout.Select(row => new TableRow(row.Select(slot => new TableCell(slot.Source == null ? [new Paragraph()] : CellBlocks(slot.Source, column * slot.Span))
            {
                GridSpan = slot.Span, VMerge = slot.Merge, Width = column * slot.Span, Shading = slot.Source?.Shading
            }).ToList()) { GridAfter = columns - row.Sum(c => c.Span) }).ToList();
            return new Table
            {
                Format = new TableFormat { StyleId = Style(Tables.GridStyle, StyleType.Table), Look = TableLook.FirstRow | TableLook.FirstColumn | TableLook.BandedRows },
                Grid = Enumerable.Repeat(column, columns).ToList(),
                Rows = rows
            };
        }

        private List<Block> CellBlocks(PastedCell cell, int width)
        {
            var blocks = Blocks(cell.Blocks, width);
            if (blocks.LastOrDefault() is not Paragraph) blocks.Add(new Paragraph());
            return blocks;
        }

        private static List<List<Slot>> Layout(PastedTable table)
        {
            var layout = new List<List<Slot>>();
            var below = new Dictionary<int, (int Rows, int Span)>();
            foreach (var row in table.Rows)
            {
                var slots = new List<Slot>();
                var next = new Dictionary<int, (int Rows, int Span)>();
                var column = 0;
                foreach (var cell in row)
                {
                    column = Continue(slots, below, next, column);
                    var span = Math.Min(cell.ColSpan, MaxColumns - column);
                    if (span <= 0) break;
                    slots.Add(new Slot(span, cell.RowSpan > 1 ? VerticalMerge.Restart : VerticalMerge.None, cell));
                    if (cell.RowSpan > 1) next[column] = (cell.RowSpan - 1, span);
                    column += span;
                }

                column = Continue(slots, below, next, column);
                foreach (var at in below.Keys.Where(k => k > column).Order().ToList())
                {
                    if (at < column || !below.ContainsKey(at)) continue;
                    if (at > column) slots.Add(new Slot(at - column, VerticalMerge.None, null));
                    column = Continue(slots, below, next, at);
                }

                layout.Add(slots);
                below = next;
            }

            return layout;
        }

        private static int Continue(List<Slot> slots, Dictionary<int, (int Rows, int Span)> below, Dictionary<int, (int Rows, int Span)> next, int column)
        {
            while (below.Remove(column, out var merge))
            {
                slots.Add(new Slot(merge.Span, VerticalMerge.Continue, null));
                if (merge.Rows > 1) next[column] = (merge.Rows - 1, merge.Span);
                column += merge.Span;
            }

            return column;
        }
    }

    private readonly record struct Slot(int Span, VerticalMerge Merge, PastedCell Source);
}
