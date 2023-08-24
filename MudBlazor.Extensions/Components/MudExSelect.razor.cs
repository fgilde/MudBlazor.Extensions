﻿using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;
using MudBlazor.Utilities.Exceptions;
using System.Diagnostics.CodeAnalysis;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Enums;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using System.Linq.Expressions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// An extended SelectBox component that allows to select multiple items and provides a search function also internally the MudExPopover is used and you can specify animations as well
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class MudExSelect<T> : IMudExSelect, IMudShadowSelectExtended, IMudExComponent
{


    #region Constructor, Injected Services, Parameters, Fields

    public MudExSelect()
    {
        Adornment = Adornment.End;
        IconSize = Size.Medium;
    }

    [Inject] private IKeyInterceptorFactory KeyInterceptorFactory { get; set; }

    private MudExList<T> _list;
    
    public MudExList<T> MudExList { get => _list; }

    private bool _dense;
    private string multiSelectionText;
    private IKeyInterceptor _keyInterceptor;
    /// <summary>
    /// The collection of items within this select
    /// </summary>
    public IReadOnlyList<MudExSelectItem<T>> Items => _items;

    protected internal List<MudExSelectItem<T>> _items = new();
    protected Dictionary<T, MudExSelectItem<T>> _valueLookup = new();
    protected Dictionary<T, MudExSelectItem<T>> _shadowLookup = new();
    private MudExInput<string> _elementReference;
    internal bool _isOpen;
    protected internal string _currentIcon { get; set; }
    internal event Action<ICollection<T>> SelectionChangedFromOutside;

    protected string Classname =>
        new MudExCssBuilder("mud-ex-select")
        .AddClass(Class)
        .Build();

    protected string InputClassname =>
        new MudExCssBuilder("mud-ex-select-input")
        .AddClass("mud-ex-select-nowrap", NoWrap)
        .AddClass(InputClass)
        .Build();

    private string _elementId = "select_" + Guid.NewGuid().ToString().Substring(0, 8);
    private string _popoverId = "selectpopover_" + Guid.NewGuid().ToString().Substring(0, 8);


    /// <summary>
    /// Render chips additional to item content above or below the select box
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public virtual Adornment RenderChipsAdditional { get; set; } = Adornment.None;

    /// <summary>
    /// Search string for filtering items
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string SearchString { get; set; }

    /// <summary>
    /// Set to true to highlight matched text in the dropdown list
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public virtual bool HighlightSearch { get; set; } = true;

    /// <summary>
    /// Specify an expression which returns the model's field for which validation messages should be displayed when multiple items are selected.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Validation)]
    public Expression<Func<IEnumerable<T>>>? ForMultiple { get; set; }

    /// <summary>
    /// Gets or Sets the function that is used to asynchronously load available items.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public Func<CancellationToken, Task<IList<T>>> AvailableItemsLoadFunc { get; set; }

    /// <summary>
    /// Gets or Sets a value indicating whether to update items on state change. That means if true the items load func will executed then
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool UpdateItemsOnStateChange { get; set; }
    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (ChildContent == null && (ItemCollection == null || !ItemCollection.Any() || UpdateItemsOnStateChange))
            ItemCollection = await GetAvailableItemsAsync();
        await base.OnParametersSetAsync();
    }
    /// <summary>
    /// returns all available items
    /// </summary>
    protected virtual Task<IList<T>> GetAvailableItemsAsync(CancellationToken cancellation = default)
        => AvailableItemsLoadFunc != null ? AvailableItemsLoadFunc(cancellation) : Task.FromResult(new List<T>(0) as IList<T>);

    /// <summary>
    /// The animation type.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public virtual AnimationType PopoverAnimation { get; set; } = AnimationType.Default;

    /// <summary>
    /// The animation timing function.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public virtual AnimationTimingFunction PopoverAnimationTimingFunction { get; set; }

    /// <summary>
    /// The dialog position.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public virtual DialogPosition PopoverAnimationPosition { get; set; } = DialogPosition.TopCenter;

    /// <summary>
    /// User class names for the input, separated by space
    /// </summary>
    [SafeCategory(CategoryTypes.FormComponent.Appearance)]
    [Parameter] public virtual string InputClass { get; set; }

    /// <summary>
    /// User style names for the input, separated by space
    /// </summary>
    [SafeCategory(CategoryTypes.FormComponent.Appearance)]
    [Parameter] public virtual string InputStyle { get; set; }

    /// <summary>
    /// Fired when dropdown opens.
    /// </summary>
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>
    /// Fired when dropdown closes.
    /// </summary>
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// Add the MudSelectItems here
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListBehavior)]
    public virtual RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Optional presentation template for items
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public virtual RenderFragment<T> ItemTemplate { get; set; }

    /// <summary>
    /// Optional presentation template for selected items
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public virtual RenderFragment<T> ItemSelectedTemplate { get; set; }

    /// <summary>
    /// Optional presentation template for disabled items
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public virtual RenderFragment<T> ItemDisabledTemplate { get; set; }

    /// <summary>
    /// Function to be invoked when checking whether an item should be disabled or not. Works both with renderfragment and ItemCollection approach.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListBehavior)]
    public virtual Func<T, bool> ItemDisabledFunc { get; set; }

    /// <summary>
    /// Classname for item template or chips.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListBehavior)]
    public virtual string TemplateClass { get; set; }

    /// <summary>
    /// If true the active (hilighted) item select on tab key. Designed for only single selection. Default is true.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Selecting)]
    public virtual bool SelectValueOnTab { get; set; } = true;

    /// <summary>
    /// If false multiline text show. Default is false.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Selecting)]
    public virtual bool NoWrap { get; set; }

    /// <summary>
    /// User class names for the popover, separated by space
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public virtual string PopoverClass { get; set; }

    /// <summary>
    /// User class names for the popover, separated by space
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public virtual bool DisablePopoverPadding { get; set; }

    /// <summary>
    /// If true, selected items doesn't have a selected background color.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public virtual bool DisableSelectedItemStyle { get; set; }

    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual string SearchBoxPlaceholder { get; set; } = "Filter...";

    /// <summary>
    /// If true, compact vertical padding will be applied to all Select items.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public virtual bool Dense
    {
        get { return _dense; }
        set { _dense = value; }
    }

    /// <summary>
    /// The Open Select Icon
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public virtual string OpenIcon { get; set; } = Icons.Material.Filled.ArrowDropDown;

    /// <summary>
    /// Dropdown color of select. Supports theme colors.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public virtual Color Color { get; set; } = Color.Primary;

    /// <summary>
    /// The Close Select Icon
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public virtual string CloseIcon { get; set; } = Icons.Material.Filled.ArrowDropUp;

    /// <summary>
    /// The value presenter.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public virtual ValuePresenter ValuePresenter { get; set; } = ValuePresenter.Text;

    /// <summary>
    /// If set to true and the MultiSelection option is set to true, a "select all" checkbox is added at the top of the list of items.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListBehavior)]
    public virtual bool SelectAll { get; set; }

    /// <summary>
    /// Sets position of the Select All checkbox
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public virtual SelectAllPosition SelectAllPosition { get; set; } = SelectAllPosition.BeforeSearchBox;

    /// <summary>
    /// Define the text of the Select All option.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public virtual string SelectAllText { get; set; } = "Select All";

    /// <summary>
    /// Function to define a customized multiselection text.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public virtual Func<List<T>, string> MultiSelectionTextFunc { get; set; }

    /// <summary>
    /// Custom search func for searchbox. If doesn't set, it search with "Contains" logic by default. Passed value and searchString values.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListBehavior)]
    public virtual Func<T, string, bool> SearchFunc { get; set; }

    /// <summary>
    /// If not null, select items will automatically created regard to the collection.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public virtual ICollection<T> ItemCollection { get; set; } = null;

    /// <summary>
    /// Allows virtualization. Only work is ItemCollection parameter is not null.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual bool Virtualize { get; set; }

    /// <summary>
    /// If true, chips has close button and remove from SelectedValues when pressed the close button.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual bool ChipCloseable { get; set; } = true;

    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual string ChipClass { get; set; }

    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual Variant ChipVariant { get; set; } = Variant.Filled;

    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual Size ChipSize { get; set; } = Size.Small;

    /// <summary>
    /// Parameter to define the delimited string separator.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public virtual string Delimiter { get; set; } = ", ";

    /// <summary>
    /// If true popover width will be the same as the select component.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public virtual bool RelativeWidth { get; set; } = true;

    /// <summary>
    /// Sets the maxheight the Select can have when open.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public virtual int MaxHeight { get; set; } = 300;

    /// <summary>
    /// Set the anchor origin point to determen where the popover will open from.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public virtual Origin AnchorOrigin { get; set; } = Origin.BottomCenter;

    /// <summary>
    /// Sets the transform origin point for the popover.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public virtual Origin TransformOrigin { get; set; } = Origin.TopCenter;

    /// <summary>
    /// Sets the direction the Select menu should open.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Obsolete("Use AnchorOrigin or TransformOrigin instead.", true)]
    [Parameter] public Direction Direction { get; set; } = Direction.Bottom;

    /// <summary>
    /// If true, the Select menu will open either before or after the input (left/right).
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Obsolete("Use AnchorOrigin or TransformOrigin instead.", true)]
    [Parameter] public bool OffsetX { get; set; }

    /// <summary>
    /// If true, the Select menu will open either before or after the input (top/bottom).
    /// </summary>
    /// [ExcludeFromCodeCoverage]
    [Obsolete("Use AnchorOrigin or TransformOrigin instead.", true)]
    [Parameter] public bool OffsetY { get; set; }

    /// <summary>
    /// If true, the Select's input will not show any values that are not defined in the dropdown.
    /// This can be useful if Value is bound to a variable which is initialized to a value which is not in the list
    /// and you want the Select to show the label / placeholder instead.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public virtual bool Strict { get; set; }

    /// <summary>
    /// Show clear button.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public virtual bool Clearable { get; set; } = true;

    /// <summary>
    /// If true, shows a searchbox for filtering items. Only works with ItemCollection approach.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual bool SearchBox { get; set; } = true;

    /// <summary>
    /// If true, the search-box will be focused when the dropdown is opened.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual bool SearchBoxAutoFocus { get; set; }

    /// <summary>
    /// If true, the search-box has a clear icon.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual bool SearchBoxClearable { get; set; } = true;

    /// <summary>
    /// Search box text field variant.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual Variant SearchBoxVariant { get; set; } = Variant.Text;

    /// <summary>
    /// Search box icon position.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual Adornment SearchBoxAdornment { get; set; } = Adornment.End;

    /// <summary>
    /// If true, prevent scrolling while dropdown is open.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListBehavior)]
    public virtual bool LockScroll { get; set; } = false;

    /// <summary>
    /// Button click event for clear button. Called after text and value has been cleared.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClearButtonClick { get; set; }

    /// <summary>
    /// Custom checked icon.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public virtual string CheckedIcon { get; set; } = Icons.Material.Filled.CheckBox;

    /// <summary>
    /// Custom unchecked icon.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public virtual string UncheckedIcon { get; set; } = Icons.Material.Filled.CheckBoxOutlineBlank;

    /// <summary>
    /// Custom indeterminate icon.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListAppearance)]
    public virtual string IndeterminateIcon { get; set; } = Icons.Material.Filled.IndeterminateCheckBox;

    private bool _multiSelection = false;
    /// <summary>
    /// If true, multiple values can be selected via checkboxes which are automatically shown in the dropdown
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListBehavior)]
    public virtual bool MultiSelection
    {
        get => _multiSelection;
        set
        {
            if (value != _multiSelection)
            {
                _multiSelection = value;
                UpdateTextPropertyAsync(false).AndForget();
            }
        }
    }

    /// <summary>
    /// The MultiSelectionComponent's placement. Accepts Align.Start and Align.End
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual Align MultiSelectionAlign { get; set; } = Align.Start;

    /// <summary>
    /// The component which shows as a MultiSelection check.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual MultiSelectionComponent MultiSelectionComponent { get; set; } = MultiSelectionComponent.CheckBox;

    private IEqualityComparer<T> _comparer;
    /// <summary>
    /// The Comparer to use for comparing selected values internally.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public virtual IEqualityComparer<T> Comparer
    {
        get => _comparer;
        set
        {
            if (_comparer == value)
                return;
            _comparer = value;
            // Apply comparer and refresh selected values
            _selectedValues = new HashSet<T>(_selectedValues, _comparer);
            SelectedValues = _selectedValues;
        }
    }

    private Func<T, string> _toStringFunc = x => x?.ToString();
    /// <summary>
    /// Defines how values are displayed in the drop-down list
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.ListBehavior)]
    public virtual Func<T, string> ToStringFunc
    {
        get => _toStringFunc;
        set
        {
            if (_toStringFunc == value)
                return;
            _toStringFunc = value;
            Converter = new Converter<T>
            {
                SetFunc = _toStringFunc ?? (x => x?.ToString()),
            };
        }
    }

    #endregion


    #region Values, Texts & Items

    //This 'started' field is only for fixing multiselect keyboard navigation test. Has a very minor effect, so can be removable for a better gain.
    private bool _selectedValuesSetterStarted = false;
    private HashSet<T> _selectedValues;
    /// <summary>
    /// Set of selected values. If MultiSelection is false it will only ever contain a single value. This property is two-way bindable.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Data)]
    public IEnumerable<T> SelectedValues
    {
        get => _selectedValues ??= new HashSet<T>();
        set
        {
            if (_selectedValuesSetterStarted || (value is null && _selectedValues is null))            
                return;
            
            var set = (value ?? new HashSet<T>(_comparer)).ToList();
            if (SelectedValues.Count() != set.Count || !SelectedValues.All(x => set.Contains(x, _comparer)))
            {
                _selectedValuesSetterStarted = true;
                SelectionChangedFromOutside?.Invoke(new HashSet<T>(_selectedValues, _comparer));
                _selectedValues = new HashSet<T>(set);
                OnBeforeSelectedChanged(_selectedValues);
                SelectedValuesChanged.InvokeAsync(new HashSet<T>(SelectedValues));
                ValueChanged.InvokeAsync(Value);
                if (MultiSelection)
                    UpdateTextPropertyAsync(false).AndForget();              
                _selectedValuesSetterStarted = false;                
                Task.Delay(30).ContinueWith(_ => BeginValidateAsync());
            }
        }
    }

    /// <summary>
    /// is called before the selected items change.
    /// </summary>
    protected virtual void OnBeforeSelectedChanged(IEnumerable<T> selected)
    { }

    private MudExListItem<T> _selectedListItem;
    private HashSet<MudExListItem<T>> _selectedListItems;

    protected internal MudExListItem<T> SelectedListItem
    {
        get => _selectedListItem;

        set
        {
            if (_selectedListItem == value)
            {
                return;
            }
            _selectedListItem = value;
        }
    }

    protected internal IEnumerable<MudExListItem<T>> SelectedListItems
    {
        get => _selectedListItems;

        set
        {
            if (value == null && _selectedListItems == null)
            {
                return;
            }

            if (value != null && _selectedListItems != null && _selectedListItems.SetEquals(value))
            {
                return;
            }
            _selectedListItems = value == null ? null : value.ToHashSet();
        }
    }

    /// <summary>
    /// Fires when SelectedValues changes.
    /// </summary>
    [Parameter] public EventCallback<IEnumerable<T>> SelectedValuesChanged { get; set; }

    /// <summary>
    /// Should only be used for debugging and development purposes.
    /// </summary>
    [Parameter] public EventCallback<IEnumerable<MudExListItem<T>>> SelectedListItemsChanged { get; set; }

    protected async Task SetCustomizedTextAsync(string text, bool updateValue = true,
        List<T> selectedConvertedValues = null,
        Func<List<T>, string> multiSelectionTextFunc = null)
    {
        // The Text property of the control is updated
        Text = multiSelectionTextFunc?.Invoke(selectedConvertedValues);

        // The comparison is made on the multiSelectionText variable
        if (multiSelectionText != text)
        {
            multiSelectionText = text;
            if (!string.IsNullOrWhiteSpace(multiSelectionText))
                Touched = true;
            if (updateValue)
                await UpdateValuePropertyAsync(false);
            await TextChanged.InvokeAsync(multiSelectionText);
        }
    }

    protected override Task UpdateValuePropertyAsync(bool updateText)
    {
        // For MultiSelection of non-string T's we don't update the Value!!!
        //if (typeof(T) == typeof(string) || !MultiSelection)
        base.UpdateValuePropertyAsync(updateText).AndForget();
        return Task.CompletedTask;
    }

    protected override Task UpdateTextPropertyAsync(bool updateValue)
    {
        List<string> textList = new List<string>();
        if (Items != null && Items.Any())
        {
            if (ItemCollection != null)
            {
                textList.AddRange(from val in SelectedValues select ItemCollection.FirstOrDefault(x => x != null && (Comparer?.Equals(x, val) ?? x.Equals(val))) into collectionValue where collectionValue != null select Converter.Set(collectionValue));
            }
            else
            {
                foreach (var val in SelectedValues)
                {
                    if (!Strict && !Items.Select(x => x.Value).Contains(val))
                    {
                        textList.Add(ToStringFunc != null ? ToStringFunc(val) : Converter.Set(val));
                        continue;
                    }
                    var item = Items.FirstOrDefault(x => x != null && (x.Value == null ? val == null : Comparer?.Equals(x.Value, val) ?? x.Value.Equals(val)));
                    if (item != null)
                    {
                        textList.Add(!string.IsNullOrEmpty(item.Text) ? item.Text : Converter.Set(item.Value));
                    }
                }
            }
        }

        // when multiselection is true, we return
        // a comma separated list of selected values
        if (MultiSelection)
        {
            if (MultiSelectionTextFunc != null)
            {
                return SetCustomizedTextAsync(string.Join(Delimiter, textList),
                    selectedConvertedValues: SelectedValues.ToList(),
                    multiSelectionTextFunc: MultiSelectionTextFunc, updateValue: updateValue);
            }

            return SetTextAsync(string.Join(Delimiter, textList), updateValue: updateValue);
        }
        else
        {
            var item = Items.FirstOrDefault(x => Value == null ? x.Value == null : Comparer?.Equals(Value, x.Value) ?? Value.Equals(x.Value));
            if (item == null)
            {
                return SetTextAsync(Converter.Set(Value), false);
            }
            return SetTextAsync((!string.IsNullOrEmpty(item.Text) ? item.Text : Converter.Set(item.Value)), updateValue: updateValue);
        }
    }

    private string GetSelectTextPresenter()
    {
        return Text;
    }

    #endregion


    #region Lifecycle Methods

    protected override void OnInitialized()
    {
        base.OnInitialized();
        UpdateIcon();
        if (!MultiSelection && Value != null)
        {
            _selectedValues = new HashSet<T>(_comparer) { Value };
        }
        else if (MultiSelection && SelectedValues != null)
        {
            // TODO: Check this line again
            SetValueAsync(SelectedValues.FirstOrDefault()).AndForget();
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateIcon();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {

        if (firstRender)
        {
            _keyInterceptor = KeyInterceptorFactory.Create();
            await _keyInterceptor.Connect(_elementId, new KeyInterceptorOptions()
            {
                //EnableLogging = true,
                TargetClass = "mud-input-control",
                Keys = {
                        new KeyOptions { Key=" ", PreventDown = "key+none" }, //prevent scrolling page, toggle open/close
                        new KeyOptions { Key="ArrowUp", PreventDown = "key+none" }, // prevent scrolling page, instead hilight previous item
                        new KeyOptions { Key="ArrowDown", PreventDown = "key+none" }, // prevent scrolling page, instead hilight next item
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
            _keyInterceptor.KeyDown += HandleKeyDown;
            _keyInterceptor.KeyUp += HandleKeyUp;
            await UpdateTextPropertyAsync(false);
            _list?.ForceUpdateItems();
            SelectedListItem = Items.FirstOrDefault(x => x.Value != null && Value != null && x.Value.Equals(Value))?.ListItem;
            StateHasChanged();
        }
        //Console.WriteLine("Select rendered");
        await base.OnAfterRenderAsync(firstRender);
    }

    public void ForceUpdateItems()
    {
        _list?.ForceUpdateItems();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            if (_keyInterceptor != null)
            {
                _keyInterceptor.KeyDown -= HandleKeyDown;
                _keyInterceptor.KeyUp -= HandleKeyUp;
            }
            _keyInterceptor?.Dispose();
        }
    }

    #endregion


    #region Events (Key, Focus)

    internal async void HandleKeyDown(KeyboardEventArgs obj)
    {
        if (Disabled || ReadOnly)
            return;

        if (_list != null && _isOpen)
        {
            await _list.HandleKeyDown(obj);
        }

        switch (obj.Key)
        {
            case "Tab":
                await CloseMenu();
                break;
            case "ArrowUp":
                if (obj.AltKey)
                {
                    await CloseMenu();
                }
                else if (!_isOpen)
                {
                    await OpenMenu();
                }
                break;
            case "ArrowDown":
                if (obj.AltKey)
                {
                    await OpenMenu();
                }
                else if (!_isOpen)
                {
                    await OpenMenu();
                }
                break;
            case " ":
                await ToggleMenu();
                break;
            case "Escape":
                await CloseMenu();
                break;
            case "Enter":
            case "NumpadEnter":
                if (!MultiSelection)
                {
                    if (!_isOpen)
                    {
                        await OpenMenu();
                    }
                    else
                    {
                        await CloseMenu();
                    }
                    break;
                }
                else
                {
                    if (!_isOpen)
                    {
                        await OpenMenu();
                        break;
                    }
                    else
                    {
                        await _elementReference.SetText(Text);
                        break;
                    }
                }
        }
        await OnKeyDown.InvokeAsync(obj);

    }

    protected internal async void HandleKeyUp(KeyboardEventArgs obj)
    {
        await OnKeyUp.InvokeAsync(obj);
    }

    protected internal async Task OnLostFocus(FocusEventArgs obj)
    {
        //if (_isOpen)
        //{
        //    // when the menu is open we immediately get back the focus if we lose it (i.e. because of checkboxes in multi-select)
        //    // otherwise we can't receive key strokes any longer
        //    _elementReference.FocusAsync().AndForget(TaskOption.Safe);
        //}
        //base.OnBlur.InvokeAsync(obj).AndForget();

        await OnBlurredAsync(obj);
    }

    public override ValueTask FocusAsync()
    {
        return _elementReference.FocusAsync();
    }

    public override ValueTask BlurAsync()
    {
        return _elementReference.BlurAsync();
    }

    public override ValueTask SelectAsync()
    {
        return _elementReference.SelectAsync();
    }

    public override ValueTask SelectRangeAsync(int pos1, int pos2)
    {
        return _elementReference.SelectRangeAsync(pos1, pos2);
    }

    #endregion


    #region PopoverState

    public async Task ToggleMenu()
    {
        if (Disabled || ReadOnly)
            return;
        if (_isOpen)
            await CloseMenu();
        else
            await OpenMenu();
    }

    public async Task OpenMenu()
    {
        if (Disabled || ReadOnly)
            return;
        _isOpen = true;
        UpdateIcon();
        StateHasChanged();

        //disable escape propagation: if selectmenu is open, only the select popover should close and underlying components should not handle escape key
        await _keyInterceptor.UpdateKey(new() { Key = "Escape", StopDown = "Key+none" });
        await OnOpen.InvokeAsync();
    }

    public async Task CloseMenu()
    {
        _isOpen = false;
        UpdateIcon();
        StateHasChanged();
        //if (focusAgain == true)
        //{
        //    StateHasChanged();
        //    await OnBlur.InvokeAsync(new FocusEventArgs());
        //    _elementReference.FocusAsync().AndForget(TaskOption.Safe);
        //    StateHasChanged();
        //}

        //enable escape propagation: the select popover was closed, now underlying components are allowed to handle escape key
        await _keyInterceptor.UpdateKey(new() { Key = "Escape", StopDown = "none" });

        await OnClose.InvokeAsync();
    }

    #endregion


    #region Item Registration & Selection

    public async Task SelectOption(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            if (!MultiSelection)
                await CloseMenu();
            return;
        }
        await SelectOption(_items[index].Value);
    }

    public async Task SelectOption(object obj)
    {
        var value = (T)obj;
        if (MultiSelection)
        {
            await UpdateTextPropertyAsync(false);
            //UpdateSelectAllChecked();
            await BeginValidateAsync();
        }
        else
        {
            // single selection
            // CloseMenu(true) doesn't close popover in BSS
            await CloseMenu();

            if (EqualityComparer<T>.Default.Equals(Value, value))
            {
                StateHasChanged();
                return;
            }

            await SetValueAsync(value);
            //await UpdateTextPropertyAsync(false);
            _elementReference.SetText(Text).AndForget();
            //_selectedValues.Clear();
            //_selectedValues.Add(value);
        }

        //await SelectedValuesChanged.InvokeAsync(SelectedValues);
        //if (MultiSelection && typeof(T) == typeof(string))
        //await SetValueAsync((T)(object)Text, updateText: false);
        await InvokeAsync(StateHasChanged);
    }

    //TODO: will override this method when core library will have the base one.
    public override async Task ForceUpdate()
    {
        await base.ForceUpdate();
        if (!MultiSelection)
        {
            SelectedValues = new HashSet<T>(_comparer) { Value };
        }
        else
        {
            await SelectedValuesChanged.InvokeAsync(new HashSet<T>(SelectedValues, _comparer));
        }
    }

    public async Task BeginValidatePublic()
    {
        await BeginValidateAsync();
    }

    protected internal bool Add(MudExSelectItem<T> item)
    {
        if (item == null)
            return false;
        bool? result = null;
        if (!_items.Select(x => x.Value).Contains(item.Value))
        {
            _items.Add(item);

            if (item.Value != null)
            {
                _valueLookup[item.Value] = item;
                if (item.Value.Equals(Value) && !MultiSelection)
                    result = true;
            }
        }
        //UpdateSelectAllChecked();
        if (!result.HasValue)
        {
            result = item.Value?.Equals(Value);
        }
        return result == true;
    }

    protected internal void Remove(MudExSelectItem<T> item)
    {
        _items.Remove(item);
        if (item.Value != null)
            _valueLookup.Remove(item.Value);
    }

    public void RegisterShadowItem(MudExSelectItem<T> item)
    {
        if (item == null || item.Value == null)
            return;
        _shadowLookup[item.Value] = item;
    }

    public void UnregisterShadowItem(MudExSelectItem<T> item)
    {
        if (item == null || item.Value == null)
            return;
        _shadowLookup.Remove(item.Value);
    }

    #endregion


    #region Clear

    /// <summary>
    /// Extra handler for clearing selection.
    /// </summary>
    protected async ValueTask SelectClearButtonClickHandlerAsync(MouseEventArgs e)
    {        
        await SetValueAsync(default, false);
        await SetTextAsync(default, false);
        _selectedValues.Clear();
        SelectedListItem = null;
        SelectedListItems = null;
        await BeginValidateAsync();
        StateHasChanged();
        await SelectedValuesChanged.InvokeAsync(new HashSet<T>(SelectedValues, _comparer));
        await OnClearButtonClick.InvokeAsync(e);
    }

    [ExcludeFromCodeCoverage]
    [Obsolete("Use Clear instead.", true)]
    public Task ClearAsync() => Clear();

    /// <summary>
    /// Clear the selection
    /// </summary>
    public async Task Clear()
    {
        await SetValueAsync(default, false);
        await SetTextAsync(default, false);
        _selectedValues.Clear();
        await BeginValidateAsync();
        StateHasChanged();
        await SelectedValuesChanged.InvokeAsync(new HashSet<T>(SelectedValues, _comparer));
    }

    protected override void ResetValue()
    {
        base.ResetValue();
        SelectedValues = null;
    }

    #endregion

    protected bool IsValueInList
    {
        get
        {
            if (Value == null)
                return false;
            //return _shadowLookup.TryGetValue(Value, out var _);
            foreach (var value in Items.Select(x => x.Value))
            {
                if (Comparer != null ? Comparer.Equals(value, Value) : value.Equals(Value)) //(Converter.Set(item.Value) == Converter.Set(Value))
                {
                    return true;
                }
            }
            return false;
        }
    }

    protected void UpdateIcon()
    {
        _currentIcon = !string.IsNullOrWhiteSpace(AdornmentIcon) ? AdornmentIcon : _isOpen ? CloseIcon : OpenIcon;
    }

    public void CheckGenericTypeMatch(object select_item)
    {
        var itemT = select_item.GetType().GenericTypeArguments[0];
        if (itemT != typeof(T))
            throw new GenericTypeMismatchException("MudExSelect", "MudExSelectItem", typeof(T), itemT);
    }

    /// <summary>
    /// Fixes issue #4328
    /// Returns true when MultiSelection is true and it has selected values(Since Value property is not used when MultiSelection=true
    /// </summary>
    /// <param name="value"></param>
    /// <returns>True when component has a value</returns>
    protected override bool HasValue(T value)
    {
        if (MultiSelection)
            return SelectedValues?.Count() > 0;
        else
            return base.HasValue(value);
    }

    protected async Task ChipClosed(MudChip chip)
    {
        if (chip == null || SelectedValues == null)
        {
            return;
        }
        //SelectedValues = SelectedValues.Where(x => Converter.Set(x)?.ToString() != chip.Value?.ToString());
        SelectedValues = SelectedValues.Where(x => x.Equals(chip.Value) == false);
        await SelectedValuesChanged.InvokeAsync(SelectedValues);
    }
}


internal interface IMudExSelect
{
    void CheckGenericTypeMatch(object select_item);
    bool MultiSelection { get; set; }
}

internal interface IMudShadowSelectExtended
{
}