using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

public partial class MudExSpreadsheetGrid
{
    private static readonly string[] ChartHandles = ["nw", "n", "ne", "e", "se", "s", "sw", "w"];
    private readonly Dictionary<string, ChartCache> _chartCache = new();
    private string _selectedChart;

    private sealed record ChartCache(SheetChart Chart, ChartData Data, List<(Worksheet Sheet, CellRange Range)> Sources);

    /// <summary>Raised on a double click on a chart so the host can open its chart dialog.</summary>
    [Parameter] public EventCallback<SheetChart> ChartEditRequested { get; set; }

    /// <summary>The selected chart, or null while cells are selected.</summary>
    public SheetChart SelectedChart => _selectedChart == null ? null : _sheet?.Charts.FirstOrDefault(c => c.Id == _selectedChart);

    /// <summary>Called by the browser when a chart was clicked.</summary>
    /// <param name="id">The chart id.</param>
    /// <returns>A task that completes when the chart is selected.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnChartSelect(string id)
    {
        if (Chart(id) is not { } chart || Locked(chart)) return;
        if (_editing) await CommitAsync(0, 0, false);
        _selectedChart = id;
        StateHasChanged();
    }

    /// <summary>Called by the browser after a chart was moved or resized with the pointer.</summary>
    /// <param name="id">The chart id.</param>
    /// <param name="left">New left edge in canvas pixels.</param>
    /// <param name="top">New top edge in canvas pixels.</param>
    /// <param name="width">New width in pixels.</param>
    /// <param name="height">New height in pixels.</param>
    /// <returns>A task that completes when the chart moved.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnChartMoved(string id, double left, double top, double width, double height)
    {
        if (Chart(id) is not { } chart || Locked(chart)) return;
        if (_editing) await CommitAsync(0, 0, false);
        _selectedChart = id;
        var (from, fromOffset) = Anchor(left, top);
        var (to, toOffset) = Anchor(left + Math.Max(1, width), top + Math.Max(1, height));
        await TryWriteAsync(() => _workbook.Execute(new MoveChartCommand(_sheet, id, from, to, fromOffset, toOffset)));
        StateHasChanged();
    }

    /// <summary>Called by the browser after a double click on a chart.</summary>
    /// <param name="id">The chart id.</param>
    /// <returns>A task that completes when the host was told.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnChartDoubleClick(string id)
    {
        if (Chart(id) is not { } chart || Locked(chart)) return;
        await OnChartSelect(id);
        await ChartEditRequested.InvokeAsync(chart);
    }

    internal void SelectChart(string id)
    {
        _selectedChart = id;
        StateHasChanged();
    }

    private SheetChart Chart(string id) => _sheet?.Charts.FirstOrDefault(c => c.Id == id);

    private bool Locked(SheetChart chart) => !CanEdit || chart.ReadOnly || chart.Kind == ChartKind.Other;

    private (CellRef Cell, (double X, double Y) Offset) Anchor(double x, double y)
    {
        var col = ColumnAt(Math.Max(HeaderWidth, x));
        var row = RowAt(Math.Max(HeaderHeight, y));
        return (new CellRef(row, col), (Math.Max(0, (x - X(col)) / _zoom), Math.Max(0, (y - Y(row)) / _zoom)));
    }

    private async Task<bool> ChartKeyAsync(KeyboardEventArgs e)
    {
        if (e.Key is "Control" or "Shift" or "Alt" or "Meta") return true;
        var chart = SelectedChart;
        _selectedChart = null;
        StateHasChanged();
        if (chart == null) return false;
        if (e.Key is "Delete" or "Backspace" && !Locked(chart))
        {
            await TryWriteAsync(() => _workbook.Execute(new DeleteChartCommand(_sheet, chart.Id)));
            return true;
        }

        return e.Key == "Escape";
    }

    private IEnumerable<SheetChart> VisibleCharts(GridPane p)
    {
        if (_sheet.Charts.Count == 0 || p.R1 < p.R0 || p.C1 < p.C0) yield break;
        var window = CellRange.FromBounds(p.R0, p.C0, p.R1, p.C1);
        foreach (var c in _sheet.Charts)
            if (CellRange.FromBounds(Math.Min(c.From.Row, c.To.Row), Math.Min(c.From.Column, c.To.Column), Math.Max(c.From.Row, c.To.Row), Math.Max(c.From.Column, c.To.Column)).Intersects(window))
                yield return c;
    }

    private (double Left, double Top, double Width, double Height) ChartBox(SheetChart c)
    {
        var left = X(c.From.Column) + c.FromOffset.X * _zoom;
        var top = Y(c.From.Row) + c.FromOffset.Y * _zoom;
        return (left, top, Math.Max(1, X(c.To.Column) + c.ToOffset.X * _zoom - left), Math.Max(1, Y(c.To.Row) + c.ToOffset.Y * _zoom - top));
    }

    private string ChartLayerStyle(GridPane p) => p.Kind == 0 ? null : PaneBackground(p) + ";overflow:hidden";

    private static string ChartStyle((double Left, double Top, double Width, double Height) b) =>
        $"left:{Px(b.Left)};top:{Px(b.Top)};width:{Px(b.Width)};height:{Px(b.Height)}";

    internal ChartData ChartDataOf(SheetChart chart)
    {
        if (_chartCache.TryGetValue(chart.Id, out var cache) && (ReferenceEquals(cache.Chart, chart) || cache.Chart.Series.SequenceEqual(chart.Series)))
            return cache.Data;
        var data = chart.Kind == ChartKind.Other ? null : ChartDataResolver.Resolve(_workbook, chart);
        _chartCache[chart.Id] = new ChartCache(chart, data, ChartDataResolver.Sources(_workbook, chart));
        return data;
    }

    private void InvalidateCharts(WorkbookChangedEventArgs e)
    {
        if (_chartCache.Count == 0) return;
        if (e.Structure.Count > 0)
        {
            _chartCache.Clear();
            return;
        }

        foreach (var c in e.SingleCells) InvalidateCharts(c.Sheet, new CellRange(c.Cell, c.Cell));
        foreach (var (sheet, range) in e.ValueRanges) InvalidateCharts(sheet, range);
        foreach (var f in e.FormattedRanges) InvalidateCharts(f.Sheet, f.Range);
    }

    private void InvalidateCharts(CalculatedEventArgs e)
    {
        if (_chartCache.Count == 0) return;
        foreach (var c in e.Cells) InvalidateCharts(c.Sheet, new CellRange(c.Cell, c.Cell));
    }

    private void InvalidateCharts(Worksheet sheet, CellRange range)
    {
        foreach (var (id, cache) in _chartCache)
            if (cache.Sources.Any(s => s.Sheet == sheet && s.Range.Intersects(range)))
                _chartCache.Remove(id);
    }
}
