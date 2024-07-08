using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Css;
using MudBlazor.Extensions.Core.Enums;
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

    /// <summary>
    /// Filter manager
    /// </summary>
    protected HierarchicalFilter<TItem> FilterManager = new();

    [Parameter] public string ToolBarStyle { get; set; }
    [Parameter] public string ToolBarClass { get; set; }
    [Parameter] public bool WrapToolBarContent {get; set; }

    /// <summary>
    /// Returns the filtered items as a collection of <see cref="TreeViewItemContext{TItem}"/>
    /// </summary>
    public IReadOnlyCollection<TreeViewItemContext<TItem>> FilteredItems
    {
        get
        {
            return FilterManager.FilteredItems().Select(n => new
            {
                Item = n,
                Search = FilterManager.GetMatchedSearch(n)
            }).Where(r => r.Search.Found).Select(res => CreateContext(res.Item, res.Search.Term)).ToHashSet();
        }
    }

    /// <summary>
    /// Gets or Sets MudExColor BackgroundColor Property.
    /// </summary>
    [Parameter]
    public MudExColor BackgroundColor { get; set; } = Color.Default;

    /// <summary>
    /// Placeholder for the filter box
    /// </summary>
    [Parameter] public string FilterPlaceholder { get; set; } = "Filter";

    /// <summary>
    /// Style for the filter box
    /// </summary>
    [Parameter] public string FilterBoxStyle { get; set; }

    /// <summary>
    /// Css class for the filter box
    /// </summary>
    [Parameter] public string FilterBoxClass { get; set; }

    /// <summary>
    /// Background color for the item
    /// </summary>
    [Parameter] public MudExColor ItemBackgroundColor { get; set; }

    /// <summary>
    /// Background color for the selected item
    /// </summary>
    [Parameter] public MudExColor SelectedItemBackgroundColor { get; set; }

    /// <summary>
    /// Foreground color for the selected item
    /// </summary>
    [Parameter] public MudExColor SelectedItemColor { get; set; } = MudExColor.Primary;

    /// <summary>
    /// Border color for the selected item
    /// </summary>
    [Parameter] public MudExColor SelectedItemBorderColor { get; set; }

    /// <summary>
    /// Width for the item
    /// </summary>
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
    /// If true the chips will be placed to the right in the input
    /// </summary>
    [Parameter] public bool? FilterChipsPlacedRight { get; set; }

    /// <summary>
    /// Set to false to disable multiple filters
    /// </summary>
    [Parameter] public bool FilterMultiple { get; set; } = true;

    /// <summary>
    /// Controls if the expand button is at the start or end 
    /// </summary>
    [Parameter] public LeftOrRight? ExpandButtonDirection { get; set; }

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
    [IgnoreOnObjectEdit]
    public IReadOnlyCollection<TItem> Items
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
    /// This function controls how a separator will be detected. Default is if the item ToString() equals '-'
    /// </summary>
    [Parameter] public Func<TItem, bool> IsSeparatorDetectFunc { get; set; } = n => n?.ToString() == "-";

    /// <summary>
    /// Here you can specify a function to determine if a node is allowed to be selected
    /// </summary>
    [Parameter] public Func<TItem, bool> AllowedToSelectFunc { get; set; }

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
    /// Additional content for the toolbar
    /// </summary>
    [Parameter] public RenderFragment ToolBarContent { get; set; }

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
    [IgnoreOnObjectEdit]
    public TItem SelectedNode
    {
        get => _selectedNode;
        set => Set(ref _selectedNode, LastSelectedNode = value, _ =>
        {
            SelectedNodeChanged.InvokeAsync(value);
            if(ExpandOnClick)
                ExpandTo(value);
        });
    }

    /// <summary>
    /// Expand the treeview to the given value
    /// </summary>
    public void ExpandTo(TItem value)
    {
        value.Path().Apply(e => SetExpanded(e, true));
    }

    /// <summary>
    /// The last selected node independent of <see cref="IsAllowedToSelect"/>
    /// </summary>
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
            if (FilterManager.Filter != value)
            {
                FilterManager.Filter = value;
                if (ExpandOnFilter)
                    SetAllExpanded(FilterManager.HasFilters, _ => true);
                FilterChanged.InvokeAsync(value);
            }
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
                
                FiltersChanged.InvokeAsync(value);
            }
        }
    }

    /// <summary>
    /// All items flatted
    /// </summary>
    public IReadOnlyCollection<TItem> FlattedItems() => FilterManager.FilteredItems().Recursive(n => n.Children ?? Enumerable.Empty<TItem>()).ToHashSet();

    /// <summary>
    /// Returns true if the given node is expanded
    /// </summary>
    public virtual bool IsExpanded(TItem node) => _expanded.Contains(node);

    /// <summary>
    /// Expand all nodes
    /// </summary>
    public virtual void ExpandAll() => _expanded = new HashSet<TItem>(Items.Recursive(n => n.Children ?? Enumerable.Empty<TItem>()));

    /// <summary>
    /// Collapse all nodes
    /// </summary>
    public virtual void CollapseAll() => _expanded.Clear();

    /// <summary>
    /// Returns true if the given node is selected
    /// </summary>
    public virtual bool IsSelected(TItem node) => node?.Equals(_selectedNode) == true; // TODO: implement multiselect

    /// <summary>
    /// Returns true if the given node is selected
    /// </summary>
    public virtual bool IsFocused(TItem node) => node?.Equals(LastSelectedNode) == true; 

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

    /// <summary>
    /// Set to true to automatically expand the node when clicked and is not expanded
    /// </summary>
    [Parameter] public bool ExpandOnClick { get; set; } = true;

    /// <summary>
    /// Set to true to automatically collapse the node when clicked and is already expanded
    /// </summary>
    [Parameter] public bool CollapseOnClick { get; set; } = true;

    /// <summary>
    /// Set to true to allow selection of nodes with children
    /// </summary>
    [Parameter] public bool AllowSelectionOfNonEmptyNodes { get; set; } = false;

    /// <summary>
    /// Set to true to allow selection of nodes with children
    /// </summary>
    [Parameter] public bool InitiallyExpanded { get; set; } = false;

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender && InitiallyExpanded)
        {
            SetAllExpanded(true);
        }
    }

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

    /// <summary>
    /// Returns true if the given node is allowed to be selected
    /// </summary>
    public virtual bool IsAllowedToSelect(TItem node)
    {
        if(AllowedToSelectFunc != null)
            return AllowedToSelectFunc(node);
        return AllowSelectionOfNonEmptyNodes || !node.HasChildren();
    }

    private void ExecuteAutoExpand(TItem node)
    {
        if (node != null && (ExpandOnClick || CollapseOnClick))
        {
            var isExpanded = IsExpanded(node);
            if (isExpanded && CollapseOnClick)
                SetExpanded(node, false);
            else if (!isExpanded && ExpandOnClick)
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
        predicate ??= n => n.HasChildren();
        Items?.Recursive(n => n.Children.EmptyIfNull()).Where(predicate).Apply(e => SetExpanded(e, expand));
    }

    /// <summary>
    /// Toggle the expanded state of the given node
    /// </summary>
    public virtual void ToggleExpand(TItem node)
    {
        SetExpanded(node, !IsExpanded(node));
    }

    /// <summary>
    /// Set the expanded state of the given node
    /// </summary>
    public virtual void SetExpanded(TItem context, bool expanded)
    {
        if (expanded && !IsExpanded(context))
            _expanded.Add(context);
        else if (!expanded && IsExpanded(context))
            _expanded.Remove(context);
        InvokeAsync(StateHasChanged);
    }
    
    /// <summary>
    /// Class for use in the filter box
    /// </summary>
    protected virtual string GetFilterClassStr()
    {
        return MudExCssBuilder.From(FilterBoxClass)
            .AddClass("mud-ex-property-filter-full-width", FilterMode == PropertyFilterMode.AlwaysVisible)
            .ToString();
    }

    /// <summary>
    /// Css class for item content
    /// </summary>
    protected virtual string ItemContentClassStr(TreeViewItemContext<TItem> context)
    {
        return MudExCssBuilder.From(ItemContentClass)
            .AddClass($"mud-typography-{ItemContentTypo.GetDescription()}")
            .AddClass("trimmer")
            .ToString();
    }

    /// <summary>
    /// Style for item content
    /// </summary>
    protected virtual string ItemContentStyleStr(TreeViewItemContext<TItem> context)
    {
        return MudExStyleBuilder.FromStyle(ItemContentStyle)
            //.WithWidth(100, CssUnit.Percentage)
            .WithColor(SelectedItemColor, context.Selected && SelectedItemColor.IsSet())
            .ToString();
    }

    /// <summary>
    /// Css class for item container
    /// </summary>
    protected virtual string ItemClassStr(TreeViewItemContext<TItem> context)
    {
        return MudExCssBuilder.From(ItemContentClass)
            .ToString();
    }

    /// <summary>
    /// Style for item container
    /// </summary>
    protected string ItemStyleStr(TItem context, string mergeWith = "") => ItemStyleStr(CreateContext(context, Filter), mergeWith);

    /// <summary>
    /// Css class for item container
    /// </summary>
    protected string ItemClassStr(TItem context) => ItemClassStr(CreateContext(context, Filter));

    /// <summary>
    /// Style for item container
    /// </summary>
    protected virtual string ItemStyleStr(TreeViewItemContext<TItem> context, string mergeWith = "")
    {
        return MudExStyleBuilder.FromStyle(ItemStyle)
            .WithCursor(Cursor.Pointer, IsAllowedToSelect(context.Value))
            .WithWidth(ItemWidth)
            .WithOutline(1, BorderStyle.Solid, SelectedItemBorderColor, context.Selected && SelectedItemBorderColor.IsSet())
            .WithBackgroundColor(SelectedItemBackgroundColor, context.Selected && SelectedItemBackgroundColor.IsSet())
            .WithBackgroundColor(ItemBackgroundColor, !context.Selected && ItemBackgroundColor.IsSet())
            .AddRaw(mergeWith, !string.IsNullOrEmpty(mergeWith))
            .ToString();
    }

    /// <summary>
    /// Style for main control
    /// </summary>
    protected string StyleStr()
    {
        return MudExStyleBuilder.FromStyle(Style)
            .WithBackground(BackgroundColor, BackgroundColor.IsSet())
            .ToString();
    }

    /// <summary>
    /// Get the term to highlight
    /// </summary>
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

    protected virtual string ToolBarClassStr()
    {
        return MudExCssBuilder.From(ToolBarClass)
            .AddClass("mud-ex-treeview-toolbar")
            .ToString();
    }

    protected virtual string ToolBarStyleStr()
    {
        return MudExStyleBuilder.FromStyle(ToolBarStyle)
            .WithHeight("auto")
            .WithPaddingLeft(0)
            .WithPaddingRight(0)
            .ToString();
    }
}