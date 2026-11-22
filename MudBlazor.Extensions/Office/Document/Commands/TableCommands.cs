namespace MudBlazor.Extensions.Office.Document;

/// <summary>
/// Inserts a table of empty cells at a position: a paragraph is split if the position lies inside it, at its start the table goes before it, at its
/// end after it. The grid divides the section's text width evenly and the table uses the built-in style <c>TableGrid</c>, added on demand. An empty
/// paragraph follows the table when nothing else would, and when the paragraph ends a section the section then ends at that empty paragraph, so the
/// table stays in the paragraph's section. The caret moves into the first cell.
/// </summary>
public sealed class InsertTableCommand : TextDocumentCommand
{
    private readonly DocumentPosition _at;
    private readonly int _rows;
    private readonly int _columns;

    /// <summary>Creates the command.</summary>
    /// <param name="at">Where to insert the table.</param>
    /// <param name="rows">The number of rows, at least 1.</param>
    /// <param name="columns">The number of columns, 1 to 63.</param>
    /// <exception cref="ArgumentOutOfRangeException">The number of rows or columns is out of range.</exception>
    public InsertTableCommand(DocumentPosition at, int rows, int columns)
    {
        ArgumentNullException.ThrowIfNull(at);
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(columns, 63);
        (_at, _rows, _columns) = (at, rows, columns);
    }

    /// <inheritdoc />
    public override string Description => "Insert table";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var position = document.Snap(new DocumentRange(_at, _at)).Start;
        var (paragraph, offset) = document.Resolve(position);
        var width = Math.Max(1, document.SectionOf(position.BlockId).Page.TextWidth / _columns);
        var table = (Table)document.Identify(new Table
        {
            Format = new TableFormat { StyleId = Tables.GridStyle, Look = TableLook.FirstRow | TableLook.FirstColumn | TableLook.BandedRows },
            Grid = Enumerable.Repeat(width, _columns).ToList(),
            Rows = Enumerable.Range(0, _rows).Select(_ => new TableRow(Enumerable.Range(0, _columns).Select(_ => new TableCell([new Paragraph()]) { Width = width }).ToList())).ToList()
        });
        List<Block> blocks;
        string movedEnd = null;
        var endsSection = position.Path.Count == 0 && document.SectionList.Any(s => s.LastBlockId == paragraph.Id);
        if (offset == 0)
            blocks = [table, paragraph];
        else if (offset == paragraph.Length && endsSection)
        {
            var empty = new Paragraph { Id = document.NewId(), Format = paragraph.Format with { Numbering = null }, MarkFormat = paragraph.MarkFormat, Source = paragraph.Source };
            blocks = [paragraph, table, empty];
            movedEnd = empty.Id;
        }
        else if (offset == paragraph.Length)
            blocks = document.NextSibling(paragraph.Id) == null ? [paragraph, table, new Paragraph { Id = document.NewId() }] : [paragraph, table];
        else
        {
            var (left, right) = InlineEdit.Split(paragraph.Inlines, offset);
            var tail = new Paragraph { Id = document.NewId(), Format = paragraph.Format, MarkFormat = paragraph.MarkFormat, Inlines = right, Source = paragraph.Source };
            blocks = [paragraph with { Inlines = left }, table, tail];
            movedEnd = tail.Id;
        }

        var edits = document.EditBlock(paragraph.Id, blocks);
        if (movedEnd != null && position.Path.Count == 0 && edits[0].Container < 0)
        {
            var (before, after) = document.SectionsAfterEdit([], paragraph.Id, movedEnd);
            edits[0] = edits[0] with { SectionsBefore = before, SectionsAfter = after };
        }

        var styles = StyleDefinitions.With(document.Styles, Tables.GridStyle, StyleType.Table);
        var first = ((Paragraph)table.Rows[0].Cells[0].Blocks[0]).Id;
        return (document.WithDefinitions(edits, styles, null), d => Caret(d, first, 0));
    }
}

/// <summary>Inserts rows with the cell structure and formats of the neighbour row (the row above, or the first row when inserting at the top), each cell holding one empty paragraph of the same format.</summary>
/// <param name="tableId">The id of the table, at any depth.</param>
/// <param name="index">The index of the first new row, 0 to the number of rows.</param>
/// <param name="count">The number of rows, at least 1.</param>
public sealed class InsertTableRowsCommand(string tableId, int index, int count) : TableCommand(tableId)
{
    /// <inheritdoc />
    public override string Description => "Insert rows";

