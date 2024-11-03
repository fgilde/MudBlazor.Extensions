using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Options;
using MudBlazor.Services;
using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// MudExList is a component that allows you to select an item from a list of items. It is a wrapper for MudList.
/// </summary>
public partial class MudExList<T> : IDisposable
{
    private bool _keyDownHandled;
    private string[] _keysToForwardFromSearchBox = { "ArrowUp", "ArrowDown", "Enter", "NumpadEnter" };

    /// <summary>
    /// Gets or Sets the Localizer Pattern.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string LocalizerPattern { get; set; } = "{0}";

    /// <summary>
    /// Renders the item name
    /// </summary>
    public virtual string ItemNameRender(T item)
    {
        var res = ToStringFunc != null ? ToStringFunc(item) : Converter.Set(item);
        if (!string.IsNullOrWhiteSpace(res) && !string.IsNullOrWhiteSpace(LocalizerPattern))
        {
            return LocalizerToUse != null ? LocalizerToUse[string.Format(LocalizerPattern, item)] : string.Format(LocalizerPattern, res);
        }

        return res;
    }

    #region Parameters, Fields, Injected Services

    [Inject] IKeyInterceptorFactory KeyInterceptorFactory { get; set; }
    [Inject] IScrollManager ScrollManagerExtended { get; set; }

    // Fields used in more than one place (or protected and internal ones) are shown here.
    // Others are next to the relevant parameters. (Like _selectedValue)
    private string _elementId = "list_" + Guid.NewGuid().ToString().Substring(0, 8);
    private List<MudExListItem<T>> _items = new();
    private List<MudExList<T>> _childLists = new();
    internal MudExListItem<T> LastActivatedItem;
    internal bool? AllSelected = false;

    /// <summary>
    /// Class
    /// </summary>
    protected string Classname =>
    new CssBuilder("mud-ex-list")
       .AddClass("mud-ex-list-padding", !DisablePadding)
      .AddClass(Class)
    .Build();

    /// <summary>
    /// Style
    /// </summary>
    protected string StyleStr =>
    new StyleBuilder()
        .AddStyle("max-height", $"{MaxItems * (!Dense ? 48 : 36) + (DisablePadding ? 0 : 16)}px", MaxItems != null)
        .AddStyle("overflow-y", "auto", MaxItems != null)
        .AddStyle(Style)
        .Build();

    /// <summary>
    /// Search string
    /// </summary>
    [Parameter] public string SearchString { get; set; }

    /// <summary>
    /// Reference to the Select
    /// </summary>
    [CascadingParameter, IgnoreOnObjectEdit] protected MudExSelect<T> MudExSelect { get; set; }

    /// <summary>
    /// Reference to the Autocomplete
    /// </summary>
    [CascadingParameter, IgnoreOnObjectEdit] protected MudAutocomplete<T> MudAutocomplete { get; set; }

    /// <summary>
    /// Reference to the Parent List
    /// </summary>
    [CascadingParameter, IgnoreOnObjectEdit] protected MudExList<T> ParentList { get; set; }

