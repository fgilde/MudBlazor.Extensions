using System.Globalization;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Row or column axis of a worksheet.</summary>
public enum Axis
{
    /// <summary>Rows.</summary>
    Row,

    /// <summary>Columns.</summary>
    Column
}

/// <summary>
/// A worksheet with sparse cell storage. Only cells with content or a non-default style are stored, and
/// row and column sizes are stored only where they differ from the defaults. All mutation goes through commands.
/// </summary>
public sealed class Worksheet
{
    /// <summary>Standard column width in pixels, the initial <see cref="DefaultColumnWidth"/>.</summary>
    public const double StandardColumnWidth = 64;

    /// <summary>Standard row height in pixels, the initial <see cref="DefaultRowHeight"/>.</summary>
    public const double StandardRowHeight = 20;

    private Dictionary<long, Cell> _cells = new();
    private HashSet<long> _formulaKeys = new();
    private HashSet<long> _spillKeys = new();
    private readonly Dictionary<int, double> _columnWidths = new();
    private readonly Dictionary<int, double> _rowHeights = new();
    private readonly HashSet<int> _hiddenColumns = new();
    private readonly HashSet<int> _hiddenRows = new();
    private HashSet<int> _filteredRows = new();
    private HashSet<int> _hiddenOrFiltered;
    private readonly Dictionary<int, int> _columnStyles = new();
    private readonly Dictionary<int, int> _rowStyles = new();
    private List<CellRange> _merges = new();
    private CellRange? _usedRange;

    internal Worksheet(string name, Workbook workbook)
    {
        Name = name;
        Workbook = workbook;
    }

    internal Workbook Workbook { get; }

    internal bool Modified { get; set; }

    private XlsxSheetPart Part => Workbook?.Package?.Sheets.Find(p => p.Worksheet == this);

    /// <summary>The sheet name.</summary>
    public string Name { get; internal set; }

    /// <summary>Width in pixels of columns without an explicit width.</summary>
    public double DefaultColumnWidth { get; internal set; } = StandardColumnWidth;

    /// <summary>Height in pixels of rows without an explicit height.</summary>
    public double DefaultRowHeight { get; internal set; } = StandardRowHeight;

    /// <summary>Tab color as #RRGGBB, or null.</summary>
    public string TabColor { get; internal set; }

    /// <summary>True if the sheet tab is hidden.</summary>
    public bool Hidden { get; internal set; }

    /// <summary>First scrolling cell of a frozen pane (rows above and columns left of it stay fixed), or null.</summary>
    public CellRef? FreezeAt { get; internal set; }

    /// <summary>Merged ranges.</summary>
    public IReadOnlyList<CellRange> Merges => _merges;

    /// <summary>Explicit column widths (only deviations from the default), keyed by zero-based column.</summary>
    public IReadOnlyDictionary<int, double> ColumnWidths => _columnWidths;

    /// <summary>Explicit row heights (only deviations from the default), keyed by zero-based row.</summary>
    public IReadOnlyDictionary<int, double> RowHeights => _rowHeights;

    /// <summary>Column styles (style ids, only columns with a style), keyed by zero-based column.</summary>
    public IReadOnlyDictionary<int, int> ColumnStyles => _columnStyles;

    /// <summary>Row styles (style ids, only rows with a style), keyed by zero-based row.</summary>
    public IReadOnlyDictionary<int, int> RowStyles => _rowStyles;

    /// <summary>Zero-based indexes of hidden columns.</summary>
    public IReadOnlySet<int> HiddenColumns => _hiddenColumns;

    /// <summary>Zero-based indexes of hidden rows, whether hidden by hand or by the auto filter.</summary>
    public IReadOnlySet<int> HiddenRows => _filteredRows.Count == 0 ? _hiddenRows : _hiddenOrFiltered ??= [.. _hiddenRows, .. _filteredRows];

    /// <summary>Zero-based indexes of rows hidden by the auto filter, tracked apart from rows hidden by hand.</summary>
    public IReadOnlySet<int> FilteredRows => _filteredRows;

    /// <summary>The auto filter, or null.</summary>
    public AutoFilter AutoFilter { get; internal set; }

