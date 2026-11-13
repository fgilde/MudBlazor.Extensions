using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office;
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
    private string _problem;

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
        try
        {
            var series = _rows.Select(r =>
            {
                var name = r.Name?.Trim() ?? "";
                var nameRef = name.StartsWith('=') ? Ref(name, r.Source?.NameRef) : null;
                var keepName = r.Source != null && nameRef == r.Source.NameRef;
                return new SheetChartSeries(nameRef, keepName ? r.Source.Name : nameRef == null ? name : null, Ref(r.Categories, r.Source?.CategoriesRef),
                    Ref(r.Values, r.Source?.ValuesRef), r.Source?.Color);
            }).ToList();
            MudDialog.Close(DialogResult.Ok<IWorkbookCommand>(new UpdateChartCommand(Sheet, Chart.Id, _kind, _title ?? "", _legend, series, _stacked)));
        }
        catch (ArgumentException e)
        {
            _problem = e.Message;
        }
    }

    private string Ref(string text, string original)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim().TrimStart('=');
        if (text == original) return text;
        var bang = text.LastIndexOf('!');
        var sheet = bang < 0 ? Workbook.QuoteSheetName(Sheet.Name) : text[..bang];
        var name = sheet.Length > 1 && sheet[0] == '\'' && sheet[^1] == '\'' ? sheet[1..^1].Replace("''", "'") : sheet;
        if (!CellRange.TryParse(text[(bang + 1)..], out _) || Sheet.Workbook.FindSheet(name) == null)
            throw new ArgumentException(TryLocalize("'{0}' is not a reference to cells of a sheet, for example {1}!B2:B5.", text, Workbook.QuoteSheetName(Sheet.Name)));
        return sheet + "!" + text[(bang + 1)..];
    }

    private void Cancel() => MudDialog.Cancel();
}
