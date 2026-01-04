using Nextended.Core;
using Nextended.Core.Extensions;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Filter for hierarchical data
/// </summary>
public class HierarchicalFilter<T> 
    where T : IHierarchical<T>
{
    private IReadOnlyCollection<T> _items;
    private List<string> _filters;
    private string _filter;
    private HierarchicalFilterBehaviour _filterBehaviour;
    private IReadOnlyCollection<T> _cachedFilteredItems;
    private Dictionary<T, (bool Found, string Term)> _matchCache = new();
    private string _cacheKey;

    /// <summary>
    /// Items to filter
    /// </summary>
    public IReadOnlyCollection<T> Items
    {
        get => _items;
        set
        {
            if (_items != value)
            {
                _items = value;
                InvalidateCache();
            }
        }
    }

    /// <summary>
    /// Filters to apply
    /// </summary>
    public List<string> Filters
    {
        get => _filters;
        set
        {
            if (_filters != value)
            {
                _filters = value;
                InvalidateCache();
            }
        }
    }

    /// <summary>
    /// Filter to apply
    /// </summary>
    public string Filter
    {
        get => _filter;
        set
        {
            if (_filter != value)
            {
                _filter = value;
                InvalidateCache();
            }
        }
    }

    /// <summary>
    /// Filter behaviour
    /// </summary>
    public HierarchicalFilterBehaviour FilterBehaviour
    {
        get => _filterBehaviour;
        set
        {
            if (_filterBehaviour != value)
            {
                _filterBehaviour = value;
                InvalidateCache();
            }
        }
    }

    /// <summary>
    /// Text function
    /// </summary>
    public Func<T, string> TextFunc { get; set; } = (i => i?.ToString());

    /// <summary>
    /// Match function for filter
    /// </summary>
    public Func<T, string, bool> MatchFunc { get; set; }

    /// <summary>
    /// Returns true if a filter is present
    /// </summary>
    public bool HasFilters => Filters?.Any(s => !string.IsNullOrWhiteSpace(s)) == true || !string.IsNullOrEmpty(Filter);

    private void InvalidateCache()
    {
        _cachedFilteredItems = null;
        _matchCache.Clear();
        _cacheKey = null;
    }

    private string GetCacheKey()
    {
        if (_cacheKey == null)
        {
            var filtersStr = string.Join("|", Filters?.Where(f => !string.IsNullOrEmpty(f)) ?? Enumerable.Empty<string>());
            _cacheKey = $"{FilterBehaviour}:{Filter}:{filtersStr}";
        }
        return _cacheKey;
    }

    private bool MatchesFilter(T node, string text)
    {
        var textFn = TextFunc ?? (n => n?.ToString());
        var textNode = Check.TryCatch<string, Exception>(() => textFn(node)) ?? string.Empty;
        var matchFn = MatchFunc ?? ((n, t) => textNode.Contains(t, StringComparison.OrdinalIgnoreCase));
        return matchFn(node, text);
    }

    /// <summary>
    /// Returns filtered items only if FilterBehaviour is Flat and there are filters
    /// </summary>
    public IReadOnlyCollection<T>? FilteredItems()
    {
        if (FilterBehaviour == HierarchicalFilterBehaviour.Flat && HasFilters)
        {
            if (_cachedFilteredItems != null)
                return _cachedFilteredItems;

            var filters = Filters.EmptyIfNull().Concat(new []{Filter}).Where(f => !string.IsNullOrEmpty(f)).Distinct().ToList();
            _cachedFilteredItems = Items.Recursive(e => e.GetLoadedChildren()).Where(e =>
                    filters.Any(filter => e is IAsyncHierarchical<T> { IsLoading: true }  || MatchesFilter(e, filter)))
                .ToHashSet();
            return _cachedFilteredItems;
        }
        return Items;
    }

    /// <summary>
    /// Returns a tuple with a boolean indicating if the node matches the search and the term that matched
    /// </summary>
    public (bool Found, string Term) GetMatchedSearch(T node)
    {
        if (node == null)
            return (false, string.Empty);

        // Check cache first
        if (_matchCache.TryGetValue(node, out var cachedResult))
            return cachedResult;

        var result = ComputeMatchedSearch(node);
        
        // Cache the result before returning
        if (!_matchCache.ContainsKey(node))
            _matchCache[node] = result;
            
        return result;
    }

    private (bool Found, string Term) ComputeMatchedSearch(T node)
    {
        if (FilterBehaviour == HierarchicalFilterBehaviour.Flat || !HasFilters)
            return (true, string.Empty);

        var filters = Filters.EmptyIfNull().ToList();
        if (!string.IsNullOrEmpty(Filter))
            filters.Add(Filter);

        // Check direct match first (more common case)
        foreach (var filter in filters)
        {
            if (node is IAsyncHierarchical<T> { IsLoading: true } || MatchesFilter(node, filter))
                return (true, filter);
        }

        // Check children recursively - GetMatchedSearch will handle caching for children
        var children = node?.GetLoadedChildren() ?? Enumerable.Empty<T>();
        foreach (var child in children.Recursive(n => n.GetLoadedChildren()))
        {
            if (GetMatchedSearch(child).Found)
                return (true, string.Empty);
        }

        return (false, string.Empty);
    }
}


/// <summary>
/// The behaviour of the hierarchical filter
/// </summary>
public enum HierarchicalFilterBehaviour
{
    /// <summary>
    /// Default behaviour will not change the tree structure
    /// </summary>
    Default,

    /// <summary>
    /// Flat behaviour will flatten the tree structure and only show the nodes that match the filter
    /// </summary>
    Flat
}