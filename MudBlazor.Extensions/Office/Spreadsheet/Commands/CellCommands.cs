namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Sets a constant cell value and removes any formula.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="cell">The cell to set.</param>
/// <param name="value">The new value.</param>
public sealed class SetValueCommand(Worksheet sheet, CellRef cell, CellValue value) : IWorkbookCommand
{
    private CellState _before;

    /// <inheritdoc />
    public string Description => "Set value";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        _before = sheet.Capture(cell);
        var target = sheet.Ensure(cell);
        target.Value = value;
        target.Formula = null;
        target.SpillAnchor = null;
        sheet.Prune(cell);
        wb.Notify(sheet, cell);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        sheet.Restore(cell, _before);
        wb.Notify(sheet, cell);
    }
}

/// <summary>
/// Sets a block of constant values in one undo step and one <see cref="Workbook.Changed"/> event (reported in
/// <see cref="WorkbookChangedEventArgs.ValueRanges"/>), the fast way to fill a sheet from code. Values are stored as given, without
/// parsing; formulas in the block are removed, styles are kept and <see cref="CellValue.Empty"/> clears a cell's content.
/// </summary>
/// <param name="sheet">The sheet.</param>
/// <param name="origin">The top-left cell of the block.</param>
/// <param name="values">The values by row and column.</param>
public sealed class SetValuesCommand(Worksheet sheet, CellRef origin, CellValue[,] values) : IWorkbookCommand
{
    private Dictionary<long, CellState> _before;
    private CellRange? _range;

    /// <inheritdoc />
    public string Description => "Set values";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        ArgumentNullException.ThrowIfNull(values);
        var (rows, columns) = (values.GetLength(0), values.GetLength(1));
        if (rows == 0 || columns == 0) return;
        var start = origin.Relative;
        if (!Worksheet.InBounds(start) || (long)start.Row + rows > CellRef.MaxRows || (long)start.Column + columns > CellRef.MaxColumns)
            throw new ArgumentOutOfRangeException(nameof(values), "The block lies outside the worksheet limits.");
        var range = CellRange.FromBounds(start.Row, start.Column, start.Row + rows - 1, start.Column + columns - 1);
        _range = range;
        _before = new Dictionary<long, CellState>();
        foreach (var c in sheet.ExistingIn(range)) _before[c.Key] = sheet.Capture(c);
        sheet.Reserve((int)Math.Min(int.MaxValue, (long)sheet.CellCount + (long)rows * columns - _before.Count));
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < columns; c++)
            {
                var at = new CellRef(start.Row + r, start.Column + c);
                var value = values[r, c];
                var existed = _before.TryGetValue(at.Key, out var old);
                if (value.Kind == CellValueKind.Empty)
                {
                    if (existed) sheet.Restore(at, new CellState(default, null, old.StyleId));
                    continue;
                }

                var cell = sheet.Ensure(at);
                cell.Value = value;
                if (!existed || old.Formula == null && old.SpillAnchor == null) continue;
                cell.Formula = null;
                cell.SpillAnchor = null;
                sheet.Sync(at);
            }

        wb.NotifyValues(sheet, range);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        if (_range is not { } range) return;
        foreach (var c in range.Cells()) sheet.Restore(c, _before.GetValueOrDefault(c.Key));
        wb.NotifyValues(sheet, range);
    }
}

/// <summary>Sets the formula text of a cell. The stored text never has a leading equals sign; the value is left to the engine.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="cell">The cell to set.</param>
/// <param name="formula">Formula text, with or without a leading equals sign.</param>
public sealed class SetFormulaCommand(Worksheet sheet, CellRef cell, string formula) : IWorkbookCommand
{
    private CellState _before;

    /// <inheritdoc />
    public string Description => "Set formula";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        _before = sheet.Capture(cell);
        var text = formula?.TrimStart();
        if (text != null && text.StartsWith('=')) text = text[1..];
        var target = sheet.Ensure(cell);
        target.Formula = string.IsNullOrEmpty(text) ? null : text;
        target.SpillAnchor = null;
        sheet.Prune(cell);
        wb.Notify(sheet, cell);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        sheet.Restore(cell, _before);
        wb.Notify(sheet, cell);
    }
}

