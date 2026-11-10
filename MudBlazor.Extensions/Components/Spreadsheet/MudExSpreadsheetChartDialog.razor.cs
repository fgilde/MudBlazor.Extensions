using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;
using SheetChartSeries = MudBlazor.Extensions.Office.Spreadsheet.ChartSeries;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The chart dialog of <see cref="MudExSpreadsheetEditor"/>: chart type, title, legend, stacking and the series with their name, category and
/// value references. Closes with an <see cref="UpdateChartCommand"/> as result data.
/// </summary>
public partial class MudExSpreadsheetChartDialog
{
    private static readonly ChartKind[] Kinds = [ChartKind.Column, ChartKind.Bar, ChartKind.Line, ChartKind.Pie, ChartKind.Area, ChartKind.Scatter];
    private ChartKind _kind;
    private string _title;
    private bool _legend;
    private bool _stacked;
    private List<SeriesRow> _rows = [];

    private sealed class SeriesRow
    {
        public SheetChartSeries Source { get; init; }

        public string Name { get; set; }

        public string Categories { get; set; }

        public string Values { get; set; }
    }

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The sheet holding the chart.</summary>
    [Parameter] public Worksheet Sheet { get; set; }

    /// <summary>The chart to edit.</summary>
    [Parameter] public SheetChart Chart { get; set; }

    private bool Valid => _rows.Count > 0 && _rows.All(r => !string.IsNullOrWhiteSpace(r.Values));

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _kind = Chart.Kind;
        _title = Chart.Title;
        _legend = Chart.ShowLegend;
        _stacked = Chart.Stacked;
        _rows = Chart.Series.Select(s => new SeriesRow
        {
            Source = s,
            Name = s.NameRef != null ? "=" + s.NameRef : s.Name,
            Categories = s.CategoriesRef,
            Values = s.ValuesRef
        }).ToList();
        if (_rows.Count == 0) _rows.Add(new SeriesRow());
    }

    private void Ok()
    {
        if (!Valid) return;
        var series = _rows.Select(r =>
        {
            var name = r.Name?.Trim() ?? "";
            var nameRef = name.StartsWith('=') ? name[1..] : null;
            var keepName = r.Source != null && nameRef == r.Source.NameRef;
            return new SheetChartSeries(nameRef, keepName ? r.Source.Name : nameRef == null ? name : null, Ref(r.Categories), Ref(r.Values), r.Source?.Color);
        }).ToList();
        MudDialog.Close(DialogResult.Ok<IWorkbookCommand>(new UpdateChartCommand(Sheet, Chart.Id, _kind, _title ?? "", _legend, series, _stacked)));
    }

    private static string Ref(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim().TrimStart('=');

    private void Cancel() => MudDialog.Cancel();
}