    private protected override Table Change(TextDocument document, Table table)
    {
        if (index < 0 || index > table.Rows.Count) throw new ArgumentOutOfRangeException(nameof(index));
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        var neighbour = table.Rows[Math.Max(0, index - 1)];
        var below = index < table.Rows.Count ? table.Rows[index] : null;
        var header = index < table.Rows.TakeWhile(r => r.IsHeader).Count();
        var rows = Enumerable.Range(0, count).Select(_ => neighbour with
        {
            Copy = true,
            IsHeader = header,
            Cells = Tables.Layout(neighbour).Select(p => Tables.Emptied(p.Cell) with
            {
                VMerge = index > 0 && p.Cell.VMerge != VerticalMerge.None && below != null && Tables.At(below, p.Column, p.Cell.GridSpan) is { VMerge: VerticalMerge.Continue }
                    ? VerticalMerge.Continue
                    : VerticalMerge.None
            }).ToList()
        });
        return table with { Rows = [.. table.Rows.Take(index), .. rows, .. table.Rows.Skip(index)] };
    }
}

/// <summary>Deletes rows; deleting all rows removes the table. Vertical merges cut by the deletion restart in the first remaining row.</summary>
/// <param name="tableId">The id of the table, at any depth.</param>
/// <param name="index">The index of the first row to delete.</param>
/// <param name="count">The number of rows, at least 1.</param>
public sealed class DeleteTableRowsCommand(string tableId, int index, int count) : TableCommand(tableId)
{
    /// <inheritdoc />
    public override string Description => "Delete rows";

    private protected override Table Change(TextDocument document, Table table)
    {
        if (index < 0 || count < 1 || index + count > table.Rows.Count) throw new ArgumentOutOfRangeException(nameof(count));
        return count == table.Rows.Count ? null : table with { Rows = [.. table.Rows.Take(index), .. table.Rows.Skip(index + count)] };
    }
}

/// <summary>
/// Inserts grid columns with the width of the neighbour column (the one before, or the first when inserting at the start). Cells whose span covers the
/// insertion point widen; elsewhere new cells with the format of the neighbour cell are added.
/// </summary>
/// <param name="tableId">The id of the table, at any depth.</param>
/// <param name="gridIndex">The grid index of the first new column, 0 to the number of grid columns.</param>
/// <param name="count">The number of columns, at least 1.</param>
public sealed class InsertTableColumnsCommand(string tableId, int gridIndex, int count) : TableCommand(tableId)
{
    /// <inheritdoc />
    public override string Description => "Insert columns";

    private protected override Table Change(TextDocument document, Table table)
    {
        if (gridIndex < 0 || gridIndex > table.Grid.Count) throw new ArgumentOutOfRangeException(nameof(gridIndex));
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        var width = table.Grid.Count == 0 ? 1440 : table.Grid[Math.Max(0, gridIndex - 1)];
        var rows = table.Rows.Select(row =>
        {
            var layout = Tables.Layout(row);
            var end = layout.Count == 0 ? row.GridBefore : layout[^1].Column + layout[^1].Cell.GridSpan;
            if (gridIndex < row.GridBefore) return row with { GridBefore = row.GridBefore + count };
            if (gridIndex > end) return row with { GridAfter = row.GridAfter + count };
            if (layout.FindIndex(p => p.Column < gridIndex && gridIndex < p.Column + p.Cell.GridSpan) is var inside and >= 0)
            {
                var cell = layout[inside].Cell;
                return row with { Cells = Tables.With(row.Cells, inside, cell with { GridSpan = cell.GridSpan + count, Width = cell.Width + width * count }) };
            }

            var at = layout.FindIndex(p => p.Column == gridIndex);
            if (at < 0) at = layout.Count;
            var model = row.Cells[Math.Max(0, at - 1)];
            var added = Enumerable.Range(0, count).Select(_ => Tables.Emptied(model) with { GridSpan = 1, Width = width });
            return row with { Cells = [.. row.Cells.Take(at), .. added, .. row.Cells.Skip(at)] };
        }).ToList();
        return table with { Grid = [.. table.Grid.Take(gridIndex), .. Enumerable.Repeat(width, count), .. table.Grid.Skip(gridIndex)], Rows = rows };
    }
}

