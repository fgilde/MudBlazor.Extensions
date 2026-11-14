namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>Adds an empty table on top of the other elements.</summary>
/// <param name="page">The page.</param>
/// <param name="rows">The number of rows, at least 1.</param>
/// <param name="columns">The number of columns, at least 1.</param>
/// <param name="x">The left edge in page coordinates.</param>
/// <param name="y">The top edge in page coordinates.</param>
public sealed class AddTableCommand(NotebookPage page, int rows, int columns, double x, double y) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <summary>The created table, available after the command was applied.</summary>
    public TableElement Table { get; private set; }

    /// <inheritdoc />
    public string Description => "Insert table";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        if (rows < 1) throw new ArgumentOutOfRangeException(nameof(rows));
        if (columns < 1) throw new ArgumentOutOfRangeException(nameof(columns));
        Table = new TableElement
        {
            X = NotebookEdit.Number(x, nameof(x)), Y = NotebookEdit.Number(y, nameof(y)), Width = columns * TableEdit.DefaultColumnWidth,
            ColumnWidths = Enumerable.Repeat(TableEdit.DefaultColumnWidth, columns).ToList(), Rows = Enumerable.Range(0, rows).Select(_ => TableEdit.EmptyRow(columns)).ToList()
        };
        elements.Add(Table);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Replaces the text of a table cell.</summary>
/// <param name="page">The page.</param>
/// <param name="tableId">The id of a <see cref="TableElement"/>.</param>
/// <param name="row">The row index.</param>
/// <param name="column">The column index.</param>
/// <param name="paragraphs">The new paragraphs, checked like those of <see cref="SetTextCommand"/>.</param>
public sealed class SetTableCellCommand(NotebookPage page, string tableId, int row, int column, IReadOnlyList<NoteParagraph> paragraphs) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => "Edit cell";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements => TableEdit.Change(elements, tableId, table =>
    {
        if (row < 0 || row >= table.Rows.Count) throw new ArgumentOutOfRangeException(nameof(row));
        if (column < 0 || column >= table.ColumnWidths.Count) throw new ArgumentOutOfRangeException(nameof(column));
        var cells = table.Rows[row].ToList();
        cells[column] = cells[column] with { Paragraphs = NotebookEdit.Text(paragraphs, nameof(paragraphs)) };
        var result = table.Rows.ToList();
        result[row] = cells;
        return (result, table.ColumnWidths);
    }));

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Inserts empty rows.</summary>
/// <param name="page">The page.</param>
/// <param name="tableId">The id of a <see cref="TableElement"/>.</param>
/// <param name="index">The position of the first new row, from 0 to the row count.</param>
/// <param name="count">The number of rows.</param>
public sealed class InsertTableRowsCommand(NotebookPage page, string tableId, int index, int count = 1) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => "Insert rows";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements => TableEdit.Change(elements, tableId, table =>
    {
        TableEdit.CheckInsert(index, count, table.Rows.Count);
        var result = table.Rows.ToList();
        result.InsertRange(index, Enumerable.Range(0, count).Select(_ => TableEdit.EmptyRow(table.ColumnWidths.Count)));
        return (result, table.ColumnWidths);
    }));

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Deletes rows; a table keeps at least one row, delete the table element instead.</summary>
/// <param name="page">The page.</param>
/// <param name="tableId">The id of a <see cref="TableElement"/>.</param>
/// <param name="index">The first row to delete.</param>
/// <param name="count">The number of rows.</param>
public sealed class DeleteTableRowsCommand(NotebookPage page, string tableId, int index, int count = 1) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => "Delete rows";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements => TableEdit.Change(elements, tableId, table =>
    {
        TableEdit.CheckDelete(index, count, table.Rows.Count, "row");
        var result = table.Rows.ToList();
        result.RemoveRange(index, count);
        return (result, table.ColumnWidths);
    }));

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Inserts empty columns of the default width; the table grows by their width.</summary>
/// <param name="page">The page.</param>
/// <param name="tableId">The id of a <see cref="TableElement"/>.</param>
/// <param name="index">The position of the first new column, from 0 to the column count.</param>
/// <param name="count">The number of columns.</param>
public sealed class InsertTableColumnsCommand(NotebookPage page, string tableId, int index, int count = 1) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => "Insert columns";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements => TableEdit.Change(elements, tableId, table =>
    {
        TableEdit.CheckInsert(index, count, table.ColumnWidths.Count);
        var widths = table.ColumnWidths.ToList();
        widths.InsertRange(index, Enumerable.Repeat(TableEdit.DefaultColumnWidth, count));
        return (table.Rows.Select(r =>
        {
            var cells = r.ToList();
            cells.InsertRange(index, TableEdit.EmptyRow(count));
            return (IReadOnlyList<NoteTableCell>)cells;
        }).ToList(), widths);
    }));

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Deletes columns; a table keeps at least one column, delete the table element instead.</summary>
/// <param name="page">The page.</param>
/// <param name="tableId">The id of a <see cref="TableElement"/>.</param>
/// <param name="index">The first column to delete.</param>
/// <param name="count">The number of columns.</param>
public sealed class DeleteTableColumnsCommand(NotebookPage page, string tableId, int index, int count = 1) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => "Delete columns";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements => TableEdit.Change(elements, tableId, table =>
    {
        TableEdit.CheckDelete(index, count, table.ColumnWidths.Count, "column");
        var widths = table.ColumnWidths.ToList();
        widths.RemoveRange(index, count);
        return (table.Rows.Select(r =>
        {
            var cells = r.ToList();
            cells.RemoveRange(index, count);
            return (IReadOnlyList<NoteTableCell>)cells;
        }).ToList(), widths);
    }));

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Sets the column widths; the table width becomes their sum.</summary>
/// <param name="page">The page.</param>
/// <param name="tableId">The id of a <see cref="TableElement"/>.</param>
/// <param name="widths">One positive width in pixels per column.</param>
public sealed class SetTableColumnWidthsCommand(NotebookPage page, string tableId, IReadOnlyList<double> widths) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => "Column width";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements => TableEdit.Change(elements, tableId, table =>
    {
        if (widths == null || widths.Count != table.ColumnWidths.Count) throw new ArgumentException("The widths must be given one per column.", nameof(widths));
        return (table.Rows, widths.Select(w => NotebookEdit.Positive(w, nameof(widths))).ToList());
    }));

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Makes the first row of a table a header row or a normal row.</summary>
/// <param name="page">The page.</param>
/// <param name="tableId">The id of a <see cref="TableElement"/>.</param>
/// <param name="headerRow">True for a header row.</param>
public sealed class SetTableHeaderRowCommand(NotebookPage page, string tableId, bool headerRow) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => "Header row";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        var table = NotebookEdit.Find<TableElement>(elements, tableId);
        if (table.HeaderRow != headerRow) NotebookEdit.Replace(elements, table).HeaderRow = headerRow;
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

