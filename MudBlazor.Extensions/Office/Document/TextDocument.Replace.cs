namespace MudBlazor.Extensions.Office.Document;

public sealed partial class TextDocument
{
    internal (IReadOnlyList<BlockEdit> Edits, Func<TextDocument, DocumentRange> After) ReplaceRange(
        DocumentRange range, IReadOnlyList<IReadOnlyList<Inline>> lines, Func<Paragraph, Paragraph> adjustLast = null, InsertedContent inserted = null)
    {
        var (startPosition, endPosition) = Snap(range);
        if (startPosition.Equals(endPosition) && lines is [{ Count: 0 }]) return ([], _ => new DocumentRange(startPosition, startPosition));
        var (first, start) = Resolve(startPosition);
        var (last, end) = Resolve(endPosition);
        var from = Locate(startPosition.BlockId);
        var to = Locate(endPosition.BlockId);
        if (from.Container != to.Container) throw new ArgumentException("The range must not span the body and a header or footer.", nameof(range));
        var cut = new RangeCut(this, first, start, last, end, DeletedMarkers(first, start, last, end), lines, adjustLast, inserted);
        var before = Container(from.Container).Skip(from.Index).Take(to.Index - from.Index + 1).ToList();
        var after = cut.List(before, [new CellStep(0, 0, 0), .. startPosition.Path], [new CellStep(0, 0, to.Index - from.Index), .. endPosition.Path]).ToList();
        var caret = cut.Caret.Id;
        var offset = cut.CaretOffset;
        DocumentRange After(TextDocument document) => new(document.PositionOf(caret, offset), document.PositionOf(caret, offset));

        var index = from.Index;
        while (before.Count > 0 && after.Count > 0 && ReferenceEquals(before[0], after[0]))
        {
            before.RemoveAt(0);
            after.RemoveAt(0);
            index++;
        }

        while (before.Count > 0 && after.Count > 0 && ReferenceEquals(before[^1], after[^1]))
        {
            before.RemoveAt(before.Count - 1);
            after.RemoveAt(after.Count - 1);
        }

        if (before.Count == 0 && after.Count == 0) return ([], After);
        var edit = new BlockEdit(from.Container, index, before, after);
        if (from.Container < 0)
        {
            var topLevel = startPosition.Path.Count == 0;
            var merged = topLevel && endPosition.Path.Count == 0 && first.Id != last.Id;
            var deleted = before.Select(b => b.Id).Except(after.Select(b => b.Id)).ToHashSet();
            if (merged) deleted.Add(first.Id);
            var (sectionsBefore, sectionsAfter) = SectionsAfterEdit(deleted, topLevel ? merged ? last.Id : first.Id : null, caret);
            edit = edit with { SectionsBefore = sectionsBefore, SectionsAfter = sectionsAfter };
        }

        return ([edit], After);
    }

    private List<Inline> DeletedMarkers(Paragraph first, int start, Paragraph last, int end)
    {
        var deleted = first.Id == last.Id
            ? InlineEdit.Split(first.Inlines, start, end).Middle
            : InlineEdit.Split(first.Inlines, start).Right
                .Concat(ParagraphsBetween(first.Id, last.Id).Skip(1).Where(p => p.Id != last.Id).SelectMany(p => p.Inlines))
                .Concat(InlineEdit.Split(last.Inlines, end).Left);
        return deleted.Where(i => i is Marker { Kind: not "proofErr" }).ToList();
    }