    /// <summary>Conditional formatting in file order; change it with the rule commands.</summary>
    public IReadOnlyList<ConditionalFormatting> ConditionalFormats { get; internal set; } = [];

    /// <summary>Data validation rules in file order; change them with the rule commands.</summary>
    public IReadOnlyList<DataValidation> DataValidations { get; internal set; } = [];

    /// <summary>Tables on this sheet; change them with the table commands.</summary>
    public IReadOnlyList<SheetTable> Tables { get; internal set; } = [];

    /// <summary>
    /// Hyperlinks on this sheet in file order; change them with <see cref="SetLinkCommand"/> and <see cref="RemoveLinkCommand"/>. Links may overlap,
    /// then the later one applies to the shared cells (see <see cref="LinkAt"/>).
    /// </summary>
    public IReadOnlyList<CellLink> Links { get; internal set; } = [];

    /// <summary>Finds the hyperlink that applies to a cell: the last link covering it.</summary>
    /// <param name="cell">The cell.</param>
    /// <returns>The link, or null.</returns>
    public CellLink LinkAt(CellRef cell) => Links.LastOrDefault(l => l.Area.Contains(cell.Relative));

    /// <summary>Notes on this sheet; change them with <see cref="SetNoteCommand"/> and <see cref="RemoveNoteCommand"/>.</summary>
    public IReadOnlyList<CellNote> Notes { get; internal set; } = [];

    /// <summary>Charts on this sheet in drawing order; change them with the chart commands.</summary>
    public IReadOnlyList<SheetChart> Charts { get; internal set; } = [];

    /// <summary>
    /// True if the source file has threaded comments on this sheet. Their legacy notes are shown but cannot be edited, and the comment parts are
    /// saved unchanged.
    /// </summary>
    public bool NotesReadOnly { get; internal set; }

    /// <summary>Finds the table containing a cell.</summary>
    /// <param name="cell">The cell address.</param>
    /// <returns>The table, or null.</returns>
    public SheetTable TableAt(CellRef cell) => Tables.FirstOrDefault(t => t.Range.Contains(cell));

    /// <summary>Gets the stored cell, or null if the address holds no content and no style.</summary>
    /// <param name="r">The cell address.</param>
    public Cell this[CellRef r] => InBounds(r) ? _cells.GetValueOrDefault(r.Key) : null;

    /// <summary>Style id applied to a whole column (0 if none).</summary>
    /// <param name="col">Zero-based column.</param>
    /// <returns>The style id.</returns>
    public int ColumnStyle(int col) => _columnStyles.GetValueOrDefault(col);

    /// <summary>Style id applied to a whole row (0 if none).</summary>
    /// <param name="row">Zero-based row.</param>
    /// <returns>The style id.</returns>
    public int RowStyle(int row) => _rowStyles.GetValueOrDefault(row);

    /// <summary>Style id that renders a cell: its own style if set, else the row style, else the column style, else 0.</summary>
    /// <param name="r">The cell address.</param>
    /// <returns>The effective style id.</returns>
    public int EffectiveStyleId(CellRef r)
    {
        if (_cells.TryGetValue(r.Key, out var cell) && cell.StyleId != 0) return cell.StyleId;
        return _rowStyles.TryGetValue(r.Row, out var rs) ? rs : _columnStyles.GetValueOrDefault(r.Column);
    }

    /// <summary>Enumerates all stored cells in no particular order.</summary>
    /// <returns>Address and cell pairs.</returns>
    public IEnumerable<(CellRef Ref, Cell Cell)> UsedCells()
    {
        foreach (var (key, cell) in _cells)
            yield return (CellRef.FromKey(key), cell);
    }

    /// <summary>Bounding range of all cells holding a value or formula (style-only cells are ignored), or A1 if there are none.</summary>
    public CellRange UsedRange => _usedRange ??= ComputeUsedRange();

    /// <summary>Width of a column in pixels (hidden columns keep their stored width).</summary>
    /// <param name="c">Zero-based column.</param>
    /// <returns>The width.</returns>
    public double ColumnWidth(int c) => _columnWidths.TryGetValue(c, out var w) ? w : DefaultColumnWidth;

