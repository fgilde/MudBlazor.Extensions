namespace MudBlazor.Extensions.Office.Document;

internal sealed record BlockEdit(int Container, int Index, IReadOnlyList<Block> Before, IReadOnlyList<Block> After)
{
    public IReadOnlyList<Section> SectionsBefore { get; init; }

    public IReadOnlyList<Section> SectionsAfter { get; init; }
}

public sealed partial class TextDocument
{
    internal void ApplyEdits(IReadOnlyList<BlockEdit> edits, bool forward)
    {
        var ordered = forward ? edits : edits.Reverse().ToList();
        foreach (var edit in ordered)
        {
            var expected = forward ? edit.Before : edit.After;
            var blocks = edit.Container < HeaderFooterList.Count ? Container(edit.Container) : [];
            if (edit.Index + expected.Count > blocks.Count || expected.Where((b, k) => !ReferenceEquals(blocks[edit.Index + k], b)).Any())
                throw new InvalidOperationException("The document changed since the command was applied.");
        }

        foreach (var edit in ordered) Splice(edit, forward);
    }

    internal List<BlockEdit> MapParagraphs(IEnumerable<string> paragraphIds, Func<Paragraph, Paragraph> map)
    {
        var targets = new HashSet<string>();
        var tops = new SortedSet<(int Container, int Index)>();
        foreach (var id in paragraphIds)
        {
            if (id == null || !TryLocate(id, out var location) || location.Block is not Paragraph)
                throw new ArgumentException($"{id} is not a paragraph of the document.", nameof(paragraphIds));
            targets.Add(id);
            tops.Add((location.Container, location.Index));
        }

        var edits = new List<BlockEdit>();
        foreach (var (container, index) in tops)
        {
            var top = Container(container)[index];
            var mapped = Map(top, targets, map);
            if (!ReferenceEquals(mapped, top)) edits.Add(new BlockEdit(container, index, [top], [mapped]));
        }

        return edits;
    }

    internal (IReadOnlyList<BlockEdit> Edits, Func<TextDocument, DocumentRange> After) MapInlines(DocumentRange range, bool includeMarkers, Func<Inline, Inline> map)
    {
        range = Snap(range);
        var first = Resolve(range.Start);
        var last = Resolve(range.End);
        var spans = ParagraphsIn(range).ToDictionary(p => p.Id, p => (From: p.Id == first.Paragraph.Id ? first.Offset : 0, To: p.Id == last.Paragraph.Id ? last.Offset : p.Length));
        var edits = MapParagraphs(spans.Keys, paragraph =>
        {
            var (from, to) = spans[paragraph.Id];
            if (from >= to) return paragraph;
            var (left, middle, right) = InlineEdit.Split(paragraph.Inlines, from, to);
            var mapped = middle.Select(i => i is Marker && !includeMarkers ? i : map(i)).ToList();
            return SameItems(mapped, middle) ? paragraph : paragraph with { Inlines = InlineEdit.Normalize([.. left, .. mapped, .. right]) };
        });
        return (edits, _ => range);
    }

    internal DocumentRange Snap(DocumentRange range)
    {
        ArgumentNullException.ThrowIfNull(range.Start, nameof(range));
        ArgumentNullException.ThrowIfNull(range.End, nameof(range));
        var (start, end) = range.Normalize(this);
        var empty = start.Equals(end);
        start = start with { Offset = SnapOffset(Resolve(start).Paragraph.Text, start.Offset, true) };
        return new DocumentRange(start, empty ? start : end with { Offset = SnapOffset(Resolve(end).Paragraph.Text, end.Offset, false) });
    }

    internal static int SnapOffset(string text, int offset, bool down) =>
        offset > 0 && offset < text.Length && char.IsHighSurrogate(text[offset - 1]) && char.IsLowSurrogate(text[offset]) ? down ? offset - 1 : offset + 1 : offset;

