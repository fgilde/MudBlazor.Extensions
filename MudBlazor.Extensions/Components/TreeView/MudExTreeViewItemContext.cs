using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;


/// <summary>
/// Context for the tree view item.
/// </summary>
public class TreeViewItemContext<T> : TreeItemData<T>
    where T : IHierarchical<T>
{
    public TreeViewItemContext(T value,
        bool selected,
        bool expanded,
        bool focused,
        string search,
        MudExTreeViewBase<T> treeView,
        object tag = null,
        string highlight = null)
    {
        Value = value;
        if(Value != null)
            Value.OnChildrenLoaded += Value_OnChildrenLoaded;
        Selected = selected;
        Expanded = expanded;
        Focused = focused;
        Search = search;
        Tag = tag;
        TreeView = treeView;
        Highlight = highlight;
    }

    private void Value_OnChildrenLoaded(IHierarchical<T> arg1, HashSet<T> arg2)
    {
        TreeView?.Update();
    }

    public bool NeedsLoadChildren => HasChildren && Value?.LoadChildrenFunc != null; // TODO: use Value.NeedsLoadChildren();

    public override bool HasChildren => Value?.HasChildren() ?? false;

    /// <summary>
    /// Is true if the item is focused.
    /// </summary>
    public bool Focused { get; }

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