/// <summary>Deletes grid columns; cells inside them are removed, cells partly inside them narrow. Deleting all columns removes the table.</summary>
/// <param name="tableId">The id of the table, at any depth.</param>
/// <param name="gridIndex">The grid index of the first column to delete.</param>
/// <param name="count">The number of columns, at least 1.</param>
public sealed class DeleteTableColumnsCommand(string tableId, int gridIndex, int count) : TableCommand(tableId)
{
    /// <inheritdoc />
    public override string Description => "Delete columns";

    private protected override Table Change(TextDocument document, Table table)
    {
        if (gridIndex < 0 || count < 1 || gridIndex + count > table.Grid.Count) throw new ArgumentOutOfRangeException(nameof(count));
        if (count == table.Grid.Count) return null;
        var stop = gridIndex + count;
        int Overlap(int from, int span) => Math.Max(0, Math.Min(from + span, stop) - Math.Max(from, gridIndex));
        int Removed(int from, int span) => Enumerable.Range(Math.Max(from, gridIndex), Overlap(from, span)).Sum(i => table.Grid[i]);
        var rows = new List<TableRow>();
        foreach (var row in table.Rows)
        {
            var layout = Tables.Layout(row);
            var after = layout.Count == 0 ? row.GridBefore : layout[^1].Column + layout[^1].Cell.GridSpan;
            var cells = new List<TableCell>();
            foreach (var (column, cell) in layout)
                if (Overlap(column, cell.GridSpan) is var overlap && overlap < cell.GridSpan)
                    cells.Add(overlap == 0 ? cell : cell with { GridSpan = cell.GridSpan - overlap, Width = cell.Width - Removed(column, cell.GridSpan) });
            if (cells.Count > 0)
                rows.Add(cells.Count == row.Cells.Count && TextDocument.SameItems(cells, row.Cells) && Overlap(0, row.GridBefore) == 0 && Overlap(after, row.GridAfter) == 0
                    ? row
                    : row with { Cells = cells, GridBefore = row.GridBefore - Overlap(0, row.GridBefore), GridAfter = row.GridAfter - Overlap(after, row.GridAfter) });
        }

        return rows.Count == 0 ? null : table with { Grid = [.. table.Grid.Take(gridIndex), .. table.Grid.Skip(stop)], Rows = rows };
    }
}

/// <summary>
/// Merges a rectangle of grid cells: horizontal merges use <see cref="TableCell.GridSpan"/>, vertical merges <see cref="VerticalMerge.Restart"/> and
/// <see cref="VerticalMerge.Continue"/>. The contents of non-empty cells are appended into the top-left cell in reading order. A rectangle that cuts
/// through a horizontally or vertically merged cell throws <see cref="ArgumentException"/>; one that already is a single merged cell changes nothing.
/// </summary>
/// <param name="tableId">The id of the table, at any depth.</param>
/// <param name="firstRow">The first row.</param>
/// <param name="firstGridColumn">The first grid column.</param>
/// <param name="lastRow">The last row.</param>
/// <param name="lastGridColumn">The last grid column.</param>
public sealed class MergeTableCellsCommand(string tableId, int firstRow, int firstGridColumn, int lastRow, int lastGridColumn) : TableCommand(tableId)
{
    /// <inheritdoc />
    public override string Description => "Merge cells";

