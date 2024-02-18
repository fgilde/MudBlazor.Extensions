using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;


namespace MudBlazor.Extensions.Components;

/// <summary>
/// A component that allows the user to select a value from a list of possible values and display it as Chips or default Combobox.
/// </summary>
/// <typeparam name="T"></typeparam>
[Obsolete($"Use {nameof(MudExSelect<T>)} instead and set {nameof(MultiSelection)} to true and {nameof(ValuePresenter)} to {nameof(Options.ValuePresenter.Chip)}.")]
public partial class MudExChipSelect<T>
{

    private RenderFragment Inherited() => builder => base.BuildRenderTree(builder);


    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        MultiSelection = true;        
        ValuePresenter = ViewMode == ViewMode.ChipsOnly ? Options.ValuePresenter.Chip : Options.ValuePresenter.Text;                
    }

    #region Obsolete

    private ViewMode _viewMode = ViewMode.ChipsOnly;

    /// <summary>
    /// Gets or Sets the AutoFocus for the filter input.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    [Obsolete($"Use {nameof(SearchBoxAutoFocus)} instead.")]
    public bool AutoFocusFilter
    {
        get => SearchBoxAutoFocus;
        set => SearchBoxAutoFocus = value;
    }


    /// <summary>
    /// Gets or Sets the color of the chip in the component.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    [Obsolete($"Use {nameof(Color)} instead.")]
    public virtual MudExColor ChipColor
    {
        get => Color;
        set => Color = value;
    }


    /// <summary>
    /// Gets or Sets the option to enable filtering in the component.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    [Obsolete($"Use {nameof(SearchBox)} instead.")]
    public virtual bool FilterEnabled
    {
        get => SearchBox;
        set => SearchBox = value;
    }


    /// <summary>
    /// Gets or Sets the option to enable multi-select functionality in the component.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]    
    [Obsolete($"Use {nameof(MultiSelection)} instead.")]
    public virtual bool MultiSelect
    {
        get => MultiSelection;
        set => MultiSelection = value;
    }


    /// <summary>
    /// Gets or Sets the list of items that are available for selection.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    [Obsolete($"Use {nameof(ItemCollection)} instead.")]
    public IList<T> AvailableItems
    {
        get => ItemCollection?.ToList();
        set => ItemCollection = value;
    }
    
    /// <summary>
    /// Search filter
    /// </summary>
    [Parameter]
    [Obsolete($"Use {nameof(SearchString)} instead.")]
    public string Filter
    {
        get => SearchString;
        set => SearchString = value;
    }

    /// <summary>
    /// Gets or Sets the view mode for the component.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    [Obsolete($"Use {nameof(RenderChipsAdditional)} in combination with {nameof(ValuePresenter)} instead.")]
    public virtual ViewMode ViewMode
    {
        get => _viewMode;
        set
        {
            _viewMode = value;
            ValuePresenter = ViewMode == ViewMode.ChipsOnly ? Options.ValuePresenter.Chip : Options.ValuePresenter.Text;
            RenderChipsAdditional = ViewMode switch
            {
                ViewMode.ChipsAdditionalAbove => Adornment.Start,
                ViewMode.ChipsAdditionalBelow => Adornment.End,
                _ => Adornment.None
            };
            StateHasChanged();
        }
    }

    #endregion

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