    /// <summary>Height of a row in pixels (hidden rows keep their stored height).</summary>
    /// <param name="r">Zero-based row.</param>
    /// <returns>The height.</returns>
    public double RowHeight(int r) => _rowHeights.TryGetValue(r, out var h) ? h : DefaultRowHeight;

    /// <summary>Tests whether a column is hidden.</summary>
    /// <param name="c">Zero-based column.</param>
    /// <returns>True if hidden.</returns>
    public bool IsColumnHidden(int c) => _hiddenColumns.Contains(c);

    /// <summary>Tests whether a row is hidden, by hand or by the auto filter.</summary>
    /// <param name="r">Zero-based row.</param>
    /// <returns>True if hidden.</returns>
    public bool IsRowHidden(int r) => _hiddenRows.Contains(r) || _filteredRows.Contains(r);

    /// <summary>Tests whether the auto filter hides a row.</summary>
    /// <param name="r">Zero-based row.</param>
    /// <returns>True if filtered out.</returns>
    public bool IsRowFiltered(int r) => _filteredRows.Contains(r);

    /// <summary>Enumerates the rows of a range that are neither hidden by hand nor filtered out, so edits in a filtered range hit only visible rows.</summary>
    /// <param name="range">The range.</param>
    /// <returns>Zero-based row indexes in ascending order.</returns>
    public IEnumerable<int> VisibleRowsIn(CellRange range)
    {
        for (var r = range.Top; r <= range.Bottom; r++)
            if (!IsRowHidden(r))
                yield return r;
    }

    internal static bool InBounds(CellRef r) => r.Row >= 0 && r.Row < CellRef.MaxRows && r.Column >= 0 && r.Column < CellRef.MaxColumns;

    internal static void Check(CellRef r)
    {
        if (!InBounds(r)) throw new ArgumentOutOfRangeException(nameof(r), "The cell lies outside the worksheet limits.");
    }

    internal static void Check(CellRange range)
    {
        Check(range.Start);
        Check(range.End);
    }

    private CellRange ComputeUsedRange()
    {
        int top = int.MaxValue, left = int.MaxValue, bottom = -1, right = -1;
        foreach (var (key, cell) in _cells)
        {
            if (!cell.HasContent) continue;
            var c = CellRef.FromKey(key);
            top = Math.Min(top, c.Row);
            bottom = Math.Max(bottom, c.Row);
            left = Math.Min(left, c.Column);
            right = Math.Max(right, c.Column);
        }

        return bottom < 0 ? CellRange.FromBounds(0, 0, 0, 0) : CellRange.FromBounds(top, left, bottom, right);
    }

    internal Cell Ensure(CellRef r)
    {
        Check(r);
        _usedRange = null;
        var key = r.Key;
        if (_cells.TryGetValue(key, out var cell)) return cell;
        if (_cells.Count >= CellLimit) throw TooManyCells();
        cell = new Cell();
        _cells[key] = cell;
        return cell;
    }

    internal void Reserve(int count) => _cells.EnsureCapacity(count);

    internal int CellLimit { get; set; } = int.MaxValue;

    internal int CellCount => _cells.Count;

    internal static InvalidDataException TooManyCells() => new($"The file has more cells than {nameof(Workbook)}.{nameof(Workbook.MaxCells)} allows.");

    internal IReadOnlyCollection<long> FormulaKeys => _formulaKeys;

    internal IReadOnlyCollection<long> SpillKeys => _spillKeys;

    internal void Sync(CellRef r)
    {
        var key = r.Key;
        if (_cells.TryGetValue(key, out var cell))
        {
            Track(key, cell, _formulaKeys, _spillKeys);
            return;
        }

        _formulaKeys.Remove(key);
        _spillKeys.Remove(key);
    }

    private static void Track(long key, Cell cell, HashSet<long> formulas, HashSet<long> spills)
    {
        if (cell.Formula != null) formulas.Add(key);
        else formulas.Remove(key);
        if (cell.SpillAnchor != null) spills.Add(key);
        else spills.Remove(key);
    }

    internal int CellCapacity => _cells.EnsureCapacity(0);

    internal void Prune(CellRef r)
    {
        _usedRange = null;
        if (_cells.TryGetValue(r.Key, out var cell) && cell.State.IsEmpty) _cells.Remove(r.Key);
        Sync(r);
    }

