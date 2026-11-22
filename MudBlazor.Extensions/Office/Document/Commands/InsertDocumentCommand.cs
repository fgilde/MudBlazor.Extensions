using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Document.FormatXml;

namespace MudBlazor.Extensions.Office.Document;

internal sealed record InsertedContent(IReadOnlyList<Block> Middle, Paragraph First, Paragraph Last);

/// <summary>
/// Replaces a range with the content of another document, for example one read from HTML. The first paragraph of the source merges into the paragraph
/// at the start of the range and the last into the paragraph at its end; the blocks in between are inserted. A merged paragraph takes the source
/// paragraph's formatting where the target paragraph has no text on that side, otherwise it keeps its own; properties the model does not cover stay
/// those of the target paragraph. One undo step restores the original blocks, styles and lists. Styles the document lacks are copied by id
/// (existing ids keep their definition), lists get new definitions and instances, pictures are copied once. Bookmarks and other markers are dropped,
/// fields and other unmodelled inline content become their displayed text, unmodelled blocks their preview, and pictures of other types than PNG,
/// JPEG, GIF and BMP are left out.
/// </summary>
public sealed class InsertDocumentCommand : TextDocumentCommand
{
    private readonly DocumentRange _range;
    private readonly TextDocument _source;

    /// <summary>Creates the command.</summary>
    /// <param name="range">The range to replace, in either direction; an empty range inserts at a position.</param>
    /// <param name="source">The document to insert; it is read when the command is applied first and not changed.</param>
    /// <exception cref="ArgumentNullException">The source is null.</exception>
    public InsertDocumentCommand(DocumentRange range, TextDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        (_range, _source) = (range, source);
    }

