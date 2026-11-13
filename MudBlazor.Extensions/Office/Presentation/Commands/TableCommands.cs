namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>
/// Adds a table on top of the other shapes, with the style <see cref="TableShape.DefaultStyleId"/>, a header row, banded rows, equal column widths
/// and rows of 370840 EMU. The frame gets the size of the columns and rows.
/// </summary>
/// <param name="slide">The slide.</param>
/// <param name="rows">The number of rows, at least 1.</param>
/// <param name="columns">The number of columns, at least 1.</param>
/// <param name="transform">The position and width in slide coordinates; the width is shared by the columns.</param>
public sealed class InsertTableCommand(Slide slide, int rows, int columns, ShapeTransform transform) : IDocumentCommand<Presentation>
{
    private const long RowHeight = 370840;

    /// <summary>The created table, available after the command was applied.</summary>
    public TableShape Table { get; private set; }

    /// <inheritdoc />
    public string Description => "Insert table";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ShapeEdit.Validate(transform);
        if (Table == null)
        {
            var id = ShapeEdit.NextId(slide);
            var width = transform.Width / columns;
            Table = new TableShape
            {
                Id = id, Name = $"Table {id - 1}", Master = slide.Layout?.Master, StyleId = TableShape.DefaultStyleId, FirstRow = true, BandedRows = true,
                Transform = transform with { Width = width * columns, Height = RowHeight * rows },
                ColumnWidths = Enumerable.Repeat(width, columns).ToList(),
                Rows = Enumerable.Range(0, rows).Select(_ => new TableRow { Height = RowHeight, Cells = Enumerable.Range(0, columns).Select(_ => new TableCell()).ToList() }).ToList()
            };
        }

        slide.ShapeList.Add(Table);
        document.Notify(slide, PresentationChangeKind.Content);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        slide.ShapeList.Remove(Table);
        document.Notify(slide, PresentationChangeKind.Content);
    }
}

/// <summary>Replaces the text of a table cell. Build the body with <c>with</c> from the current one, as for <see cref="SetTextCommand"/>.</summary>
/// <param name="slide">The slide.</param>
/// <param name="table">The id of a <see cref="TableShape"/>.</param>
/// <param name="row">The row index.</param>
/// <param name="column">The column index of a cell that is not merged away.</param>
/// <param name="body">The new text.</param>
public sealed class SetTableCellTextCommand(Slide slide, uint table, int row, int column, TextBody body) : IDocumentCommand<Presentation>
{
    private readonly TableChange _change = new(slide, table);

    /// <inheritdoc />
    public string Description => "Edit text";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        ArgumentNullException.ThrowIfNull(body);
        _change.Apply(document, t =>
        {
            var grid = TableEdit.Grid(t.Rows);
            var cell = TableEdit.Cell(grid, row, column);
            grid[row][column] = cell with { Text = body };
            t.Rows = TableEdit.Rows(t.Rows, grid);
        });
    }

    /// <inheritdoc />
    public void Revert(Presentation document) => _change.Revert(document);
}

/// <summary>Inserts empty rows with the height and cell formatting of the row above. A merged range that the new rows run through grows.</summary>
/// <param name="slide">The slide.</param>
/// <param name="table">The id of a <see cref="TableShape"/>.</param>
/// <param name="index">The index of the first new row, from 0 to the row count.</param>
/// <param name="count">The number of rows, at least 1.</param>
public sealed class InsertTableRowsCommand(Slide slide, uint table, int index, int count) : IDocumentCommand<Presentation>
{
    private readonly TableChange _change = new(slide, table);

    /// <inheritdoc />
    public string Description => "Insert rows";