    internal CellState Capture(CellRef r)
    {
        Check(r);
        return _cells.TryGetValue(r.Key, out var cell) ? cell.State : default;
    }

    internal void Restore(CellRef r, CellState state)
    {
        _usedRange = null;
        if (state.IsEmpty)
        {
            _cells.Remove(r.Key);
            Sync(r);
            return;
        }

        var cell = Ensure(r);
        cell.Value = state.Value;
        cell.Formula = state.Formula;
        cell.StyleId = state.StyleId;
        cell.SpillAnchor = state.SpillAnchor;
        Sync(r);
    }

    internal List<CellRef> ExistingIn(CellRange range)
    {
        var list = new List<CellRef>();
        if (range.Area > _cells.Count)
        {
            foreach (var key in _cells.Keys)
            {
                var c = CellRef.FromKey(key);
                if (range.Contains(c)) list.Add(c);
            }
        }
        else
        {
            foreach (var c in range.Cells())
                if (_cells.ContainsKey(c.Key)) list.Add(c);
        }

        return list;
    }

    internal void ReplaceMerges(IEnumerable<CellRange> merges) => _merges = merges.ToList();

    internal List<CellRange> CopyMerges() => [.. _merges];

    internal double? SizeOrNull(Axis axis, int index) =>
        Sizes(axis).TryGetValue(index, out var size) ? size : null;

    internal void SetSize(Axis axis, int index, double? size)
    {
        if (size is null) Sizes(axis).Remove(index);
        else Sizes(axis)[index] = size.Value;
    }

    internal double DefaultSize(Axis axis) => axis == Axis.Row ? DefaultRowHeight : DefaultColumnWidth;

    internal void SetDefaultSize(Axis axis, double size)
    {
        if (axis == Axis.Row) DefaultRowHeight = size;
        else DefaultColumnWidth = size;
    }

    internal List<KeyValuePair<int, double>> SizeEntries(Axis axis) => Sizes(axis).ToList();

    internal void ReplaceSizes(Axis axis, IEnumerable<KeyValuePair<int, double>> entries)
    {
        var sizes = Sizes(axis);
        sizes.Clear();
        foreach (var (i, v) in entries) sizes[i] = v;
    }

    internal int AxisStyle(Axis axis, int index) => AxisStyles(axis).GetValueOrDefault(index);

    internal void SetAxisStyle(Axis axis, int index, int styleId)
    {
        if (styleId == 0) AxisStyles(axis).Remove(index);
        else AxisStyles(axis)[index] = styleId;
    }

    internal List<int> AxisStyleKeys(Axis axis) => AxisStyles(axis).Keys.ToList();

    internal void SetHidden(Axis axis, int index, bool hidden)
    {
        _hiddenOrFiltered = null;
        Workbook.HiddenVersion++;
        if (hidden) HiddenSet(axis).Add(index);
        else HiddenSet(axis).Remove(index);
    }

    internal bool IsHiddenByHand(Axis axis, int index) => HiddenSet(axis).Contains(index);

    internal void SetFiltered(IEnumerable<int> rows)
    {
        _hiddenOrFiltered = null;
        Workbook.HiddenVersion++;
        _filteredRows = [.. rows];
    }

    internal HashSet<int> EvaluateFilters(AutoFilter filter, IReadOnlyList<SheetTable> tables)
    {
        var rows = filter?.Evaluate(this) ?? [];
        foreach (var t in tables)
            if (t.Filter is { Columns.Count: > 0 } f)
                rows.UnionWith(f.Evaluate(this));
        return rows;
    }

    internal void ReapplyFilters() => SetFiltered(EvaluateFilters(AutoFilter, Tables));

    internal static bool FiltersLost(IReadOnlyList<SheetTable> before, IReadOnlyList<SheetTable> after, bool resized)
    {
        foreach (var t in before)
        {
            if (t.Filter is not { Columns.Count: > 0 } f) continue;
            if (after.FirstOrDefault(x => x.Id == t.Id)?.Filter is not { } g || g.Columns.Count < f.Columns.Count
                || resized && (g.Range.RowCount != f.Range.RowCount || g.Range.ColumnCount != f.Range.ColumnCount)) return true;
        }

        return false;
    }

