using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

public class TreeViewItemContext<TItem, TComponent> 
    where TItem : IHierarchical<TItem>
    where TComponent : MudExTreeViewBase<TItem, TComponent>
{
    public TreeViewItemContext(TItem item, 
        bool isSelected, 
        bool isExpanded, 
        string search,
        bool renderedAsMenuItem,
        TComponent treeView)
    {
        Item = item;
        IsSelected = isSelected;
        IsExpanded = isExpanded;
        Search = search;
        RenderedAsMenuItem = renderedAsMenuItem;
        TreeView = treeView;        
    }

    public TItem Item { get; }
    public bool IsSelected { get; }
    public bool IsExpanded { get; }
    public string Search { get; }
    public bool RenderedAsMenuItem { get; }
    public TComponent TreeView { get; }
}


public class TreeViewItemContext<T> where T : IHierarchical<T>
{
    public TreeViewItemContext(T item,
        bool isSelected,
        bool isExpanded,
        string search,
        bool renderedAsMenuItem,
        TreeViewMode viewMode,
        MudExTreeView<T> treeView)
    {
        Item = item;
        IsSelected = isSelected;
        IsExpanded = isExpanded;
        Search = search;
        RenderedAsMenuItem = renderedAsMenuItem;
        ViewMode = viewMode;
        TreeView = treeView;
    }

    public TreeViewMode ViewMode { get; set; }
    public T Item { get; }
    public bool IsSelected { get; }
    public bool IsExpanded { get; }
    public string Search { get; }
    public bool RenderedAsMenuItem { get; }
    public MudExTreeView<T> TreeView { get; }
}