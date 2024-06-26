using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;



public class TreeViewItemContext<T> where T : IHierarchical<T>
{
    public TreeViewItemContext(T item,
        bool isSelected,
        bool isExpanded,
        bool isFocused,
        string search,
        MudExTreeViewBase<T> treeView,
        object tag = null,
        string highlight = null)
    {
        Item = item;
        IsSelected = isSelected;
        IsExpanded = isExpanded;
        IsFocused = isFocused;
        Search = search;
        Tag = tag;
        TreeView = treeView;
        Highlight = highlight;
    }

    public T Item { get; }
    public bool IsSelected { get; }
    public bool IsExpanded { get; }
    public bool IsFocused { get; }
    public string Search { get; }
    public string Highlight { get; }
    public object Tag { get; }
    public MudExTreeViewBase<T> TreeView { get; }
}