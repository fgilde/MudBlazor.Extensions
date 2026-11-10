namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Sets the auto filter range of a sheet without criteria, or removes the auto filter; either way the rows it filtered show again.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="range">The range including its header row, or null to remove the auto filter.</param>
public sealed class SetAutoFilterCommand(Worksheet sheet, CellRange? range) : IWorkbookCommand
{
    private FilterState _before;

    /// <inheritdoc />
    public string Description => range == null ? "Remove filter" : "Filter";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        if (range is { } r) Worksheet.Check(r);
        if (range is { } area && sheet.Tables.Any(t => t.Range.Intersects(area))) throw new InvalidOperationException("An auto filter cannot overlap a table.");
        _before = FilterState.Capture(sheet);
        FilterState.Evaluate(sheet, range is { } g ? new AutoFilter(g) : null, sheet.Tables).Restore(wb, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _before.Restore(wb, sheet);
}

/// <summary>Sets or (with null criteria) clears the criteria of one auto filter column and re-applies all criteria, hiding and showing rows.</summary>
/// <param name="sheet">The sheet; it must have an auto filter.</param>
/// <param name="column">Zero-based sheet column inside the auto filter range.</param>
/// <param name="criteria">The criteria, or null to show all values of the column.</param>
public sealed class ApplyFilterCommand(Worksheet sheet, int column, FilterCriteria criteria) : IWorkbookCommand
{
    private FilterState _before;

    /// <inheritdoc />
    public string Description => "Filter";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        if (sheet.AutoFilter is not { } filter) throw new InvalidOperationException("The sheet has no auto filter.");
        if (column < filter.Range.Left || column > filter.Range.Right) throw new ArgumentOutOfRangeException(nameof(column));
        _before = FilterState.Capture(sheet);
        FilterState.Evaluate(sheet, filter.WithColumn(column, criteria), sheet.Tables).Restore(wb, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _before.Restore(wb, sheet);
}

/// <summary>Sets or (with null criteria) clears the criteria of one column of a table's filter and re-applies all filters of the sheet, hiding and showing rows.</summary>
/// <param name="sheet">The sheet holding the table.</param>
/// <param name="table">The table name; the table must show filter buttons (<see cref="SheetTable.Filter"/> is not null).</param>
/// <param name="column">Zero-based sheet column inside the table.</param>
/// <param name="criteria">The criteria, or null to show all values of the column.</param>
public sealed class ApplyTableFilterCommand(Worksheet sheet, string table, int column, FilterCriteria criteria) : IWorkbookCommand
{
    private FilterState _before;

    /// <inheritdoc />
    public string Description => "Filter";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        var target = sheet.Tables.FirstOrDefault(t => t.Matches(table)) ?? throw new InvalidOperationException($"The sheet has no table '{table}'.");
        if (target.Filter is not { } filter) throw new InvalidOperationException($"The table '{table}' has no filter buttons.");
        if (column < filter.Range.Left || column > filter.Range.Right) throw new ArgumentOutOfRangeException(nameof(column));
        _before = FilterState.Capture(sheet);
        var tables = sheet.Tables.Select(t => t == target ? t.WithFilter(filter.WithColumn(column, criteria)) : t).ToList();
        FilterState.Evaluate(sheet, sheet.AutoFilter, tables).Restore(wb, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _before.Restore(wb, sheet);
}

internal sealed record FilterState(AutoFilter Filter, IReadOnlySet<int> Rows, IReadOnlyList<SheetTable> Tables)
{
    public static FilterState Capture(Worksheet sheet) => new(sheet.AutoFilter, sheet.FilteredRows, sheet.Tables);

    public static FilterState Evaluate(Worksheet sheet, AutoFilter filter, IReadOnlyList<SheetTable> tables)
    {
        return new FilterState(filter, sheet.EvaluateFilters(filter, tables), tables);
    }

    public void Restore(Workbook wb, Worksheet sheet)
    {
        sheet.AutoFilter = Filter;
        sheet.Tables = Tables;
        sheet.SetFiltered(Rows);
        wb.Notify(StructureChangeKind.Layout, sheet);
    }
}
