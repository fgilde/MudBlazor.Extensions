using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Status bar of a spreadsheet: average, count and sum of the selected cells, formatted like the active cell, and a zoom slider.
/// </summary>
public partial class MudExSpreadsheetStatusBar
{
    private (Worksheet Sheet, SpreadsheetSelection Selection, int Version) _key;
    private (int Count, int Numbers, double Sum) _summary;

    /// <summary>The sheet the selection belongs to.</summary>
    [Parameter] public Worksheet Sheet { get; set; }

    /// <summary>The selected cells.</summary>
    [Parameter] public SpreadsheetSelection Selection { get; set; }

    /// <summary>Changes whenever the workbook changed, so the summary is computed again.</summary>
    [Parameter] public int Version { get; set; }

    /// <summary>Zoom factor, 1 is 100%.</summary>
    [Parameter] public double Zoom { get; set; } = 1;

    /// <summary>Raised when the user changes the zoom.</summary>
    [Parameter] public EventCallback<double> ZoomChanged { get; set; }

    private int Percent => (int)Math.Round(Zoom * 100);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var key = (Sheet, Selection, Version);
        if (key == _key)
            return;
        _key = key;
        _summary = Summarize(Sheet, Selection);
    }

    internal void ShowSelection(SpreadsheetSelection selection)
    {
        Selection = selection;
        OnParametersSet();
        StateHasChanged();
    }

    internal static (int Count, int Numbers, double Sum) Summarize(Worksheet sheet, SpreadsheetSelection selection)
    {
        if (sheet == null || selection == null)
            return default;
        var used = sheet.UsedRange;
        int count = 0, numbers = 0;
        double sum = 0;
        foreach (var range in selection.Ranges.Select(r => r.Normalized))
        {
            if (!range.Intersects(used))
                continue;
            var clipped = CellRange.FromBounds(Math.Max(range.Top, used.Top), Math.Max(range.Left, used.Left), Math.Min(range.Bottom, used.Bottom), Math.Min(range.Right, used.Right));
            foreach (var at in clipped.Cells())
            {
                var value = sheet[at]?.Value ?? default;
                if (value.Kind == CellValueKind.Empty)
                    continue;
                count++;
                if (value.Kind != CellValueKind.Number)
                    continue;
                numbers++;
                sum += value.NumberValue;
            }
        }

        return (count, numbers, sum);
    }

    private string Format(double value)
    {
        var format = Sheet == null || Selection == null ? NumberFormat.General : Sheet.Workbook.Styles[Sheet.EffectiveStyleId(Selection.Active)].NumberFormat;
        if (format == "@")
            format = NumberFormat.General;
        return NumberFormat.Format(CellValue.Number(value), format, CultureInfo.CurrentCulture, out _);
    }

    private Task OnSliderChangedAsync(ChangeEventArgs e)
        => double.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) ? SetZoomAsync(percent / 100) : Task.CompletedTask;

    private Task SetZoomAsync(double zoom) => ZoomChanged.InvokeAsync(Math.Clamp(Math.Round(zoom, 2), 0.1, 4));
}
