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
    /// <summary>Default column width in pixels.</summary>
    public const double DefaultColumnWidth = 64;

    /// <summary>Default row height in pixels.</summary>
    public const double DefaultRowHeight = 20;

    private Dictionary<long, Cell> _cells = new();
    private readonly Dictionary<int, double> _columnWidths = new();
    private readonly Dictionary<int, double> _rowHeights = new();
    private readonly HashSet<int> _hiddenColumns = new();
    private readonly HashSet<int> _hiddenRows = new();
    private List<CellRange> _merges = new();

    internal Worksheet(string name) => Name = name;

    /// <summary>The sheet name.</summary>
    public string Name { get; internal set; }

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

    /// <summary>Zero-based indexes of hidden columns.</summary>
    public IReadOnlySet<int> HiddenColumns => _hiddenColumns;

    /// <summary>Zero-based indexes of hidden rows.</summary>
    public IReadOnlySet<int> HiddenRows => _hiddenRows;

    /// <summary>Gets the stored cell, or null if the address holds no content and no style.</summary>
    /// <param name="r">The cell address.</param>
    public Cell this[CellRef r] => _cells.GetValueOrDefault(r.Key);

    /// <summary>Enumerates all stored cells in no particular order.</summary>
    /// <returns>Address and cell pairs.</returns>
    public IEnumerable<(CellRef Ref, Cell Cell)> UsedCells()
    {
        foreach (var (key, cell) in _cells)
            yield return (CellRef.FromKey(key), cell);
    }

    /// <summary>Bounding range of all stored cells, or A1 for an empty sheet.</summary>
    public CellRange UsedRange
    {
        get
        {
            if (_cells.Count == 0) return CellRange.FromBounds(0, 0, 0, 0);
            int top = int.MaxValue, left = int.MaxValue, bottom = 0, right = 0;
            foreach (var key in _cells.Keys)
            {
                var c = CellRef.FromKey(key);
                top = Math.Min(top, c.Row);
                bottom = Math.Max(bottom, c.Row);
                left = Math.Min(left, c.Column);
                right = Math.Max(right, c.Column);
            }

            return CellRange.FromBounds(top, left, bottom, right);
        }
    }

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

    /// <summary>Tests whether a row is hidden.</summary>
    /// <param name="r">Zero-based row.</param>
    /// <returns>True if hidden.</returns>
    public bool IsRowHidden(int r) => _hiddenRows.Contains(r);

    internal Cell Ensure(CellRef r)
    {
        var key = r.Key;
        if (_cells.TryGetValue(key, out var cell)) return cell;
        cell = new Cell();
        _cells[key] = cell;
        return cell;
    }

    internal void Prune(CellRef r)
    {
        if (_cells.TryGetValue(r.Key, out var cell) && cell.State.IsEmpty) _cells.Remove(r.Key);
    }

    internal CellState Capture(CellRef r) => _cells.TryGetValue(r.Key, out var cell) ? cell.State : default;

    internal void Restore(CellRef r, CellState state)
    {
        if (state.IsEmpty)
        {
            _cells.Remove(r.Key);
            return;
        }

        var cell = Ensure(r);
        cell.Value = state.Value;
        cell.Formula = state.Formula;
        cell.StyleId = state.StyleId;
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

    internal void SetHidden(Axis axis, int index, bool hidden)
    {
        if (hidden) HiddenSet(axis).Add(index);
        else HiddenSet(axis).Remove(index);
    }

    internal void Insert(Axis axis, int index, int count)
    {
        var limit = axis == Axis.Row ? CellRef.MaxRows : CellRef.MaxColumns;
        var rebuilt = new Dictionary<long, Cell>(_cells.Count);
        foreach (var (key, cell) in _cells)
        {
            var c = CellRef.FromKey(key);
            var v = axis == Axis.Row ? c.Row : c.Column;
            if (v >= index) v += count;
            if (v >= limit)
                throw new InvalidOperationException("Inserting would push cells beyond the worksheet limits.");
            rebuilt[(axis == Axis.Row ? new CellRef(v, c.Column) : new CellRef(c.Row, v)).Key] = cell;
        }

        _cells = rebuilt;
        Remap(axis, v => v >= index ? v + count : v);
        _merges = _merges.Select(m => ShiftRange(m, axis, Start(m, axis) >= index ? count : 0, End(m, axis) >= index ? count : 0)).ToList();
        if (FreezeAt is { } f)
        {
            var v = axis == Axis.Row ? f.Row : f.Column;
            if (index < v) FreezeAt = axis == Axis.Row ? new CellRef(v + count, f.Column) : new CellRef(f.Row, v + count);
        }
    }

    internal DeleteSnapshot Delete(Axis axis, int index, int count)
    {
        var end = index + count;
        var removed = new List<(CellRef, CellState)>();
        var kept = new Dictionary<long, Cell>(_cells.Count);
        foreach (var (key, cell) in _cells)
        {
            var c = CellRef.FromKey(key);
            var v = axis == Axis.Row ? c.Row : c.Column;
            if (v < index) kept[key] = cell;
            else if (v < end) removed.Add((c, cell.State));
            else kept[(axis == Axis.Row ? new CellRef(v - count, c.Column) : new CellRef(c.Row, v - count)).Key] = cell;
        }

        var snapshot = new DeleteSnapshot(removed, Sizes(axis).Where(p => p.Key >= index && p.Key < end).ToList(),
            HiddenSet(axis).Where(i => i >= index && i < end).ToList(), CopyMerges(), FreezeAt);
        _cells = kept;
        Remap(axis, v => v < index ? v : v < end ? -1 : v - count);
        _merges = _merges.Select(m => ShrinkRange(m, axis, index, count)).Where(m => m is { IsSingleCell: false }).Select(m => m.Value).ToList();
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
        _merges = [.. snapshot.Merges];
        FreezeAt = snapshot.Freeze;
    }

    private Dictionary<int, double> Sizes(Axis axis) => axis == Axis.Row ? _rowHeights : _columnWidths;

    private HashSet<int> HiddenSet(Axis axis) => axis == Axis.Row ? _hiddenRows : _hiddenColumns;

    private void Remap(Axis axis, Func<int, int> map)
    {
        var sizes = Sizes(axis);
        var moved = sizes.Select(p => (Index: map(p.Key), p.Value)).Where(p => p.Index >= 0).ToList();
        sizes.Clear();
        foreach (var (i, v) in moved) sizes[i] = v;

        var hidden = HiddenSet(axis);
        var movedHidden = hidden.Select(map).Where(i => i >= 0).ToList();
        hidden.Clear();
        hidden.UnionWith(movedHidden);
    }

    private static int Start(CellRange r, Axis axis) => axis == Axis.Row ? r.Top : r.Left;

    private static int End(CellRange r, Axis axis) => axis == Axis.Row ? r.Bottom : r.Right;

    private static CellRange ShiftRange(CellRange r, Axis axis, int startShift, int endShift) => axis == Axis.Row
        ? CellRange.FromBounds(r.Top + startShift, r.Left, r.Bottom + endShift, r.Right)
        : CellRange.FromBounds(r.Top, r.Left + startShift, r.Bottom, r.Right + endShift);

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
    List<CellRange> Merges,
    CellRef? Freeze);
