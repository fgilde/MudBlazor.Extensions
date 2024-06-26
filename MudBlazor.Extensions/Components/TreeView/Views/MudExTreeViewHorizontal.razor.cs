using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Core;
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

    [Parameter] public bool SkipSeparator { get; set; } = true;
    [Parameter] public bool AllowColumnSizeChange { get; set; } = true;

    /// <summary>
    /// Border radius of the node leave null to use the default value from theme
    /// </summary>
    [Parameter] public MudExSize<double>? NodeBorderRadius { get; set; }
    [Parameter] public MudExSize<double> ColumnWidth { get; set; } = 250;
    [Parameter] public MudExSize<double> NodePadding { get; set; } = 24;
    [Parameter] public MudExSize<double> LineWidth { get; set; } = 2;
    [Parameter] public MudExColor LineColor { get; set; } = MudExColor.Primary;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (!IsOverwritten(nameof(SelectedItemBorderColor)))
            SelectedItemBorderColor = SelectedItemColor;
        base.OnInitialized();
    }

    private void OnWheel(WheelEventArgs e)
    {
        var siblings = SiblingOfSelected();

        if (SelectedNode == null || !siblings.Any())
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
            var parent = SelectedNode;
            while (parent != null && parent.Parent != null)
            {
                parent = parent.Parent;
            }
            NodeClick(parent ?? toSelect);
        }
        else if (args.Code == "End" && SelectedNode?.HasChildren() == true)
        {
            var lastOrDefault = SelectedNode.Children.Recursive(n => n.Children ?? Enumerable.Empty<T>()).ToList();
            NodeClick(lastOrDefault.FirstOrDefault(n => !n.HasChildren()));
        }
        else if (args.Code == "PageDown" && siblings.Any())
        {
            NodeClick(siblings.LastOrDefault());
        }
        else if (args.Code == "PageUp" && siblings.Any())
        {
            NodeClick(siblings.FirstOrDefault());
        }
        else if (args.Code == "ArrowLeft" && SelectedNode != null && SelectedNode.Parent != null && FilterManager.GetMatchedSearch(SelectedNode.Parent).Found)
        {
            NodeClick(SelectedNode.Parent);
        }
        else if (args.Code == "ArrowRight" && SelectedNode?.HasChildren() == true)
        {
            NodeClick(SelectedNode.Children.FirstOrDefault(n => FilterManager.GetMatchedSearch(n).Found));
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
        if (SelectedNode != null && SelectedNode.Parent != null && SelectedNode.Parent.HasChildren())
            return SelectedNode.Parent.Children.Where(n => FilterManager.GetMatchedSearch(n).Found).ToArray();
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
            .ToString();
    }


    public string GetNodeStyle(T node, double top)
    {
        return MudExStyleBuilder.Default
            .WithWidth(ColumnWidth)
            .With("--tree-node-line-width", LineWidth)
            .With("--tree-node-line-color", LineColor.ToCssStringValue())
            .With("--tree-node-padding", NodePadding)
            .WithTransform($"translateY(calc({top * 100}% + 0px))")
            .Style;
    }

    protected override string ItemStyleStr(TreeViewItemContext<T> context)
    {
        return MudExStyleBuilder.FromStyle(base.ItemStyleStr(context))
            .WithHeight(Dense ? 18 : 25)
            .WithBackgroundColor(SelectedItemBackgroundColor, (IsInPath(context.Item) || IsSelected(context.Item)) && SelectedItemBackgroundColor.IsSet())
            .WithBorderColor(SelectedItemBorderColor, (IsInPath(context.Item) || IsSelected(context.Item)) && SelectedItemBorderColor.IsSet())
            .WithBorderRadius(NodeBorderRadius, (IsInPath(context.Item) || IsSelected(context.Item)) && NodeBorderRadius.HasValue)
            .Style;
    }

    private string InnerItemClassStr()
    {
        return MudExCssBuilder.Default.
            AddClass("mud-ex-simple-flex")
            .AddClass("mud-ex-flex-reverse-end", ReverseExpandButton)
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        _= UpdateTreeWidth();
    }

    private async Task UpdateTreeWidth()
    {
        _treeWidth = await JsRuntime.DInvokeAsync<double>((w, id) => w.document.getElementById(id).getBoundingClientRect().width, _treeId);
    }

    public string ScrollHorizontalTree(T node)
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

