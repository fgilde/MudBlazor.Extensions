using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Css;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using Nextended.Core.Extensions;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

public abstract partial class MudExTreeViewBase<TItem> : MudExBaseComponent<MudExTreeViewBase<TItem>>
    where TItem : IHierarchical<TItem>
{
    private string _highlight;
    private HashSet<TItem> _expanded = new();
    private TItem _selectedNode;

    protected HierarchicalFilter<TItem> FilterManager = new();

    [Parameter] public MudExColor ItemBackgroundColor { get; set; }
    [Parameter] public MudExColor SelectedItemBackgroundColor { get; set; }
    [Parameter] public MudExColor SelectedItemColor { get; set; } = MudExColor.Primary;
    [Parameter] public MudExColor SelectedItemBorderColor { get; set; }

    [Parameter] public MudExSize<double> ItemWidth { get; set; } = "100%";
    
    /// <summary>
    /// Typographic style for the item content only used if <see cref="ItemContentTemplate"/> and <see cref="ItemTemplate"/> are not set
    /// </summary>
    [Parameter] public Typo ItemContentTypo { get; set; } = Typo.body1;

    /// <summary>
    /// Filter mode for the filter box
    /// </summary>
    [Parameter] public PropertyFilterMode FilterMode { get; set; } = PropertyFilterMode.AlwaysVisible;

    /// <summary>
    /// Behaviour for the highlighting of an applied filter
    /// </summary>
    [Parameter] public FilterHighlighting FilterHighlighting { get; set; } = FilterHighlighting.ActiveFilterOnly;

    /// <summary>
    /// Set to false to disable multiple filters
    /// </summary>
    [Parameter] public bool FilterMultiple { get; set; } = true;

    /// <summary>
    /// If true the expand button will be on the other right side of the item content
    /// </summary>
    [Parameter] public bool ReverseExpandButton { get; set; }

    /// <summary>
    /// Dense mode for the treeview
    /// </summary>
    [Parameter] public bool Dense { get; set; }

    /// <summary>
    /// If true the treeview will expand all nodes on filter
    /// </summary>
    [Parameter] public bool ExpandOnFilter { get; set; }

    /// <summary>
    /// Style for Item itself this only is used if <see cref="ItemContentTemplate"/> and <see cref="ItemTemplate"/> are not set
    /// </summary>
    [Parameter] public string ItemStyle { get; set; }

    /// <summary>
    /// Class for Item itself this only is used if <see cref="ItemContentTemplate"/> and <see cref="ItemTemplate"/> are not set
    /// </summary>
    [Parameter] public string ItemClass { get; set; }

    /// <summary>
    /// Style for Item content this only is used if <see cref="ItemContentTemplate"/> and <see cref="ItemTemplate"/> are not set
    /// </summary>
    [Parameter] public string ItemContentStyle { get; set; }

    /// <summary>
    /// Class for Item content this only is used if <see cref="ItemContentTemplate"/> and <see cref="ItemTemplate"/> are not set
    /// </summary>
    [Parameter] public string ItemContentClass { get; set; }

    /// <summary>
    /// Items to display in the treeview
    /// </summary>
    [Parameter]
    public HashSet<TItem> Items
    {
        get => FilterManager.Items;
        set => FilterManager.Items = value;
    }

    /// <summary>
    /// Function to get the text of an item this is used for the filter and the item content if <see cref="ItemContentTemplate"/> and <see cref="ItemTemplate"/> are not set
    /// </summary>
    [Parameter]
    public Func<TItem, string> TextFunc
    {
        get => FilterManager.TextFunc;
        set => FilterManager.TextFunc = value;
    }

    /// <summary>
    /// Behaviour for the filtering
    /// </summary>
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
    [Parameter] public RenderFragment<TreeViewItemContext<TItem>> ItemTemplate { get; set; }

    /// <summary>
    /// Item content template for the item itself without the requirement to change outer element like to control the expand button etc.
    /// </summary>
    [Parameter] public RenderFragment<TreeViewItemContext<TItem>> ItemContentTemplate { get; set; }

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

    /// <summary>
    /// Callback when the selected node changes
    /// </summary>
    [Parameter] public EventCallback<TItem> SelectedNodeChanged { get; set; }

    /// <summary>
    /// Callback when the selected node changes
    /// </summary>
    [Parameter] public EventCallback<TItem> NodeClicked { get; set; }

    /// <summary>
    /// Callback when the filter changes
    /// </summary>
    [Parameter] public EventCallback<string> FilterChanged { get; set; }

    /// <summary>
    /// Callback when the filters change
    /// </summary>
    [Parameter] public EventCallback<List<string>> FiltersChanged { get; set; }

    /// <summary>
    /// Selected node
    /// </summary>
    [Parameter]
    public TItem SelectedNode
    {
        get => _selectedNode;
        set => Set(ref _selectedNode, LastSelectedNode = value, _ =>
        {
            SelectedNodeChanged.InvokeAsync(value).AndForget();
            if(AutoExpand)
                ExpandTo(value);
        });
    }

    public void ExpandTo(TItem value)
    {
        value.Path().Apply(e => SetExpanded(e, true));
    }

    protected TItem LastSelectedNode;

    /// <summary>
    /// Filter for the treeview
    /// </summary>
    [Parameter]
    public string Filter
    {
        get => FilterManager.Filter;
        set
        {
            FilterManager.Filter = value;
            FilterChanged.InvokeAsync(value).AndForget();
        }
    }

    /// <summary>
    /// Filters for the treeview
    /// </summary>
    [Parameter]
    public List<string> Filters
    {
        get => FilterManager.Filters;
        set
        {
            if (value?.Any() != true)
                _highlight = null;
            
            if (FilterManager.Filters != value)
            {
                FilterManager.Filters = value;
                if(_highlight != null && value?.Contains(_highlight) != true)
                    _highlight = null;
                
                if (ExpandOnFilter)
                    SetAllExpanded(FilterManager.HasFilters, _ => true);
                
                FiltersChanged.InvokeAsync(value).AndForget();
            }
        }
    }

    public HashSet<TItem> FlattedItems() => FilterManager.FilteredItems().Recursive(n => n.Children ?? Enumerable.Empty<TItem>()).ToHashSet();

    /// <summary>
    /// Returns true if the given node is expanded
    /// </summary>
    public bool IsExpanded(TItem node) => _expanded.Contains(node);

    /// <summary>
    /// Expand all nodes
    /// </summary>
    public void ExpandAll() => _expanded = new HashSet<TItem>(Items.Recursive(n => n.Children ?? Enumerable.Empty<TItem>()));

    /// <summary>
    /// Collapse all nodes
    /// </summary>
    public void CollapseAll() => _expanded.Clear();

    /// <summary>
    /// Returns true if the given node is selected
    /// </summary>
    public bool IsSelected(TItem node) => node?.Equals(_selectedNode) == true; // TODO: implement multiselect

    /// <summary>
    /// Returns true if the given node is selected
    /// </summary>
    public bool IsFocused(TItem node) => node?.Equals(LastSelectedNode) == true; 

    /// <summary>
    /// Returns true if the given node is a separator
    /// </summary>
    public virtual bool IsSeparator(TItem node) => IsSeparatorDetectFunc?.Invoke(node) == true;

    /// <summary>
    /// Creates a context for the given item
    /// </summary>
    protected virtual TreeViewItemContext<TItem> CreateContext(TItem item, string search, object tag = null)
    {
        return new TreeViewItemContext<TItem>(item, IsSelected(item), IsExpanded(item), IsFocused(item), search, this, tag, GetTermToHighlight(search));
    }


    [Parameter] public bool AutoExpand { get; set; } = true;
    [Parameter] public bool AutoCollapse { get; set; } = true;
    [Parameter] public bool AllowSelectionOfNonEmptyNodes { get; set; } = false;

    /// <summary>
    /// Here you can specify a function to determine if a node is allowed to be selected
    /// </summary>
    [Parameter] public Func<TItem, bool> AllowedToSelectFunc { get; set; }

    /// <summary>
    /// On node click
    /// </summary>
    protected virtual void NodeClick(TItem node)
    {
        if (IsSeparator(node))
            return;
        LastSelectedNode = node;
        ExecuteAutoExpand(node);
        if(IsAllowedToSelect(node))
            SelectedNode = node;
        InvokeAsync(StateHasChanged);
    }

    public virtual bool IsAllowedToSelect(TItem node)
    {
        if(AllowedToSelectFunc != null)
            return AllowedToSelectFunc(node);
        return AllowSelectionOfNonEmptyNodes || !node.HasChildren();
    }

    private void ExecuteAutoExpand(TItem node)
    {
        if (node != null && (AutoExpand || AutoCollapse))
        {
            var isExpanded = IsExpanded(node);
            if (isExpanded && AutoCollapse)
                SetExpanded(node, false);
            else if (!isExpanded && AutoExpand)
                SetExpanded(node, true);
        }
    }

    /// <summary>
    /// The current path
    /// </summary>
    public IEnumerable<TItem> Path() => LastSelectedNode != null ? LastSelectedNode.Path() : Enumerable.Empty<TItem>();

    /// <summary>
    /// Returns true if the given node is in the current path
    /// </summary>
    public bool IsInPath(TItem node) => Path()?.Contains(node) == true;


    /// <summary>
    /// Set all nodes expanded where the predicate returns true
    /// </summary>
    public void SetAllExpanded(bool expand, Func<TItem, bool> predicate = null)
    {
        //predicate ??= n => ExpandMode == ExpandMode.SingleExpand || n.Parent == null;
        predicate ??= n => n.Parent == null;
        Items?.Recursive(n => n.Children.EmptyIfNull()).Where(predicate).Apply(e => SetExpanded(e, expand));
    }

    /// <summary>
    /// Toggle the expanded state of the given node
    /// </summary>
    public void ToggleExpand(TItem node)
    {
        SetExpanded(node, !IsExpanded(node));
    }

    /// <summary>
    /// Set the expanded state of the given node
    /// </summary>
    public void SetExpanded(TItem context, bool expanded)
    {
        if (expanded && !IsExpanded(context))
            _expanded.Add(context);
        else if (!expanded && IsExpanded(context))
            _expanded.Remove(context);
    }
    
    /// <summary>
    /// Class for use in the filter box
    /// </summary>
    protected virtual string GetFilterClassStr()
    {
        return MudExCssBuilder.Default
            .AddClass("mud-ex-property-filter-full-width", FilterMode == PropertyFilterMode.AlwaysVisible)
            .ToString();
    }

    protected virtual string ItemContentClassStr(TreeViewItemContext<TItem> context)
    {
        return MudExCssBuilder.From(ItemContentClass)
            .AddClass($"mud-typography-{ItemContentTypo.GetDescription()}")
            .AddClass("trimmer")
            .ToString();
    }

    protected virtual string ItemContentStyleStr(TreeViewItemContext<TItem> context)
    {
        return MudExStyleBuilder.FromStyle(ItemContentStyle)
            //.WithWidth(100, CssUnit.Percentage)
            .WithColor(SelectedItemColor, context.IsSelected && SelectedItemColor.IsSet())
            .ToString();
    }

    protected virtual string ItemClassStr(TreeViewItemContext<TItem> context)
    {
        return MudExCssBuilder.From(ItemContentClass)
            .ToString();
    }

    protected string ItemStyleStr(TItem context) => ItemStyleStr(CreateContext(context, Filter));
    protected string ItemClassStr(TItem context) => ItemClassStr(CreateContext(context, Filter));

    protected virtual string ItemStyleStr(TreeViewItemContext<TItem> context)
    {
        return MudExStyleBuilder.FromStyle(ItemStyle)
            .WithCursor(Cursor.Pointer, IsAllowedToSelect(context.Item))
            .WithWidth(ItemWidth)
            .WithOutline(1, BorderStyle.Solid, SelectedItemBorderColor, context.IsSelected && SelectedItemBorderColor.IsSet())
            .WithBackgroundColor(SelectedItemBackgroundColor, context.IsSelected && SelectedItemBackgroundColor.IsSet())
            .WithBackgroundColor(ItemBackgroundColor, !context.IsSelected && ItemBackgroundColor.IsSet())
            .ToString();
    }

    public virtual string GetTermToHighlight(string search)
    {
        return FilterHighlighting == FilterHighlighting.ActiveFilterOnly ? _highlight ?? Filter : search;
    }

    private void OnFilterChipLeave(ChipMouseEventArgs<string> arg)
    {
        _highlight = null;
    }

    private void OnFilterChipHover(ChipMouseEventArgs<string> arg)
    {
        _highlight = arg.Value;
    }
}