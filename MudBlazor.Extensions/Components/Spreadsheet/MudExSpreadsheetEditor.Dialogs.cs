using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

public partial class MudExSpreadsheetEditor
{
    private SpreadsheetContextMenu _contextMenu;

    private static DialogOptions DialogDefaults => new() { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };

    private Task OnDialogRequestedAsync(SpreadsheetDialog dialog) => dialog switch
    {
        SpreadsheetDialog.Find => FindReplaceAsync(false),
        SpreadsheetDialog.Replace => FindReplaceAsync(true),
        SpreadsheetDialog.GoTo => GoToAsync(),
        SpreadsheetDialog.InsertFunction => InsertFunctionDialogAsync(),
        SpreadsheetDialog.Hyperlink => InsertLinkAsync(),
        _ => NameManagerAsync()
    };

    private Task OnContextMenuRequestedAsync(SpreadsheetContextMenuEventArgs args) =>
        IsReadOnly || _contextMenu == null ? Task.CompletedTask : _contextMenu.OpenAsync(args);

    private async Task FindReplaceAsync(bool replace)
    {
        if (ActiveSheet == null || !await CommitPendingEditAsync()) return;
        await DialogService.ShowAsync<MudExSpreadsheetFindReplaceDialog>(TryLocalize("Find and Replace"), new DialogParameters
        {
            { nameof(MudExSpreadsheetFindReplaceDialog.Workbook), _workbook },
            { nameof(MudExSpreadsheetFindReplaceDialog.Sheet), ActiveSheet },
            { nameof(MudExSpreadsheetFindReplaceDialog.Start), CurrentSelection.Active },
            { nameof(MudExSpreadsheetFindReplaceDialog.Replace), replace && CanEdit },
            { nameof(MudExSpreadsheetFindReplaceDialog.CanReplace), CanEdit },
            { nameof(MudExSpreadsheetFindReplaceDialog.Select), (Func<Worksheet, CellRef, Task>)SelectCellAsync },
            { nameof(MudExSpreadsheetFindReplaceDialog.Localizer), Localizer }
        }, DialogDefaults);
    }

    private async Task SelectCellAsync(Worksheet sheet, CellRef cell)
    {
        if (_grid != null)
            await _grid.GoToAsync(Office.Spreadsheet.Workbook.QuoteSheetName(sheet.Name) + "!" + cell.ToA1());
    }

    private async Task GoToAsync()
    {
        if (ActiveSheet == null || !await CommitPendingEditAsync()) return;
        var reference = await DialogService.ShowAsync<MudExSpreadsheetGoToDialog>(TryLocalize("Go To"), new DialogParameters
        {
            { nameof(MudExSpreadsheetGoToDialog.Names), _workbook.DefinedNames },
            { nameof(MudExSpreadsheetGoToDialog.Localizer), Localizer }
        }, DialogDefaults);
        if (await reference.Result is { Canceled: false, Data: string text } && _grid != null && !await _grid.GoToAsync(text))
            await ShowNoticeAsync(TryLocalize("Reference is not valid."));
        await FocusGridAsync();
    }

    private async Task InsertFunctionDialogAsync()
    {
        if (!CanEdit || !await CommitPendingEditAsync()) return;
        var reference = await DialogService.ShowAsync<MudExSpreadsheetInsertFunctionDialog>(TryLocalize("Insert Function"), new DialogParameters
        {
            { nameof(MudExSpreadsheetInsertFunctionDialog.Functions), (_engine?.Functions ?? FunctionRegistry.Default).Functions.Select(f => f.Info).ToList() },
            { nameof(MudExSpreadsheetInsertFunctionDialog.Localizer), Localizer }
        }, DialogDefaults);
        if (await reference.Result is { Canceled: false, Data: FunctionInfo function })
            await InsertFunctionAsync(function.Name);
        else
            await FocusGridAsync();
    }

    private async Task NameManagerAsync()
    {
        if (!CanEdit || !await CommitPendingEditAsync()) return;
        var r = ActiveRange;
        var target = new CellRange(r.Start with { RowAbsolute = true, ColumnAbsolute = true }, r.End with { RowAbsolute = true, ColumnAbsolute = true });
        var reference = await DialogService.ShowAsync<MudExSpreadsheetNameManagerDialog>(TryLocalize("Name Manager"), new DialogParameters
        {
            { nameof(MudExSpreadsheetNameManagerDialog.Workbook), _workbook },
            { nameof(MudExSpreadsheetNameManagerDialog.DefaultRefersTo), Office.Spreadsheet.Workbook.QuoteSheetName(ActiveSheet.Name) + "!" + target.ToA1() },
            { nameof(MudExSpreadsheetNameManagerDialog.Localizer), Localizer }
        }, DialogDefaults);
        await reference.Result;
        await FocusGridAsync();
    }

    internal Task FilterBySelectedValueAsync() => RunAsync(() =>
    {
        var sheet = ActiveSheet;
        var active = CurrentSelection.Active;
        var text = _grid?.DisplayText(active) ?? "";
        using (_workbook.Undo.BeginTransaction("Filter"))
        {
            if (sheet.AutoFilter?.Range.Contains(active) != true)
                _workbook.Execute(new SetAutoFilterCommand(sheet, _grid?.CurrentRegion(active) ?? ActiveRange));
            var shown = text.Length == 0 ? new HashSet<string>() : new HashSet<string>(StringComparer.OrdinalIgnoreCase) { text };
            _workbook.Execute(new ApplyFilterCommand(sheet, active.Column, FilterCriteria.Values(shown, text.Length == 0)));
        }
    });
}
