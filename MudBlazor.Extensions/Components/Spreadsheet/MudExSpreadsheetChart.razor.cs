using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Shows one chart of a worksheet with <see cref="MudExOfficeChart"/>, and charts the editor cannot draw as a placeholder.
/// It renders again only when its data, size or look changed.
/// </summary>
public partial class MudExSpreadsheetChart
{
    private static readonly string[] DefaultColors = ["#4472C4", "#ED7D31", "#A5A5A5", "#FFC000", "#5B9BD5", "#70AD47", "#264478", "#9E480E", "#636363", "#997300"];

    private object _look;
    private ChartData _data;
    private (double Width, double Height) _size;
    private bool _changed = true;
    private ChartSnapshot _snapshot;

    /// <summary>The chart to show.</summary>
    [Parameter] public SheetChart Chart { get; set; }

    /// <summary>The chart's data, see <see cref="ChartDataResolver.Resolve"/>. A new instance renders the chart again.</summary>
    [Parameter] public ChartData Data { get; set; }

    /// <summary>Width in pixels.</summary>
    [Parameter] public double Width { get; set; }

    /// <summary>Height in pixels.</summary>
    [Parameter] public double Height { get; set; }

    private bool Supported => Chart is { ReadOnly: false } && Chart.Kind != ChartKind.Other && Data != null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var look = Chart == null ? null : (object)(Chart.Kind, Chart.Title, Chart.ShowLegend, Chart.Stacked, Chart.ReadOnly, string.Join("|", Chart.Series.Select(s => s.Color)));
        if (ReferenceEquals(Data, _data) && _size == (Width, Height) && Equals(look, _look)) return;
        _look = look;
        _data = Data;
        _size = (Width, Height);
        _changed = true;
        _snapshot = Supported ? Snapshot() : null;
    }

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        var changed = _changed;
        _changed = false;
        return changed;
    }

    private ChartSnapshot Snapshot() => new(
        Chart.Kind, Chart.Title ?? "", Data.Categories,
        [.. Data.Series.Select((s, i) => new ChartSnapshotSeries(s.Name, [.. s.Values.Select(v => double.IsNaN(v) ? (double?)null : v)], Color(i)))],
        Chart.ShowLegend)
    {
        Stacked = Chart.Stacked
    };

    private string Color(int index) =>
        index < Chart.Series.Count && Chart.Series[index].Color is { } c ? c : DefaultColors[index % DefaultColors.Length];
}
