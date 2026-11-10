using System.Globalization;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>
/// Sorts the rows of a range in a stable way, in Excel order: numbers, text, booleans, errors, and blank cells last (also when descending).
/// Values, formulas (moved relatively, as when copying), styles and row heights move with their rows.
/// </summary>
/// <param name="sheet">The sheet.</param>
/// <param name="range">The range to sort.</param>
/// <param name="keys">Sort levels, most significant first; their columns must lie inside the range.</param>
/// <param name="hasHeader">True if the first row of the range is a header that stays in place.</param>
public sealed class SortCommand(Worksheet sheet, CellRange range, IReadOnlyList<SortKey> keys, bool hasHeader) : IWorkbookCommand
{
    private List<(CellRef Ref, CellState State)> _before;
    private List<(int Row, double? Height)> _heights;
    private CellRange _data;

    /// <inheritdoc />
    public string Description => "Sort";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        Worksheet.Check(range);
        if (keys == null || keys.Count == 0) throw new ArgumentException("At least one sort key is required.", nameof(keys));
        if (keys.Any(k => k.Column < range.Left || k.Column > range.Right))
            throw new ArgumentException("Every sort key must lie inside the range.", nameof(keys));
        _before = null;
        var (top, bottom) = (range.Top + (hasHeader ? 1 : 0), Math.Min(range.Bottom, sheet.UsedRange.Bottom));
        if (top > bottom) return;
        _data = CellRange.FromBounds(top, range.Left, bottom, range.Right);
        var rows = Enumerable.Range(_data.Top, _data.RowCount).ToList();
        _before = sheet.ExistingIn(_data).Select(c => (c, sheet.Capture(c))).ToList();
        _heights = rows.Select(r => (r, sheet.SizeOrNull(Axis.Row, r))).ToList();
        var byRow = _before.ToLookup(p => p.Ref.Row);
        var order = rows.Order(Comparer<int>.Create(Compare)).ToList();

        foreach (var (c, _) in _before) sheet.Restore(c, default);
        for (var i = 0; i < order.Count; i++)
        {
            var (from, to) = (order[i], _data.Top + i);
            foreach (var (c, state) in byRow[from])
                sheet.Restore(new CellRef(to, c.Column), new CellState(state.Value,
                    state.Formula == null ? null : ReferenceShifter.Translate(state.Formula, to - from, 0), state.StyleId));
            sheet.SetSize(Axis.Row, to, _heights[from - _data.Top].Height);
        }

        Notify(wb);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        if (_before == null) return;
        foreach (var c in sheet.ExistingIn(_data)) sheet.Restore(c, default);
        foreach (var (c, state) in _before) sheet.Restore(c, state);
        foreach (var (row, height) in _heights) sheet.SetSize(Axis.Row, row, height);
        Notify(wb);
    }

    private void Notify(Workbook wb)
    {
        wb.NotifyValues(sheet, _data);
        wb.NotifyFormatted(sheet, _data);
        wb.Notify(StructureChangeKind.Layout, sheet);
    }

    private int Compare(int a, int b)
    {
        foreach (var key in keys)
        {
            var x = sheet[new CellRef(a, key.Column)]?.Value ?? default;
            var y = sheet[new CellRef(b, key.Column)]?.Value ?? default;
            var (rx, ry) = (Rank(x), Rank(y));
            if (rx == 4 || ry == 4)
            {
                if (rx != ry) return rx.CompareTo(ry);
                continue;
            }

            var order = rx != ry ? rx.CompareTo(ry) : x.Kind switch
            {
                CellValueKind.Number => x.NumberValue.CompareTo(y.NumberValue),
                CellValueKind.Text => string.Compare(x.TextValue, y.TextValue, CultureInfo.CurrentCulture,
                    key.CaseSensitive ? CompareOptions.None : CompareOptions.IgnoreCase),
                CellValueKind.Boolean => x.BooleanValue.CompareTo(y.BooleanValue),
                _ => 0
            };
            if (order != 0) return key.Descending ? -order : order;
        }

        return 0;
    }

    private static int Rank(CellValue v) => v.Kind switch
    {
        CellValueKind.Number => 0,
        CellValueKind.Text => 1,
        CellValueKind.Boolean => 2,
        CellValueKind.Error => 3,
        _ => 4
    };
}
