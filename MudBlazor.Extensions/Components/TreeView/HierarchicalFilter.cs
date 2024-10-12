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
    /// <summary>
    /// Items to filter
    /// </summary>
    public IReadOnlyCollection<T> Items { get; set; }

    /// <summary>
    /// Filters to apply
    /// </summary>
    public List<string> Filters { get; set; }

    /// <summary>
    /// Filter to apply
    /// </summary>
    public string Filter { get; set; }

    /// <summary>
    /// Filter behaviour
    /// </summary>
    public HierarchicalFilterBehaviour FilterBehaviour { get; set; }

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
            var filters = Filters.EmptyIfNull().Concat(new []{Filter}).Where(f => !string.IsNullOrEmpty(f)).Distinct();
            return Items.Recursive(e => e?.Children ?? Enumerable.Empty<T>(), a => a.ValidForRecursion()).Where(e =>
                    filters.Any(filter => MatchesFilter(e, filter)))
                .ToHashSet();
        }
        return Items;
    }

    /// <summary>
    /// Returns a tuple with a boolean indicating if the node matches the search and the term that matched
    /// </summary>
    public (bool Found, string Term) GetMatchedSearch(T node)
    {
        if (FilterBehaviour == HierarchicalFilterBehaviour.Flat || !HasFilters)
            return (true, string.Empty);

        if ((node?.Children ?? Enumerable.Empty<T>()).Recursive(n => n?.Children ?? Enumerable.Empty<T>(), a => a.ValidForRecursion()).Any(n => GetMatchedSearch(n).Found))
            return (true, string.Empty);


        var filters = Filters.EmptyIfNull().ToList();
        if (!string.IsNullOrEmpty(Filter))
            filters.Add(Filter);
        foreach (var filter in filters)
        {
            if (MatchesFilter(node, filter))
                return (true, filter);
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