    private protected override Table Change(TextDocument document, Table table)
    {
        if (firstRow < 0 || lastRow < firstRow || lastRow >= table.Rows.Count || firstGridColumn < 0 || lastGridColumn < firstGridColumn || lastGridColumn >= table.Grid.Count)
            throw new ArgumentOutOfRangeException(nameof(lastRow), "The range lies outside the table.");
        if (firstRow == lastRow && firstGridColumn == lastGridColumn) return table;
        var span = lastGridColumn - firstGridColumn + 1;
        var ranges = new List<(int First, int Last)>();
        for (var r = firstRow; r <= lastRow; r++)
        {
            var layout = Tables.Layout(table.Rows[r]);
            var first = layout.FindIndex(p => p.Column == firstGridColumn);
            var last = layout.FindIndex(p => p.Column + p.Cell.GridSpan - 1 == lastGridColumn);
            if (first < 0 || last < first) throw new ArgumentException("The range cuts through a merged cell or a row without cells.", nameof(firstGridColumn));
            ranges.Add((first, last));
        }

        bool Inside(int column, TableCell cell) => column <= lastGridColumn && column + cell.GridSpan - 1 >= firstGridColumn;
        if (Tables.Layout(table.Rows[firstRow]).Any(p => Inside(p.Column, p.Cell) && p.Cell.VMerge == VerticalMerge.Continue) ||
            lastRow + 1 < table.Rows.Count && Tables.Layout(table.Rows[lastRow + 1]).Any(p => Inside(p.Column, p.Cell) && p.Cell.VMerge == VerticalMerge.Continue))
            throw new ArgumentException("The range cuts through a vertically merged cell.", nameof(firstRow));
        var merged = ranges.SelectMany((range, k) => table.Rows[firstRow + k].Cells.Skip(range.First).Take(range.Last - range.First + 1)).ToList();
        if (merged.Count == ranges.Count && (merged.Count == 1 || merged[0].VMerge == VerticalMerge.Restart && merged.Skip(1).All(c => c.VMerge == VerticalMerge.Continue && Tables.IsEmpty(c))))
            return table;
        var content = merged.Where(c => !Tables.IsEmpty(c)).SelectMany(c => c.Blocks).ToList();
        var width = merged[0].Width == null ? null : (int?)table.Grid.Skip(firstGridColumn).Take(span).Sum();
        var rows = table.Rows.ToList();
        for (var k = 0; k < ranges.Count; k++)
        {
            var row = rows[firstRow + k];
            var (first, last) = ranges[k];
            var cell = row.Cells[first];
            var replacement = k == 0
                ? cell with { GridSpan = span, Width = width, VMerge = ranges.Count > 1 ? VerticalMerge.Restart : cell.VMerge, Blocks = content.Count > 0 ? content : cell.Blocks }
                : Tables.Emptied(cell) with { GridSpan = span, Width = width, VMerge = VerticalMerge.Continue };
            rows[firstRow + k] = row with { Cells = [.. row.Cells.Take(first), replacement, .. row.Cells.Skip(last + 1)] };
        }

        return table with { Rows = rows };
    }
}

/// <summary>Splits a merged cell back into its grid cells: every row of a vertical merge gets its own cells again and the content stays in the top-left cell.</summary>
/// <param name="tableId">The id of the table, at any depth.</param>
/// <param name="row">The row of the merged cell.</param>
/// <param name="gridColumn">A grid column the merged cell covers.</param>
public sealed class SplitTableCellCommand(string tableId, int row, int gridColumn) : TableCommand(tableId)
{
    /// <inheritdoc />
    public override string Description => "Split cell";

    private protected override Table Change(TextDocument document, Table table)
    {
        if (row < 0 || row >= table.Rows.Count) throw new ArgumentOutOfRangeException(nameof(row));
        var (column, cell) = Tables.Layout(table.Rows[row]).FirstOrDefault(p => p.Column <= gridColumn && gridColumn < p.Column + p.Cell.GridSpan);
        if (cell == null) throw new ArgumentOutOfRangeException(nameof(gridColumn));
        var span = cell.GridSpan;
        var top = row;
        if (cell.VMerge == VerticalMerge.Continue)
            while (top > 0 && Tables.At(table.Rows[top], column, span) is { VMerge: VerticalMerge.Continue } && Tables.At(table.Rows[top - 1], column, span) is { VMerge: not VerticalMerge.None })
                top--;
        var bottom = cell.VMerge == VerticalMerge.None ? row : top;
        while (cell.VMerge != VerticalMerge.None && bottom + 1 < table.Rows.Count && Tables.At(table.Rows[bottom + 1], column, span) is { VMerge: VerticalMerge.Continue }) bottom++;
        if (span == 1 && top == bottom) return table;
        var rows = table.Rows.ToList();
        for (var r = top; r <= bottom; r++)
        {
            var current = rows[r];
            var index = Tables.Layout(current).FindIndex(p => p.Column == column && p.Cell.GridSpan == span);
            var merged = current.Cells[index];
            var pieces = Enumerable.Range(0, span).Select(i => (i == 0 ? merged : Tables.Emptied(merged)) with
            {
                GridSpan = 1, VMerge = VerticalMerge.None, Width = merged.Width == null ? null : table.Grid[column + i]
            });
            rows[r] = current with { Cells = [.. current.Cells.Take(index), .. pieces, .. current.Cells.Skip(index + 1)] };
        }

        return table with { Rows = rows };
    }
}

