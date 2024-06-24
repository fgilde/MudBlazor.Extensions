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
        }
        else if (args.Code == "PageDown" && siblings.Any())
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