    /// <summary>
    /// BackgroundColor for SearchBox
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.List.Appearance)]
    public MudExColor SearchBoxBackgroundColor { get; set; } = "var(--mud-palette-background)";

    /// <summary>
    /// Func to group by items collection
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.List.Behavior)]
    public Func<T, object> GroupBy { get; set; }

    /// <summary>
    /// Set to true to enable grouping with the GroupBy func
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.List.Behavior)]
    public bool GroupingEnabled { get; set; }

    /// <summary>
    /// Sticky header for item group.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.List.Behavior)]
    public bool GroupsSticky { get; set; } = true;

    /// <summary>
    /// Set to true to use an expansion panel to nest items.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.List.Behavior)]
    public bool GroupsNested { get; set; }

    /// <summary>
    /// Sets the group's expanded state on popover opening. Works only if GroupsNested is true.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.List.Behavior)]
    public bool GroupsInitiallyExpanded { get; set; } = true;


    /// <summary>
    /// The color of the selected List Item.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.List.Appearance)]
    public MudExColor Color { get; set; } = MudBlazor.Color.Primary;

    /// <summary>
    /// Child content of component.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Optional presentation template for items
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListBehavior)]
    public RenderFragment<T> ItemTemplate { get; set; }

    /// <summary>
    /// Optional presentation template for selected items
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListBehavior)]
    public RenderFragment<T> ItemSelectedTemplate { get; set; }

    /// <summary>
    /// Optional presentation template for disabled items
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListBehavior)]
    public RenderFragment<T> ItemDisabledTemplate { get; set; }

    /// <summary>
    /// Optional presentation template for select all item
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListBehavior)]
    public RenderFragment SelectAllTemplate { get; set; }

    /// <summary>
    /// Converter for the value. If not set, the default converter is used.
    /// </summary>
    [Parameter, IgnoreOnObjectEdit]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public DefaultConverter<T> Converter { get; set; } = new();

    private IEqualityComparer<T> _comparer;

    /// <summary>
    /// Comparer for the value. If not set, the default comparer is used.
    /// </summary>
    [Parameter, IgnoreOnObjectEdit]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public IEqualityComparer<T> Comparer
    {
        get => _comparer;
        set
        {
            if (Equals(_comparer, value))
                return;
            _comparer = value;
            // Apply comparer and refresh selected values
            if (_selectedValues == null)
            {
                return;
            }
            _selectedValues = new HashSet<T>(_selectedValues, _comparer);
            SelectedValues = _selectedValues;
        }
    }

    private Func<T, string> _toStringFunc = x => x?.ToString();
    /// <summary>
    /// Defines how values are displayed in the drop-down list
    /// </summary>
    [Parameter, IgnoreOnObjectEdit]
    [SafeCategory(CategoryTypes.FormComponent.ListBehavior)]
    public Func<T, string> ToStringFunc
    {
        get => _toStringFunc;
        set
        {
            if (_toStringFunc == value)
                return;
            _toStringFunc = value;
            Converter = new DefaultConverter<T>
            {
                SetFunc = _toStringFunc ?? (x => x?.ToString()),
            };
        }
    }

    /// <summary>
    /// Predefined enumerable items. If it's not null, creates list items automatically.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public ICollection<T> ItemCollection { get; set; } = null;

    /// <summary>
    /// Custom search func for search box. If it doesn't set, it searches with "Contains" logic by default. Passed value and searchString values.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListBehavior)]
    public Func<T, string, bool> SearchFunc { get; set; }

    /// <summary>
    /// If true, shows a search box for filtering items. Only works with ItemCollection approach.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public bool SearchBox { get; set; }

    /// <summary>
    /// Search box text field variant.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public Variant SearchBoxVariant { get; set; } = Variant.Text;

    /// <summary>
    /// Search box icon position.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public Adornment SearchBoxAdornment { get; set; } = Adornment.End;

    /// <summary>
    /// If true, the search-box will be focused when the dropdown is opened.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public bool SearchBoxAutoFocus { get; set; } = true;

    /// <summary>
    /// If true, the search-box has a clear icon.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public bool SearchBoxClearable { get; set; } = true;

    /// <summary>
    /// SearchBox's CSS classes, separated by space.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public string ClassSearchBox { get; set; }

    /// <summary>
    /// Placeholder for the search box.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public string SearchBoxPlaceholder { get; set; }

    /// <summary>
    /// Allows virtualization. Only work if ItemCollection parameter is not null.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public bool Virtualize { get; set; }

    /// <summary>
    /// Set max items to show in list. Other items can be scrolled. Works if list items populated with ItemCollection parameter.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public int? MaxItems { get; set; } = null;

    /// <summary>
    /// Over scan value for Virtualized list. Default is 2.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public int OverscanCount { get; set; } = 2;

    private bool _multiSelection = false;
    /// <summary>
    /// Allows multi selection and adds MultiSelectionComponent for each list item.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public bool MultiSelection
    {
        get => _multiSelection;

        set
        {
            if (ParentList != null)
            {
                _multiSelection = ParentList.MultiSelection;
                return;
            }
            if (_multiSelection == value)
            {
                return;
            }
            _multiSelection = value;
            if (!_setParametersDone)
            {
                return;
            }
            if (!_multiSelection)
            {
                if (!_centralCommanderIsProcessing)
                {
                    HandleCentralValueCommander("MultiSelectionOff");
                }

                UpdateSelectedStyles();
            }
        }
    }

    /// <summary>
    /// The MultiSelectionComponent's placement. Accepts Align.Start and Align.End
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public Align MultiSelectionAlign { get; set; } = Align.Start;

    /// <summary>
    /// The component which shows as a MultiSelection check.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public MultiSelectionComponent MultiSelectionComponent { get; set; } = MultiSelectionComponent.CheckBox;

    /// <summary>
    /// Set true to make the list items clickable. This is also the precondition for list selection to work.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Selecting)]
    public bool Clickable { get; set; }

    /// <summary>
    /// If true the active (highlighted) item select on tab key. Designed for only single selection. Default is true.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Selecting)]
    public bool SelectValueOnTab { get; set; } = true;

    /// <summary>
    /// If true, vertical padding will be removed from the list.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool DisablePadding { get; set; }

    /// <summary>
    /// If true, selected items doesn't have a selected background color.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool DisableSelectedItemStyle { get; set; }

    /// <summary>
    /// If true, compact vertical padding will be applied to all list items.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool Dense { get; set; }

    /// <summary>
    /// If true, the left and right padding is removed on all list items.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool Gutters { get; set; } = true;

    /// <summary>
    /// If true, will disable the list item if it has onclick.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public bool Disabled { get; set; }

    /// <summary>
    /// If set to true and the MultiSelection option is set to true, a "select all" checkbox is added at the top of the list of items.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListBehavior)]
    public bool SelectAll { get; set; }

    /// <summary>
    /// Sets position of the Select All checkbox
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public SelectAllPosition SelectAllPosition { get; set; } = SelectAllPosition.BeforeSearchBox;

    /// <summary>
    /// Define the text of the Select All option.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public string SelectAllText { get; set; } = "Select All";

    /// <summary>
    /// If true, change background color to secondary for all nested item headers.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool SecondaryBackgroundForNestedItemHeader { get; set; }

    /// <summary>
    /// Fired on the KeyDown event.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>
    /// Fired on the OnFocusOut event.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// Fired on the OnDoubleClick event.
    /// </summary>
    [Parameter] public EventCallback<ListItemClickEventArgs<T>> OnDoubleClick { get; set; }

    #endregion


    #region Values & Items (Core: Be careful if you change something inside the region, it affects all logic and also Select and Autocomplete)

    bool _centralCommanderIsProcessing = false;
    bool _centralCommanderResultRendered = false;

    /// <summary>
    /// CentralCommander has a simple aim: Prevent racing conditions. It has two mechanism to do this:
    /// (1) When this method is running, it doesn't allow to run a second one. That guarantees to different value parameters can not call this method at the same time.
    /// (2) When this method runs once, prevents all value setters until OnAfterRender runs. That guarantees to have proper values.
    /// </summary>
    /// <param name="changedValueType"></param>
    /// <param name="updateStyles"></param>
    protected void HandleCentralValueCommander(string changedValueType, bool updateStyles = true)
    {
        if (!_setParametersDone || !IsFullyRendered) // TODO: Check this
        {
            return;
        }
        if (_centralCommanderIsProcessing)
        {
            return;
        }
        _centralCommanderIsProcessing = true;

        if (changedValueType == nameof(SelectedValue))
        {
            if (!MultiSelection)
            {
                SelectedValues = new HashSet<T>(_comparer) { SelectedValue };
                UpdateSelectedItem();
            }
        }
        else if (changedValueType == nameof(SelectedValues))
        {
            if (MultiSelection)
            {
                SelectedValue = SelectedValues == null ? default(T) : SelectedValues.LastOrDefault();
                UpdateSelectedItem();
            }
        }
        else if (changedValueType == nameof(SelectedItem))
        {
            if (!MultiSelection)
            {
                SelectedItems = new HashSet<MudExListItem<T>>() { SelectedItem };
                UpdateSelectedValue();
            }
        }
        else if (changedValueType == nameof(SelectedItems))
        {
            if (MultiSelection)
            {
                SelectedItem = SelectedItems?.LastOrDefault();
                UpdateSelectedValue();
            }
        }
        else if (changedValueType == "MultiSelectionOff")
        {
            SelectedValue = SelectedValues == null ? default(T) : SelectedValues.FirstOrDefault();
            SelectedValues = SelectedValue == null ? null : new HashSet<T>(_comparer) { SelectedValue };
            UpdateSelectedItem();
        }

        _centralCommanderResultRendered = false;
        _centralCommanderIsProcessing = false;
        if (updateStyles)
        {
            UpdateSelectedStyles();
        }
    }

    /// <summary>
    /// Update selected.
    /// </summary>
    protected internal void UpdateSelectedItem()
    {
        var items = CollectAllMudListItems(true);

        if (MultiSelection && (SelectedValues == null || !SelectedValues.Any()))
        {
            SelectedItem = null;
            SelectedItems = null;
            return;
        }

        SelectedItem = items.FirstOrDefault(x => SelectedValue == null ? x.Value == null : Comparer?.Equals(x.Value, SelectedValue) ?? x.Value.Equals(SelectedValue));
        SelectedItems = SelectedValues == null ? null : items.Where(x => SelectedValues.Contains(x.Value, _comparer));
    }

    /// <summary>
    /// Update selected value
    /// </summary>
    protected internal void UpdateSelectedValue()
    {
        if (!MultiSelection && SelectedItem == null)
        {
            SelectedValue = default(T);
            SelectedValues = null;
            return;
        }

        SelectedValue = SelectedItem == null ? default(T) : SelectedItem.Value;
        SelectedValues = SelectedItems?.Select(x => x.Value).ToHashSet(_comparer);
    }

    private T _selectedValue;

    /// <summary>
    /// The current selected value.
    /// Note: Make the list Clickable or set MultiSelection true for item selection to work.
    /// </summary>
    [Parameter, IgnoreOnObjectEdit]
    [SafeCategory(CategoryTypes.List.Selecting)]
    public T SelectedValue
    {
        get => _selectedValue;
        set
        {
            if (Converter.Set(_selectedValue) != Converter.Set(default(T)) && !_firstRendered)
            {
                return;
            }
            if (!_centralCommanderResultRendered && _firstRendered)
            {
                return;
            }
            if (ParentList != null)
            {
                return;
            }
            if ((_selectedValue != null && value != null && _selectedValue.Equals(value)) || (_selectedValue == null && value == null))
            {
                return;
            }

            _selectedValue = value;
            HandleCentralValueCommander(nameof(SelectedValue));
            _= SelectedValueChanged.InvokeAsync(_selectedValue);
        }
    }

    private HashSet<T> _selectedValues;
    /// <summary>
    /// The current selected values. Holds single value (SelectedValue) if MultiSelection is false.
    /// </summary>
    [Parameter, IgnoreOnObjectEdit]
    [SafeCategory(CategoryTypes.List.Selecting)]
    public IEnumerable<T> SelectedValues
    {
        get => _selectedValues;

        set
        {
            if (value == null && !_firstRendered)
            {
                return;
            }
            if (!_centralCommanderResultRendered && _firstRendered)
            {
                return;
            }
            if (ParentList != null)
            {
                return;
            }
            //var set = value ?? new List<T>();
            if (value == null && _selectedValues == null)
            {
                return;
            }

            if (value != null && _selectedValues != null && _selectedValues.SetEquals(value))
            {
                return;
            }
            // This return condition(s) can be discussed. It is also important when we add experimental select, because commenting one more return condition causes infinite loops.
            //if (SelectedValues.Count() == set.Count() && _selectedValues != null && _selectedValues.All(x => set.Contains(x)))
            //{
            //    return;
            //}

            _selectedValues = value == null ? null : new HashSet<T>(value, _comparer);
            if (!_setParametersDone)
            {
                return;
            }
            HandleCentralValueCommander(nameof(SelectedValues));
            SelectedValuesChanged.InvokeAsync(SelectedValues == null ? null : new HashSet<T>(SelectedValues, _comparer));
        }
    }

    private MudExListItem<T> _selectedItem;
    /// <summary>
    /// The current selected list item.
    /// Note: make the list Clickable or MultiSelection or both for item selection to work.
    /// </summary>
    [Parameter, IgnoreOnObjectEdit]
    [SafeCategory(CategoryTypes.List.Selecting)]
    public MudExListItem<T> SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!_centralCommanderResultRendered && _firstRendered)
            {
                return;
            }
            if (_selectedItem == value)
                return;

            _selectedItem = value;
            if (!_setParametersDone)
            {
                return;
            }
            HandleCentralValueCommander(nameof(SelectedItem));
            _ = SelectedItemChanged.InvokeAsync(_selectedItem);
        }
    }

    private HashSet<MudExListItem<T>> _selectedItems;
    /// <summary>
    /// The current selected list items.
    /// Note: make the list Clickable for item selection to work.
    /// </summary>
    [Parameter, IgnoreOnObjectEdit]
    [SafeCategory(CategoryTypes.List.Selecting)]
    public IEnumerable<MudExListItem<T>> SelectedItems
    {
        get => _selectedItems;
        set
        {
            if (!_centralCommanderResultRendered && _firstRendered)
            {
                return;
            }

            if (value == null && _selectedItems == null)
            {
                return;
            }

            if (value != null && _selectedItems != null && _selectedItems.SetEquals(value))
                return;

            _selectedItems = value?.ToHashSet();
            if (!_setParametersDone)
            {
                return;
            }
            HandleCentralValueCommander(nameof(SelectedItems));
            _ = SelectedItemsChanged.InvokeAsync(_selectedItems);
        }
    }

    /// <summary>
    /// Called whenever the selection changed. Can also be called even MultiSelection is true.
    /// </summary>
    [Parameter] public EventCallback<T> SelectedValueChanged { get; set; }

    /// <summary>
    /// Called whenever selected values changes. Can also be called even MultiSelection is false.
    /// </summary>
    [Parameter] public EventCallback<IEnumerable<T>> SelectedValuesChanged { get; set; }

    /// <summary>
    /// Called whenever the selected item changed. Can also be called even MultiSelection is true.
    /// </summary>
    [Parameter] public EventCallback<MudExListItem<T>> SelectedItemChanged { get; set; }

    /// <summary>
    /// Called whenever the selected items changed. Can also be called even MultiSelection is false.
    /// </summary>
    [Parameter] public EventCallback<IEnumerable<MudExListItem<T>>> SelectedItemsChanged { get; set; }

    /// <summary>
    /// Get all MudListItems in the list.
    /// </summary>
    public List<MudExListItem<T>> GetAllItems()
    {
        return CollectAllMudListItems();
    }

    /// <summary>
    /// Get all items that holds value.
    /// </summary>
    public List<MudExListItem<T>> GetItems()
    {
        return CollectAllMudListItems(true);
    }

    #endregion


    #region Lifecycle Methods, Dispose & Register

    bool _setParametersDone = false;

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (_centralCommanderIsProcessing)
        {
            return Task.CompletedTask;
        }

        if (MudExSelect != null || MudAutocomplete != null)
        {
            return Task.CompletedTask;
        }

        _ = base.SetParametersAsync(parameters);

        _setParametersDone = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (ParentList != null)
        {
            ParentList.Register(this);
        }
    }

    internal event Action ParametersChanged;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ParametersChanged?.Invoke();
    }

    private IKeyInterceptor _keyInterceptor;
    private bool _firstRendered = false;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            _firstRendered = false;
            _keyInterceptor = KeyInterceptorFactory.Create();

            await _keyInterceptor.Connect(_elementId, new KeyInterceptorOptions()
            {
                //EnableLogging = true,
                TargetClass = "mud-ex-list-item",
                Keys = {
                        new KeyOptions { Key=" ", PreventDown = "key+none" }, //prevent scrolling page, toggle open/close
                        new KeyOptions { Key="ArrowUp", PreventDown = "key+none" }, // prevent scrolling page, instead highlight previous item
                        new KeyOptions { Key="ArrowDown", PreventDown = "key+none" }, // prevent scrolling page, instead highlight next item
                        new KeyOptions { Key="Home", PreventDown = "key+none" },
                        new KeyOptions { Key="End", PreventDown = "key+none" },
                        new KeyOptions { Key="Escape" },
                        new KeyOptions { Key="Enter", PreventDown = "key+none" },
                        new KeyOptions { Key="NumpadEnter", PreventDown = "key+none" },
                        new KeyOptions { Key="a", PreventDown = "key+ctrl" }, // select all items instead of all page text
                        new KeyOptions { Key="A", PreventDown = "key+ctrl" }, // select all items instead of all page text
                        new KeyOptions { Key="/./", SubscribeDown = true, SubscribeUp = true }, // for our users
                    },
            });

            if (MudExSelect == null && MudAutocomplete == null)
            {
                if (!MultiSelection && SelectedValue != null)
                {
                    HandleCentralValueCommander(nameof(SelectedValue));
                }
                else if (MultiSelection && SelectedValues != null)
                {
                    HandleCentralValueCommander(nameof(SelectedValues));
                }
            }

            if (MudExSelect != null || MudAutocomplete != null)
            {
                if (MultiSelection)
                {
                    UpdateSelectAllState();
                    if (MudExSelect != null)
                    {
                        SelectedValues = MudExSelect.SelectedValues;
                    }
                    else if (MudAutocomplete != null)
                    {
                        // Uncomment on Autocomplete Phase. Currently autocomplete doesn't have "SelectedValues".
                        //SelectedValues = MudAutocomplete.SelectedValues;
                    }
                    HandleCentralValueCommander(nameof(SelectedValues));
                }
                else
                {
                    // These updated style method cause to fail some tests after adding select phase. Can be discussed later.
                    //UpdateSelectedStyles();
                    UpdateLastActivatedItem(SelectedValue);
                }
            }
            if (SelectedValues != null)
            {
                UpdateLastActivatedItem(SelectedValues.LastOrDefault());
            }
            if (LastActivatedItem != null && !(MultiSelection && AllSelected == true))
            {
                await ScrollToMiddleAsync(LastActivatedItem);
            }
            _firstRendered = true;
        }

        _centralCommanderResultRendered = true;
    }

    /// <summary>
    /// Dispose the component.
    /// </summary>
    public void Dispose()
    {
        ParametersChanged = null;
        ParentList?.Unregister(this);
    }

    /// <summary>
    /// Register list item.
    /// </summary>
    protected internal void Register(MudExListItem<T> item)
    {
        _items.Add(item);
        if (SelectedValue != null && Equals(item.Value, SelectedValue))
        {
            item.SetSelected(true);
            //TODO check if item is the selectable for a nested list, and deselect this.
            //SelectedItem = item;
            //SelectedItemChanged.InvokeAsync(item);
        }

        if (MultiSelection && SelectedValues != null && SelectedValues.Contains(item.Value))
        {
            item.SetSelected(true);
        }
    }

    /// <summary>
    /// Unregister list item.
    /// </summary>
    protected internal void Unregister(MudExListItem<T> item)
    {
        _items.Remove(item);
    }

    /// <summary>
    /// Register child list.
    /// </summary>
    protected internal void Register(MudExList<T> child)
    {
        _childLists.Add(child);
    }

    /// <summary>
    /// Unregister child list.
    /// </summary>
    protected internal void Unregister(MudExList<T> child)
    {
        _childLists.Remove(child);
    }

    #endregion


    #region Events (Key, Focus)

    /// <summary>
    /// Key down handler for search box.
    /// </summary>
    protected internal async Task SearchBoxHandleKeyDown(KeyboardEventArgs obj)
    {
        _keyDownHandled = false;
        if (Disabled || (!Clickable && !MultiSelection))
            return;
        
        if (obj.Key == "Escape")
        {
            if (string.IsNullOrEmpty(SearchString))
            {
                if (MudExSelect != null)
                {
                    await _searchField.BlurAsync();
                    MudExSelect.HandleKeyDown(obj);
                }
            }
            else
            {
                SearchString = string.Empty;
            }
            _keyDownHandled = true;
        }

        if (!_keyDownHandled && _keysToForwardFromSearchBox.Contains(obj.Key))
            _keyDownHandled = true;

        if (_keyDownHandled)
        {
            //MudExSelect?.HandleKeyDown(obj);
            await HandleKeyDown(obj);
        }
    }

    MudBaseInput<string> _searchField;

    /// <summary>
    /// Search field.
    /// </summary>
    public MudBaseInput<string> SearchField => _searchField;

    /// <summary>
    /// Key down handler
    /// </summary>
    protected internal async Task HandleKeyDown(KeyboardEventArgs obj)
    {
        if (Disabled || (!Clickable && !MultiSelection))
            return;
        if (ParentList != null)
        {
            //await ParentList.HandleKeyDown(obj);
            return;
        }

        var key = obj.Key.ToLowerInvariant();
        if (key.Length == 1 && key != " " && !(obj.CtrlKey || obj.ShiftKey || obj.AltKey || obj.MetaKey))
        {
            await ActiveFirstItem(key);
            return;
        }
        switch (obj.Key)
        {
            case "Tab":
                if (!MultiSelection && SelectValueOnTab)
                {
                    SetSelectedValue(LastActivatedItem);
                }
                break;
            case "ArrowUp":
                await ActiveAdjacentItem(-1);
                break;
            case "ArrowDown":
                await ActiveAdjacentItem(1);
                break;
            case "Home":
                await ActiveFirstItem();
                break;
            case "End":
                await ActiveLastItem();
                break;
            case "Enter":
            case "NumpadEnter":
                if (LastActivatedItem == null)
                {
                    return;
                }
                SetSelectedValue(LastActivatedItem);
                break;
            case "a":
            case "A":
                if (obj.CtrlKey)
                {
                    if (MultiSelection)
                    {
                        SelectAllItems(AllSelected);
                    }
                }
                break;
            case "f":
            case "F":
                if (obj.CtrlKey && obj.ShiftKey)
                {
                    SearchBox = !SearchBox;
                    StateHasChanged();
                }
                break;
        }
        await OnKeyDown.InvokeAsync(obj);
    }

    /// <summary>
    /// Handler for focus leave.
    /// </summary>
    protected async Task HandleOnFocusOut()
    {
        DeactiveAllItems();
        await OnFocusOut.InvokeAsync();
    }

    /// <summary>
    /// Scroll handler.
    /// </summary>
    protected void HandleOnScroll()
    {
        if (Virtualize)
        {
            UpdateSelectedStyles();
        }
    }

    #endregion


    #region Select

    /// <summary>
    /// Set selected value.
    /// </summary>
    protected internal void SetSelectedValue(T value, bool force = false)
    {
        if ((!Clickable && !MultiSelection) && !force)
            return;

        //Make sure its the most parent one before continue method.
        if (ParentList != null)
        {
            ParentList?.SetSelectedValue(value);
            return;
        }

        if (!MultiSelection)
        {
            SelectedValue = value;
        }
        else
        {
            if (SelectedValues.Contains(value, _comparer))
            {
                SelectedValues = SelectedValues?.Where(x => Comparer != null ? !Comparer.Equals(x, value) : !x.Equals(value)).ToHashSet(_comparer);
            }
            else
            {
                SelectedValues = SelectedValues.Append(value).ToHashSet(_comparer);
            }
        }
        UpdateLastActivatedItem(value);
    }

    /// <summary>
    /// Set the selected value.
    /// </summary>
    protected internal void SetSelectedValue(MudExListItem<T> item, bool force = false)
    {
        if (item == null || ((!Clickable && !MultiSelection) && !force))
            return;

        //Make sure it's the most parent one before continue method
        if (ParentList != null)
        {
            ParentList?.SetSelectedValue(item);
            return;
        }

        if (!MultiSelection)
        {
            SelectedValue = item.Value;
        }
        else
        {
            SelectedValues = item.IsSelected
                ?
                SelectedValues?.Where(x => Comparer != null ? !Comparer.Equals(x, item.Value) : !x.Equals(item.Value))
                : SelectedValues == null
                    ? new HashSet<T>(_comparer) { item.Value }
                    : SelectedValues.Append(item.Value).ToHashSet(_comparer);
        }

        UpdateSelectAllState();
        LastActivatedItem = item;
    }

    /// <summary>
    /// Update selected styles.
    /// </summary>
    protected internal void UpdateSelectedStyles(bool deselectFirst = true, bool update = true)
    {
        var items = CollectAllMudListItems(true);
        if (deselectFirst)
        {
            DeselectAllItems(items);
        }

        if (!IsSelectable())
        {
            return;
        }

        if (!MultiSelection)
        {
            items.FirstOrDefault(x => SelectedValue == null ? x.Value == null : SelectedValue.Equals(x == null ? null : x.Value))?.SetSelected(true);
        }
        else if (SelectedValues != null)
        {
            items.Where(x => SelectedValues.Contains(x.Value, Comparer == null ? null : Comparer)).ToList().ForEach(x => x.SetSelected(true));
        }

        if (update)
        {
            StateHasChanged();
        }
    }

    /// <summary>
    /// Returns true if the item is selectable.
    /// </summary>
    /// <returns></returns>
    protected bool IsSelectable() => Clickable || MultiSelection;

    /// <summary>
    /// Unselect all items.
    /// </summary>
    /// <param name="items"></param>
    protected void DeselectAllItems(List<MudExListItem<T>> items)
    {
        foreach (var listItem in items)
            listItem?.SetSelected(false);
    }

    /// <summary>
    /// Collects all MudListItems.
    /// </summary>
    protected List<MudExListItem<T>> CollectAllMudListItems(bool exceptNestedAndExceptional = false)
    {
        var items = new List<MudExListItem<T>>();

        if (ParentList != null)
        {
            items.AddRange(ParentList._items);
            foreach (var list in ParentList._childLists)
                items.AddRange(list._items);
        }
        else
        {
            items.AddRange(_items);
            foreach (var list in _childLists)
                items.AddRange(list._items);
        }

        if (!exceptNestedAndExceptional)
        {
            return items;
        }
        else
        {
            return items.Where(x => x.NestedList == null && !x.IsFunctional).ToList();
        }
    }

    #endregion


    #region SelectAll

    /// <summary>
    /// Update select all state.
    /// </summary>
    protected internal void UpdateSelectAllState()
    {
        if (MultiSelection)
        {
            if (_selectedValues == null || !_selectedValues.Any())
            {
                AllSelected = false;
            }
            else if (ItemCollection != null && ItemCollection.Count == _selectedValues.Count)
            {
                AllSelected = true;
            }
            else if (ItemCollection == null && CollectAllMudListItems(true).Count() == _selectedValues.Count)
            {
                AllSelected = true;
            }
            else
            {
                AllSelected = null;
            }
        }
    }

    /// <summary>
    /// Icon for select all checkbox.
    /// </summary>
    protected string SelectAllCheckBoxIcon => AllSelected.HasValue ? AllSelected.Value ? CheckedIcon : UncheckedIcon : IndeterminateIcon;

    /// <summary>
    /// Custom checked icon.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public string CheckedIcon { get; set; } = Icons.Material.Filled.CheckBox;

    /// <summary>
    /// Custom unchecked icon.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public string UncheckedIcon { get; set; } = Icons.Material.Filled.CheckBoxOutlineBlank;

    /// <summary>
    /// Custom indeterminate icon.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public string IndeterminateIcon { get; set; } = Icons.Material.Filled.IndeterminateCheckBox;

    /// <summary>
    /// Select all items.
    /// </summary>
    protected void SelectAllItems(bool? deselect = false)
    {
        var items = CollectAllMudListItems(true);
        if (deselect == true)
        {
            foreach (var item in items.Where(item => item.IsSelected))
                item.SetSelected(false, returnIfDisabled: true);

            AllSelected = false;
        }
        else
        {
            foreach (var item in items.Where(item => !item.IsSelected))
                item.SetSelected(true, returnIfDisabled: true);

            AllSelected = true;
        }

        SelectedValues = ItemCollection != null
            ? deselect == true ? Enumerable.Empty<T>() : ItemCollection.ToHashSet(_comparer)
            : items.Where(x => x.IsSelected).Select(y => y.Value).ToHashSet(_comparer);

        if (MudExSelect != null)
            _ = MudExSelect.BeginValidatePublic();
    }

    #endregion


    #region Active (highlight)

    /// <summary>
    /// Returns the index of the active item.
    /// </summary>
    protected int GetActiveItemIndex()
    {
        var items = CollectAllMudListItems(true);
        if (LastActivatedItem == null)
        {
            var a = items.FindIndex(x => x.IsActive);
            return a;
        }
        else
        {
            var a = items.FindIndex(x => LastActivatedItem.Value == null ? x.Value == null : Comparer != null ? Comparer.Equals(LastActivatedItem.Value, x.Value) : LastActivatedItem.Value.Equals(x.Value));
            return a;
        }
    }

    /// <summary>
    /// returns the value of the active item.
    /// </summary>
    protected T GetActiveItemValue()
    {
        var items = CollectAllMudListItems(true);
        return LastActivatedItem == null ? items.FirstOrDefault(x => x.IsActive).Value : LastActivatedItem.Value;
    }

    /// <summary>
    /// Update last activated item.
    /// </summary>
    protected internal void UpdateLastActivatedItem(T value)
    {
        var items = CollectAllMudListItems(true);
        LastActivatedItem = items.FirstOrDefault(x => value == null ? x.Value == null : Comparer != null ? Comparer.Equals(value, x.Value) : value.Equals(x.Value));
    }

    /// <summary>
    /// Deactivate all items.
    /// </summary>
    protected void DeactiveAllItems(List<MudExListItem<T>> items = null)
    {
        items ??= CollectAllMudListItems(true);

        foreach (var item in items)
        {
            item.SetActive(false);
        }
    }