internal static class TableEdit
{
    public const double DefaultColumnWidth = 120;

    public static IReadOnlyList<NoteTableCell> EmptyRow(int columns) =>
        Enumerable.Range(0, columns).Select(_ => new NoteTableCell { Paragraphs = [new NoteParagraph()] }).ToList();

    public static void Change(List<NoteElement> elements, string tableId,
        Func<TableElement, (IReadOnlyList<IReadOnlyList<NoteTableCell>> Rows, IReadOnlyList<double> Widths)> change)
    {
        var table = NotebookEdit.Find<TableElement>(elements, tableId);
        var (rows, widths) = change(table);
        if (widths.SequenceEqual(table.ColumnWidths) && rows.Count == table.Rows.Count && rows.Zip(table.Rows).All(r => r.First.SequenceEqual(r.Second))) return;
        var changed = NotebookEdit.Replace(elements, table);
        (changed.Rows, changed.ColumnWidths, changed.Width) = (rows, widths, widths.Sum());
    }

    public static void CheckInsert(int index, int count, int length)
    {
        if (index < 0 || index > length) throw new ArgumentOutOfRangeException(nameof(index));
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
    }

    public static void CheckDelete(int index, int count, int length, string what)
    {
        if (index < 0 || count < 1 || index + count > length) throw new ArgumentOutOfRangeException(nameof(index));
        if (count == length) throw new ArgumentException($"A table keeps at least one {what}; delete the table instead.", nameof(count));
    }
}
