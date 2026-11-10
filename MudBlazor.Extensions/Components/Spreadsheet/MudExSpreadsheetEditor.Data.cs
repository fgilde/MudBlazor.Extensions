using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;
using SortKey = MudBlazor.Extensions.Office.Spreadsheet.SortKey;

namespace MudBlazor.Extensions.Components;

public partial class MudExSpreadsheetEditor
{
    private static readonly (string Name, string Color)[] BarColors =
        [("Blue", "#638EC6"), ("Green", "#63C384"), ("Red", "#FF555A"), ("Orange", "#FFB628"), ("Light Blue", "#008AEF"), ("Purple", "#D6007B")];

    private static readonly (string Name, string[] Colors)[] ScaleColors =
    [
        ("Green - Yellow - Red", ["#63BE7B", "#FFEB84", "#F8696B"]),
        ("Red - Yellow - Green", ["#F8696B", "#FFEB84", "#63BE7B"]),
        ("Green - White - Red", ["#63BE7B", "#FCFCFF", "#F8696B"]),
        ("Blue - White - Red", ["#5A8AC6", "#FCFCFF", "#F8696B"]),
        ("White - Red", ["#FCFCFF", "#F8696B"]),
        ("Green - White", ["#63BE7B", "#FCFCFF"])
    ];

    internal static readonly (string Name, string Set)[] IconSets =
    [
        ("3 Arrows", "3Arrows"), ("3 Arrows (Gray)", "3ArrowsGray"), ("4 Arrows", "4Arrows"), ("5 Arrows", "5Arrows"), ("3 Triangles", "3Triangles"),
        ("3 Traffic Lights", "3TrafficLights1"), ("4 Traffic Lights", "4TrafficLights"), ("3 Symbols", "3Symbols"), ("3 Flags", "3Flags"),
        ("3 Stars", "3Stars"), ("4 Ratings", "4Rating"), ("5 Quarters", "5Quarters")
    ];

    private static readonly (string Id, string Name, string Kind)[] HighlightRules =
    [
        ("greater", "Greater Than...", "greaterThan"), ("less", "Less Than...", "lessThan"), ("between", "Between...", "between"),
        ("equal", "Equal To...", "equal"), ("text", "Text that Contains...", "containsText"), ("date", "A Date Occurring...", "timePeriod"),
        ("duplicate", "Duplicate Values...", "duplicate")
    ];

    private static readonly (string Id, string Name, string Kind)[] TopBottomRules =
    [
        ("top10", "Top 10 Items...", "top10"), ("top10p", "Top 10%...", "top10Percent"), ("bottom10", "Bottom 10 Items...", "bottom10"),
        ("bottom10p", "Bottom 10%...", "bottom10Percent"), ("above", "Above Average...", "aboveAverage"), ("below", "Below Average...", "belowAverage")
    ];

    private bool HasFilterCriteria => ActiveSheet?.AutoFilter?.Columns.Count > 0;

    private bool FilterOn => ActiveSheet is { } sheet && (sheet.AutoFilter != null || sheet.TableAt(CurrentSelection.Active)?.HasAutoFilter == true);

    private (CellRange Range, bool Header) SortTarget()
    {
        var sheet = ActiveSheet;
        var active = CurrentSelection.Active;
        if (!ActiveRange.IsSingleCell) return (ActiveRange, false);
        if (sheet.TableAt(active) is { } table) return (MudExSpreadsheetGrid.SortRange(table, default), table.HeaderRow);
        if (sheet.AutoFilter is { } filter && filter.Range.Contains(active)) return (filter.Range, true);
        var region = _grid?.CurrentRegion(active) ?? ActiveRange;
        return (region, HasHeader(region));
    }

