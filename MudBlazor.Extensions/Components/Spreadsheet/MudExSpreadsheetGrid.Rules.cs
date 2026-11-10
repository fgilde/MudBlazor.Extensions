using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

public partial class MudExSpreadsheetGrid
{
    private CellRef? _filterOpen;
    private bool _listOpen;

    private enum EditCheck
    {
        Write,
        Retry,
        Cancel
    }

    private readonly record struct FilterHead(CellRef Cell, SheetTable Table, bool Filtered);

    private IEnumerable<FilterHead> FilterHeads(GridPane p)
    {
        if (p.R1 < p.R0 || p.C1 < p.C0) yield break;
        if (_sheet.AutoFilter is { } filter && filter.Range.Top >= p.R0 && filter.Range.Top <= p.R1)
            for (var c = Math.Max(p.C0, filter.Range.Left); c <= Math.Min(p.C1, filter.Range.Right); c++)
                if (W(c) > 0 && H(filter.Range.Top) > 0)
                    yield return new FilterHead(new CellRef(filter.Range.Top, c), null, filter.Columns.ContainsKey(c));
        foreach (var t in _sheet.Tables)
        {
            if (!t.HasAutoFilter || !t.HeaderRow || t.Range.Top < p.R0 || t.Range.Top > p.R1) continue;
            for (var c = Math.Max(p.C0, t.Range.Left); c <= Math.Min(p.C1, t.Range.Right); c++)
                if (W(c) > 0 && H(t.Range.Top) > 0)
                    yield return new FilterHead(new CellRef(t.Range.Top, c), t, false);
        }
    }

    private string FilterButtonStyle(CellRef cell)
    {
        var size = Math.Min(H(cell.Row) - 2, Math.Round(17 * _zoom));
        return $"left:{Px(X(cell.Column + 1) - size - 2)};top:{Px(Y(cell.Row + 1) - size - 2)};width:{Px(size)};height:{Px(size)}";
    }

    private void OnFilterOpenChanged(CellRef cell, bool open)
    {
        if (open) _filterOpen = cell;
        else if (_filterOpen == cell) _filterOpen = null;
    }

    private void CloseFilter()
    {
        _filterOpen = null;
        StateHasChanged();
    }

    internal static CellRange SortRange(SheetTable table, CellRange filterRange) =>
        table == null ? filterRange : CellRange.FromBounds(table.Range.Top, table.Range.Left, table.DataRange.Bottom, table.Range.Right);

    private async Task SortFromFilterAsync(FilterHead head, bool descending)
    {
        CloseFilter();
        var range = SortRange(head.Table, _sheet.AutoFilter?.Range ?? default);
        await TryWriteAsync(() => _workbook.Execute(new SortCommand(_sheet, range, [new SortKey(head.Cell.Column, descending)], true)));
    }

    private async Task ApplyFilterAsync(FilterHead head, FilterCriteria criteria)
    {
        CloseFilter();
        await TryWriteAsync(() => _workbook.Execute(new ApplyFilterCommand(_sheet, head.Cell.Column, criteria)));
    }

    private DataValidation ListValidation
    {
        get
        {
            if (!CanEdit || _sheet.DataValidations.Count == 0) return null;
            var at = _selection.Active;
            var dv = _sheet.DataValidations.FirstOrDefault(v => v.Ranges.Any(r => r.Contains(at)));
            return dv is { Type: ValidationType.List, ShowDropDown: true } ? dv : null;
        }
    }

    private string ListButtonStyle
    {
        get
        {
            var r = MergeAt(_selection.Active) ?? new CellRange(_selection.Active, _selection.Active);
            var size = Math.Round(17 * _zoom);
            return $"left:{Px(X(r.Right + 1) + 1)};top:{Px(Y(r.Bottom + 1) - size - 1)};width:{Px(size)};height:{Px(size)}";
        }
    }

    private IReadOnlyList<string> ListItems(DataValidation dv) => _listOpen ? DataValidator.ListItems(_workbook, _sheet, dv) : [];

    private async Task PickListItemAsync(string item)
    {
        _listOpen = false;
        if (_editing) EndEditState();
        var cell = MergeAt(_selection.Active)?.Start ?? _selection.Active;
        await TryWriteAsync(() => WriteEdit(item, cell, false));
        StateHasChanged();
        await FocusAsync();
    }

    private async Task<EditCheck> CheckEditAsync(string text, CellRef cell)
    {
        if (_sheet.DataValidations.Count == 0 || ProposedValue(cell, text) is not { } value) return EditCheck.Write;
        var result = DataValidator.Validate(_workbook, _sheet, cell, value);
        if (result.IsValid || result.Validation is not { } dv) return EditCheck.Write;
        if (DialogService == null) return EditCheck.Retry;
        var title = string.IsNullOrEmpty(dv.ErrorTitle) ? TryLocalize("Data Validation") : dv.ErrorTitle;
        var message = string.IsNullOrEmpty(dv.Error) ? TryLocalize("This value doesn't match the data validation restrictions defined for this cell.") : dv.Error;
        switch (dv.ErrorStyle)
        {
            case "warning":
                var answer = await DialogService.ShowMessageBoxAsync(title, message + " " + TryLocalize("Continue?"), TryLocalize("Yes"), TryLocalize("No"), TryLocalize("Cancel"));
                return answer switch { true => EditCheck.Write, false => EditCheck.Retry, _ => EditCheck.Cancel };
            case "information":
                return await DialogService.ShowMessageBoxAsync(title, message, TryLocalize("OK"), cancelText: TryLocalize("Cancel")) == true ? EditCheck.Write : EditCheck.Cancel;
            default:
                return await DialogService.ShowMessageBoxAsync(title, message, TryLocalize("Retry"), cancelText: TryLocalize("Cancel")) == true ? EditCheck.Retry : EditCheck.Cancel;
        }
    }

    private CellValue? ProposedValue(CellRef cell, string text)
    {
        if (text.Length > 1 && text[0] == '=') return null;
        return text.Length == 0 ? CellValue.Empty : ParseInput(cell, text).Value;
    }
}
