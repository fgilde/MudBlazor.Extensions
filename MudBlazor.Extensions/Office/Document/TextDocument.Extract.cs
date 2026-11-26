namespace MudBlazor.Extensions.Office.Document;

public sealed partial class TextDocument
{
    /// <summary>
    /// True if this document was made by <see cref="Extract"/> from a range that ended at the start of a later paragraph: its last paragraph is
    /// whole, paragraph mark included.
    /// </summary>
    internal bool EndsWithParagraphMark { get; private set; }

    /// <summary>
    /// Copies a range into a new document, for the clipboard only; never written as .docx (its <c>Source</c> elements belong to another package).
    /// The first and last paragraphs are cut at the range and keep their formats, every block in between is copied whole; when both ends lie in the
    /// same table cell only that cell's blocks are taken by the same rule, any other table the range touches is copied whole. A range ending at
    /// offset 0 of a later paragraph leaves that paragraph out and sets <see cref="EndsWithParagraphMark"/>. Bookmark, comment, permission and move
    /// ranges cut in half by the range are dropped, complete ones kept. The result shares <see cref="Styles"/>, <see cref="Numbering"/> and the
    /// theme with this document, holds copies of the media its images use and has its own block ids; an empty range gives a document without blocks.
    /// </summary>
    /// <param name="range">The range, in either direction.</param>
    /// <returns>The new document.</returns>
    /// <exception cref="ArgumentException">The range spans the body and a header or footer.</exception>
    internal TextDocument Extract(DocumentRange range)
    {
        var (start, end) = Snap(range);
        var copy = Create();
        copy.BlockList.Clear();
        copy.InvalidateIndex();
        (copy.Styles, copy.Numbering, copy.Theme) = (Styles, Numbering, Theme);
        if (start.Equals(end)) return copy;
        var from = Locate(start.BlockId);
        var to = Locate(end.BlockId);
        if (from.Container != to.Container) throw new ArgumentException("The range must not span the body and a header or footer.", nameof(range));
        var blocks = CutOut(Container(from.Container), [new CellStep(0, 0, from.Index), .. start.Path], [new CellStep(0, 0, to.Index), .. end.Path],
            Resolve(start).Offset, Resolve(end).Offset, out var mark);
        blocks = WithoutHalfMarkers(blocks);
        foreach (var image in InlinesOf(blocks).OfType<InlineImage>())
            if (image.MediaPart != null && Media.TryGetValue(image.MediaPart, out var media))
                copy.PutMedia(image.MediaPart, (byte[])media.Data.Clone(), media.ContentType);
        copy.BlockList.AddRange(blocks.Select(copy.Identify));
        copy.InvalidateIndex();
        copy.EndsWithParagraphMark = mark;
        return copy;
    }

    private static List<Block> CutOut(IReadOnlyList<Block> blocks, CellStep[] from, CellStep[] to, int start, int end, out bool mark)
    {
        var (i, j) = (from[0].Block, to[0].Block);
        if (i == j && from.Length > 1 && to.Length > 1 && (from[1].Row, from[1].Cell) == (to[1].Row, to[1].Cell))
            return CutOut(((Table)blocks[i]).Rows[from[1].Row].Cells[from[1].Cell].Blocks, from[1..], to[1..], start, end, out mark);
        mark = to.Length == 1 && end == 0 && j > i;
        var result = new List<Block>();
        for (var k = i; k <= (mark ? j - 1 : j); k++)
        {
            var (cutStart, cutEnd) = (k == i && from.Length == 1, k == j && to.Length == 1);
            result.Add(blocks[k] is Paragraph paragraph && (cutStart || cutEnd)
                ? paragraph with { Inlines = InlineEdit.Split(paragraph.Inlines, cutStart ? start : 0, cutEnd ? end : paragraph.Length).Middle }
                : blocks[k]);
        }

        mark &= result[^1] is Paragraph;
        return result;
    }

    private static List<Block> WithoutHalfMarkers(List<Block> blocks)
    {
        var paragraphs = ParagraphsOf(blocks).ToList();
        var halves = paragraphs.SelectMany(p => p.Inlines).OfType<Marker>()
            .Where(m => m.Kind.EndsWith("Start", StringComparison.Ordinal) || m.Kind.EndsWith("End", StringComparison.Ordinal))
            .GroupBy(m => (Kind: m.Kind[..^(m.Kind.EndsWith("Start", StringComparison.Ordinal) ? 5 : 3)], Id: (string)m.Source?.Attribute(FormatXml.W + "id") ?? m.Name))
            .Where(g => g.Select(m => m.Kind.EndsWith("Start", StringComparison.Ordinal)).Distinct().Count() < 2)
            .SelectMany(g => g)
            .ToHashSet<Inline>(ReferenceEqualityComparer.Instance);
        if (halves.Count == 0) return blocks;
        var targets = paragraphs.Where(p => p.Inlines.Any(halves.Contains)).Select(p => p.Id).ToHashSet();
        return blocks.Select(b => Map(b, targets, p => p with { Inlines = p.Inlines.Where(i => !halves.Contains(i)).ToList() })).ToList();
    }

    private static IEnumerable<Inline> InlinesOf(IEnumerable<Block> blocks) => blocks.SelectMany(block => block switch
    {
        Paragraph paragraph => paragraph.Inlines,
        Table table => InlinesOf(table.Rows.SelectMany(r => r.Cells).SelectMany(c => c.Blocks)),
        OpaqueBlock { Preview: { } preview } => InlinesOf(preview),
        _ => []
    });
}