/// <summary>
/// Changes the style of every cell in a range. Ranges spanning whole columns or rows set column or row styles and restyle
/// only existing cells; other ranges create style-only cells where needed.
/// </summary>
/// <param name="sheet">The sheet.</param>
/// <param name="range">The range to format.</param>
/// <param name="change">Maps the current effective style of a cell to its new style.</param>
public sealed class SetStyleCommand(Worksheet sheet, CellRange range, Func<CellStyle, CellStyle> change) : IWorkbookCommand
{
    private readonly List<(CellRef Ref, int StyleId)> _cellsBefore = new();
    private readonly List<(Axis Axis, int Index, int StyleId)> _axesBefore = new();

    /// <summary>Creates a command that replaces the style of every cell in the range with a fixed style.</summary>
    /// <param name="sheet">The sheet.</param>
    /// <param name="range">The range to format.</param>
    /// <param name="style">The style to apply.</param>
    public SetStyleCommand(Worksheet sheet, CellRange range, CellStyle style) : this(sheet, range, _ => style)
    {
    }

    /// <inheritdoc />
    public string Description => "Format cells";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        Worksheet.Check(range);
        _cellsBefore.Clear();
        _axesBefore.Clear();
        var cache = new Dictionary<int, int>();
        int Next(int effective)
        {
            if (!cache.TryGetValue(effective, out var next))
                cache[effective] = next = wb.Styles.GetOrAdd(change(wb.Styles[effective]));
            return next;
        }

        var wholeColumns = range.IsWholeColumns;
        var wholeRows = range.IsWholeRows;
        var cells = wholeColumns || wholeRows ? sheet.ExistingIn(range) : range.Cells();
        foreach (var c in cells)
        {
            var effective = sheet.EffectiveStyleId(c);
            var next = Next(effective);
            if (next == effective) continue;
            _cellsBefore.Add((c, sheet[c]?.StyleId ?? 0));
            sheet.Ensure(c).StyleId = next;
        }

        if (wholeColumns) StyleAxis(Axis.Column, Enumerable.Range(range.Left, range.ColumnCount), Next);
        if (wholeRows) StyleAxis(Axis.Row, wholeColumns ? sheet.AxisStyleKeys(Axis.Row) : Enumerable.Range(range.Top, range.RowCount), Next);
        wb.NotifyFormatted(sheet, range.Normalized);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        foreach (var (c, id) in _cellsBefore)
        {
            sheet.Ensure(c).StyleId = id;
            sheet.Prune(c);
        }

        foreach (var (axis, index, id) in _axesBefore) sheet.SetAxisStyle(axis, index, id);
        wb.NotifyFormatted(sheet, range.Normalized);
    }

    private void StyleAxis(Axis axis, IEnumerable<int> indexes, Func<int, int> next)
    {
        foreach (var i in indexes.ToList())
        {
            var old = sheet.AxisStyle(axis, i);
            _axesBefore.Add((axis, i, old));
            sheet.SetAxisStyle(axis, i, next(old));
        }
    }
}

/// <summary>Clears the contents (and optionally the formatting) of a range.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="range">The range to clear.</param>
/// <param name="clearStyle">True to clear formatting as well.</param>
public sealed class ClearCommand(Worksheet sheet, CellRange range, bool clearStyle = false) : IWorkbookCommand
{
    private readonly List<(CellRef Ref, CellState State)> _before = new();

    /// <inheritdoc />
    public string Description => clearStyle ? "Clear all" : "Clear contents";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        Worksheet.Check(range);
        _before.Clear();
        foreach (var c in sheet.ExistingIn(range))
        {
            var state = sheet.Capture(c);
            _before.Add((c, state));
            sheet.Restore(c, new CellState(default, null, clearStyle ? 0 : state.StyleId));
            wb.Notify(sheet, c);
        }

        if (clearStyle) wb.NotifyFormatted(sheet, range.Normalized);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        foreach (var (c, state) in _before)
        {
            sheet.Restore(c, state);
            wb.Notify(sheet, c);
        }

        if (clearStyle) wb.NotifyFormatted(sheet, range.Normalized);
    }
}
