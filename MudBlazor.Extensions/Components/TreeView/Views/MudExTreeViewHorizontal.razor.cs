using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeViewHorizontal<T> 
    where T : IHierarchical<T>
{
    [Parameter] public bool SkipSeparator { get; set; } = true;

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


    public string GetTransformStyle(T node, double top)
    {
        return $"transform: translateY(calc({top * 100}% + 0px))";
    }

    public string ScrollHorizontalTree(T node)
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

}

