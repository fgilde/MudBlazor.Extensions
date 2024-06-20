using Microsoft.AspNetCore.Components;
using Nextended.Core.Extensions;
using Nextended.Core.Types;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeView<T> where T : IHierarchical<T>
{
    [Parameter] public bool SkipSeparator { get; set; } = true;
    [Parameter] public string BackLinkLabel { get; set; } = "Back to {0}";
    [Parameter] public bool ReverseExpandButton { get; set; }
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

    private AnimationDirection? _animationDirection;
    private void NodeClick(T node)
    {
        if (IsSeparator(node))
            return;
        _animationDirection = node == null || selectedNode?.Parent?.Equals(node) == true || selectedNode?.Parent?.Parent?.Equals(node) == true ? AnimationDirection.In : AnimationDirection.Out;
        if(!node.HasChildren())
            _animationDirection = null;
        selectedNode = node;
        if (selectedNode != null && ViewMode == TreeViewMode.Default)
        {
            SetExpanded(node, !IsExpanded(node));
        }
    }
    private void OnWheel(WheelEventArgs e)
    {
        var siblings = SiblingOfSelected();

        if (selectedNode == null || !siblings.Any())
            return;

        var currentIndex = SelectedNodeIndexInPath(siblings);
        var newIndex = e.DeltaY < 0 ? currentIndex - 1 : currentIndex + 1;

        while (newIndex >= 0 && newIndex < siblings.Length && SkipSeparator && IsSeparator(siblings[newIndex]))
        {
            newIndex = e.DeltaY < 0 ? newIndex - 1 : newIndex + 1;
        }

        if (newIndex >= 0 && newIndex < siblings.Length)
            NodeClick(siblings[newIndex]);
    }


    private void KeyDown(KeyboardEventArgs args)
    {
        if (!new[] { "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown" }.Contains(args.Code))
            return;

        var siblings = SiblingOfSelected();

        if (args.Code == "ArrowLeft" && selectedNode != null && selectedNode.Parent != null)
        {
            NodeClick(selectedNode.Parent);
        }
        else if (args.Code == "ArrowRight" && selectedNode?.HasChildren() == true)
        {
            NodeClick(selectedNode.Children.First());
        }
        else if (new[] { "ArrowUp", "ArrowDown" }.Contains(args.Code) && siblings.Any())
        {
            var currentIndex = SelectedNodeIndexInPath(siblings);
            var newIndex = args.Code == "ArrowUp" ? currentIndex - 1 : currentIndex + 1;

            while (newIndex >= 0 && newIndex < siblings.Length && SkipSeparator && IsSeparator(siblings[newIndex]))
            {
                newIndex = args.Code == "ArrowUp" ? newIndex - 1 : newIndex + 1;
            }

            if (newIndex >= 0 && newIndex < siblings.Length)
            {
                NodeClick(siblings[newIndex]);
            }
        }
    }


    private T[] SiblingOfSelected()
    {
        if (selectedNode != null && selectedNode.Parent != null && selectedNode.Parent.HasChildren())
            return selectedNode.Parent.Children.ToArray();
        return FilteredItems()?.ToArray() ?? Array.Empty<T>();
    }

    private int SelectedNodeIndexInPath()
    {
        return SelectedNodeIndexInPath(SiblingOfSelected());
    }

    private int SelectedNodeIndexInPath(T[] nodeChildren)
    {
        return Math.Max(nodeChildren.EmptyIfNull().ToArray().IndexOf(nodeChildren.FirstOrDefault(IsInPath)), 0);
    }

    private string GetNodeClass(T node)
    {
        return MudExCssBuilder.From("mud-ex-horizontal-tree-node")
            .AddClass("node-selected", IsInPath(node) || IsSelected(node))
            .AddClass("node-expandable", node.HasChildren())
            .ToString();
    }

    public double NodeOffset(T node)
    {
        return NodeOffset((node.Children ?? Enumerable.Empty<T>()).ToList());
    }

    private double NodeOffset(List<T> children)
    {
        var indexOf = children.IndexOf(children.FirstOrDefault(IsInPath));
        var indexOfSelected = Math.Max(indexOf, 0);

        return ((children.Count - 1) / 2.0) - indexOfSelected;
    }


    private string GetTransformStyle(T node, double top)
    {
        return $"transform: translateY({top * 100}%)";
    }

    private string ScrollHorizontalTree(T node)
    {
        var depth = this.Path().ToArray().IndexOf(node);
        //var treeWidth = this.el.getBoundingClientRect().width;
        //var nodeWidth = (this.el.querySelector(`#node-${node.id}`).parentNode as HTMLElement).clientWidth;
        //var scrollWrapper = this.el.querySelector('.horizontal-tree-scroll-wrapper') as HTMLElement;

        decimal nodeWidth = 298;
        var treeWidth = 600;
        var scrollBy = Math.Ceiling((((depth + 1) * nodeWidth) - treeWidth) / nodeWidth);
        return MudExStyleBuilder.Default.WithTransform($"translateX(-{scrollBy * nodeWidth}px)").Style;

        //scrollWrapper.style.transform = `translateX(-${ scrollBy* nodeWidth}px)`;
    }


    public IEnumerable<T> Path()
    {
        if (selectedNode != null)
        {
            return selectedNode.Path();
        }

        return Enumerable.Empty<T>();
    }

    private bool IsInPath(T node)
    {
        var path = Path();
        var result = path?.Contains(node) == true;
        return result;
    }

    private HashSet<T>? FilteredItems()
    {
        if (FilterMode == FilterMode.Flat && HasFilters)
        {
            return Items.Recursive(e => e?.Children ?? Enumerable.Empty<T>()).Where(e =>
                    Filters.EmptyIfNull().Concat(new[] { Filter }).Distinct().Any(filter => MatchesFilter(e, filter)))
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

    private bool ShouldRenderViewMode(TreeViewMode mode)
    {
        return mode == ViewMode; //ViewMode.HasFlag(mode);
    }

    private string ListItemClassStr()
    {
        return MudExCssBuilder.Default.
            AddClass("mud-ex-simple-flex")
            .AddClass("mud-ex-flex-reverse-end", ReverseExpandButton)
            .ToString();
    }

    private string TreeItemClassStr()
    {
        return MudExCssBuilder.Default
            .AddClass("mud-ex-treeview-item-reverse-space-between", ReverseExpandButton)
            .ToString();
    }

    private string ListBoxStyleStr()
    {
        if (_animationDirection == null)
        {
            return string.Empty;
        }
        var duration = TimeSpan.FromMilliseconds(300);
        Task.Delay(duration).ContinueWith(_ =>
        {
            _animationDirection = null;
            InvokeAsync(StateHasChanged);
        });
        return MudExStyleBuilder.Default.WithAnimation(
            AnimationType.Slide,
            duration,
            _animationDirection,
            AnimationTimingFunction.EaseInOut,
             DialogPosition.CenterRight, when: _animationDirection != null).Style;
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
