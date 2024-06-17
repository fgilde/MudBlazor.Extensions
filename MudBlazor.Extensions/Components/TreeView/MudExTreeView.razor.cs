using Microsoft.AspNetCore.Components;
using Nextended.Core.Extensions;
using Nextended.Core.Types;
using Microsoft.AspNetCore.Components.Web;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeView<T> where T: IHierarchical<T>
{
    [Parameter] public bool Dense { get; set; }
    [Parameter] public HashSet<T> Items { get; set; }
    [Parameter] public Func<T, string> TextFunc { get; set; } = n => n?.ToString();
    [Parameter] public FilterMode FilterMode { get; set; }
    [Parameter] public TreeViewMode ViewMode { get; set; } = TreeViewMode.Horizontal;

    /// <summary>
    /// Full item template if this is set you need to handle the outer items based on ViewMode on your own. 
    /// Also, the expand/collapse buttons, and you need to decide on your own if and how you use the <see cref="ItemContentTemplate"/>
    /// </summary>
    [Parameter] public RenderFragment<TreeViewItemContext<T>> ItemTemplate { get; set; }
    
    /// <summary>
    /// Item content template for the item itself without the requirement to change outer element like to control the expand button etc.
    /// </summary>
    [Parameter] public RenderFragment<TreeViewItemContext<T>> ItemContentTemplate { get; set; }

    /// <summary>
    /// This function controls how a separator will be detected. Default is if the item ToString() equals '-'
    /// </summary>
    [Parameter] public Func<T, bool> IsSeparatorDetectFunc { get; set; } = n => n?.ToString() == "-";

    /// <summary>
    /// The expand/collapse icon.
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.TreeView.Appearance)]
    public string ExpandedIcon { get; set; } = Icons.Material.Filled.ChevronRight;


    public bool HasFilters => Filters?.Any(s => !string.IsNullOrWhiteSpace(s)) == true || !string.IsNullOrEmpty(Filter);
    private T selectedNode;
    private List<string> _filters;

    [Parameter]
    public string Filter { get; set; }

    [Parameter]
    public List<string> Filters
    {
        get => _filters;
        set
        {
            if (_filters != value)
            {
                _filters = value;
                SetAllExpanded(HasFilters, entry => true);
            }
        }
    }    
    private RenderFragment Inherited() => builder => base.BuildRenderTree(builder);
    private HashSet<T> _expanded = new();
    public bool IsExpanded(T node) => _expanded.Contains(node);
    public void ExpandAll() => _expanded = new HashSet<T>(Items.Recursive(n => n.Children ?? Enumerable.Empty<T>()));
    public void CollapseAll() => _expanded.Clear();
    public bool IsSelected(T node) => node?.Equals(selectedNode) == true; // TODO: implement multiselect

    public virtual bool IsSeparator(T node) => IsSeparatorDetectFunc?.Invoke(node) == true;

    private void NodeClick(T node)
    {
        selectedNode = node;
        if(ViewMode == TreeViewMode.Default)
        {
            SetExpanded(node, !IsExpanded(node));
        }
    }
    private void OnWheel(WheelEventArgs e)
    {
        // Logik hier implementieren
    }

    private string GetNodeClass(T node)
    {
        var classes = "horizontal-tree-node";
        if (IsInPath(node) || IsSelected(node))
        {
            classes += " node-selected";
        }
        if (HasChild(node))
        {
            classes += " node-expandable";
        }
        return classes;
    }

    public double NodeOffset(T node)
    {
        var children = (node.Children ?? Enumerable.Empty<T>()).ToList();
        var indexOf = children.IndexOf(children.FirstOrDefault(IsInPath));
        var indexOfSelected = Math.Max(indexOf, 0);

        return (children.Count - 1) / 2 - indexOfSelected;        
    }

    private string GetTransformStyle(T node)
    {
        return $"transform: translateY({NodeOffset(node) * 100}%)";
    }


    private bool HasChild(T node)
    {
        return node?.Children?.Any() == true;
    }

    public IEnumerable<T> Path()
    {
        if (selectedNode != null)
            return selectedNode.Path();
        return Enumerable.Empty<T>();
    }

    private bool IsInPath(T node)
    {        
        var path = Path();
        var result = path?.Contains(node) == true;
        //var s = string.Join("/", path.Select(n => n.ToString()));
        //Console.WriteLine($"IsInPath: {node} = {result} Path {s}");
        return result;
    }

    private HashSet<T>? FilteredItems()
    {
        if (FilterMode == FilterMode.Flat && HasFilters)
        {
            return Items.Recursive(e => e?.Children ?? Enumerable.Empty<T>()).Where(e =>
                    Filters?.Any(filter => MatchesFilter(e, filter)) == true)
                .ToHashSet();
        }
        return Items;
    }

    private bool MatchesFilter(T node, string text)
    { 
        return TextFunc(node).Contains(text, StringComparison.InvariantCultureIgnoreCase);
    }


    private (bool Found, string? Term) GetMatchedSearch(T node)
    {
        if (FilterMode == FilterMode.Flat || !HasFilters)
            return (true, string.Empty);

        if ((node?.Children ?? Enumerable.Empty<T>()).Recursive(n => n?.Children ?? Enumerable.Empty<T>()).Any(n => GetMatchedSearch(n).Found))
            return (true, string.Empty);


        var filters = Filters.EmptyIfNull().ToList();
        if (!string.IsNullOrEmpty(Filter))
            filters.Add(Filter);
        foreach (var filter in filters)
        {
            if (MatchesFilter(node, filter))
                return (true, filter); ;
        }

        return (false, string.Empty); ;
    }

    private void SetAllExpanded(bool expand, Func<T, bool> predicate = null)
    {
        //predicate ??= n => ExpandMode == ExpandMode.SingleExpand || n.Parent == null;
        predicate ??= n => n.Parent == null;
        Items?.Recursive(n => n.Children.EmptyIfNull()).Where(predicate).Apply(e => SetExpanded(e, expand));
    }

    public void SetExpanded(T context, bool expanded)
    {
        if (expanded && !IsExpanded(context))
            _expanded.Add(context);
        else if (!expanded && IsExpanded(context))
            _expanded.Remove(context);
    }

}


public enum FilterMode
{
    Default,
    Flat
}

public enum TreeViewMode
{
    Default,
    Horizontal,
    Breadcrumb,
    List
}

public class RenderIf<T> : System.Attribute
{

}