    /// <inheritdoc />
    public void Apply(Presentation document) => _change.Apply(document, t =>
    {
        TableEdit.CheckInsert(t.Rows.Count, index, count);
        var height = t.Rows[Math.Max(0, index - 1)].Height;
        var rows = t.Rows.ToList();
        rows.InsertRange(index, Enumerable.Repeat(new TableRow { Height = height }, count));
        t.Rows = TableEdit.Rows(rows, TableEdit.Insert(TableEdit.Grid(t.Rows), index, count));
        TableEdit.Resize(t, 0, height * count);
    });

    /// <inheritdoc />
    public void Revert(Presentation document) => _change.Revert(document);
}

/// <summary>
/// Deletes rows. A merged range shrinks; if its first row is deleted, the remaining part keeps the text. Deleting every row deletes the table.
/// </summary>
/// <param name="slide">The slide.</param>
/// <param name="table">The id of a <see cref="TableShape"/>.</param>
/// <param name="index">The index of the first row to delete.</param>
/// <param name="count">The number of rows, at least 1.</param>
public sealed class DeleteTableRowsCommand(Slide slide, uint table, int index, int count) : IDocumentCommand<Presentation>
{
    private readonly TableChange _change = new(slide, table);

    /// <inheritdoc />
    public string Description => "Delete rows";

    /// <inheritdoc />
    public void Apply(Presentation document) => _change.Apply(document, t =>
    {
        TableEdit.CheckDelete(t.Rows.Count, index, count);
        var removed = t.Rows.Skip(index).Take(count).Sum(r => r.Height);
        t.Rows = TableEdit.Rows(t.Rows.Where((_, r) => r < index || r >= index + count).ToList(), TableEdit.Delete(TableEdit.Grid(t.Rows), index, count));
        TableEdit.Resize(t, 0, -removed);
    });

    /// <inheritdoc />
    public void Revert(Presentation document) => _change.Revert(document);
}

/// <summary>Inserts empty columns with the width and cell formatting of the column to the left. A merged range that the new columns run through grows.</summary>
/// <param name="slide">The slide.</param>
/// <param name="table">The id of a <see cref="TableShape"/>.</param>
/// <param name="index">The index of the first new column, from 0 to the column count.</param>
/// <param name="count">The number of columns, at least 1.</param>
public sealed class InsertTableColumnsCommand(Slide slide, uint table, int index, int count) : IDocumentCommand<Presentation>
{
    private readonly TableChange _change = new(slide, table);

    /// <inheritdoc />
    public string Description => "Insert columns";

    /// <inheritdoc />
    public void Apply(Presentation document) => _change.Apply(document, t =>
    {
        TableEdit.CheckInsert(t.ColumnWidths.Count, index, count);
        var width = t.ColumnWidths[Math.Max(0, index - 1)];
        var widths = t.ColumnWidths.ToList();
        widths.InsertRange(index, Enumerable.Repeat(width, count));
        t.ColumnWidths = widths;
        t.Rows = TableEdit.Rows(t.Rows, TableEdit.Transpose(TableEdit.Insert(TableEdit.Transpose(TableEdit.Grid(t.Rows)), index, count)));
        TableEdit.Resize(t, width * count, 0);
    });

    /// <inheritdoc />
    public void Revert(Presentation document) => _change.Revert(document);
}

/// <summary>
/// Deletes columns. A merged range shrinks; if its first column is deleted, the remaining part keeps the text. Deleting every column deletes the table.
/// </summary>
/// <param name="slide">The slide.</param>
/// <param name="table">The id of a <see cref="TableShape"/>.</param>
/// <param name="index">The index of the first column to delete.</param>
/// <param name="count">The number of columns, at least 1.</param>
public sealed class DeleteTableColumnsCommand(Slide slide, uint table, int index, int count) : IDocumentCommand<Presentation>
{
    private readonly TableChange _change = new(slide, table);

    /// <inheritdoc />
    public string Description => "Delete columns";

