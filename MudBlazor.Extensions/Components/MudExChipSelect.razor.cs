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

    protected RenderFragment Inherited() => builder => base.BuildRenderTree(builder);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        MultiSelection = true;
        SearchBox = true;
        ValuePresenter = ViewMode == ViewMode.ChipsOnly ? Enums.ValuePresenter.Chip : Enums.ValuePresenter.Text;
        ChipCloseable = true;
        Clearable = true;        
    }

    /// <summary>
    /// Gets or Sets the Localizer Pattern.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string LocalizerPattern { get; set; } = "{0}";



    /// <summary>
    /// Gets or Sets the AutoFocus for the filter input.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AutoFocusFilter { get; set; }





    /// <summary>
    /// Gets or Sets whether to render the validation component.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool RenderValidationComponent { get; set; } = true;

  

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
    /// Gets or Sets the view mode for the component.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public virtual ViewMode ViewMode
    {
        get => _viewMode;
        set
        {
            _viewMode = value;
            ValuePresenter = ViewMode == ViewMode.ChipsOnly ? Enums.ValuePresenter.Chip : Enums.ValuePresenter.Text;
        }
    }


    /// <summary>
    /// Gets or Sets the option to enable filtering in the component.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public virtual bool FilterEnabled { get; set; } = true;


    /// <summary>
    /// Gets or Sets the option to enable multi-select functionality in the component.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    [Obsolete("Use MultiSelection instead.")]
    public virtual bool MultiSelect { 
        get => MultiSelection;
        set => MultiSelection = value;
    }

    /// <summary>
    /// Gets or Sets the option to use a custom item renderer in the selection popover.
    /// </summary>
    [Parameter, SafeCategory("Common")]
    public virtual bool UseCustomItemRenderInSelectionPopover { get; set; } = false;


    /// <summary>
    /// Gets or Sets the list of items that are available for selection.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    [Obsolete("Use ItemCollection instead.")]
    public IList<T> AvailableItems
    {
        get => ItemCollection?.ToList();
        set => ItemCollection = value;
    }

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
    /// Gets or sets the currently selected items in the component.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    [Obsolete("Use SelectedValues instead")]
    public IEnumerable<T> Selected {
        get => SelectedValues;
        set => SelectedValues = value;
      }


    private void RaiseChanged()
    {
        if (true)
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
    private ViewMode _viewMode = ViewMode.ChipsOnly;
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
    protected virtual string MultiSelectionTextFunc(List<T> arg) 
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