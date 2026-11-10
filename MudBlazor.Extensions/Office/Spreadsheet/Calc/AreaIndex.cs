namespace MudBlazor.Extensions.Office.Spreadsheet;

internal sealed class AreaIndex
{
    private const int BlockRows = 1024;
    private const int MaxBuckets = 256;
    private const int WholeColumn = -1;

    private readonly Dictionary<CellNode, List<RefArea>> _precedents = new();
    private readonly Dictionary<CellNode, HashSet<CellNode>> _cells = new();
    private readonly Dictionary<(Worksheet Sheet, int Column, int Block), HashSet<(CellRange Range, CellNode Dependent)>> _buckets = new();
    private readonly Dictionary<Worksheet, HashSet<(CellRange Range, CellNode Dependent)>> _wideBySheet = new();

    public int Count => _precedents.Count;

    public long Operations { get; private set; }

    public IReadOnlyDictionary<CellNode, List<RefArea>> Entries => _precedents;

    public void Clear()
    {
        _precedents.Clear();
        _cells.Clear();
        _buckets.Clear();
        _wideBySheet.Clear();
    }

    public void Set(CellNode node, List<RefArea> precedents)
    {
        Remove(node);
        _precedents[node] = precedents;
        foreach (var area in precedents) Index(area, node, true);
    }

    public bool TryGet(CellNode node, out List<RefArea> precedents) => _precedents.TryGetValue(node, out precedents);

    public bool Remove(CellNode node)
    {
        if (!_precedents.Remove(node, out var old)) return false;
        foreach (var area in old) Index(area, node, false);
        return true;
    }

    public IEnumerable<(CellRange Range, CellNode Dependent)> Matches(CellNode node)
    {
        var cell = node.Cell;
        if (_cells.TryGetValue(node, out var direct))
            foreach (var d in direct)
                yield return (new CellRange(cell, cell), d);

        foreach (var block in new[] { cell.Row / BlockRows, WholeColumn })
            if (_buckets.TryGetValue((node.Sheet, cell.Column, block), out var ranges))
                foreach (var entry in ranges)
                    if (entry.Range.Contains(cell))
                        yield return entry;

        if (_wideBySheet.TryGetValue(node.Sheet, out var wide))
            foreach (var entry in wide)
                if (entry.Range.Contains(cell))
                    yield return entry;
    }

    private void Index(RefArea area, CellNode node, bool add)
    {
        Operations++;
        var r = area.Range;
        if (r.IsSingleCell)
        {
            Toggle(_cells, new CellNode(area.Sheet, r.Start.Key), node, add);
            return;
        }

        var blocks = r.Bottom / BlockRows - r.Top / BlockRows + 1;
        if ((long)blocks * r.ColumnCount <= MaxBuckets)
        {
            for (var c = r.Left; c <= r.Right; c++)
                for (var b = r.Top / BlockRows; b <= r.Bottom / BlockRows; b++)
                    Toggle(_buckets, (area.Sheet, c, b), (r, node), add);
        }
        else if (r.ColumnCount <= MaxBuckets)
        {
            for (var c = r.Left; c <= r.Right; c++)
                Toggle(_buckets, (area.Sheet, c, WholeColumn), (r, node), add);
        }
        else
        {
            Toggle(_wideBySheet, area.Sheet, (r, node), add);
        }
    }

    private static void Toggle<TKey, T>(Dictionary<TKey, HashSet<T>> map, TKey key, T item, bool add)
    {
        if (add)
        {
            if (!map.TryGetValue(key, out var set)) map[key] = set = new HashSet<T>();
            set.Add(item);
        }
        else if (map.TryGetValue(key, out var set) && set.Remove(item) && set.Count == 0)
        {
            map.Remove(key);
        }
    }
}