    internal Block NextSibling(string blockId)
    {
        if (!TryLocate(blockId, out var location)) throw new ArgumentException($"{blockId} is not a block of the document.", nameof(blockId));
        var top = Container(location.Container);
        if (location.Path.Length == 0) return location.Index + 1 < top.Count ? top[location.Index + 1] : null;
        var (row, cell, block) = location.Path[^1];
        var blocks = ((Table)Walk(top[location.Index], location.Path[..^1])).Rows[row].Cells[cell].Blocks;
        return block + 1 < blocks.Count ? blocks[block + 1] : null;
    }

    internal (IReadOnlyList<Section> Before, IReadOnlyList<Section> After) SectionsAfterEdit(ICollection<string> deletedMarks, string markOwner, string newOwner)
    {
        var after = SectionList.Where(s => s.LastBlockId == null || s.LastBlockId == markOwner || !deletedMarks.Contains(s.LastBlockId))
            .Select(s => s.LastBlockId != null && s.LastBlockId == markOwner ? s with { LastBlockId = newOwner } : s)
            .ToList();
        return after.SequenceEqual(SectionList) ? (null, null) : (SectionList.ToList(), after);
    }

    internal static bool SameItems<T>(IReadOnlyList<T> a, IReadOnlyList<T> b) where T : class =>
        a.Count == b.Count && !a.Where((item, i) => !ReferenceEquals(item, b[i])).Any();

    private void Splice(BlockEdit edit, bool forward)
    {
        var (remove, insert) = forward ? (edit.Before, edit.After) : (edit.After, edit.Before);
        if (edit.Container < 0)
        {
            BlockList.RemoveRange(edit.Index, remove.Count);
            BlockList.InsertRange(edit.Index, insert);
        }
        else
        {
            var part = HeaderFooterList[edit.Container];
            HeaderFooterList[edit.Container] = part with { Blocks = [.. part.Blocks.Take(edit.Index), .. insert, .. part.Blocks.Skip(edit.Index + remove.Count)] };
        }

        InvalidateIndex();
        var removed = remove.SelectMany(Subtree).ToHashSet<Block>(ReferenceEqualityComparer.Instance);
        var inserted = insert.SelectMany(Subtree).ToHashSet<Block>(ReferenceEqualityComparer.Instance);
        var structure = !removed.Select(b => b.Id).ToHashSet().SetEquals(inserted.Select(b => b.Id));
        _changedIds.UnionWith(removed.Where(b => !inserted.Contains(b)).Concat(inserted.Where(b => !removed.Contains(b))).Select(b => b.Id));
        Track(null, (edit.Container < 0 ? DocumentChangeKind.Content : DocumentChangeKind.HeadersFooters) | (structure ? DocumentChangeKind.Structure : DocumentChangeKind.None));
        if ((forward ? edit.SectionsAfter : edit.SectionsBefore) is not { } sections) return;
        SectionList.Clear();
        SectionList.AddRange(sections);
        Track(null, DocumentChangeKind.Sections);
    }

    private static Block Map(Block block, ISet<string> targets, Func<Paragraph, Paragraph> map) => block switch
    {
        Paragraph paragraph => targets.Contains(paragraph.Id) ? map(paragraph) : paragraph,
        Table table => MapCells(table, (_, _, cell) => cell.Blocks.Select(b => Map(b, targets, map)).ToList()),
        _ => block
    };

    private static Table MapCells(Table table, Func<int, int, TableCell, IReadOnlyList<Block>> map)
    {
        var rows = table.Rows.Select((row, r) =>
        {
            var cells = row.Cells.Select((cell, c) => map(r, c, cell) is var blocks && SameItems(blocks, cell.Blocks) ? cell : cell with { Blocks = blocks }).ToList();
            return SameItems(cells, row.Cells) ? row : row with { Cells = cells };
        }).ToList();
        return SameItems(rows, table.Rows) ? table : table with { Rows = rows };
    }
}