    private sealed class RangeCut(
        TextDocument document, Paragraph first, int start, Paragraph last, int end, IReadOnlyList<Inline> markers,
        IReadOnlyList<IReadOnlyList<Inline>> lines, Func<Paragraph, Paragraph> adjustLast, InsertedContent inserted)
    {
        public Paragraph Caret { get; private set; }

        public int CaretOffset { get; private set; }

        public IReadOnlyList<Block> List(IReadOnlyList<Block> blocks, CellStep[] from, CellStep[] to, bool cell = false)
        {
            if (from == null && to == null) return Cleared(blocks);
            var i = from?[0].Block ?? -1;
            var j = to?[0].Block ?? blocks.Count;
            var merged = from is { Length: 1 } && to is { Length: 1 };
            var result = from != null ? blocks.Take(i).ToList() : [];
            if (from is { Length: 1 })
                result.AddRange(StartParagraphs(merged ? InlineEdit.Split(last.Inlines, end).Right : []));
            else if (from != null)
                result.Add(CutTable((Table)blocks[i], from[1..], j == i ? to[1..] : null));
            if (to != null && j > i && !merged)
                result.Add(to.Length == 1 ? Truncated(last, InlineEdit.Split(last.Inlines, end).Right) : CutTable((Table)blocks[j], null, to[1..]));
            if (to != null) result.AddRange(blocks.Skip(j + 1));
            if (cell && result.LastOrDefault() is not Paragraph)
                result.Add(blocks[^1] is Paragraph paragraph ? paragraph with { Inlines = [] } : new Paragraph { Id = document.NewId() });
            return result;
        }

        private Table CutTable(Table table, CellStep[] from, CellStep[] to) => MapCells(table, (r, c, cell) =>
        {
            var isStart = from != null && from[0].Row == r && from[0].Cell == c;
            var isEnd = to != null && to[0].Row == r && to[0].Cell == c;
            if (isStart || isEnd) return List(cell.Blocks, isStart ? from : null, isEnd ? to : null, true);
            var afterStart = from == null || (r, c).CompareTo((from[0].Row, from[0].Cell)) > 0;
            var beforeEnd = to == null || (r, c).CompareTo((to[0].Row, to[0].Cell)) < 0;
            return afterStart && beforeEnd ? Cleared(cell.Blocks) : cell.Blocks;
        });

        private IReadOnlyList<Block> Cleared(IReadOnlyList<Block> blocks) =>
            blocks is [Paragraph { Inlines.Count: 0 }]
                ? blocks
                : [blocks.OfType<Paragraph>().FirstOrDefault() is { } paragraph ? paragraph with { Inlines = [] } : new Paragraph { Id = document.NewId() }];

        private List<Block> StartParagraphs(IReadOnlyList<Inline> tail)
        {
            var paragraphs = new List<Block>();
            var leftEmpty = InlineEdit.Split(first.Inlines, start).Left.Sum(i => i.Length) == 0;
            var tailEmpty = tail.Sum(i => i.Length) == 0;
            for (var k = 0; k < lines.Count; k++)
            {
                var inlines = k == 0 ? [.. InlineEdit.Split(first.Inlines, start).Left, .. markers, .. lines[k]] : lines[k].ToList();
                if (k == lines.Count - 1)
                {
                    CaretOffset = inlines.Sum(i => i.Length);
                    inlines = InlineEdit.Normalize([.. inlines, .. tail]);
                }
                else
                    inlines = InlineEdit.Normalize(inlines);

                var owner = k > 0 && inserted != null ? last : first;
                var paragraph = k == 0
                    ? Truncated(first, inlines)
                    : new Paragraph { Id = document.NewId(), Format = owner.Format, MarkFormat = owner.MarkFormat, Inlines = inlines, Source = owner.Source };
                var formatFrom = k == 0 && leftEmpty && (lines.Count > 1 || tailEmpty) ? inserted?.First : k > 0 && k == lines.Count - 1 && tailEmpty ? inserted?.Last : null;
                if (formatFrom != null) paragraph = paragraph with { Format = formatFrom.Format, MarkFormat = formatFrom.MarkFormat };
                paragraphs.Add(paragraph);
                if (k == 0 && inserted != null) paragraphs.AddRange(inserted.Middle);
            }

            if (adjustLast != null) paragraphs[^1] = adjustLast((Paragraph)paragraphs[^1]);
            Caret = (Paragraph)paragraphs[^1];
            return paragraphs;
        }

        private static Paragraph Truncated(Paragraph paragraph, IReadOnlyList<Inline> inlines) =>
            paragraph.Inlines.SequenceEqual(inlines) ? paragraph : paragraph with { Inlines = inlines };
    }
}