#pragma warning disable BL0005

    /// <summary>
    /// Active first item.
    /// </summary>
    public async Task ActiveFirstItem(string startChar = null)
    {
        var items = CollectAllMudListItems(true);
        if (items == null || items.Count == 0 || items[0].Disabled)
        {
            return;
        }
        DeactiveAllItems(items);

        if (string.IsNullOrWhiteSpace(startChar))
        {
            items[0].SetActive(true);
            LastActivatedItem = items[0];
            if (items[0].ParentListItem != null && !items[0].ParentListItem.Expanded)
            {
                items[0].ParentListItem.Expanded = true;
            }
            await ScrollToMiddleAsync(items[0]);
            return;
        }

        // find first item that starts with the letter
        var possibleItems = items.Where(x => (x.Text ?? Converter.Set(x.Value) ?? "").StartsWith(startChar, StringComparison.CurrentCultureIgnoreCase)).ToList();
        if (!possibleItems.Any())
        {
            if (LastActivatedItem == null)
            {
                return;
            }
            LastActivatedItem.SetActive(true);
            if (LastActivatedItem.ParentListItem != null && !LastActivatedItem.ParentListItem.Expanded)
            {
                LastActivatedItem.ParentListItem.Expanded = true;
            }
            await ScrollToMiddleAsync(LastActivatedItem);
            return;
        }

        var theItem = possibleItems.FirstOrDefault(x => x == LastActivatedItem);
        if (theItem == null)
        {
            possibleItems[0].SetActive(true);
            LastActivatedItem = possibleItems[0];
            if (LastActivatedItem.ParentListItem != null && !LastActivatedItem.ParentListItem.Expanded)
            {
                LastActivatedItem.ParentListItem.Expanded = true;
            }
            await ScrollToMiddleAsync(possibleItems[0]);
            return;
        }

        if (theItem == possibleItems.LastOrDefault())
        {
            possibleItems[0].SetActive(true);
            LastActivatedItem = possibleItems[0];
            if (LastActivatedItem.ParentListItem != null && !LastActivatedItem.ParentListItem.Expanded)
            {
                LastActivatedItem.ParentListItem.Expanded = true;
            }
            await ScrollToMiddleAsync(possibleItems[0]);
        }
        else
        {
            var item = possibleItems[possibleItems.IndexOf(theItem) + 1];
            item.SetActive(true);
            LastActivatedItem = item;
            if (LastActivatedItem.ParentListItem != null && !LastActivatedItem.ParentListItem.Expanded)
            {
                LastActivatedItem.ParentListItem.Expanded = true;
            }
            await ScrollToMiddleAsync(item);
        }
    }

    /// <summary>
    /// Active adjacent item.
    /// </summary>
    public async Task ActiveAdjacentItem(int changeCount)
    {
        var items = CollectAllMudListItems(true);
        if (items == null || items.Count == 0)
        {
            return;
        }
        int index = GetActiveItemIndex();
        if (index + changeCount >= items.Count || 0 > index + changeCount)
        {
            return;
        }
        if (items[index + changeCount].Disabled)
        {
            // Recursive
            await ActiveAdjacentItem(changeCount > 0 ? changeCount + 1 : changeCount - 1);
            return;
        }
        DeactiveAllItems(items);
        items[index + changeCount].SetActive(true);
        LastActivatedItem = items[index + changeCount];

        if (items[index + changeCount].ParentListItem != null && !items[index + changeCount].ParentListItem.Expanded)
        {
            items[index + changeCount].ParentListItem.Expanded = true;
        }

        await ScrollToMiddleAsync(items[index + changeCount]);
    }

    /// <summary>
    /// Active previous item.
    /// </summary>
    public async Task ActivePreviousItem()
    {
        var items = CollectAllMudListItems(true);
        if (items == null || items.Count == 0)
        {
            return;
        }
        int index = GetActiveItemIndex();
        if (0 > index - 1)
        {
            return;
        }
        DeactiveAllItems(items);
        items[index - 1].SetActive(true);
        LastActivatedItem = items[index - 1];

        if (items[index - 1].ParentListItem != null && !items[index - 1].ParentListItem.Expanded)
        {
            items[index - 1].ParentListItem.Expanded = true;
        }

        await ScrollToMiddleAsync(items[index - 1]);
    }

    /// <summary>
    /// Active last item.
    /// </summary>
    public async Task ActiveLastItem()
    {
        var items = CollectAllMudListItems(true);
        if (items == null || items.Count == 0)
        {
            return;
        }
        var properLastIndex = items.Count - 1;
        DeactiveAllItems(items);
        for (int i = 0; i < items.Count; i++)
        {
            if (!items[properLastIndex - i].Disabled)
            {
                properLastIndex -= i;
                break;
            }
        }
        items[properLastIndex].SetActive(true);
        LastActivatedItem = items[properLastIndex];

        if (items[properLastIndex].ParentListItem != null && !items[properLastIndex].ParentListItem.Expanded)
        {
            items[properLastIndex].ParentListItem.Expanded = true;
        }

        await ScrollToMiddleAsync(items[properLastIndex]);
    }
