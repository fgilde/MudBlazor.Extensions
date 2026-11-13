using System.ComponentModel;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlideCanvas
{
    private static readonly string[] TableEdges = ["top", "right", "bottom", "left"];

    private (uint Table, int Row, int Column, int Row2, int Column2)? _cells;

    internal TableShape SelectedTable => _selection is [var id] && Find(id) is TableShape table ? table : null;

    internal (int Row, int Column, int RowCount, int ColumnCount)? SelectedCells =>
        _cells is { } c && SelectedTable is { } table && table.Id == c.Table ? SlideTableStyles.Range(table, c.Row, c.Column, c.Row2, c.Column2) : null;

    internal IReadOnlyList<(TableShape Table, int Row, int Column, AutoShape Shape)> SelectedCellShapes
    {
        get
        {
            if (SelectedTable is not { } table)
                return [];
            var master = table.Master ?? Slide.Layout?.Master;
            var styles = SlideTableStyles.For(table, master);
            var range = SelectedCells ?? (0, 0, table.Rows.Count, table.ColumnWidths.Count);
            return SlideTableStyles.Anchors(table)
                .Where(a => a.Row >= range.Row && a.Row < range.Row + range.RowCount && a.Column >= range.Column && a.Column < range.Column + range.ColumnCount)
                .Select(a => (table, a.Row, a.Column, SlideTableStyles.TextShape(table, a.Row, a.Column, styles[a.Row][a.Column], master))).ToList();
        }
    }

    /// <summary>Called by the browser when the user clicked into the selected table or dragged over its cells.</summary>
    /// <param name="table">The id of the selected table.</param>
    /// <param name="row">The row where the selection started.</param>
    /// <param name="column">The column where the selection started.</param>
    /// <param name="row2">The row where the selection ends.</param>
    /// <param name="column2">The column where the selection ends.</param>
    /// <returns>A task that completes when the cells are shown as selected.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnCellSelectAsync(uint table, int row, int column, int row2, int column2) => InvokeAsync(async () =>
    {
        if (ReadOnly || SelectedTable?.Id != table)
            return;
        if (_text != null)
            await CommitTextAsync();
        await SelectCellsAsync((table, row, column, row2, column2));
    });

    /// <summary>Called by the browser when the user dragged a column or row border of the selected table.</summary>
    /// <param name="table">The id of the table.</param>
    /// <param name="columnWidths">The new column widths in EMU.</param>
    /// <param name="rowHeights">The new row heights in EMU.</param>
    /// <returns>A task that completes when the sizes are set.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnTableSizesAsync(uint table, long[] columnWidths, long[] rowHeights) => InvokeAsync(() =>
    {
        if (ReadOnly || Slide == null || Find(table) is not TableShape shape || columnWidths?.Length != shape.ColumnWidths.Count || rowHeights?.Length != shape.Rows.Count)
            return;
        Presentation.Execute(new SetTableSizesCommand(Slide, table, columnWidths.Select(w => Math.Max(1, w)).ToList(), rowHeights.Select(h => Math.Max(1, h)).ToList()));
        StateHasChanged();
    });

    private Task SelectCellsAsync((uint Table, int Row, int Column, int Row2, int Column2)? cells)
    {
        if (cells != _cells)
        {
            _cells = cells;
            _raiseSelection = true;
        }

        StateHasChanged();
        return RaiseSelectionAsync();
    }

    private async Task BeginCellTextAsync(string text = null, double[] caret = null)
    {
        if (ReadOnly || SelectedTable is not { } table || _cells is not { } cells || cells.Table != table.Id)
            return;
        CommitText();
        var (row, column) = SlideTableStyles.Anchor(table, cells.Row, cells.Column);
        var master = table.Master ?? Slide.Layout?.Master;
        var shape = SlideTableStyles.TextShape(table, row, column, SlideTableStyles.For(table, master)[row][column], master);
        _text = new TextSession(Slide, shape, null, text, caret) { Cell = (table, row, column) };
        await SelectCellsAsync((table.Id, row, column, row, column));
    }

    private void CommitCell(TextSession session, TextBody body)
    {
        var (table, row, column) = session.Cell!.Value;
        if (!ReferenceEquals(body, session.Shape.Text) && table.Rows.ElementAtOrDefault(row)?.Cells.ElementAtOrDefault(column) is { IsMergedAway: false })
            Presentation.Execute(new SetTableCellTextCommand(session.Slide, table.Id, row, column, body));
    }

    private async Task OnTextTabbedAsync((SlideTextParagraphDto[] Paragraphs, bool Back) tab)
    {
        if (_text is not { Cell: { } cell } session)
            return;
        var (table, row, column) = cell;
        session.Draft = tab.Paragraphs;
        CommitText();
        _cells = (table.Id, row, column, row, column);
        await MoveCellAsync(tab.Back, true);
    }

    private async Task MoveCellAsync(bool back, bool edit)
    {
        if (SelectedTable is not { } table || _cells is not { } cells)
            return;
        var anchors = SlideTableStyles.Anchors(table).Select(a => (a.Row, a.Column)).ToList();
        var index = anchors.IndexOf(SlideTableStyles.Anchor(table, cells.Row, cells.Column)) + (back ? -1 : 1);
        if (index >= anchors.Count)
        {
            Presentation.Execute(new InsertTableRowsCommand(Slide, table.Id, table.Rows.Count, 1));
            anchors = [SlideTableStyles.Anchor(table, table.Rows.Count - 1, 0)];
            index = 0;
        }

        var (row, column) = anchors[Math.Max(0, index)];
        if (edit)
        {
            _cells = (table.Id, row, column, row, column);
            await BeginCellTextAsync();
        }
        else
            await SelectCellsAsync((table.Id, row, column, row, column));
    }

    private async Task<bool> HandleCellKeyAsync(KeyboardEventArgs e, bool ctrl)
    {
        if (_cells == null || SelectedTable is not { } table)
            return false;
        switch (e.Key)
        {
            case "Tab" when !ctrl:
                await MoveCellAsync(e.ShiftKey, false);
                return true;
            case "F2" or "Enter" when !ctrl:
                await BeginCellTextAsync();
                return true;
            case "Escape":
                await SelectCellsAsync(null);
                return true;
            case "Delete" or "Backspace":
                using (Presentation.Undo.BeginTransaction("Clear cells"))
                    foreach (var (_, row, column, shape) in SelectedCellShapes.Where(c => table.Rows[c.Row].Cells[c.Column].HasText))
                        Presentation.Execute(new SetTableCellTextCommand(Slide, table.Id, row, column, shape.Text with { Paragraphs = [shape.Text.Paragraphs[0] with { Runs = [] }] }));
                StateHasChanged();
                return true;
            case { Length: 1 } when !ctrl:
                await BeginCellTextAsync(e.Key);
                return true;
            default:
                return false;
        }
    }

    private static string Sizes(SlideShape shape, bool columns) =>
        shape is TableShape table ? string.Join(",", columns ? table.ColumnWidths : table.Rows.Select(r => r.Height)) : null;

    private static List<double> Edges(IEnumerable<long> sizes)
    {
        var list = sizes.ToList();
        var sum = (double)Math.Max(1, list.Sum());
        var edges = new List<double> { 0 };
        foreach (var size in list)
            edges.Add(edges[^1] + size / sum * 100);
        return edges;
    }

    private static string RangeStyle(List<double> xs, List<double> ys, (int Row, int Column, int RowCount, int ColumnCount) range) =>
        $"left:{MudExSlideShapeView.Num(xs[range.Column])}%;top:{MudExSlideShapeView.Num(ys[range.Row])}%;" +
        $"width:{MudExSlideShapeView.Num(xs[range.Column + range.ColumnCount] - xs[range.Column])}%;height:{MudExSlideShapeView.Num(ys[range.Row + range.RowCount] - ys[range.Row])}%;";
}
