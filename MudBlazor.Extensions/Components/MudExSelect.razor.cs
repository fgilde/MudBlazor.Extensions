using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;
using MudBlazor.Utilities.Exceptions;
using System.Diagnostics.CodeAnalysis;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using System.Linq.Expressions;
using System.Reflection;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// An extended SelectBox component that allows to select multiple items and provides a search function also internally the MudExPopover is used, and you can specify animations as well
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class MudExSelect<T> : IMudExSelect, IMudExShadowSelect, IMudExComponent
{

    /// <summary>
    /// Gets or Sets the Localizer Pattern.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string LocalizerPattern { get; set; } = "{0}";

    /// <summary>
    /// The CSS classes applied to the outer <c>div</c>.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>null</c>.  Multiple classes must be separated by spaces.
    /// </remarks>
    [Category(CategoryTypes.FormComponent.Appearance)]
    [Parameter]
    public string? OuterClass { get; set; }

    /// <summary>
    /// Renders the item name
    /// </summary>
    public virtual string ItemNameRender(T item)
    {
        var res = ToStringFunc != null && item != null ? ToStringFunc(item) : Converter.Set(item);
        if (!string.IsNullOrWhiteSpace(res) && !string.IsNullOrWhiteSpace(LocalizerPattern))
        {
            return LocalizerToUse != null ? LocalizerToUse[string.Format(LocalizerPattern, item)] : string.Format(LocalizerPattern, res);
        }

        return res;
    }

    #region Constructor, Injected Services, Parameters, Fields

    /// <summary>
    /// Constructor
    /// </summary>
    public MudExSelect()
    {
        Adornment = Adornment.End;
    }

    [Inject] private IKeyInterceptorService KeyInterceptorService { get; set; }

    private MudExList<T> _list;

    /// <summary>
    /// Reference to the internal MudExList
    /// </summary>
    public MudExList<T> MudExList => _list;

    private bool _dense;
    private string _multiSelectionText;
    /// <summary>
    /// The collection of items within this select
    /// </summary>
    public IReadOnlyList<MudExSelectItem<T>> Items => ItemList;

    /// <summary>
    /// Items
    /// </summary>
    protected internal List<MudExSelectItem<T>> ItemList = new();

    /// <summary>
    /// Lookup for items
    /// </summary>
    protected Dictionary<T, MudExSelectItem<T>> ValueLookup = new();

    /// <summary>
    /// Shadow lookup for items
    /// </summary>
    protected Dictionary<T, MudExSelectItem<T>> ShadowLookup = new();
    private MudExInput<string> _elementReference;

    internal bool IsOpen;

    /// <summary>
    /// The current Icon
    /// </summary>
    protected internal string CurrentIcon { get; set; }
    internal event Action<ICollection<T>> SelectionChangedFromOutside;

    protected string OuterClassname =>
        new MudExCssBuilder("mud-select")
            .AddClass("mud-ex-select")
            .AddClass("mud-width-full", FullWidth)
            .AddClass(OuterClass)
            .Build();

    /// <summary>
    /// Class to be applied
    /// </summary>
    protected string Classname =>
        new MudExCssBuilder("mud-ex-select")
            .AddClass("mud-select")
            .AddClass("mud-ex-select-variant-text", Variant == Variant.Text)
            .AddClass(Class)
            .Build();

    /// <summary>
    /// Class to be applied to the inner input element
    /// </summary>
    protected string InputClassname =>
        new MudExCssBuilder("mud-ex-select-input")
            .AddClass("mud-select-input")
        .AddClass("mud-ex-select-readonly", ReadOnly)
        .AddClass("mud-ex-select-no-dropdown", ReadOnly && HideDropDownWhenReadOnly)
        .AddClass("mud-ex-select-nowrap", NoWrap)
        .AddClass("mud-ex-select-input-variant-text", Variant == Variant.Text)
        .AddClass(InputClass)
        .Build();

    /// <summary>
    /// Style to be applied to chip
    /// </summary>
    protected string ChipStyleStr =>
        new MudExStyleBuilder()
            .AddRaw(StyleChip)
            .WithColorForVariant(ChipVariant, Color, !Color.IsColor)
            .Build();

    private string _elementId = "select_" + Guid.NewGuid().ToString().Substring(0, 8);
    private string _popoverId = "selectpopover_" + Guid.NewGuid().ToString().Substring(0, 8);

    /// <summary>
    /// If true the item template is use for the selection list, otherwise its use only if ValuePresenter is ItemContent or Chip
    /// </summary>
    [Parameter] public bool UseItemTemplateForSelection { get; set; } = true;

    /// <summary>
    /// If true dropdown icon is not shown when ReadOnly is true
    /// </summary>
    [Parameter] public bool HideDropDownWhenReadOnly { get; set; } = true;

    /// <summary>
    /// Style applied to chip
    /// </summary>
    [Parameter] public string StyleChip { get; set; }

    /// <summary>
    ///  Func to group by items collection
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.List.Appearance)]
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
    /// Set to true to use a expansion panel to nest items.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.List.Behavior)]
    public bool GroupsNested { get; set; }

    /// <summary>
    /// Sets the group's expanded state on popover opening. Works only if GroupsNested is true.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.List.Behavior)]
    public bool GroupsInitiallyExpanded { get; set; } = true;

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
    public Expression<Func<IEnumerable<T>>> ForMultiple { get; set; }

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
        if (UpdateItemsOnStateChange)
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
    /// Optional presentation template for items
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public virtual RenderFragment SelectAllTemplate { get; set; }

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
    /// If true the active (highlighted) item select on tab key. Designed for only single selection. Default is true.
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

    /// <summary>
    /// Placeholder for the search box.
    /// </summary>
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

    /// <summary>
    /// Class to be applied to the chip.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual string ChipClass { get; set; }

    /// <summary>
    /// Variant to be applied to the chip.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual Variant ChipVariant { get; set; } = Variant.Filled;

    /// <summary>
    /// Size to be applied to the chip.
    /// </summary>
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
    public virtual DropdownWidth RelativeWidth { get; set; } = DropdownWidth.Relative;

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
    /// If true, shows a search box for filtering items. Only works with ItemCollection approach.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual bool SearchBox { get; set; } = true;

    /// <summary>
    /// If true, the search-box will be focused when the dropdown is opened.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public virtual bool SearchBoxAutoFocus { get; set; } = true;

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
                _ = UpdateTextPropertyAsync(false);
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
            if (Equals(_comparer, value))
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
    /// Set of selected values. If MultiSelection is false it will only ever contain a single value. This property is two-way bind able.
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

                if (NeedsValueUpdateForNonMultiSelection()) // No binding so we need to update the value manually
                {
                    _ = SetValueAsync(_selectedValues.LastOrDefault());
                    //Value = _selectedValues.LastOrDefault();
                }

                try
                {
                    ValueChanged.InvokeAsync(Value);
                }
                catch (Exception)
                {
                    // BUG: After newest MudBlazor update we have to catch this exception
                }
                _ = UpdateTextPropertyAsync(false);
                _selectedValuesSetterStarted = false;
                Task.Delay(30).ContinueWith(_ => BeginValidateAsync());
            }
        }
    }

    /// <summary>
    /// Setting value extra can cause a dead loop on object edit, so we should only set when necessary
    /// </summary>
    protected virtual bool NeedsValueUpdateForNonMultiSelection()
    {
        var comparer = Comparer ??= EqualityComparer<T>.Default;
        return !MultiSelection && !ValueChanged.HasDelegate && !comparer.Equals(Value, SelectedValues.LastOrDefault());
    }

    /// <summary>
    /// is called before the selected items change.
    /// </summary>
    protected virtual void OnBeforeSelectedChanged(IEnumerable<T> selected)
    { }

    private HashSet<MudExListItem<T>> _selectedListItems;

    /// <summary>
    /// SelectedListItem
    /// </summary>
    protected internal MudExListItem<T> SelectedListItem { get; set; }

    /// <summary>
    /// Selected List Items
    /// </summary>
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

    /// <summary>
    /// Set custom text for the select input field.
    /// </summary>
    protected async Task SetCustomizedTextAsync(string text, bool updateValue = true,
        List<T> selectedConvertedValues = null,
        Func<List<T>, string> multiSelectionTextFunc = null)
    {
        // The Text property of the control is updated
        Text = multiSelectionTextFunc?.Invoke(selectedConvertedValues);

        // The comparison is made on the multiSelectionText variable
        if (_multiSelectionText != text)
        {
            _multiSelectionText = text;
            if (!string.IsNullOrWhiteSpace(_multiSelectionText))
                Touched = true;
            if (updateValue)
                await UpdateValuePropertyAsync(false);
            await TextChanged.InvokeAsync(_multiSelectionText);
        }
    }

    /// <summary>
    /// Updates the value property.
    /// </summary>
    protected override Task UpdateValuePropertyAsync(bool updateText)
    {
        // For MultiSelection of non-string T's we don't update the Value!!!
        //if (typeof(T) == typeof(string) || !MultiSelection)
        return base.UpdateValuePropertyAsync(updateText);
        //return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the text property.
    /// </summary>
    protected override Task UpdateTextPropertyAsync(bool updateValue)
    {
        List<string> textList = new List<string>();
        if (Items != null && Items.Any())
        {
            if (ItemCollection != null)
            {
                textList.AddRange(from val in SelectedValues select ItemCollection.FirstOrDefault(x => x != null && (Comparer?.Equals(x, val) ?? x.Equals(val))) into collectionValue where collectionValue != null select ItemNameRender(collectionValue));
            }
            else
            {
                foreach (var val in SelectedValues)
                {
                    if (!Strict && !Items.Select(x => x.Value).Contains(val))
                    {
                        textList.Add(ItemNameRender(val));
                        continue;
                    }
                    var item = Items.FirstOrDefault(x => x != null && (x.Value == null ? val == null : Comparer?.Equals(x.Value, val) ?? x.Value.Equals(val)));
                    if (item != null)
                    {
                        textList.Add(!string.IsNullOrEmpty(item.Text) ? item.Text : ItemNameRender(item.Value));
                    }
                }
            }
        }

        // when multi selection is true, we return a comma separated list of selected values
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


        var resultItem = Items?.FirstOrDefault(x => Value == null ? x.Value == null : Comparer?.Equals(Value, x.Value) ?? Value.Equals(x.Value));
        
        if (Value != null && !MultiSelection && !_initialSet) // FIX: #140 for chips
        {
            _initialSet = true;
            _selectedValues = new HashSet<T>(_comparer) { Value };
        }
        return resultItem == null ? SetTextAsync(ItemNameRender(Value), false) : SetTextAsync((!string.IsNullOrEmpty(resultItem.Text) && resultItem.Value is null ? resultItem.Text : ItemNameRender(resultItem.Value)), updateValue: updateValue);
    }
    
    bool _initialSet = false;

    private string GetSelectTextPresenter()
    {
        return Text;
    }

    #endregion


    #region Lifecycle Methods

    /// <inheritdoc />
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
            _ = SetValueAsync(SelectedValues.FirstOrDefault());
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateIcon();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {

        if (firstRender)
        {
            if (ChildContent == null && (ItemCollection == null || !ItemCollection.Any()))
            {
                ItemCollection = await GetAvailableItemsAsync();
            }

            var keys = new[]{
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
            };
            var options = new KeyInterceptorOptions("mud-input-control", keys);
            await KeyInterceptorService.SubscribeAsync(_elementId, options, HandleKeyDown, HandleKeyUp);

            await UpdateTextPropertyAsync(false);
            _list?.ForceUpdateItems();
            SelectedListItem = Items.FirstOrDefault(x => x.Value != null && Value != null && x.Value.Equals(Value))?.ListItem;
            StateHasChanged();
        }
        //Console.WriteLine("Select rendered");
        await base.OnAfterRenderAsync(firstRender);
    }
    
    private T ValueToRender => SelectedListItem != null ? SelectedListItem.Value ?? Value : Value;

    /// <summary>
    /// Force the update of the items in the select menu.
    /// </summary>
    public void ForceUpdateItems()
    {
        _list?.ForceUpdateItems();
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await base.DisposeAsyncCore();
        await KeyInterceptorService.UnsubscribeAsync(_elementId);
    }


    #endregion


    #region Events (Key, Focus)

    internal async void HandleKeyDown(KeyboardEventArgs obj)
    {
        if (Disabled || ReadOnly)
            return;

        if (_list != null && IsOpen)
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
                else if (!IsOpen)
                {
                    await OpenMenu();
                }
                break;
            case "ArrowDown":
                if (obj.AltKey)
                {
                    await OpenMenu();
                }
                else if (!IsOpen)
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
                    if (!IsOpen)
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
                    if (!IsOpen)
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

    /// <summary>
    /// Handler for the keyup event.
    /// </summary>
    /// <param name="obj"></param>
    protected internal async void HandleKeyUp(KeyboardEventArgs obj)
    {
        await OnKeyUp.InvokeAsync(obj);
    }

    private bool GetOpen()
    {
        FieldInfo field = GetType().GetField("_open", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        var value = field?.GetValue(this);
        return value is bool b ? b : IsOpen;
    }

    private async Task OnFocusOutAsync(FocusEventArgs focusEventArgs)
    {
        if (GetOpen())
        {
            // when the menu is open we immediately get back the focus if we lose it (i.e. because of checkboxes in multi-select)
            // otherwise we can't receive key strokes any longer
            await FocusAsync();
        }
    }

    /// <summary>
    /// Called when the component lost the focus.
    /// </summary>
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

    /// <summary>
    /// Focuses the component.
    /// </summary>
    public override ValueTask FocusAsync() => _elementReference.FocusAsync();

    /// <summary>
    /// Blur the component.
    /// </summary>
    public override ValueTask BlurAsync() => _elementReference.BlurAsync();

    /// <summary>
    /// Selects the text of the input.
    /// </summary>
    public override ValueTask SelectAsync() => _elementReference.SelectAsync();

    /// <summary>
    /// Selects the text of the input in the given range.
    /// </summary>
    public override ValueTask SelectRangeAsync(int pos1, int pos2) => _elementReference.SelectRangeAsync(pos1, pos2);

    #endregion


    #region PopoverState

    /// <summary>
    /// Toggle the menu.
    /// </summary>
    public async Task ToggleMenu()
    {
        if (Disabled || ReadOnly)
            return;
        if (IsOpen)
            await CloseMenu();
        else
            await OpenMenu();
    }

    /// <summary>
    /// Open the menu.
    /// </summary>
    public async Task OpenMenu()
    {
        if (Disabled || ReadOnly)
            return;
        IsOpen = true;
        UpdateIcon();
        StateHasChanged();

        // TODO: MudBlazor 8
        //await _keyInterceptor.UpdateKey(new() { Key = "Escape", StopDown = "Key+none" });
        await OnOpen.InvokeAsync();
    }

    /// <summary>
    /// Close the menu.
    /// </summary>
    public async Task CloseMenu()
    {
        IsOpen = false;
        UpdateIcon();
        StateHasChanged();

        // TODO: MudBlazor 8
        //await _keyInterceptor.UpdateKey(new() { Key = "Escape", StopDown = "none" });

        await OnClose.InvokeAsync();
    }

    #endregion


    #region Item Registration & Selection

    /// <summary>
    /// Selects the given option by index.
    /// </summary>
    public async Task SelectOption(int index)
    {
        if (index < 0 || index >= ItemList.Count)
        {
            if (!MultiSelection)
                await CloseMenu();
            return;
        }
        await SelectOption(ItemList[index].Value);
    }

    /// <summary>
    /// Selects the given option.
    /// </summary>
    public async Task SelectOption(object obj, bool force = true)
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

            await SetValueAsync(value, force: force);

            _ = _elementReference.SetText(Text);
            //_selectedValues.Clear();
            //_selectedValues.Add(value);
        }

        //await SelectedValuesChanged.InvokeAsync(SelectedValues);
        //if (MultiSelection && typeof(T) == typeof(string))
        //await SetValueAsync((T)(object)Text, updateText: false);
        await InvokeAsync(StateHasChanged);
    }

    //TODO: will override this method when core library will have the base one.
    /// <summary>
    /// Force the component to update.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Begin validation for the component.
    /// </summary>
    public async Task BeginValidatePublic()
    {
        await BeginValidateAsync();
    }

    /// <summary>
    /// Adds the given item to the list of selected items.
    /// </summary>
    protected internal bool Add(MudExSelectItem<T> item)
    {
        if (item == null)
            return false;
        bool? result = null;
        if (!ItemList.Select(x => x.Value).Contains(item.Value))
        {
            ItemList.Add(item);

            if (item.Value != null)
            {
                ValueLookup[item.Value] = item;
                if (item.Value.Equals(Value) && !MultiSelection)
                    result = true;
            }
        }
        //UpdateSelectAllChecked();
        result ??= item.Value?.Equals(Value);
        return result == true;
    }

    /// <summary>
    /// Removes the given item from the list of selected items.
    /// </summary>
    protected internal void Remove(MudExSelectItem<T> item)
    {
        ItemList.Remove(item);
        if (item.Value != null)
            ValueLookup.Remove(item.Value);
    }

    /// <summary>
    /// Registers the given item.
    /// </summary>
    public void RegisterShadowItem(MudExSelectItem<T> item)
    {
        if (item == null || item.Value == null)
            return;
        ShadowLookup[item.Value] = item;
    }

    /// <summary>
    /// Unregisters the given item.
    /// </summary>
    public void UnregisterShadowItem(MudExSelectItem<T> item)
    {
        if (item == null || item.Value == null)
            return;
        ShadowLookup.Remove(item.Value);
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

    /// <summary>
    /// Clear the selection
    /// </summary>
    /// <returns></returns>
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



    #endregion

    /// <summary>
    /// Returns true if the value is in the list of items.
    /// </summary>
    protected bool IsValueInList =>
        Value != null &&
        Items.Select(x => x.Value).Any(value => Comparer?.Equals(value, Value) ?? value.Equals(Value));

    /// <summary>
    /// Updates the icon.
    /// </summary>
    protected void UpdateIcon()
    {
        CurrentIcon = !string.IsNullOrWhiteSpace(AdornmentIcon) ? AdornmentIcon : IsOpen ? CloseIcon : OpenIcon;
    }

    /// <summary>
    /// Ensures that the generic type of the select item matches the generic type of the select.
    /// </summary>
    public void CheckGenericTypeMatch(object selectItem)
    {
        var itemT = selectItem.GetType().GenericTypeArguments[0];
        if (itemT != typeof(T))
            throw new GenericTypeMismatchException("MudExSelect", "MudExSelectItem", typeof(T), itemT);
    }

    /// <summary>
    /// Fixes issue #4328
    /// Returns true when MultiSelection is true, and it has selected values(Since Value property is not used when MultiSelection=true
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


    /// <summary>
    /// Called when a chip is closed.
    /// </summary>
    protected async Task ChipClosed<T>(MudChip<T> chip)
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

/// <summary>
/// Interface for the MudExSelect component.
/// </summary>
internal interface IMudExSelect
{
    /// <summary>
    /// Ensures that the generic type of the select item matches the generic type of the select.
    /// </summary>
    void CheckGenericTypeMatch(object selectItem);

    /// <summary>
    /// True when multi selection is enabled.
    /// </summary>
    bool MultiSelection { get; set; }
}

/// <summary>
/// Shadow list identifier
/// </summary>
internal interface IMudExShadowSelect
{
}