#pragma warning restore BL0005

    #endregion


    #region Others (Clear, Scroll, Search)

    /// <summary>
    /// Clears value(s) and item(s) and deactivates all items.
    /// </summary>
    public void Clear()
    {
        var items = CollectAllMudListItems();
        if (!MultiSelection)
        {
            SelectedValue = default;
        }
        else
        {
            SelectedValues = null;
        }

        DeselectAllItems(items);
        DeactiveAllItems();
        UpdateSelectAllState();
    }

    /// <summary>
    /// Scroll to middle.
    /// </summary>
    protected internal ValueTask ScrollToMiddleAsync(MudExListItem<T> item)
    {
        return ScrollManagerExtended.ScrollIntoViewAsync($"#{item.ItemId}", ScrollBehavior.Auto);
    }

    /// <summary>
    /// Returns all searched items.
    /// </summary>
    protected ICollection<T> GetSearchedItems() =>
        !SearchBox || ItemCollection == null || SearchString == null ? ItemCollection :
        SearchFunc != null ? ItemCollection.Where(x => SearchFunc.Invoke(x, SearchString)).ToList() :
        ItemCollection
            .Where(x => Converter.Set(x).Contains(SearchString, StringComparison.InvariantCultureIgnoreCase))
            .ToList();

    /// <summary>
    /// Force update.
    /// </summary>
    public async Task ForceUpdate()
    {
        await Task.Delay(1);
        UpdateSelectedStyles();
    }

    /// <summary>
    /// Force update items.
    /// </summary>
    public void ForceUpdateItems()
    {
        var items = GetAllItems();
        SelectedItem = items.FirstOrDefault(x => x.Value != null && x.Value.Equals(SelectedValue));
        SelectedItems = items.Where((x => x.Value != null && SelectedValues.Contains(x.Value)));
    }

    /// <summary>
    /// Handler for double click.
    /// </summary>
    protected Task OnDoubleClickHandler(MouseEventArgs args, T itemValue) => OnDoubleClick.InvokeAsync(new ListItemClickEventArgs<T> { MouseEventArgs = args, ItemValue = itemValue });

    #endregion

    private MudExSize<double> GetStickyTop()
    {
        if (!SearchBox)
            return -8;
        var result = -8;
        if (SearchBox)
        {
            result += 90;
            if (SearchBoxVariant is Variant.Outlined or Variant.Filled)
                result += 19;

            if (SelectAll && SelectAllPosition == SelectAllPosition.AfterSearchBox)
                result += 50;

        }

        return result - 23;
    }
}

/// <summary>
/// Arguments for list item click event.
/// </summary>
public class ListItemClickEventArgs<T>
{
    /// <summary>
    /// Mouse event arguments.
    /// </summary>
    public MouseEventArgs MouseEventArgs { get; set; }

    /// <summary>
    /// Value of the item.
    /// </summary>
    public T ItemValue { get; set; }
}