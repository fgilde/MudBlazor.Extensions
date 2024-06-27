using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeViewHorizontal<T> 
    where T : IHierarchical<T>
{
    private readonly string _treeId = $"mud-ex-tree-{Guid.NewGuid().ToFormattedId()}";
    private double _treeWidth;
    private bool _showSplitter;

    /// <summary>
    /// Set to false if you want to break keyboard or wheel navigation on a separator
    /// </summary>
    [Parameter] public bool SkipSeparator { get; set; } = true;

    /// <summary>
    /// If true, the <see cref="ColumnWidth"/> can be changed by dragging the splitter
    /// </summary>
    [Parameter] public bool AllowColumnSizeChange { get; set; } = true;

    /// <summary>
    /// Border radius of the node leave null to use the default value from theme
    /// </summary>
    [Parameter] public MudExSize<double>? NodeBorderRadius { get; set; }

    /// <summary>
    /// Width of the tree column
    /// </summary>
    [Parameter] public MudExSize<double> ColumnWidth { get; set; } = 250;

    /// <summary>
    /// Padding of the node
    /// </summary>
    [Parameter] public MudExSize<double> NodePadding { get; set; } = 24;

    /// <summary>
    /// Strength of the line between nodes
    /// </summary>
    [Parameter] public MudExSize<double> LineWidth { get; set; } = 2;

    /// <summary>
    /// Color of the line between nodes
    /// </summary>
    [Parameter] public MudExColor LineColor { get; set; } = MudExColor.Primary;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (!IsOverwritten(nameof(SelectedItemBorderColor)))
            SelectedItemBorderColor = SelectedItemColor;
        if (!IsOverwritten(nameof(CollapseOnClick)))
            CollapseOnClick = false;
        if (!IsOverwritten(nameof(ExpandOnClick)))
            ExpandOnClick = true;
        if (!IsOverwritten(nameof(AllowSelectionOfNonEmptyNodes)))
            AllowSelectionOfNonEmptyNodes = true;
        base.OnInitialized();
    }

    private void OnWheel(WheelEventArgs e)
    {
        var siblings = SiblingOfSelected();

        if (LastSelectedNode == null || !siblings.Any())
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

    protected bool KeyDownHandled { get; set; }

    private void KeyDown(KeyboardEventArgs args)
    {
        KeyDownHandled = false;
        if (!new[] { "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "PageDown", "PageUp", "End", "Home" }.Contains(args.Code))
            return;

        var siblings = SiblingOfSelected();
        var selectedNode = LastSelectedNode;
        if (args.Code == "Home")
        {
            var toSelect = FilterManager.FilteredItems().EmptyIfNull().FirstOrDefault();
            var parent = selectedNode;
            while (parent != null && parent.Parent != null)
            {
                parent = parent.Parent;
            }
            if (!ExpandOnClick || CollapseOnClick)
                CollapseAll();
            KeyDownHandled = true;
            NodeClick(parent ?? toSelect);
        }
        else if (args.Code == "End" && selectedNode?.HasChildren() == true)
        {
            var lastOrDefault = selectedNode.Children.Recursive(n => n.Children ?? Enumerable.Empty<T>()).ToList();
            var node = lastOrDefault.FirstOrDefault(n => !n.HasChildren());
            ExpandTo(node);
            KeyDownHandled = true;
            NodeClick(node);
        }
        else if (args.Code == "PageDown" && siblings.Any())
        {
            KeyDownHandled = true;
            NodeClick(siblings.LastOrDefault());
        }
        else if (args.Code == "PageUp" && siblings.Any())
        {
            KeyDownHandled = true;
            NodeClick(siblings.FirstOrDefault());
        }
        else if (args.Code == "ArrowLeft" && selectedNode != null && selectedNode.Parent != null && FilterManager.GetMatchedSearch(selectedNode.Parent).Found)
        {
            if(!ExpandOnClick || CollapseOnClick)
                SetExpanded(selectedNode.Parent, false);
            KeyDownHandled = true;
            NodeClick(selectedNode.Parent);
        }
        else if (args.Code == "ArrowRight" && selectedNode?.HasChildren() == true)
        {
            SetExpanded(selectedNode, true);
            KeyDownHandled = true;
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
                KeyDownHandled = true;
                NodeClick(siblings[newIndex]);
            }
        }
    }


    private T[] SiblingOfSelected()
    {
        if (LastSelectedNode != null && LastSelectedNode.Parent != null && LastSelectedNode.Parent.HasChildren())
            return LastSelectedNode.Parent.Children.Where(n => FilterManager.GetMatchedSearch(n).Found).ToArray();
        return FilterManager.FilteredItems()?.Where(n => FilterManager.GetMatchedSearch(n).Found).ToArray() ?? Array.Empty<T>();
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
            .AddClass("node-expanded", IsExpanded(node))
            .ToString();
    }


    private string GetNodeStyle(T node, double top)
    {
        return MudExStyleBuilder.Default
            .WithWidth(ColumnWidth)
            .With("--tree-node-line-width", LineWidth)
            .With("--tree-node-line-color", LineColor.ToCssStringValue())
            .With("--tree-node-padding", NodePadding)
            .WithTransform($"translateY(calc({top * 100}% + 0px))")
            .Style;
    }

    private bool RenderAsSelected(T node)
    {
        return IsInPath(node) || IsSelected(node) || IsFocused(node);
    }

    /// <inheritdoc />
    protected override string ItemStyleStr(TreeViewItemContext<T> context, string mergeWith = "")
    {
        return MudExStyleBuilder.FromStyle(base.ItemStyleStr(context, mergeWith))
            .WithHeight(Dense ? 18 : 25)
            .WithBackgroundColor(SelectedItemBackgroundColor, RenderAsSelected(context.Item) && SelectedItemBackgroundColor.IsSet())
            .WithBorderColor(SelectedItemBorderColor, RenderAsSelected(context.Item) && SelectedItemBorderColor.IsSet())
            .WithBorderRadius(NodeBorderRadius, RenderAsSelected(context.Item) && NodeBorderRadius.HasValue)
            .Style;
    }

    private string InnerItemClassStr()
    {
        return MudExCssBuilder.Default.
            AddClass("mud-ex-simple-flex")
            .AddClass("mud-ex-flex-reverse-end", ExpandButtonDirection == LeftOrRight.Left)
            .ToString();
    }


    private double NodeOffset(T node)
    {
        return NodeOffset((node.Children?.Where(n => FilterManager.GetMatchedSearch(n).Found) ?? Enumerable.Empty<T>()).ToList());
    }

    private double NodeOffset(List<T> children)
    {
        var indexOf = children.IndexOf(children.FirstOrDefault(IsInPath));
        var indexOfSelected = Math.Max(indexOf, 0);

        return ((children.Count - 1) / 2.0) - indexOfSelected;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        _= UpdateTreeWidth();
    }

    private async Task UpdateTreeWidth()
    {
        _treeWidth = await JsRuntime.DInvokeAsync<double>((w, id) => w.document.getElementById(id).getBoundingClientRect().width, _treeId);
    }

    private string ScrollHorizontalTree(T node)
    {
        var depth = this.Path().ToArray().IndexOf(node);
        double nodeWidth = ColumnWidth;

        var treeWidth = Math.Max(_treeWidth, ColumnWidth);
        var scrollBy = Math.Ceiling(((depth + 1) * nodeWidth - treeWidth) / nodeWidth);
        return MudExStyleBuilder.Default.WithTransform($"translateX(-{scrollBy * nodeWidth}px)").Style;
    }

    private void ColumnSizeChanged(SplitterEventArgs obj)
    {
        ColumnWidth = obj?.FirstElementRect?.Width ?? ColumnWidth;
    }
}

