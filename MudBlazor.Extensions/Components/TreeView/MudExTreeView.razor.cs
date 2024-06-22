using Microsoft.AspNetCore.Components;
using Nextended.Core.Extensions;
using Nextended.Core.Types;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeView<T> 
    where T : IHierarchical<T>
{
    [Parameter] public bool SkipSeparator { get; set; } = true;
    [Parameter] public string BackLinkLabel { get; set; } = "Back to {0}";
    [Parameter] public bool ReverseExpandButton { get; set; }
    [Parameter] public bool Dense { get; set; }

    [Parameter]
    public HashSet<T> Items
    {
        get => FilterManager.Items;
        set => FilterManager.Items = value;
    }

    [Parameter]
    public Func<T, string> TextFunc
    {
        get => FilterManager.TextFunc;
        set => FilterManager.TextFunc = value;
    }

    [Parameter]
    public HierarchicalFilterBehaviour FilterBehaviour
    {
        get => FilterManager.FilterBehaviour;
        set => FilterManager.FilterBehaviour = value;
    }
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

    protected HierarchicalFilter<T> FilterManager = new();

    private T selectedNode;

    [Parameter]
    public string Filter
    {
        get => FilterManager.Filter;
        set => FilterManager.Filter = value;
    }

    [Parameter]
    public List<string> Filters
    {
        get => FilterManager.Filters;
        set
        {
            if (FilterManager.Filters != value)
            {
                FilterManager.Filters = value;
                SetAllExpanded(FilterManager.HasFilters, _ => true);
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
    protected virtual void NodeClick(T node)
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
        if (!new[] { "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "PageDown", "PageUp", "End", "Home" }.Contains(args.Code))
            return;

        var siblings = SiblingOfSelected();
        if (args.Code == "Home")
        {
            var toSelect = FilterManager.FilteredItems().EmptyIfNull().FirstOrDefault();
            var parent = selectedNode;
            while (parent != null && parent.Parent != null)
            {
                parent = parent.Parent;
            }
            NodeClick(parent ?? toSelect);
        }
        else if (args.Code == "End" && selectedNode?.HasChildren() == true)
        {
            var lastOrDefault = selectedNode.Children.Recursive(n => n.Children ?? Enumerable.Empty<T>()).ToList();
            NodeClick(lastOrDefault.FirstOrDefault(n => !n.HasChildren()));
        }else if (args.Code == "PageDown" && siblings.Any())
        {
            NodeClick(siblings.LastOrDefault());
        }
        else if (args.Code == "PageUp" && siblings.Any())
        {
            NodeClick(siblings.FirstOrDefault());
        }
        else if (args.Code == "ArrowLeft" && selectedNode != null && selectedNode.Parent != null && FilterManager.GetMatchedSearch(selectedNode.Parent).Found)
        {
            NodeClick(selectedNode.Parent);
        }
        else if (args.Code == "ArrowRight" && selectedNode?.HasChildren() == true)
        {
            NodeClick(selectedNode.Children.FirstOrDefault(n => FilterManager.GetMatchedSearch(n).Found));
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
            return selectedNode.Parent.Children.Where(n => FilterManager.GetMatchedSearch(n).Found).ToArray();
        return FilterManager.FilteredItems()?.Where(n => FilterManager.GetMatchedSearch(n).Found).ToArray() ?? Array.Empty<T>();
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
        return NodeOffset((node.Children?.Where(n => FilterManager.GetMatchedSearch(n).Found) ?? Enumerable.Empty<T>()).ToList());
    }

    private double NodeOffset(List<T> children)
    {
        var indexOf = children.IndexOf(children.FirstOrDefault(IsInPath));
        var indexOfSelected = Math.Max(indexOf, 0);

        return ((children.Count - 1) / 2.0) - indexOfSelected;
    }


    private string GetTransformStyle(T node, double top)
    {
        return $"transform: translateY(calc({top * 100}% + 0px))";
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





public enum TreeViewMode
{
    Default,
    Horizontal,
    Breadcrumb,
    List
}
