using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;


/// <summary>
/// Context for the tree view item.
/// </summary>
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

    /// <summary>
    /// The item itself.
    /// </summary>
    public T Item { get; }

    /// <summary>
    /// Is true if the item is selected.
    /// </summary>
    public bool IsSelected { get; }

    /// <summary>
    /// Is true if the item is expanded.
    /// </summary>
    public bool IsExpanded { get; }

    /// <summary>
    /// Is true if the item is focused.
    /// </summary>
    public bool IsFocused { get; }

    /// <summary>
    /// The string that was used to search for this item.
    /// </summary>
    public string Search { get;  }

    /// <summary>
    /// The recommended string to highlight in the item.
    /// </summary>
    public string Highlight { get; }

    /// <summary>
    /// The tag of the item.
    /// </summary>
    public object Tag { get; }

    /// <summary>
    /// The owning tree view.
    /// </summary>
    public MudExTreeViewBase<T> TreeView { get; }
}