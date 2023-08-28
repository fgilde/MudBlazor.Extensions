using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using MudBlazor.Utilities;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

public partial class MudExTagField<T> : IMudExComponent
{
    protected string ChipClassname =>
          new MudExCssBuilder("d-flex")
           .AddClass("flex-wrap", WrapChips)
           .AddClass("mt-5", Variant == Variant.Filled)
           .Build();


    MudExTextField<T> _textFieldExtendedReference;
    T _internalValue;

    /// <summary>
    /// /The list of values.
    /// </summary>
    [Parameter]
    public List<string> Values { get; set; }

    /// <summary>
    /// Fires when values changed
    /// </summary>
    [Parameter]
    public EventCallback<List<string>> ValuesChanged { get; set; }

    [Parameter]
    public Size ChipSize { get; set; }

    [Parameter]
    public bool SetChipsOnEnter { get; set; } = true;

    /// <summary>
    /// The char that created a new chip with current value.
    /// </summary>
    [Parameter]
    public char[] Delimiters { get; set; }

    [Parameter]
    public string ClassChip { get; set; }

    [Parameter]
    public string StyleChip { get; set; }

    [Parameter]
    public MudExColor ChipColor { get; set; }

    [Parameter]
    public Variant ChipVariant { get; set; }

    [Parameter]
    public bool WrapChips { get; set; }

    /// <summary>
    /// Determines that chips have close button. Default is true.
    /// </summary>
    [Parameter]
    public bool Closeable { get; set; } = true;

    [Parameter]
    public int MaxChips { get; set; }

    [Parameter]
    public MudExSize<double> ChipsMaxWidth { get; set; } = new(80, CssUnit.Percentage);

    protected string ChipStyleName =>
        new MudExStyleBuilder()
            .AddRaw(StyleChip)
            .WithColorForVariant(ChipVariant, ChipColor, !ChipColor.IsColor)    
            .Build();

    protected async Task HandleKeyDown(KeyboardEventArgs args)
    {

        if (((Delimiters?.Contains(args.Key[0]) == true && args.Key.Length == 1 ) || (SetChipsOnEnter && args.Key == "Enter") ) && _internalValue != null)
        {
            await SetChips();
            StateHasChanged();
        }

        if (args.Key == "Backspace" && string.IsNullOrEmpty(Converter.Set(_internalValue)) && Values.Any())
        {
            Values.RemoveAt(Values.Count - 1);
            await ValuesChanged.InvokeAsync(Values);
        }
        await Task.Delay(10);
        await SetValueAsync(_internalValue);
        await OnKeyDown.InvokeAsync(args);
    }

    protected async Task HandleKeyUp(KeyboardEventArgs args)
    {
        await OnKeyUp.InvokeAsync(args);
    }

    protected async Task SetChips()
    {
        Values ??= new();
        Values.Add(Converter.Set(_internalValue));
        await ValuesChanged.InvokeAsync(Values);
        if (RuntimeLocation.IsServerSide)
        {
            await _textFieldExtendedReference.BlurAsync();
        }
        else
        {
            await Task.Delay(10);
        }
        await _textFieldExtendedReference.Clear();
        if (RuntimeLocation.IsServerSide)
        {
            await _textFieldExtendedReference.FocusAsync();
        }
    }

    public async Task Closed(MudChip chip)
    {
        if (Disabled || ReadOnly)        
            return;        
        Values.Remove(chip.Text);
        await ValuesChanged.InvokeAsync(Values);
        await _textFieldExtendedReference.FocusAsync();
    }
}