    /// <inheritdoc />
    public void Apply(Presentation document) => _change.Apply(document, t =>
    {
        TableEdit.CheckDelete(t.ColumnWidths.Count, index, count);
        var removed = t.ColumnWidths.Skip(index).Take(count).Sum();
        t.ColumnWidths = t.ColumnWidths.Where((_, c) => c < index || c >= index + count).ToList();
        t.Rows = TableEdit.Rows(t.Rows, TableEdit.Transpose(TableEdit.Delete(TableEdit.Transpose(TableEdit.Grid(t.Rows)), index, count)));
        TableEdit.Resize(t, -removed, 0);
    });

    /// <inheritdoc />
    public void Revert(Presentation document) => _change.Revert(document);
}

/// <summary>
/// Merges a range of cells. The texts of the cells move paragraph by paragraph into the first cell. The range must contain every merged range it touches.
/// </summary>
/// <param name="slide">The slide.</param>
/// <param name="table">The id of a <see cref="TableShape"/>.</param>
/// <param name="row">The first row of the range.</param>
/// <param name="column">The first column of the range.</param>
/// <param name="rowCount">The number of rows, at least 1.</param>
/// <param name="columnCount">The number of columns, at least 1.</param>
public sealed class MergeTableCellsCommand(Slide slide, uint table, int row, int column, int rowCount, int columnCount) : IDocumentCommand<Presentation>
{
    private readonly TableChange _change = new(slide, table);

    /// <inheritdoc />
    public string Description => "Merge cells";

    /// <inheritdoc />
    public void Apply(Presentation document) => _change.Apply(document, t =>
    {
        var grid = TableEdit.Grid(t.Rows);
        ArgumentOutOfRangeException.ThrowIfLessThan(rowCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(columnCount, 1);
        TableEdit.Cell(grid, row, column);
        TableEdit.Cell(grid, row + rowCount - 1, column + columnCount - 1, true);
        var layout = TableEdit.Layout(grid);
        var anchors = new List<(int Row, int Column)>();
        for (var r = row; r < row + rowCount; r++)
        for (var c = column; c < column + columnCount; c++)
        {
            var (ar, ac, rs, cs) = layout[r][c];
            if (ar < row || ac < column || ar + rs > row + rowCount || ac + cs > column + columnCount)
                throw new ArgumentException("The range cuts through merged cells.", nameof(rowCount));
            if ((ar, ac) == (r, c)) anchors.Add((r, c));
        }

        var first = grid[row][column];
        var others = anchors.Skip(1).Where(a => grid[a.Row][a.Column].HasText).ToList();
        var text = others.Count == 0
            ? first.Text
            : (first.Text ?? new TextBody()) with
            {
                Paragraphs = (first.HasText ? first.Text.Paragraphs : []).Concat(others.SelectMany(a => grid[a.Row][a.Column].Text.Paragraphs)).ToList()
            };
        foreach (var (r, c) in others) grid[r][c] = grid[r][c] with { Text = new TextBody { Paragraphs = [new TextParagraph()] } };
        grid[row][column] = first with { Text = text, RowSpan = rowCount, ColumnSpan = columnCount };
        t.Rows = TableEdit.Rows(t.Rows, TableEdit.Normalize(grid));
    });

    /// <inheritdoc />
    public void Revert(Presentation document) => _change.Revert(document);
}

/// <summary>Splits a merged range back into its cells. The text stays in the first cell.</summary>
/// <param name="slide">The slide.</param>
/// <param name="table">The id of a <see cref="TableShape"/>.</param>
/// <param name="row">The row of the first cell of the range.</param>
/// <param name="column">The column of the first cell of the range.</param>
public sealed class SplitTableCellCommand(Slide slide, uint table, int row, int column) : IDocumentCommand<Presentation>
{
    private readonly TableChange _change = new(slide, table);

    /// <inheritdoc />
    public string Description => "Split cells";

    /// <inheritdoc />
    public void Apply(Presentation document) => _change.Apply(document, t =>
    {
        var grid = TableEdit.Grid(t.Rows);
        grid[row][column] = TableEdit.Cell(grid, row, column) with { RowSpan = 1, ColumnSpan = 1 };
        t.Rows = TableEdit.Rows(t.Rows, TableEdit.Normalize(grid));
    });

