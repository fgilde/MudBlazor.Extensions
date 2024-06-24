using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

public abstract partial class MudExTreeViewBase<TItem, TComponent> : MudExBaseComponent<MudExTreeViewBase<TItem, TComponent>>
    where TItem : IHierarchical<TItem>
    where TComponent : MudExTreeViewBase<TItem, TComponent>
{
    [Parameter] public bool ReverseExpandButton { get; set; }
    [Parameter] public bool Dense { get; set; }

    [Parameter]
    public HashSet<TItem> Items
    {
        get => FilterManager.Items;
        set => FilterManager.Items = value;
    }

    [Parameter]
    public Func<TItem, string> TextFunc
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

    /// <summary>
    /// Full item template if this is set you need to handle the outer items based on ViewMode on your own. 
    /// Also, the expand/collapse buttons, and you need to decide on your own if and how you use the <see cref="ItemContentTemplate"/>
    /// </summary>
    [Parameter] public RenderFragment<TreeViewItemContext<TItem, TComponent>> ItemTemplate { get; set; }

    /// <summary>
    /// Item content template for the item itself without the requirement to change outer element like to control the expand button etc.
    /// </summary>
    [Parameter] public RenderFragment<TreeViewItemContext<TItem, TComponent>> ItemContentTemplate { get; set; }

    /// <summary>
    /// This function controls how a separator will be detected. Default is if the item ToString() equals '-'
    /// </summary>
    [Parameter] public Func<TItem, bool> IsSeparatorDetectFunc { get; set; } = n => n?.ToString() == "-";

    /// <summary>
    /// The expand/collapse icon.
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.TreeView.Appearance)]
    public string ExpandedIcon { get; set; } = Icons.Material.Filled.ChevronRight;

    protected HierarchicalFilter<TItem> FilterManager = new();

    [Parameter] public EventCallback<TItem> SelectedNodeChanged { get; set; }

    [Parameter]
    public TItem SelectedNode
    {
        get => _selectedNode;
        set => Set(ref _selectedNode, value, _ => SelectedNodeChanged.InvokeAsync(value).AndForget());
    }

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
    private HashSet<TItem> _expanded = new();
    private TItem _selectedNode;
    public bool IsExpanded(TItem node) => _expanded.Contains(node);
    public void ExpandAll() => _expanded = new HashSet<TItem>(Items.Recursive(n => n.Children ?? Enumerable.Empty<TItem>()));
    public void CollapseAll() => _expanded.Clear();
    public bool IsSelected(TItem node) => node?.Equals(_selectedNode) == true; // TODO: implement multiselect

    public virtual bool IsSeparator(TItem node) => IsSeparatorDetectFunc?.Invoke(node) == true;


    protected virtual TreeViewItemContext<TItem, TComponent> CreateContext(TItem item, string search, bool isMenuItem)
    {
        return new TreeViewItemContext<TItem, TComponent>(item, IsSelected(item), IsExpanded(item), search, isMenuItem, this as TComponent);
    }

    protected virtual void NodeClick(TItem node)
    {
        if (IsSeparator(node))
            return;
        SelectedNode = node;
        if (SelectedNode != null)
        {
            SetExpanded(node, !IsExpanded(node));
        }
        InvokeAsync(StateHasChanged);
    }


    public IEnumerable<TItem> Path()
    {
        return _selectedNode != null ? _selectedNode.Path() : Enumerable.Empty<TItem>();
    }

    public bool IsInPath(TItem node)
    {
        var path = Path();
        var result = path?.Contains(node) == true;
        return result;
    }


    public void SetAllExpanded(bool expand, Func<TItem, bool> predicate = null)
    {
        //predicate ??= n => ExpandMode == ExpandMode.SingleExpand || n.Parent == null;
        predicate ??= n => n.Parent == null;
        Items?.Recursive(n => n.Children.EmptyIfNull()).Where(predicate).Apply(e => SetExpanded(e, expand));
    }

    public void SetExpanded(TItem context, bool expanded)
    {
        if (expanded && !IsExpanded(context))
            _expanded.Add(context);
        else if (!expanded && IsExpanded(context))
            _expanded.Remove(context);
    }
    
    protected string ListItemClassStr()
    {
        return MudExCssBuilder.Default.
            AddClass("mud-ex-simple-flex")
            .AddClass("mud-ex-flex-reverse-end", ReverseExpandButton)
            .ToString();
    }

    public string TreeItemClassStr()
    {
        return MudExCssBuilder.Default
            .AddClass("mud-ex-treeview-item-reverse-space-between", ReverseExpandButton)
            .ToString();
    }
    
}