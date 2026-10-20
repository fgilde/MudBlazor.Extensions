namespace MudBlazor.Extensions.Components;

/// <summary>
/// The forest of <see cref="MudExOrgChart{TItem}"/>: every id gets a stable int key, cycles are broken so every item is reachable exactly once.
/// </summary>
internal sealed class OrgChartIndex<TItem>
{
    private readonly Dictionary<object, int> _keys = [];
    private readonly HashSet<int> _known = [];
    private Func<TItem, object> _id = item => item;
    private Dictionary<int, TItem> _items = [];
    private Dictionary<int, List<TItem>> _children = [];
    private Dictionary<int, int> _parents = [];

    public List<TItem> Roots { get; private set; } = [];

    public IEnumerable<int> Keys => _items.Keys;

    public TItem Item(int key) => _items[key];

    public int KeyOf(TItem item) => Key(_id(item));

    public bool TryKeyOf(TItem item, out int key) => _keys.TryGetValue(_id(item) ?? this, out key) && _items.ContainsKey(key);

    public IReadOnlyList<TItem> Children(int key) => _children.TryGetValue(key, out var children) ? children : [];

    public int? ParentKey(int key) => _parents.TryGetValue(key, out var parent) ? parent : null;

    public IEnumerable<int> Ancestors(int key)
    {
        for (var p = ParentKey(key); p is { } parent; p = ParentKey(parent))
        {
            yield return parent;
        }
    }

    public int Descendants(int key) => Children(key).Sum(c => 1 + Descendants(KeyOf(c)));

    /// <summary>Rebuilds the tree and returns the items seen for the first time with their depth.</summary>
    public List<(int Key, int Depth)> Build(IEnumerable<TItem> items, Func<TItem, object> id, Func<TItem, object> parentId, Func<TItem, IEnumerable<TItem>> children)
    {
        _id = id ?? (item => item);
        _items = [];
        _children = [];
        _parents = [];
        Roots = [];
        var all = items.ToList();
        if (children is not null)
        {
            Walk(all, children);
        }
        else
        {
            Link(all, parentId);
        }

        var added = new List<(int, int)>();
        var level = Roots.Select(KeyOf).ToList();
        for (var depth = 0; level.Count > 0; depth++)
        {
            added.AddRange(level.Where(_known.Add).Select(k => (k, depth)));
            level = level.SelectMany(k => Children(k).Select(KeyOf)).ToList();
        }

        return added;
    }

    private int Key(object id)
    {
        id ??= this;
        if (!_keys.TryGetValue(id, out var key))
        {
            _keys[id] = key = _keys.Count;
        }

        return key;
    }

    private void Walk(List<TItem> roots, Func<TItem, IEnumerable<TItem>> children)
    {
        var next = new Queue<(TItem Item, int? Parent)>(roots.Select(r => (r, (int?)null)));
        while (next.TryDequeue(out var current))
        {
            var key = KeyOf(current.Item);
            if (!_items.TryAdd(key, current.Item))
            {
                continue;
            }

            Attach(current.Item, key, current.Parent);
            foreach (var child in children(current.Item) ?? [])
            {
                next.Enqueue((child, key));
            }
        }
    }

    private void Link(List<TItem> items, Func<TItem, object> parentId)
    {
        foreach (var item in items)
        {
            _items.TryAdd(KeyOf(item), item);
        }

        foreach (var (key, item) in _items)
        {
            var pid = parentId?.Invoke(item);
            Attach(item, key, pid is not null && _keys.TryGetValue(pid, out var p) && p != key && _items.ContainsKey(p) ? p : null);
        }

        // a cycle has no root: its first member becomes one, so nothing disappears and nothing renders forever
        var reached = new HashSet<int>();
        foreach (var key in _items.Keys)
        {
            while (CycleMember(key, reached) is { } member)
            {
                _children[_parents[member]].Remove(_items[member]);
                _parents.Remove(member);
                Roots.Add(_items[member]);
            }
        }
    }

    // walks up to a root or a reached item and marks the way as reached; returns an item of a cycle on the way
    private int? CycleMember(int key, HashSet<int> reached)
    {
        var path = new List<int>();
        for (int? k = key; k is { } current && !reached.Contains(current); k = ParentKey(current))
        {
            if (path.Contains(current))
            {
                return current;
            }

            path.Add(current);
        }

        reached.UnionWith(path);
        return null;
    }

    private void Attach(TItem item, int key, int? parent)
    {
        if (parent is not { } p)
        {
            Roots.Add(item);
            return;
        }

        _parents[key] = p;
        if (!_children.TryGetValue(p, out var list))
        {
            _children[p] = list = [];
        }

        list.Add(item);
    }
}
