namespace MudBlazor.Extensions.Office.Spreadsheet;

internal readonly record struct CellNode(Worksheet Sheet, long Key)
{
    public CellRef Cell => CellRef.FromKey(Key);
}

internal readonly record struct AxisShift(Worksheet Sheet, Axis Axis, int Index, int Count)
{
    private int Limit => Axis == Axis.Row ? CellRef.MaxRows : CellRef.MaxColumns;

    public int? Forward(int v) => v < Index ? v
        : Count > 0 ? v + Count < Limit ? v + Count : null
        : v < Index - Count ? null : v + Count;

    public CellRef? Forward(CellRef c) => Forward(Axis == Axis.Row ? c.Row : c.Column) is not { } v ? null
        : Axis == Axis.Row ? new CellRef(v, c.Column) : new CellRef(c.Row, v);

    public CellRange? Forward(CellRange r)
    {
        var (s, e) = Axis == Axis.Row ? (r.Top, r.Bottom) : (r.Left, r.Right);
        int ns, ne;
        if (Count > 0)
        {
            ns = s >= Index ? s + Count : s;
            ne = e >= Index ? Math.Min(e + Count, Limit - 1) : e;
        }
        else
        {
            var end = Index - Count;
            ns = s < Index ? s : s >= end ? s + Count : Index;
            ne = e < Index ? e : e >= end ? e + Count : Index - 1;
        }

        if (ne < ns || ns >= Limit) return null;
        return Axis == Axis.Row ? CellRange.FromBounds(ns, r.Left, ne, r.Right) : CellRange.FromBounds(r.Top, ns, r.Bottom, ne);
    }

    public CellRef Back(CellRef c, out bool inserted)
    {
        var v = Axis == Axis.Row ? c.Row : c.Column;
        inserted = Count > 0 && v >= Index && v < Index + Count;
        var b = v < Index ? v : inserted ? Index : v - Count;
        return Axis == Axis.Row ? new CellRef(b, c.Column) : new CellRef(c.Row, b);
    }
}

internal sealed class DependencyGraph
{
    private const int MaxShifts = 32;
    private const int MinLazyEntries = 4096;

    private readonly AreaIndex _base = new();
    private readonly AreaIndex _fresh = new();
    private readonly List<AxisShift> _shifts = new();

    public long IndexOperations => _base.Operations + _fresh.Operations;

    public void Clear()
    {
        _base.Clear();
        _fresh.Clear();
        _shifts.Clear();
    }

    public void Set(CellNode node, List<RefArea> precedents)
    {
        Remove(node);
        (_shifts.Count == 0 ? _base : _fresh).Set(node, precedents);
    }

    public void Remove(CellNode node)
    {
        if (_fresh.Remove(node)) return;
        if (ToBase(node) is { } old) _base.Remove(old);
    }

    public List<RefArea> Precedents(CellNode node)
    {
        if (_fresh.TryGet(node, out var areas)) return areas;
        if (ToBase(node) is not { } old || !_base.TryGet(old, out areas)) return [];
        return _shifts.Count == 0 ? areas : Forward(areas);
    }

    public IEnumerable<CellNode> Dependents(CellNode node)
    {
        foreach (var (_, d) in _fresh.Matches(node)) yield return d;
        if (_shifts.Count == 0)
        {
            foreach (var (_, d) in _base.Matches(node)) yield return d;
            yield break;
        }

        var cell = node.Cell;
        var probe = new CellNode(node.Sheet, Back(node.Sheet, cell, out _).Key);
        foreach (var (range, d) in _base.Matches(probe))
            if (Forward(node.Sheet, range)?.Contains(cell) == true && Forward(d) is { } current)
                yield return current;
    }

    public void Shift(AxisShift shift)
    {
        if (_fresh.Count > 0)
        {
            var moved = Map(_fresh.Entries, [shift]);
            _fresh.Clear();
            foreach (var (node, areas) in moved) _fresh.Set(node, areas);
        }

        _shifts.Add(shift);
        if (_shifts.Count > MaxShifts || _base.Count < MinLazyEntries || _fresh.Count > _base.Count / 4) Compact();
    }

    private void Compact()
    {
        var entries = Map(_base.Entries, _shifts);
        entries.AddRange(_fresh.Entries.Select(p => (p.Key, p.Value)));
        _base.Clear();
        _fresh.Clear();
        _shifts.Clear();
        foreach (var (node, areas) in entries) _base.Set(node, areas);
    }

    private static List<(CellNode Node, List<RefArea> Areas)> Map(IReadOnlyDictionary<CellNode, List<RefArea>> entries, IReadOnlyList<AxisShift> shifts)
    {
        var list = new List<(CellNode, List<RefArea>)>(entries.Count);
        foreach (var (node, areas) in entries)
            if (Forward(node, shifts) is { } n)
                list.Add((n, Forward(areas, shifts)));
        return list;
    }

    private CellNode? ToBase(CellNode node)
    {
        if (_shifts.Count == 0) return node;
        var cell = Back(node.Sheet, node.Cell, out var inserted);
        return inserted ? null : new CellNode(node.Sheet, cell.Key);
    }

    private CellRef Back(Worksheet sheet, CellRef cell, out bool inserted)
    {
        inserted = false;
        for (var i = _shifts.Count - 1; i >= 0; i--)
        {
            if (_shifts[i].Sheet != sheet) continue;
            cell = _shifts[i].Back(cell, out var added);
            inserted |= added;
        }

        return cell;
    }

    private CellNode? Forward(CellNode node) => Forward(node, _shifts);

    private List<RefArea> Forward(List<RefArea> areas) => Forward(areas, _shifts);

    private CellRange? Forward(Worksheet sheet, CellRange range) => Forward(sheet, range, _shifts);

    private static CellNode? Forward(CellNode node, IReadOnlyList<AxisShift> shifts)
    {
        CellRef? cell = node.Cell;
        foreach (var s in shifts)
            if (s.Sheet == node.Sheet && cell is { } c)
                cell = s.Forward(c);
        return cell is { } r ? new CellNode(node.Sheet, r.Key) : null;
    }

    private static List<RefArea> Forward(List<RefArea> areas, IReadOnlyList<AxisShift> shifts)
    {
        var mapped = new List<RefArea>(areas.Count);
        foreach (var a in areas)
            if (Forward(a.Sheet, a.Range, shifts) is { } r)
                mapped.Add(new RefArea(a.Sheet, r));
        return mapped;
    }

    private static CellRange? Forward(Worksheet sheet, CellRange range, IReadOnlyList<AxisShift> shifts)
    {
        CellRange? r = range;
        foreach (var s in shifts)
            if (s.Sheet == sheet && r is { } x)
                r = s.Forward(x);
        return r;
    }
}
