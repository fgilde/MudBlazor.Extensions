namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Sets the auto filter range of a sheet without criteria, or removes the auto filter; either way all filtered rows show again.</summary>
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
        new FilterState(range is { } g ? new AutoFilter(g) : null, new HashSet<int>()).Restore(wb, sheet);
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
        var next = filter.WithColumn(column, criteria);
        new FilterState(next, next.Evaluate(sheet)).Restore(wb, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _before.Restore(wb, sheet);
}

internal sealed record FilterState(AutoFilter Filter, IReadOnlySet<int> Rows)
{
    public static FilterState Capture(Worksheet sheet) => new(sheet.AutoFilter, sheet.FilteredRows);

    public void Restore(Workbook wb, Worksheet sheet)
    {
        sheet.AutoFilter = Filter;
        sheet.SetFiltered(Rows);
        wb.Notify(StructureChangeKind.Layout, sheet);
    }
}