    private bool HasHeader(CellRange region)
    {
        if (region.RowCount < 2) return false;
        var sheet = ActiveSheet;
        var columns = Enumerable.Range(region.Left, region.ColumnCount).ToList();
        CellValueKind Kind(int row, int col) => sheet[new CellRef(row, col)]?.Value.Kind ?? CellValueKind.Empty;
        bool Bold(int row) => columns.Any(c => _workbook.Styles[sheet.EffectiveStyleId(new CellRef(row, c))].Font.Bold);
        if (columns.Any(c => Kind(region.Top, c) is not (CellValueKind.Text or CellValueKind.Empty))) return false;
        return columns.Any(c => Kind(region.Top + 1, c) is not (CellValueKind.Text or CellValueKind.Empty)) || (Bold(region.Top) && !Bold(region.Top + 1));
    }

    internal Task SortAsync(bool descending) => RunAsync(() =>
    {
        var (range, header) = SortTarget();
        _workbook.Execute(new SortCommand(ActiveSheet, range, [new SortKey(CurrentSelection.Active.Column, descending)], header));
    });

    private async Task CustomSortAsync()
    {
        if (!CanEdit) return;
        var (range, header) = SortTarget();
        await ShowCommandDialogAsync<MudExSpreadsheetSortDialog>("Sort", new DialogParameters
        {
            { nameof(MudExSpreadsheetSortDialog.Sheet), ActiveSheet },
            { nameof(MudExSpreadsheetSortDialog.Range), range },
            { nameof(MudExSpreadsheetSortDialog.HasHeader), header },
            { nameof(MudExSpreadsheetSortDialog.Column), CurrentSelection.Active.Column }
        });
    }

    private Task ToggleFilterAsync() => RunAsync(() =>
    {
        var sheet = ActiveSheet;
        var active = CurrentSelection.Active;
        if (sheet.TableAt(active) is { } table)
            _workbook.Execute(new SetTableOptionsCommand(sheet, table.Name, hasAutoFilter: !table.HasAutoFilter));
        else if (sheet.AutoFilter != null)
            _workbook.Execute(new SetAutoFilterCommand(sheet, null));
        else
            _workbook.Execute(new SetAutoFilterCommand(sheet, ActiveRange.IsSingleCell ? _grid?.CurrentRegion(active) ?? ActiveRange : ActiveRange));
    });

    private Task ClearFilterAsync() => RunAsync(() =>
    {
        var sheet = ActiveSheet;
        using (_workbook.Undo.BeginTransaction("Clear filter"))
            foreach (var column in sheet.AutoFilter?.Columns.Keys.ToList() ?? [])
                _workbook.Execute(new ApplyFilterCommand(sheet, column, null));
    });

    private Task ReapplyFilterAsync() => RunAsync(() =>
    {
        if (ActiveSheet.AutoFilter?.Columns.FirstOrDefault() is { Value: not null } first)
            _workbook.Execute(new ApplyFilterCommand(ActiveSheet, first.Key, first.Value));
    });

    private async Task DataValidationAsync()
    {
        if (!CanEdit) return;
        var sheet = ActiveSheet;
        var active = CurrentSelection.Active;
        var index = sheet.DataValidations.ToList().FindIndex(v => v.Ranges.Any(r => r.Contains(active)));
        await ShowCommandDialogAsync<MudExSpreadsheetValidationDialog>("Data Validation", new DialogParameters
        {
            { nameof(MudExSpreadsheetValidationDialog.Sheet), sheet },
            { nameof(MudExSpreadsheetValidationDialog.Ranges), CurrentSelection.Ranges.Select(r => r.Normalized).ToList() },
            { nameof(MudExSpreadsheetValidationDialog.Index), index }
        });
    }

    private Task ConditionalFormatAsync(string kind) => !CanEdit
        ? Task.CompletedTask
        : ShowCommandDialogAsync<MudExSpreadsheetConditionalFormatDialog>(kind == "manage" ? "Conditional Formatting Rules Manager" : "New Formatting Rule", new DialogParameters
        {
            { nameof(MudExSpreadsheetConditionalFormatDialog.Sheet), ActiveSheet },
            { nameof(MudExSpreadsheetConditionalFormatDialog.Ranges), CurrentSelection.Ranges.Select(r => r.Normalized).ToList() },
            { nameof(MudExSpreadsheetConditionalFormatDialog.Kind), kind }
        });

