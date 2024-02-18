using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// MudExTagField is a component that allows the user to enter a list of values as chips.
/// </summary>
public partial class MudExTagField<T>
{
    private Adornment _renderChipsAdditional = Adornment.None;

    /// <summary>
    /// Set to true to allow duplicates
    /// </summary>    
    [Parameter, SafeCategory("Behavior")]
    public string DuplicateErrorText { get; set; } = "Duplicate values";

    /// <summary>
    /// Set to true to allow duplicates
    /// </summary>    
    [Parameter, SafeCategory("Behavior")]
    public bool AllowDuplicates { get; set; }

    /// <summary>
    /// Holds a list of values to be displayed as chips.
    /// </summary>
    /// <remarks>Used to store the data of each chip.</remarks>
    [Parameter, SafeCategory("Data")]
    public List<T> Values { get; set; }

    /// <summary>
    /// Triggered when the list of values changes.
    /// </summary>
    /// <remarks>Emits the updated list of values.</remarks>
    [Parameter, SafeCategory("Behavior")]
    public EventCallback<List<T>> ValuesChanged { get; set; }

    /// <summary>
    /// Sets the size of the chips.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public Size ChipSize { get; set; }

    /// <summary>
    /// Determines whether chips are set upon pressing the enter key. Default is true.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool SetChipsOnEnter { get; set; } = true;

    /// <summary>
    /// Determines where the chips should be rendered relative to the select box.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public virtual Adornment RenderChipsAdditional
    {
        get => _renderChipsAdditional;
        set
        {
            _renderChipsAdditional = value;
            ShowVisualiser = ShouldShowVisualiser();
        }
    }

    /// <summary>
    /// Sets the delimiter characters that will create a new chip.
    /// </summary>
    [Parameter, SafeCategory("Validation")]
    public char[] Delimiters { get; set; }

    /// <summary>
    /// Sets the CSS class for the chip elements.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string ClassChip { get; set; }

    /// <summary>
    /// Sets the CSS style for the chip elements.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string StyleChip { get; set; }

    /// <summary>
    /// Sets the color of the chips.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExColor ChipColor { get; set; }

    /// <summary>
    /// Sets the visual variant of the chips.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public Variant ChipVariant { get; set; }

    /// <summary>
    /// Determines whether the chips should wrap to the next line.
    /// </summary>
    [Parameter, SafeCategory("Misc")]
    public bool WrapChips { get; set; } = true;

    /// <summary>
    /// Determines whether the chips have a close button. Default is true.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool Closeable { get; set; } = true;

    /// <summary>
    /// Sets the maximum number of chips allowed.
    /// </summary>
    [Parameter, SafeCategory("Validation")]
    public int MaxChips { get; set; }


    /// <summary>
    /// Is true when the mouse is over a chip.
    /// </summary>
    [Parameter]
    public bool IsMouseOverChip { get; set; }

    /// <summary>
    /// Called when the value of IsMouseOverChip changes.
    /// </summary>
    [Parameter] 
    public EventCallback<bool> IsMouseOverChipChanged { get; set; }

    /// <summary>
    /// Callback when the mouse enters a chip.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnChipMouseOver { get; set; }
    
    /// <summary>
    /// Callback when the mouse leaves a chip.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnChipMouseOut { get; set; }

    /// <summary>
    /// Auto clears the input after adding a chip.
    /// </summary>
    [Parameter]
    public bool AutoClear { get; set; } = true;

    /// <summary>
    /// The animation type for errors.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public AnimationType ErrorAnimation { get; set; } = AnimationType.HeadShake;

    /// <summary>
    /// Appearance service for applying styles.
    /// </summary>
    [Inject] protected MudExAppearanceService AppearanceService { get; set; }
    
    /// <summary>
    /// Style builder for building styles.
    /// </summary>
    [Inject] protected MudExStyleBuilder StyleBuilder { get; set; }

    /// <summary>
    /// Maximum width of the chips.
    /// </summary>
    [Parameter]
    public MudExSize<double> ChipsMaxWidth { get; set; } = new(80, CssUnit.Percentage);

    /// <summary>
    /// Style for the chips.
    /// </summary>
    protected string ChipStyleName =>
        new MudExStyleBuilder()
            .AddRaw(StyleChip)
            .WithColorForVariant(ChipVariant, ChipColor, !ChipColor.IsColor)    
            .Build();

    /// <summary>
    /// Class name for the chip elements.
    /// </summary>
    protected string ChipClassname =>
        new MudExCssBuilder("d-flex")
            .AddClass("flex-wrap", WrapChips)
            .AddClass("mt-5", Variant == Variant.Filled)
            .Build();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Immediate = true;
        DataVisualiser = RenderDataVisualizer;
        base.OnInitialized();
    }

    private bool ShouldShowVisualiser() => Values?.Any() == true && RenderChipsAdditional == Adornment.None;

    /// <inheritdoc />
    protected override async Task InvokeKeyDownAsync(KeyboardEventArgs args)
    {
        await base.InvokeKeyDownAsync(args);
        if (((Delimiters?.Contains(args.Key[0]) == true && args.Key.Length == 1) || (SetChipsOnEnter && args.Key == "Enter")) && Value != null)        
            await ApplyChips();        

        if (args.Key == "Backspace" && string.IsNullOrEmpty(Converter.Set(Value)) && Values.Any())
        {
            Values.RemoveAt(Values.Count - 1);
            await InvokeValuesChanged();
        }
    }

    private async Task ApplyChips()
    {
        Values ??= new();
        if (Value == null || (Values.Contains(Value) && !AllowDuplicates))
        {
            await SetErrorWithStyle(DuplicateErrorText);
            return;
        }
        SetError();
        Values.Add(Value);
        await InvokeValuesChanged();
        if (AutoClear)
        {
            if (RuntimeLocation.IsServerSide)
                await BlurAsync();
            
            await Clear();

            if (RuntimeLocation.IsServerSide)
                await FocusAsync();
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SetErrorWithStyle(string text)
    {
        SetError(text);        
        await AppearanceService.ApplyTemporarilyToAsync(StyleBuilder.Clear().WithAnimation(ErrorAnimation), this);    
    }

    private void SetError(string error = null) => Error = !string.IsNullOrEmpty(ErrorText = TryLocalize(error));

    private async Task Remove(MudChip chip)
    {        
        if (Disabled || ReadOnly)        
            return;        
        Values.Remove((T)chip.Value);
        IsMouseOverChip = false;
        await IsMouseOverChipChanged.InvokeAsync(IsMouseOverChip);
        await InvokeValuesChanged();
        await FocusAsync();
    }

    private async Task InvokeValuesChanged()
    {
        ShowVisualiser = ShouldShowVisualiser();
        ForceShrink = ShowVisualiser;
        await ValuesChanged.InvokeAsync(Values);        
    }

    private async Task HandleOnChipMouseOver(MouseEventArgs arg)
    {
        IsMouseOverChip = true;
        await IsMouseOverChipChanged.InvokeAsync(IsMouseOverChip);
        await OnChipMouseOver.InvokeAsync(arg);
    }

    private async Task HandleOnChipMouseOut(MouseEventArgs arg)
    {
        IsMouseOverChip = false;
        await IsMouseOverChipChanged.InvokeAsync(IsMouseOverChip);
        await OnChipMouseOut.InvokeAsync(arg);
    }
}