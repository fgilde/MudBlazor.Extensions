using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlideEditor
{
    private const long TableRowHeight = 370840;

    private static readonly (string Id, string Text, Func<TableShape, bool> Get, Func<Slide, uint, bool, SetTableOptionsCommand> Set)[] TableOptions =
    [
        ("pp-table-header-row", "Header Row", t => t.FirstRow, (s, id, on) => new(s, id, on, null, null, null, null, null)),
        ("pp-table-total-row", "Total Row", t => t.LastRow, (s, id, on) => new(s, id, null, null, null, on, null, null)),
        ("pp-table-banded-rows", "Banded Rows", t => t.BandedRows, (s, id, on) => new(s, id, null, on, null, null, null, null)),
        ("pp-table-first-column", "First Column", t => t.FirstColumn, (s, id, on) => new(s, id, null, null, on, null, null, null)),
        ("pp-table-last-column", "Last Column", t => t.LastColumn, (s, id, on) => new(s, id, null, null, null, null, on, null)),
        ("pp-table-banded-columns", "Banded Columns", t => t.BandedColumns, (s, id, on) => new(s, id, null, null, null, null, null, on))
    ];

    private TableShape SelectedTable => IsReadOnly ? null : _canvas?.SelectedTable;

    private (int Row, int Column, int RowCount, int ColumnCount)? SelectedCells => SelectedTable == null ? null : _canvas.SelectedCells;

    private bool CanMerge => SelectedTable is { } table && SelectedCells is { } range &&
                             SlideTableStyles.Anchors(table).Count(a => a.Row >= range.Row && a.Row < range.Row + range.RowCount && a.Column >= range.Column && a.Column < range.Column + range.ColumnCount) > 1;

    private bool CanSplit => SelectedTable is { } table && SelectedCells is { } range && table.Rows[range.Row].Cells[range.Column] is var cell && cell.RowSpan * cell.ColumnSpan > 1;

    private async Task InsertTableAsync((int Rows, int Columns) size)
    {
        if (IsReadOnly || _current == null)
            return;
        var width = _presentation.SlideWidth * 8 / 10 / size.Columns * size.Columns;
        var height = size.Rows * TableRowHeight;
        var command = new InsertTableCommand(_current, size.Rows, size.Columns,
            new ShapeTransform((_presentation.SlideWidth - width) / 2, (_presentation.SlideHeight - height) / 2, width, height));
        await RunAsync(() => _presentation.Execute(command));
        if (command.Table != null)
            await _canvas.SelectAsync([command.Table.Id]);
    }

    private Task TableAsync(Func<TableShape, (int Row, int Column, int RowCount, int ColumnCount), IDocumentCommand<Presentation>> create) => RunAsync(() =>
    {
        if (SelectedTable is { } table && SelectedCells is { } range)
            _presentation.Execute(create(table, range));
    });

    private Task InsertRowsAsync(bool below) =>
        TableAsync((t, r) => new InsertTableRowsCommand(_current, t.Id, below ? r.Row + r.RowCount : r.Row, r.RowCount));

    private Task InsertColumnsAsync(bool right) =>
        TableAsync((t, r) => new InsertTableColumnsCommand(_current, t.Id, right ? r.Column + r.ColumnCount : r.Column, r.ColumnCount));

    private Task DeleteRowsAsync() => TableAsync((t, r) => new DeleteTableRowsCommand(_current, t.Id, r.Row, r.RowCount));

    private Task DeleteColumnsAsync() => TableAsync((t, r) => new DeleteTableColumnsCommand(_current, t.Id, r.Column, r.ColumnCount));

    private Task MergeCellsAsync() => CanMerge ? TableAsync((t, r) => new MergeTableCellsCommand(_current, t.Id, r.Row, r.Column, r.RowCount, r.ColumnCount)) : Task.CompletedTask;

    private Task SplitCellAsync() => CanSplit ? TableAsync((t, r) => new SplitTableCellCommand(_current, t.Id, r.Row, r.Column)) : Task.CompletedTask;

    private Task DeleteTableAsync() => RunAsync(() =>
    {
        if (SelectedTable is { } table)
            _presentation.Execute(new DeleteShapesCommand(_current, [table.Id]));
    });

    private Task SetShadingAsync(MudExColor color) => Hex(color) is not { } hex ? Task.CompletedTask : RunAsync(() =>
    {
        if (SelectedTable is not { } table)
            return;
        var range = SelectedCells ?? (0, 0, table.Rows.Count, table.ColumnWidths.Count);
        var cells = Enumerable.Range(range.Row, range.RowCount).SelectMany(r => Enumerable.Range(range.Column, range.ColumnCount).Select(c => (r, c))).ToList();
        _presentation.Execute(new SetTableCellFillCommand(_current, table.Id, cells, new ShapeFill(ShapeFillKind.Solid, hex)));
    });

    private Task SetTableOptionAsync(Func<Slide, uint, SetTableOptionsCommand> create) => RunAsync(() =>
    {
        if (SelectedTable is { } table)
            _presentation.Execute(create(_current, table.Id));
    });
}