    /// <inheritdoc />
    public override string Description => "Paste";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var copier = new Copier(document, _source);
        var blocks = copier.Blocks(_source.Blocks);
        if (blocks.Count == 0) return document.ReplaceRange(_range, [[]]);
        var first = blocks[0] as Paragraph;
        var last = blocks.Count > 1 ? blocks[^1] as Paragraph : null;
        var middle = blocks.Skip(first != null ? 1 : 0).Take(blocks.Count - (first != null ? 1 : 0) - (last != null ? 1 : 0)).Select(document.Identify).ToList();
        IReadOnlyList<IReadOnlyList<Inline>> lines = blocks.Count == 1 && first != null ? [first.Inlines] : [first?.Inlines ?? [], last?.Inlines ?? []];
        var (edits, after) = document.ReplaceRange(_range, lines, null, new InsertedContent(middle, first, last));
        return (document.WithDefinitions(edits, copier.Styles, copier.Numbering), after);
    }

    private sealed class Copier(TextDocument target, TextDocument source)
    {
        private static readonly HashSet<string> Dropped =
            ["ins", "del", "moveFrom", "moveTo", "pPrChange", "rPrChange", "tblPrChange", "trPrChange", "tcPrChange", "sectPrChange", "cellIns", "cellDel", "cellMerge", "sectPr"];

        private static readonly XNamespace W14 = "http://schemas.microsoft.com/office/word/2010/wordml";
        private readonly Dictionary<string, DocumentStyle> _styles = new();
        private readonly Dictionary<int, int?> _numIds = new();
        private readonly Dictionary<Hyperlink, Hyperlink> _links = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<string, string> _media = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<AbstractNumbering> _abstracts = [];
        private readonly List<NumberingInstance> _instances = [];
        private int _nextAbstract = target.Numbering.Abstracts.Select(a => a.Id + 1).DefaultIfEmpty(0).Max();
        private int _nextInstance = Lists.NextId(target.Numbering);

        public DocumentStyles Styles => _styles.Count == 0
            ? null
            : new DocumentStyles([.. target.Styles.All, .. _styles.Values], target.Styles.DefaultRunFormat, target.Styles.DefaultParagraphFormat);

        public Numbering Numbering => _instances.Count == 0 ? null : new Numbering([.. target.Numbering.Abstracts, .. _abstracts], [.. target.Numbering.Instances, .. _instances]);

        public List<Block> Blocks(IEnumerable<Block> blocks)
        {
            var result = new List<Block>();
            foreach (var block in blocks)
                switch (block)
                {
                    case Paragraph paragraph:
                        result.Add(CopyParagraph(paragraph));
                        break;
                    case Table table:
                        result.Add(CopyTable(table));
                        break;
                    case OpaqueBlock { Visible: true, Preview: { } preview }:
                        result.AddRange(Blocks(preview));
                        break;
                }

            return result;
        }

        private Paragraph CopyParagraph(Paragraph paragraph) => new()
        {
            Format = paragraph.Format with { StyleId = Style(paragraph.Format.StyleId), Numbering = List(paragraph.Format.Numbering) },
            MarkFormat = paragraph.MarkFormat == null ? null : Run(paragraph.MarkFormat),
            Inlines = InlineEdit.Normalize(paragraph.Inlines.Select(CopyInline).Where(i => i != null)),
            Source = Shallow(paragraph.Source, "pPr")
        };

        private Table CopyTable(Table table) => table with
        {
            Id = null,
            Format = table.Format with { StyleId = Style(table.Format.StyleId) },
            Rows = table.Rows.Select(row => row with
            {
                Cells = row.Cells.Select(cell =>
                {
                    var blocks = Blocks(cell.Blocks);
                    if (blocks.LastOrDefault() is not Paragraph) blocks.Add(new Paragraph());
                    return cell with { Blocks = blocks, Source = Shallow(cell.Source, "tcPr") };
                }).ToList(),
                Source = Shallow(row.Source, "trPr")
            }).ToList(),
            Source = Shallow(table.Source, "tblPr", "tblGrid")
        };

        private Inline CopyInline(Inline inline)
        {
            var format = Run(inline.Format);
            var link = Link(inline.Link);
            switch (inline)
            {
                case Marker or InlineImage { MediaPart: null }:
                    return null;
                case OpaqueInline token:
                    return string.IsNullOrEmpty(token.DisplayText) ? null : new TextRun(token.DisplayText) { Format = format, Link = link };
                case InlineImage image:
                    if (!_media.TryGetValue(image.MediaPart, out var media))
                    {
                        var data = source.GetMedia(image.MediaPart);
                        var type = source.MediaContentType(image.MediaPart);
                        _media[image.MediaPart] = media = data == null || type == null || InsertImageCommand.Extension(type) == null ? null : InsertImageCommand.AddMedia(target, data, type);
                    }

                    return media == null ? null : image with { MediaPart = media, Placement = ImagePlacement.Inline, Format = format, Link = link, Source = null, Node = null };
                default:
                    return inline with { Format = format, Link = link };
            }
        }

        private Hyperlink Link(Hyperlink link)
        {
            if (link == null) return null;
            if (!_links.TryGetValue(link, out var copy)) _links[link] = copy = link with { Source = null };
            return copy;
        }

        private RunFormat Run(RunFormat format) => format with { StyleId = Style(format.StyleId) };

        private string Style(string id)
        {
            if (id == null || target.Styles.Find(id) != null || _styles.ContainsKey(id)) return id;
            var style = source.Styles.Find(id) ?? BuiltInStyles.All.FirstOrDefault(s => s.Id == id);
            if (style == null) return null;
            _styles[id] = style;
            _styles[id] = style with
            {
                BuiltIn = false,
                IsDefault = style.IsDefault && !target.Styles.All.Concat(_styles.Values).Any(s => s.IsDefault && s.Type == style.Type && s.Id != id),
                BasedOn = Style(style.BasedOn),
                Next = Style(style.Next),
                Link = Style(style.Link),
                ParagraphFormat = style.ParagraphFormat with { Numbering = List(style.ParagraphFormat.Numbering) },
                Source = null
            };
            return id;
        }

        private NumberingRef? List(NumberingRef? reference)
        {
            if (reference is not { } r || r.NumId == 0) return reference;
            if (!_numIds.TryGetValue(r.NumId, out var numId))
            {
                var instance = source.Numbering.Instances.FirstOrDefault(i => i.Id == r.NumId);
                var definition = instance == null ? null : source.Numbering.Abstracts.FirstOrDefault(a => a.Id == instance.AbstractId);
                if (definition != null)
                {
                    _abstracts.Add(definition with { Id = _nextAbstract, Source = null });
                    _instances.Add(instance with { Id = _nextInstance, AbstractId = _nextAbstract++, Source = null });
                    numId = _nextInstance++;
                }

                _numIds[r.NumId] = numId;
            }

            return numId is { } id ? r with { NumId = id } : null;
        }

        private static XElement Shallow(XElement element, params string[] keep)
        {
            if (element == null) return null;
            var copy = new XElement(element.Name, element.Attributes().Where(a => a.Name != W14 + "paraId" && a.Name != W14 + "textId"),
                element.Elements().Where(e => e.Name.Namespace == W && keep.Contains(e.Name.LocalName)).Select(e => new XElement(e)));
            copy.Descendants().Where(e => e.Name.Namespace == W && Dropped.Contains(e.Name.LocalName)).Remove();
            return copy;
        }
    }
}