/// <summary>Sets the widths of the grid columns; cells with a preferred width get the sum of the columns they span.</summary>
/// <param name="tableId">The id of the table, at any depth.</param>
/// <param name="grid">The widths in twips, one positive value per grid column.</param>
public sealed class SetTableColumnWidthsCommand(string tableId, IReadOnlyList<int> grid) : TableCommand(tableId)
{
    private readonly IReadOnlyList<int> _grid = grid?.ToArray() ?? throw new ArgumentNullException(nameof(grid));

    /// <inheritdoc />
    public override string Description => "Column widths";

    private protected override Table Change(TextDocument document, Table table)
    {
        if (_grid.Count != table.Grid.Count || _grid.Any(w => w <= 0)) throw new ArgumentException("One positive width per grid column is required.", nameof(grid));
        if (_grid.SequenceEqual(table.Grid)) return table;
        var rows = table.Rows.Select(row =>
        {
            var cells = Tables.Layout(row).Select(p => p.Cell.Width is not { } old || _grid.Skip(p.Column).Take(p.Cell.GridSpan).Sum() is var width && width == old
                ? p.Cell
                : p.Cell with { Width = width }).ToList();
            return TextDocument.SameItems(cells, row.Cells) ? row : row with { Cells = cells };
        }).ToList();
        return table with { Grid = _grid, Rows = rows };
    }
}

/// <summary>Changes the format of cells: width, shading, borders and vertical alignment. Content, spans and merges stay as they are.</summary>
/// <param name="tableId">The id of the table, at any depth.</param>
/// <param name="cells">The cells as row index and a grid column the cell covers; a merged cell covered by several of them changes once.</param>
/// <param name="change">Returns the changed cell.</param>
/// <param name="description">The undo description.</param>
public sealed class SetTableCellFormatCommand(string tableId, IReadOnlyList<(int Row, int GridColumn)> cells, Func<TableCell, TableCell> change, string description)
    : TableCommand(tableId)
{
    private readonly IReadOnlyList<(int Row, int GridColumn)> _cells = cells?.ToArray() ?? throw new ArgumentNullException(nameof(cells));
    private readonly Func<TableCell, TableCell> _change = change ?? throw new ArgumentNullException(nameof(change));

    /// <inheritdoc />
    public override string Description => description;

    private protected override Table Change(TextDocument document, Table table)
    {
        var rows = table.Rows.ToList();
        var targets = _cells.Select(p => (Row: p.Row, Cell: p.Row < 0 || p.Row >= rows.Count
            ? -1
            : Tables.Layout(rows[p.Row]).FindIndex(l => l.Column <= p.GridColumn && p.GridColumn < l.Column + l.Cell.GridSpan), p.GridColumn)).ToList();
        if (targets.FirstOrDefault(t => t.Cell < 0) is { Cell: < 0 } missing)
            throw new ArgumentOutOfRangeException(nameof(cells), $"There is no cell at row {missing.Row}, grid column {missing.GridColumn}.");
        foreach (var (r, c) in targets.Select(t => (t.Row, t.Cell)).Distinct())
        {
            var cell = rows[r].Cells[c];
            var changed = _change(cell) is { } result ? result with { Blocks = cell.Blocks, GridSpan = cell.GridSpan, VMerge = cell.VMerge, Source = cell.Source } : cell;
            if (changed != cell) rows[r] = rows[r] with { Cells = Tables.With(rows[r].Cells, c, changed) };
        }

        return TextDocument.SameItems(rows, table.Rows) ? table : table with { Rows = rows };
    }
}

/// <summary>Changes the table format (<c>w:tblPr</c>).</summary>
/// <param name="tableId">The id of the table, at any depth.</param>
/// <param name="change">Returns the changed format.</param>
/// <param name="description">The undo description.</param>
public sealed class SetTableFormatCommand(string tableId, Func<TableFormat, TableFormat> change, string description) : TableCommand(tableId)
{
    private readonly Func<TableFormat, TableFormat> _change = change ?? throw new ArgumentNullException(nameof(change));

    /// <inheritdoc />
    public override string Description => description;

    private protected override Table Change(TextDocument document, Table table) =>
        _change(table.Format) is { } format && format != table.Format ? table with { Format = format } : table;
}