    /// <inheritdoc />
    public void Revert(Presentation document) => _change.Revert(document);
}

/// <summary>Sets the column widths and row heights of a table; the frame gets their sums as width and height.</summary>
/// <param name="slide">The slide.</param>
/// <param name="table">The id of a <see cref="TableShape"/>.</param>
/// <param name="columnWidths">One width per column in EMU, each at least 0.</param>
/// <param name="rowHeights">One height per row in EMU, each at least 0.</param>
public sealed class SetTableSizesCommand(Slide slide, uint table, IReadOnlyList<long> columnWidths, IReadOnlyList<long> rowHeights) : IDocumentCommand<Presentation>
{
    private readonly TableChange _change = new(slide, table);

    /// <inheritdoc />
    public string Description => "Size";

    /// <inheritdoc />
    public void Apply(Presentation document) => _change.Apply(document, t =>
    {
        if (columnWidths?.Count != t.ColumnWidths.Count || rowHeights?.Count != t.Rows.Count || columnWidths.Concat(rowHeights).Any(v => v < 0))
            throw new ArgumentException("A table needs one width per column and one height per row, each at least 0.", nameof(columnWidths));
        t.ColumnWidths = columnWidths.ToList();
        t.Rows = t.Rows.Zip(rowHeights, (r, h) => r.Height == h ? r : r with { Height = h }).ToList();
        t.Transform = TableEdit.Frame(t) with { Width = columnWidths.Sum(), Height = rowHeights.Sum() };
    });

    /// <inheritdoc />
    public void Revert(Presentation document) => _change.Revert(document);
}

/// <summary>Sets the fill of table cells.</summary>
/// <param name="slide">The slide.</param>
/// <param name="table">The id of a <see cref="TableShape"/>.</param>
/// <param name="cells">The cells as row and column index.</param>
/// <param name="fill">
/// The fill, or null to take it from the table style: <see cref="ShapeFillKind.None"/> or <see cref="ShapeFillKind.Solid"/> with a colour that can
/// be written; other fills throw an <see cref="ArgumentException"/>.
/// </param>
public sealed class SetTableCellFillCommand(Slide slide, uint table, IReadOnlyList<(int Row, int Column)> cells, ShapeFill fill) : IDocumentCommand<Presentation>
{
    private readonly TableChange _change = new(slide, table);

    /// <inheritdoc />
    public string Description => "Format cells";

    /// <inheritdoc />
    public void Apply(Presentation document) => _change.Apply(document, t =>
    {
        if (fill is not (null or { Kind: ShapeFillKind.None }) && (fill.Kind != ShapeFillKind.Solid || SlideXml.SolidFill(fill.Color) == null))
            throw new ArgumentException("A cell fill must be no fill or a solid colour that can be written.", nameof(fill));
        var grid = TableEdit.Grid(t.Rows);
        foreach (var (r, c) in cells ?? []) grid[r][c] = TableEdit.Cell(grid, r, c, true) with { Fill = fill };
        t.Rows = TableEdit.Rows(t.Rows, grid);
    });

