using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

public partial class MudExSpreadsheetEditor
{
    private static readonly (string Id, string Name, string Icon, ChartKind Kind)[] ChartButtons =
    [
        ("xl-chart-column", "Column", Icons.Material.Filled.BarChart, ChartKind.Column),
        ("xl-chart-bar", "Bar", Icons.Material.Filled.Notes, ChartKind.Bar),
        ("xl-chart-line", "Line", Icons.Material.Filled.ShowChart, ChartKind.Line),
        ("xl-chart-pie", "Pie", Icons.Material.Filled.PieChart, ChartKind.Pie),
        ("xl-chart-area", "Area", Icons.Material.Filled.AreaChart, ChartKind.Area),
        ("xl-chart-scatter", "Scatter", Icons.Material.Filled.ScatterPlot, ChartKind.Scatter)
    ];

    private MudExRibbon _ribbon;
    private (Worksheet Sheet, CellRange Range)? _print;
    private ElementReference _printElement;
    private bool _printPending;

    /// <summary>The author written into new notes.</summary>
    [Parameter] public string Author { get; set; }

    internal bool NotesReadOnly => ActiveSheet?.NotesReadOnly != false;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!_printPending) return;
        _printPending = false;
        _ = PrintRenderedAsync();
    }

    private async Task PrintRenderedAsync()
    {
        try
        {
            if (_grid != null) await _grid.PrintAsync(_printElement);
        }
        finally
        {
            _print = null;
            await InvokeAsync(StateHasChanged);
        }
    }

    internal async Task PrintAsync()
    {
        if (ActiveSheet is not { } sheet || !await CommitPendingEditAsync()) return;
        var used = sheet.UsedRange;
        var r = ActiveRange;
        var range = r.IsSingleCell ? used
            : r.IsWholeColumns || r.IsWholeRows ? CellRange.FromBounds(r.Top, r.Left, Math.Max(r.Top, Math.Min(r.Bottom, used.Bottom)), Math.Max(r.Left, Math.Min(r.Right, used.Right)))
            : r;
        _print = (sheet, range);
        _printPending = true;
        StateHasChanged();
    }

    private async Task PrintFromBackstageAsync()
    {
        if (_ribbon != null) await _ribbon.SetFileMenuOpenAsync(false);
        await PrintAsync();
    }

    private Task InsertChartAsync(ChartKind kind) => RunAsync(() =>
    {
        var command = new InsertChartCommand(ActiveSheet, ActionRange, kind);
        _workbook.Execute(command);
        _grid?.SelectChart(command.Chart.Id);
    });

    private Task EditChartAsync(SheetChart chart) => ShowCommandDialogAsync<MudExSpreadsheetChartDialog>("Chart", new DialogParameters
    {
        { nameof(MudExSpreadsheetChartDialog.Sheet), ActiveSheet },
        { nameof(MudExSpreadsheetChartDialog.Chart), chart }
    });

    internal async Task InsertLinkAsync()
    {
        if (!CanEdit || _grid == null || !await CommitPendingEditAsync()) return;
        var sheet = ActiveSheet;
        var cell = _grid.ActiveCell;
        var existing = sheet.LinkAt(cell);
        var text = _grid.DisplayText(cell);
        var reference = await DialogService.ShowAsync<MudExSpreadsheetLinkDialog>(TryLocalize("Insert Hyperlink"), new DialogParameters
        {
            { nameof(MudExSpreadsheetLinkDialog.Workbook), _workbook },
            { nameof(MudExSpreadsheetLinkDialog.Sheet), sheet },
            { nameof(MudExSpreadsheetLinkDialog.Cell), cell },
            { nameof(MudExSpreadsheetLinkDialog.Link), existing },
            { nameof(MudExSpreadsheetLinkDialog.DisplayText), text },
            { nameof(MudExSpreadsheetLinkDialog.Localizer), Localizer }
        }, DialogDefaults);
        if (await reference.Result is not { Canceled: false, Data: CellLink link })
        {
            await FocusGridAsync();
            return;
        }

        await RunAsync(() =>
        {
            using (_workbook.Undo.BeginTransaction("Hyperlink"))
            {
                _workbook.Execute(new SetLinkCommand(sheet, existing is null ? link : existing with { Target = link.Target, Location = link.Location, Display = link.Display, Tooltip = link.Tooltip }));
                if (!string.IsNullOrEmpty(link.Display) && link.Display != text)
                    _workbook.Execute(new SetValueCommand(sheet, cell, CellValue.Text(link.Display)));
            }
        });
    }

    internal Task RemoveLinkAsync() => _grid?.ActiveCell is { } cell && ActiveSheet?.LinkAt(cell) != null
        ? RunCommandAsync(new RemoveLinkCommand(ActiveSheet, cell))
        : Task.CompletedTask;

    internal Task EditNoteAsync() => _grid?.EditNoteAsync() ?? Task.CompletedTask;

    internal async Task DeleteNoteAsync()
    {
        if (_grid == null) return;
        await _grid.DeleteNoteAsync();
        await FocusGridAsync();
    }

    internal bool HasLink => _grid != null && ActiveSheet?.LinkAt(_grid.ActiveCell) != null;

    internal bool HasNote => _grid?.NoteAt(_grid.ActiveCell) != null;
}