/// <summary>Makes the first rows repeat as header rows on every page and the others not.</summary>
/// <param name="tableId">The id of the table, at any depth.</param>
/// <param name="count">The number of header rows, 0 to the number of rows.</param>
public sealed class SetTableHeaderRowsCommand(string tableId, int count) : TableCommand(tableId)
{
    /// <inheritdoc />
    public override string Description => "Header rows";

    private protected override Table Change(TextDocument document, Table table)
    {
        if (count < 0 || count > table.Rows.Count) throw new ArgumentOutOfRangeException(nameof(count));
        var rows = table.Rows.Select((r, i) => r.IsHeader == i < count ? r : r with { IsHeader = i < count }).ToList();
        return TextDocument.SameItems(rows, table.Rows) ? table : table with { Rows = rows };
    }
}

/// <summary>Deletes a table; a table cell or a document left without a paragraph gets an empty one.</summary>
/// <param name="tableId">The id of the table, at any depth.</param>
public sealed class DeleteTableCommand(string tableId) : TableCommand(tableId)
{
    /// <inheritdoc />
    public override string Description => "Delete table";

    private protected override Table Change(TextDocument document, Table table) => null;
}

/// <summary>Base of the commands that change one table. A cell is never left without a paragraph and vertical merges stay consistent.</summary>
public abstract class TableCommand : TextDocumentCommand
{
    private readonly string _tableId;

    private protected TableCommand(string tableId) => _tableId = tableId;

    private protected sealed override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        if (_tableId == null || document.FindBlock(_tableId) is not Table table) throw new ArgumentException($"{_tableId} is not a table of the document.", "tableId");
        var changed = Change(document, table);
        if (ReferenceEquals(changed, table)) return ([], null);
        return (document.EditBlock(table.Id, changed == null ? [] : [Tables.Normalize(changed)]), null);
    }

    private protected abstract Table Change(TextDocument document, Table table);
}

internal static class Tables
{
    internal const string GridStyle = "TableGrid";

    internal static List<(int Column, TableCell Cell)> Layout(TableRow row)
    {
        var result = new List<(int, TableCell)>();
        var column = row.GridBefore;
        foreach (var cell in row.Cells)
        {
            result.Add((column, cell));
            column += cell.GridSpan;
        }

        return result;
    }

    internal static TableCell At(TableRow row, int column, int span) =>
        Layout(row).FirstOrDefault(p => p.Column == column && p.Cell.GridSpan == span).Cell;

    internal static bool IsEmpty(TableCell cell) => cell.Blocks.All(b => b is Paragraph { Length: 0 } p && p.Inlines.All(i => i is Marker));

    internal static TableCell Emptied(TableCell cell)
    {
        var paragraph = cell.Blocks.OfType<Paragraph>().FirstOrDefault();
        return cell with
        {
            Copy = true, Blocks = [new Paragraph { Format = paragraph?.Format ?? new ParagraphFormat(), MarkFormat = paragraph?.MarkFormat, Source = paragraph?.Source, Copy = true }]
        };
    }

    internal static IReadOnlyList<T> With<T>(IReadOnlyList<T> list, int index, T item)
    {
        var copy = list.ToArray();
        copy[index] = item;
        return copy;
    }

    internal static Table Normalize(Table table)
    {
        var rows = table.Rows.ToList();
        for (var r = 0; r < rows.Count; r++)
        {
            var cells = rows[r].Cells.ToList();
            var layout = Layout(rows[r]);
            for (var i = 0; i < cells.Count; i++)
            {
                var (column, cell) = layout[i];
                var state = cell.VMerge;
                if (state == VerticalMerge.Continue && (r == 0 || At(rows[r - 1], column, cell.GridSpan) is not { VMerge: not VerticalMerge.None })) state = VerticalMerge.Restart;
                if (state == VerticalMerge.Restart && (r + 1 == rows.Count || At(rows[r + 1], column, cell.GridSpan) is not { VMerge: VerticalMerge.Continue })) state = VerticalMerge.None;
                if (state != cell.VMerge) cells[i] = cells[i] with { VMerge = state };
                if (cell.Blocks.LastOrDefault() is not Paragraph) cells[i] = cells[i] with { Blocks = [.. cell.Blocks, new Paragraph()] };
            }

            if (!TextDocument.SameItems(cells, rows[r].Cells)) rows[r] = rows[r] with { Cells = cells };
        }

        return TextDocument.SameItems(rows, table.Rows) ? table : table with { Rows = rows };
    }
}