    /// <remarks>
    /// A row hidden in the file counts as filtered when it fails the criteria both in the current culture and invariantly, so the result does not depend on
    /// the reader's culture when the file was written in either; a row that passes in one of them stays hidden by hand.
    /// </remarks>
    internal void ClaimFilteredRows()
    {
        if (AutoFilter is { Columns.Count: > 0 } filter) AutoFilter = ClaimFilteredRows(filter);
    }

    internal AutoFilter ClaimFilteredRows(AutoFilter filter)
    {
        var known = filter.EvaluateKnown(this);
        known.IntersectWith(filter.EvaluateKnown(this, CultureInfo.InvariantCulture));
        var opaque = filter.HasOpaqueColumns
            ? _hiddenRows.Where(r => r > filter.Range.Top && r <= filter.Range.Bottom && !known.Contains(r)).ToHashSet()
            : [];
        var rows = _hiddenRows.Where(r => known.Contains(r) || opaque.Contains(r)).ToList();
        _hiddenRows.ExceptWith(rows);
        SetFiltered(_filteredRows.Concat(rows));
        return filter.WithOpaqueRows(opaque);
    }

    internal bool DrawingOpaque => Part is { DrawingPart: not null, OriginalDrawing: null };

    internal Action ForgetFileData(Func<CellRef, bool> where) => Part?.Forget(where);

    internal List<(CellRef Ref, CellState State)> Insert(Axis axis, int index, int count)
    {
        var limit = axis == Axis.Row ? CellRef.MaxRows : CellRef.MaxColumns;
        foreach (var (key, cell) in _cells)
        {
            var c = CellRef.FromKey(key);
            var v = axis == Axis.Row ? c.Row : c.Column;
            if (v >= index && v + count >= limit && cell.HasContent)
                throw new InvalidOperationException("Inserting would push cells beyond the worksheet limits.");
        }

        var dropped = new List<(CellRef, CellState)>();
        var rebuilt = new Dictionary<long, Cell>(_cells.Count);
        var formulas = new HashSet<long>(_formulaKeys.Count);
        var spills = new HashSet<long>(_spillKeys.Count);
        foreach (var (key, cell) in _cells)
        {
            var c = CellRef.FromKey(key);
            var v = axis == Axis.Row ? c.Row : c.Column;
            if (v >= index) v += count;
            if (v >= limit)
            {
                dropped.Add((c, cell.State));
                continue;
            }

            if (cell.SpillAnchor is { } a && (axis == Axis.Row ? a.Row : a.Column) >= index)
                cell.SpillAnchor = Moved(a, axis, count, limit);
            var moved = (axis == Axis.Row ? new CellRef(v, c.Column) : new CellRef(c.Row, v)).Key;
            rebuilt[moved] = cell;
            Track(moved, cell, formulas, spills);
        }

        _cells = rebuilt;
        _formulaKeys = formulas;
        _spillKeys = spills;
        _usedRange = null;
        Part?.Shift(axis, index, count);
        Remap(axis, v => v >= index ? v + count : v, limit);
        ShiftRules(axis, index, count);
        if (AutoFilter != null && (AutoFilter = AutoFilter.Shift(axis, index, count)) == null) ReapplyFilters();
        _merges = _merges.Select(m => ShiftRange(m, axis, Start(m, axis) >= index ? count : 0, End(m, axis) >= index ? count : 0, limit)).ToList();
        if (FreezeAt is { } f)
        {
            var v = axis == Axis.Row ? f.Row : f.Column;
            var nv = Math.Min(v + count, limit - 1);
            if (index < v) FreezeAt = axis == Axis.Row ? new CellRef(nv, f.Column) : new CellRef(f.Row, nv);
        }

        return dropped;
    }