    /// <inheritdoc />
    public void Revert(Presentation document) => _change.Revert(document);
}

/// <summary>Turns the style options of a table on or off; null keeps an option.</summary>
/// <param name="slide">The slide.</param>
/// <param name="table">The id of a <see cref="TableShape"/>.</param>
/// <param name="firstRow">The header row.</param>
/// <param name="bandedRows">Banded rows.</param>
/// <param name="firstColumn">The first column.</param>
/// <param name="lastRow">The total row.</param>
/// <param name="lastColumn">The last column.</param>
/// <param name="bandedColumns">Banded columns.</param>
public sealed class SetTableOptionsCommand(
    Slide slide, uint table, bool? firstRow, bool? bandedRows, bool? firstColumn, bool? lastRow, bool? lastColumn, bool? bandedColumns) : IDocumentCommand<Presentation>
{
    private readonly TableChange _change = new(slide, table);

    /// <inheritdoc />
    public string Description => "Table style options";

    /// <inheritdoc />
    public void Apply(Presentation document) => _change.Apply(document, t =>
    {
        t.FirstRow = firstRow ?? t.FirstRow;
        t.BandedRows = bandedRows ?? t.BandedRows;
        t.FirstColumn = firstColumn ?? t.FirstColumn;
        t.LastRow = lastRow ?? t.LastRow;
        t.LastColumn = lastColumn ?? t.LastColumn;
        t.BandedColumns = bandedColumns ?? t.BandedColumns;
    });

    /// <inheritdoc />
    public void Revert(Presentation document) => _change.Revert(document);
}

internal sealed class TableChange(Slide slide, uint id)
{
    private TableShape _table;
    private TableShape _before;
    private DeleteShapesCommand _delete;

    internal void Apply(Presentation document, Action<TableShape> change)
    {
        _table = ShapeEdit.Find<TableShape>(slide, id);
        _before = _table.Snapshot();
        _delete = null;
        try
        {
            change(_table);
        }
        catch
        {
            _table.Restore(_before);
            throw;
        }

        if (_table.Rows.Count == 0 || _table.ColumnWidths.Count == 0)
        {
            _table.Restore(_before);
            _delete = new DeleteShapesCommand(slide, [id]);
            _delete.Apply(document);
        }
        else if (_table.SameAs(_before)) _table.Restore(_before);
        else document.Notify(slide, PresentationChangeKind.Content);
    }

    internal void Revert(Presentation document)
    {
        if (_delete != null) _delete.Revert(document);
        else
        {
            _table.Restore(_before);
            document.Notify(slide, PresentationChangeKind.Content);
        }
    }
}

internal static class TableEdit
{
    internal static TableCell[][] Grid(IReadOnlyList<TableRow> rows) => rows.Select(r => r.Cells.ToArray()).ToArray();

    internal static List<TableRow> Rows(IReadOnlyList<TableRow> rows, TableCell[][] grid) =>
        rows.Zip(grid, (row, cells) => row.Cells.SequenceEqual(cells) ? row : row with { Cells = cells }).ToList();

    internal static TableCell Cell(TableCell[][] grid, int row, int column, bool mergedAway = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, grid.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, grid[row].Length);
        var cell = grid[row][column];
        return cell.IsMergedAway && !mergedAway ? throw new ArgumentException($"The cell {row},{column} is merged into another cell.", nameof(row)) : cell;
    }

