using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A component that allows the user to select a value from a list of possible values and display it as Chips or default Combobox.
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class MudExChipSelect<T>
{
    /// <summary>
    /// Gets or Sets the Localizer Pattern.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string LocalizerPattern { get; set; } = "{0}";

    /// <summary>
    /// Gets or Sets the variant of the component.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public Variant Variant { get; set; }

    /// <summary>
    /// Gets or Sets the AutoFocus for the filter input.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AutoFocusFilter { get; set; }

    /// <summary>
    /// Gets or Sets the adornment of the component.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public Adornment Adornment { get; set; } = Adornment.End;

    /// <summary>
    /// Gets or Sets the read-only status of the component.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or Sets whether to render the validation component.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool RenderValidationComponent { get; set; } = true;

    /// <summary>
    /// Gets or Sets the data For method
    /// </summary>
    [Parameter, SafeCategory("Validation")]
    public Expression<Func<IEnumerable<T>>> For { get; set; }

    /// <summary>
    /// Gets or Sets the option to disable the underline in the component.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public bool DisableUnderLine { get; set; }

    /// <summary>
    /// Gets or Sets the option to disable the underline for the validation component.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public bool DisableUnderLineForValidationComponent { get; set; } = true;

    /// <summary>
    /// Gets or Sets the CSS style for the validation component.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string StyleForValidationComponent { get; set; } = "margin-top: -38px; pointer-events: none;";

    /// <summary>
    /// Gets or Sets the color of the chip in the component.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public virtual Color ChipColor { get; set; } = Color.Primary;

    /// <summary>
    /// Gets or Sets the variant of the chip in the component.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public virtual Variant ChipVariant { get; set; } = Variant.Filled;

    /// <summary>
    /// Gets or Sets the view mode for the component.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public virtual ViewMode ViewMode { get; set; } = ViewMode.ChipsOnly;

    /// <summary>
    /// Gets or Sets the label of the component.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public virtual string Label { get; set; }

    /// <summary>
    /// Gets or Sets the helper text of the component.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public virtual string HelperText { get; set; }

    /// <summary>
    /// Gets or Sets the option to enable filtering in the component.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public virtual bool FilterEnabled { get; set; } = true;

    /// <summary>
    /// Gets or Sets the option to enable clearing of selected items in the component.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public virtual bool Clearable { get; set; } = true;

    /// <summary>
    /// Gets or Sets the option to enable multi-select functionality in the component.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public virtual bool MultiSelect { get; set; } = true;

    /// <summary>
    /// Gets or Sets the option to use a custom item renderer in the selection popover.
    /// </summary>
    [Parameter, SafeCategory("Common")]
    public virtual bool UseCustomItemRenderInSelectionPopover { get; set; } = false;

    /// <summary>
    /// Gets or Sets the RenderFragment for custom item template.
    /// </summary>
    [Parameter, SafeCategory("Common")]
    public RenderFragment<T> ItemTemplate { get; set; }

    /// <summary>
    /// Gets or Sets the list of items that are available for selection.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public IList<T> AvailableItems { get; set; }

    /// <summary>
    /// Gets or Sets the function that is used to asynchronously load available items.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public Func<CancellationToken, Task<IList<T>>> AvailableItemsLoadFunc { get; set; }

    /// <summary>
    /// Gets or Sets the function that converts an item to a string. This is used for display purposes.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public Func<T, string> ItemToStringFunc { get; set; } = (item => item?.ToString() ?? string.Empty);

    /// <summary>
    /// Gets or Sets the icon for the adornment of the component.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string AdornmentIcon { get; set; } = Icons.Material.Filled.ArrowDropDown;

    /// <summary>
    /// Gets or Sets the value of the component.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public T Value { get; set; }

    /// <summary>
    /// Gets or Sets a value indicating whether to update items on state change.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool UpdateItemsOnStateChange { get; set; }

    /// <summary>
    /// Event triggered when the selected items change.
    /// </summary>
    [Parameter, SafeCategory("Click action")]
    public EventCallback<IEnumerable<T>> SelectedChanged { get; set; }

    /// <summary>
    /// Event triggered when the value of the component changes.
    /// </summary>
    [Parameter, SafeCategory("Click action")]
    public EventCallback<T> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the currently selected items in the component.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public IEnumerable<T> Selected
    {
        get => _selected ??= new HashSet<T>();
        set
        {
            var set = (value ?? new HashSet<T>()).ToList();
            if (Selected.Count() != set.Count || !_selected.All(x => set.Contains(x)))
            {
                _selected = new HashSet<T>(set);
                OnBeforeSelectedChanged(_selected);
                RaiseChanged();
            }
        }
    }

    /// <summary>
    /// is called before the selected items change.
    /// </summary>
    protected virtual void OnBeforeSelectedChanged(IEnumerable<T> selected)
    {}

    private void RaiseChanged()
    {
        if (IsRendered)
        {
            SelectedChanged.InvokeAsync(new HashSet<T>(Selected));
            ValueChanged.InvokeAsync(Value);
        }
    }

    /// <summary>
    /// Search filter
    /// </summary>
    [Parameter]
    public string Filter
    {
        get => _filter;
        set
        {
            if (_filter != value)
            {
                _filter = value;
                StateHasChanged();
            }
        }
    }

    private IEnumerable<T> _selected;
    private string _filter;
    private string CssName => $"chip-select-{Enum.GetName(ViewMode)?.ToLower() ?? "none"} {(Selected?.Any() == true ? "with-items" : "empty")}";

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (AvailableItems == null || !AvailableItems.Any() || UpdateItemsOnStateChange)
            AvailableItems = await GetAvailableItemsAsync();
        await base.OnParametersSetAsync();
    }

    private void Remove(MudChip chip, T item)
    {
        ((HashSet<T>)_selected).Remove(item);
        RaiseChanged();
    }

    /// <summary>
    /// Render function for an item.
    /// </summary>
    public virtual string ItemNameRender(T item)
    {
        var res = ItemToStringFunc(item);
        if (!string.IsNullOrWhiteSpace(res) && LocalizerToUse != null && !string.IsNullOrWhiteSpace(LocalizerPattern))
        {
            return LocalizerToUse[string.Format(LocalizerPattern, item)];
        }

        return res;
    }

    /// <summary>
    /// returns the string for more selected items
    /// </summary>
    protected virtual string MultiSelectionTextFunc(List<string> arg) 
        => string.Join(", ", Selected.Where(a => a != null).Select(r => ItemNameRender(r)?.ToUpper(true)));

    /// <summary>
    /// returns all available items
    /// </summary>
    protected virtual Task<IList<T>> GetAvailableItemsAsync(CancellationToken cancellation = default) 
        => AvailableItemsLoadFunc != null ? AvailableItemsLoadFunc(cancellation) : Task.FromResult(new List<T>(0) as IList<T>);

    private void OnClose()
    {
        Filter = string.Empty;
    }

    private async Task OnFilterItemClick()
    {
        await FocusFilterInput();
    }

    private async Task OnOpen()
    {
        await FocusFilterInput();
       // await JsRuntime.InvokeVoidAsync("eval", $"document.querySelector('.mud-input-slot').classList.remove('mud-ex-no-events')");
    }

    private async Task FocusFilterInput()
    {        
        await JsRuntime.InvokeVoidAsync("MudExDomHelper.focusElementDelayed", ".mud-input-slot");
    }
}

/// <summary>
/// ViewMode for the ChipSelect
/// </summary>
public enum ViewMode
{
    /// <summary>
    /// Only Chips
    /// </summary>
    ChipsOnly,

    /// <summary>
    /// Chips above the filed
    /// </summary>
    ChipsAdditionalAbove,

    /// <summary>
    /// Chips below the field
    /// </summary>
    ChipsAdditionalBelow,

    /// <summary>
    /// No chips, just work as listbox
    /// </summary>
    NoChips
}