    internal DeleteSnapshot Delete(Axis axis, int index, int count)
    {
        var end = index + count;
        var limit = axis == Axis.Row ? CellRef.MaxRows : CellRef.MaxColumns;
        var removed = new List<(CellRef, CellState)>();
        var kept = new Dictionary<long, Cell>(_cells.Count);
        var formulas = new HashSet<long>(_formulaKeys.Count);
        var spills = new HashSet<long>(_spillKeys.Count);
        foreach (var (key, cell) in _cells)
        {
            var c = CellRef.FromKey(key);
            var v = axis == Axis.Row ? c.Row : c.Column;
            if (v >= index && v < end)
            {
                removed.Add((c, cell.State));
                continue;
            }

            if (cell.SpillAnchor is { } a && (axis == Axis.Row ? a.Row : a.Column) is var anchor && anchor >= index)
            {
                if (anchor >= end) cell.SpillAnchor = Moved(a, axis, -count, limit);
                else
                {
                    removed.Add((c, cell.State));
                    cell.SpillAnchor = null;
                    cell.Value = default;
                    if (cell.State.IsEmpty) continue;
                }
            }

            var moved = v < index ? key : (axis == Axis.Row ? new CellRef(v - count, c.Column) : new CellRef(c.Row, v - count)).Key;
            kept[moved] = cell;
            Track(moved, cell, formulas, spills);
        }

        var snapshot = new DeleteSnapshot(removed, Sizes(axis).Where(p => p.Key >= index && p.Key < end).ToList(),
            HiddenSet(axis).Where(i => i >= index && i < end).ToList(),
            AxisStyles(axis).Where(p => p.Key >= index && p.Key < end).ToList(), CopyMerges(), FreezeAt, AutoFilter, _filteredRows,
            ConditionalFormats, DataValidations, Tables, Links, Notes, Charts, Part?.Shift(axis, index, -count));
        _cells = kept;
        _formulaKeys = formulas;
        _spillKeys = spills;
        _usedRange = null;
        Remap(axis, v => v < index ? v : v < end ? -1 : v - count, limit);
        _merges = _merges.Select(m => ShrinkRange(m, axis, index, count)).Where(m => m is { IsSingleCell: false }).Select(m => m.Value).ToList();
        var tables = Tables;
        ShiftRules(axis, index, -count);
        var filter = AutoFilter;
        AutoFilter = filter?.Shift(axis, index, -count);
        if (filter != null && (AutoFilter == null || AutoFilter.Columns.Count < filter.Columns.Count) || FiltersLost(tables, Tables, false)) ReapplyFilters();

        if (FreezeAt is { } f)
        {
            var v = axis == Axis.Row ? f.Row : f.Column;
            var nv = v - Math.Clamp(v - index, 0, count);
            var frozen = axis == Axis.Row ? new CellRef(nv, f.Column) : new CellRef(f.Row, nv);
            FreezeAt = frozen.Row == 0 && frozen.Column == 0 ? null : frozen;
        }

        return snapshot;
    }

    internal void RestoreDeleted(Axis axis, int index, int count, DeleteSnapshot snapshot)
    {
        Insert(axis, index, count);
        foreach (var (r, state) in snapshot.Cells) Restore(r, state);
        foreach (var (i, size) in snapshot.Sizes) Sizes(axis)[i] = size;
        foreach (var i in snapshot.Hidden) HiddenSet(axis).Add(i);
        AutoFilter = snapshot.AutoFilter;
        SetFiltered(snapshot.Filtered);
        ConditionalFormats = snapshot.ConditionalFormats;
        DataValidations = snapshot.DataValidations;
        Tables = snapshot.Tables;
        Links = snapshot.Links;
        Notes = snapshot.Notes;
        Charts = snapshot.Charts;
        foreach (var (i, style) in snapshot.AxisStyles) AxisStyles(axis)[i] = style;
        _merges = [.. snapshot.Merges];
        FreezeAt = snapshot.Freeze;
        snapshot.Package?.Invoke();
    }

    private void ShiftRules(Axis axis, int index, int count)
    {
        Tables = Tables.Select(t => t.Shift(axis, index, count)).OfType<SheetTable>().ToList();
        ConditionalFormats = ConditionalFormats.Select(c => c.Shift(axis, index, count)).OfType<ConditionalFormatting>().ToList();
        DataValidations = DataValidations.Select(d => d.Shift(axis, index, count)).OfType<DataValidation>().ToList();
        Links = Links.Select(l => l.Shift(axis, index, count)).OfType<CellLink>().ToList();
        Notes = Notes.Select(n => n.Shift(axis, index, count)).OfType<CellNote>().ToList();
        Charts = Charts.Select(c => c.Shift(axis, index, count)).ToList();
    }