    internal static void CheckInsert(int length, int index, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, length);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
    }

    internal static void CheckDelete(int length, int index, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index + count, length, nameof(count));
    }

    internal static ShapeTransform Frame(TableShape table) => table.Transform ?? ShapeEdit.ToLocal(table, table.ResolvedTransform);

    internal static void Resize(TableShape table, long width, long height)
    {
        if (table.Transform is { } t && table.Rows.Count > 0 && table.ColumnWidths.Count > 0)
            table.Transform = t with { Width = Math.Max(0, t.Width + width), Height = Math.Max(0, t.Height + height) };
    }

    internal static void Scale(TableShape table, ShapeTransform? before)
    {
        if (table.Transform is not { } t) return;
        if (before?.Width != t.Width) table.ColumnWidths = Scale(table.ColumnWidths, t.Width);
        if (before?.Height == t.Height) return;
        var heights = Scale(table.Rows.Select(r => r.Height).ToList(), t.Height);
        table.Rows = table.Rows.Zip(heights, (r, h) => r.Height == h ? r : r with { Height = h }).ToList();
    }

    private static IReadOnlyList<long> Scale(IReadOnlyList<long> sizes, long total)
    {
        var sum = sizes.Sum();
        if (sum == total || sizes.Count == 0) return sizes;
        var result = new long[sizes.Count];
        var (done, cumulative) = (0L, 0L);
        for (var i = 0; i < sizes.Count; i++)
        {
            cumulative += sizes[i];
            var end = sum == 0 ? total * (i + 1) / sizes.Count : (long)Math.Round((double)cumulative * total / sum);
            (result[i], done) = (end - done, end);
        }

        return result;
    }

    internal static TableCell[][] Transpose(TableCell[][] grid) =>
        grid.Length == 0 ? grid : Enumerable.Range(0, grid[0].Length).Select(c => grid.Select(row => row[c] with { RowSpan = row[c].ColumnSpan, ColumnSpan = row[c].RowSpan }).ToArray()).ToArray();

    internal static TableCell[][] Insert(TableCell[][] grid, int index, int count)
    {
        var template = grid[Math.Max(0, index - 1)];
        var rows = grid.Select((row, r) => row.Select(cell => !cell.IsMergedAway && r < index && index < r + cell.RowSpan ? cell with { RowSpan = cell.RowSpan + count } : cell)
            .ToArray()).ToList();
        rows.InsertRange(index, Enumerable.Range(0, count).Select(_ => template.Select(cell => cell with
        {
            Text = new TextBody { Paragraphs = [new TextParagraph()] }, RowSpan = 1, ColumnSpan = 1, IsMergedAway = false
        }).ToArray()));
        return Normalize(rows.ToArray());
    }

    internal static TableCell[][] Delete(TableCell[][] grid, int index, int count)
    {
        bool Kept(int r) => r < index || r >= index + count;
        var rows = grid.Where((_, r) => Kept(r)).Select(row => row.ToArray()).ToArray();
        for (var r = 0; r < grid.Length; r++)
        for (var c = 0; c < grid[r].Length; c++)
        {
            var cell = grid[r][c];
            var remaining = cell.IsMergedAway ? [] : Enumerable.Range(r, cell.RowSpan).Where(x => x < grid.Length && Kept(x)).ToList();
            if (remaining.Count > 0) rows[remaining[0] < index ? remaining[0] : remaining[0] - count][c] = cell with { RowSpan = remaining.Count };
        }

        return Normalize(rows);
    }

    internal static TableCell[][] Normalize(TableCell[][] grid)
    {
        var layout = Layout(grid);
        return grid.Select((row, r) => row.Select((cell, c) => layout[r][c] is var (ar, ac, rs, cs) && (ar, ac) == (r, c)
            ? cell with { IsMergedAway = false, RowSpan = rs, ColumnSpan = cs }
            : cell with { IsMergedAway = true, RowSpan = 1, ColumnSpan = 1 }).ToArray()).ToArray();
    }

    internal static (int Row, int Column, int RowSpan, int ColumnSpan)[][] Layout(IReadOnlyList<IReadOnlyList<TableCell>> grid)
    {
        var owners = grid.Select(row => new (int, int, int, int)?[row.Count]).ToArray();
        for (var r = 0; r < grid.Count; r++)
        for (var c = 0; c < grid[r].Count; c++)
        {
            if (owners[r][c] != null) continue;
            var cell = grid[r][c];
            var (rows, columns) = cell.IsMergedAway ? (1, 1) : (Math.Max(1, cell.RowSpan), Math.Max(1, cell.ColumnSpan));
            var cs = 1;
            while (cs < columns && c + cs < grid[r].Count && owners[r][c + cs] == null) cs++;
            var rs = 1;
            while (rs < rows && r + rs < grid.Count && Enumerable.Range(c, cs).All(x => x < grid[r + rs].Count && owners[r + rs][x] == null)) rs++;
            for (var rr = r; rr < r + rs; rr++)
            for (var cc = c; cc < c + cs; cc++)
                owners[rr][cc] = (r, c, rs, cs);
        }

        return owners.Select(row => row.Select(o => o!.Value).ToArray()).ToArray();
    }
}