    private Task AddRuleAsync(ConditionalRule rule) => RunAsync(() =>
        _workbook.Execute(new AddConditionalFormattingCommand(ActiveSheet, new ConditionalFormatting(CurrentSelection.Ranges.Select(r => r.Normalized).ToList(), [rule]))));

    private Task AddDataBarAsync(string color) =>
        AddRuleAsync(new ConditionalRule.DataBar(new ConditionalValue(ConditionalValueType.Min), new ConditionalValue(ConditionalValueType.Max), color));

    private Task AddColorScaleAsync(string[] colors)
    {
        ConditionalValue[] values = colors.Length == 2
            ? [new(ConditionalValueType.Min), new(ConditionalValueType.Max)]
            : [new(ConditionalValueType.Min), new(ConditionalValueType.Percentile, "50"), new(ConditionalValueType.Max)];
        return AddRuleAsync(new ConditionalRule.ColorScale(values.Select((v, i) => new ColorScaleStop(v, colors[i])).ToList()));
    }

    private Task AddIconSetAsync(string set)
    {
        var count = set[0] - '0';
        var thresholds = Enumerable.Range(0, count)
            .Select(i => new ConditionalValue(ConditionalValueType.Percent, Math.Round(100.0 * i / count).ToString(CultureInfo.InvariantCulture)))
            .ToList();
        return AddRuleAsync(new ConditionalRule.IconSet(set, thresholds));
    }

    private Task ClearRulesAsync(bool entireSheet) => RunAsync(() =>
    {
        var sheet = ActiveSheet;
        using (_workbook.Undo.BeginTransaction("Clear rules"))
        {
            if (entireSheet)
                for (var i = sheet.ConditionalFormats.Count - 1; i >= 0; i--)
                    _workbook.Execute(new DeleteConditionalFormattingCommand(sheet, i));
            else
                foreach (var range in CurrentSelection.Ranges)
                    _workbook.Execute(new ClearRulesCommand(sheet, range.Normalized, dataValidations: false));
        }
    });

    private async Task FormatAsTableAsync(string style)
    {
        if (!CanEdit || !await CommitPendingEditAsync()) return;
        var sheet = ActiveSheet;
        if (sheet.TableAt(CurrentSelection.Active) is { } table)
        {
            await RunCommandAsync(new SetTableOptionsCommand(sheet, table.Name, style));
            return;
        }

        var range = ActiveRange.IsSingleCell ? _grid?.CurrentRegion(CurrentSelection.Active) ?? ActiveRange : ActiveRange;
        await ShowCommandDialogAsync<MudExSpreadsheetTableDialog>("Create Table", new DialogParameters
        {
            { nameof(MudExSpreadsheetTableDialog.Sheet), sheet },
            { nameof(MudExSpreadsheetTableDialog.Range), range },
            { nameof(MudExSpreadsheetTableDialog.HasHeaders), range.RowCount < 2 || HasHeader(range) },
            { nameof(MudExSpreadsheetTableDialog.StyleName), style }
        });
    }

    private Task ApplyCellStyleAsync(SpreadsheetCellStyle style) => ApplyStyleAsync(style.ApplyTo, "Cell style");

    private async Task ShowCommandDialogAsync<TDialog>(string title, DialogParameters parameters) where TDialog : IComponent
    {
        if (!await CommitPendingEditAsync()) return;
        parameters.Add(nameof(MudExSpreadsheetFormatCellsDialog.Localizer), Localizer);
        var reference = await DialogService.ShowAsync<TDialog>(TryLocalize(title), parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true });
        var result = await reference.Result;
        if (result is { Canceled: false, Data: IWorkbookCommand command })
            await RunCommandAsync(command);
        else
            await FocusGridAsync();
    }
}