    private static CellRef? Moved(CellRef r, Axis axis, int count, int limit)
    {
        var v = (axis == Axis.Row ? r.Row : r.Column) + count;
        return v < 0 || v >= limit ? null : axis == Axis.Row ? new CellRef(v, r.Column) : new CellRef(r.Row, v);
    }

    private Dictionary<int, double> Sizes(Axis axis) => axis == Axis.Row ? _rowHeights : _columnWidths;

    private Dictionary<int, int> AxisStyles(Axis axis) => axis == Axis.Row ? _rowStyles : _columnStyles;

    private HashSet<int> HiddenSet(Axis axis) => axis == Axis.Row ? _hiddenRows : _hiddenColumns;

    private void Remap(Axis axis, Func<int, int> map, int limit)
    {
        var sizes = Sizes(axis);
        var moved = sizes.Select(p => (Index: map(p.Key), p.Value)).Where(p => p.Index >= 0 && p.Index < limit).ToList();
        sizes.Clear();
        foreach (var (i, v) in moved) sizes[i] = v;

        var styles = AxisStyles(axis);
        var movedStyles = styles.Select(p => (Index: map(p.Key), p.Value)).Where(p => p.Index >= 0 && p.Index < limit).ToList();
        styles.Clear();
        foreach (var (i, v) in movedStyles) styles[i] = v;

        var hidden = HiddenSet(axis);
        var movedHidden = hidden.Select(map).Where(i => i >= 0 && i < limit).ToList();
        hidden.Clear();
        hidden.UnionWith(movedHidden);
        if (axis != Axis.Row) return;
        _hiddenOrFiltered = null;
        _filteredRows = [.. _filteredRows.Select(map).Where(i => i >= 0 && i < limit)];
    }

    internal static CellRange? MoveRange(CellRange r, Axis axis, int index, int count) => count >= 0
        ? ShiftRange(r, axis, Start(r, axis) >= index ? count : 0, End(r, axis) >= index ? count : 0, axis == Axis.Row ? CellRef.MaxRows : CellRef.MaxColumns)
        : ShrinkRange(r, axis, index, -count);

    private static int Start(CellRange r, Axis axis) => axis == Axis.Row ? r.Top : r.Left;

    private static int End(CellRange r, Axis axis) => axis == Axis.Row ? r.Bottom : r.Right;

    private static CellRange ShiftRange(CellRange r, Axis axis, int startShift, int endShift, int limit) => axis == Axis.Row
        ? CellRange.FromBounds(Math.Min(r.Top + startShift, limit - 1), r.Left, Math.Min(r.Bottom + endShift, limit - 1), r.Right)
        : CellRange.FromBounds(r.Top, Math.Min(r.Left + startShift, limit - 1), r.Bottom, Math.Min(r.Right + endShift, limit - 1));

    private static CellRange? ShrinkRange(CellRange r, Axis axis, int index, int count)
    {
        var s = Start(r, axis);
        var e = End(r, axis);
        var ns = s < index ? s : s >= index + count ? s - count : index;
        var ne = e < index ? e : e >= index + count ? e - count : index - 1;
        if (ne < ns) return null;
        return axis == Axis.Row
            ? CellRange.FromBounds(ns, r.Left, ne, r.Right)
            : CellRange.FromBounds(r.Top, ns, r.Bottom, ne);
    }
}

internal sealed record DeleteSnapshot(
    List<(CellRef Ref, CellState State)> Cells,
    List<KeyValuePair<int, double>> Sizes,
    List<int> Hidden,
    List<KeyValuePair<int, int>> AxisStyles,
    List<CellRange> Merges,
    CellRef? Freeze,
    AutoFilter AutoFilter,
    IReadOnlySet<int> Filtered,
    IReadOnlyList<ConditionalFormatting> ConditionalFormats,
    IReadOnlyList<DataValidation> DataValidations,
    IReadOnlyList<SheetTable> Tables,
    IReadOnlyList<CellLink> Links,
    IReadOnlyList<CellNote> Notes,
    IReadOnlyList<SheetChart> Charts,
    Action Package = null);
