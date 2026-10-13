using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
using MudBlazor.Utilities;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// MudExTagField is a component that allows the user to enter a list of values as chips.
/// </summary>
public partial class MudExTagField<T>
{
    private Adornment _renderChipsAdditional = Adornment.None;
    private List<T> _values;
    private string _trackedText;
    private bool _committing;
    private bool _chipError;
    private List<T> _suggestions = [];
    private bool _suggestionsOpen;
    private int _activeSuggestion = -1;
    private CancellationTokenSource _searchCts;

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
    public List<T> Values
    {
        get => _values;
        set
        {
            if ((value != null && _values == null) || (value == null && _values != null) ||
                (value != null && _values != null && !value.SequenceEqual(_values)))
            {
                _values = value;
                _ = InvokeValuesChanged();
            }
        }
    }

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
    public EventCallback<ChipMouseEventArgs<T>> OnChipMouseOver { get; set; }
    
    /// <summary>
    /// Callback when the mouse leaves a chip.
    /// </summary>
    [Parameter]
    public EventCallback<ChipMouseEventArgs<T>> OnChipMouseOut { get; set; }

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
    /// Returns the suggestions for the typed text, shown in a dropdown below the field. Values that are already chips are not offered (unless <see cref="AllowDuplicates"/>).
    /// Arrow keys move the highlight, Enter or a click picks the highlighted suggestion.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public Func<string, CancellationToken, Task<IEnumerable<T>>> SearchFunc { get; set; }

    /// <summary>
    /// Maximum number of suggestions shown. 0 shows all returned by <see cref="SearchFunc"/>.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public int MaxSuggestions { get; set; }

    /// <summary>
    /// Returns the error text for an invalid value, null for a valid one.
    /// Invalid typed text is not added, stays in the field and shows the error; invalid values already in <see cref="Values"/> are shown as error colored chips.
    /// </summary>
    [Parameter, SafeCategory("Validation")]
    public Func<T, string> ValidateChip { get; set; }

    /// <summary>
    /// Decides per value whether its chip can be removed. A fixed chip has no close icon and Backspace does not remove it. Default is every chip.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public Func<T, bool> IsChipClosable { get; set; }

    /// <summary>
    /// If true, the typed text is added as chip(s) when the field loses focus. Default is false.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool CommitOnBlur { get; set; }
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
    /// Style for chips with an invalid value (the chip color is the error color).
    /// </summary>
    protected string ChipInvalidStyleName => new MudExStyleBuilder().AddRaw(StyleChip).Build();

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

    /// <inheritdoc />
    protected override async Task SetTextAndUpdateValueAsync(string text, bool updateValue = true)
    {
        _trackedText = text;
        if (_committing)
        {
            await base.SetTextAndUpdateValueAsync(text, updateValue);
            return;
        }

        if (_chipError)
            await SetErrorAsync();

        if (HasSeparator(text))
        {
            await CommitAsync(text);
            return;
        }

        await base.SetTextAndUpdateValueAsync(text, updateValue);
        await SearchAsync(text);
    }

    private bool ShouldShowVisualiser() => Values?.Any() == true && RenderChipsAdditional == Adornment.None;

    private char[] Separators => Delimiters == null ? [] : Delimiters.Contains(' ') ? [.. Delimiters, '\n', '\r', '\t'] : Delimiters;

    private bool HasSeparator(string text) => text != null && Separators.Length > 0 && text.IndexOfAny(Separators) >= 0;

    /// <inheritdoc />
    protected override async Task InvokeKeyDownAsync(KeyboardEventArgs args)
    {
        await base.InvokeKeyDownAsync(args);
        var pickable = _suggestionsOpen && _activeSuggestion >= 0 && _activeSuggestion < _suggestions.Count;
        switch (args.Key)
        {
            case "ArrowDown" or "ArrowUp" when SearchFunc != null:
                await MoveSuggestionAsync(args.Key == "ArrowDown");
                break;
            case "Enter" when pickable:
                await PickAsync(_suggestions[_activeSuggestion]);
                break;
            case "Enter" when SetChipsOnEnter:
                await CommitAsync(_trackedText);
                break;
            case "Escape" when _suggestionsOpen || !string.IsNullOrEmpty(_trackedText):
                CloseSuggestions();
                await SetErrorAsync();
                await SetTextCoreAsync(string.Empty);
                break;
            case "Backspace" when string.IsNullOrEmpty(_trackedText) && Values?.Count > 0 && IsClosable(Values[^1]):
                Values.RemoveAt(Values.Count - 1);
                await InvokeValuesChanged();
                break;
        }
    }

    /// <inheritdoc />
    protected override async Task OnBlurredAsync(FocusEventArgs obj)
    {
        await base.OnBlurredAsync(obj);
        // the suggestion list keeps the focus in the input while it is clicked (mousedown is prevented), so a blur is the field really being left
        CloseSuggestions();
        if (CommitOnBlur && !_committing)
            await CommitAsync(_trackedText, focusAfter: false);
    }

    private async Task SearchAsync(string text)
    {
        if (SearchFunc == null || Disabled || ReadOnly)
            return;
        _searchCts?.Cancel();
        var cts = _searchCts = new CancellationTokenSource();
        IEnumerable<T> found;
        try
        {
            found = await SearchFunc(text ?? string.Empty, cts.Token) ?? [];
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cts.IsCancellationRequested)
            return;
        var items = found.Where(s => AllowDuplicates || Values?.Contains(s) != true);
        _suggestions = (MaxSuggestions > 0 ? items.Take(MaxSuggestions) : items).ToList();
        _activeSuggestion = _suggestions.Count > 0 ? 0 : -1;
        _suggestionsOpen = _suggestions.Count > 0;
        await InvokeAsync(StateHasChanged);
    }

    private async Task MoveSuggestionAsync(bool down)
    {
        if (!_suggestionsOpen || _suggestions.Count == 0)
        {
            await SearchAsync(_trackedText);
            return;
        }

        _activeSuggestion = (_activeSuggestion + (down ? 1 : -1) + _suggestions.Count) % _suggestions.Count;
        await InvokeAsync(StateHasChanged);
    }

    private void CloseSuggestions()
    {
        _searchCts?.Cancel();
        _suggestionsOpen = false;
    }

    private async Task PickAsync(T item)
    {
        CloseSuggestions();
        await ApplyAsync([new Entry(ConvertSet(item), item, null)], clearText: true);
    }

    private Task CommitAsync(string text, bool focusAfter = true)
    {
        string[] tokens = Separators.Length > 0 ? text?.Split(Separators, StringSplitOptions.RemoveEmptyEntries) ?? [] : string.IsNullOrWhiteSpace(text) ? [] : [text];
        var entries = tokens.Select(token =>
        {
            var value = ConvertGet(token);
            return new Entry(token, value, ConversionError ? ConversionErrorMessage : null);
        }).ToList();
        return ApplyAsync(entries, clearText: AutoClear, focusAfter);
    }

    private async Task ApplyAsync(List<Entry> entries, bool clearText, bool focusAfter = true)
    {
        CloseSuggestions();
        var added = new List<T>();
        var rejected = new List<string>();
        var duplicates = new List<string>();
        string error = null;
        foreach (var entry in entries)
        {
            var entryError = entry.ConversionError ?? (entry.Value is null ? null : ValidateChip?.Invoke(entry.Value));
            if (entryError != null)
            {
                if (!rejected.Contains(entry.Text))
                    rejected.Add(entry.Text);
                error ??= entryError;
            }
            else if (!AllowDuplicates && (Values?.Contains(entry.Value) == true || added.Contains(entry.Value)))
            {
                duplicates.Add(entry.Text);
            }
            else
            {
                added.Add(entry.Value);
            }
        }

        // a single repeated entry keeps its text and says why; inside a longer paste duplicates are dropped silently
        if (added.Count == 0 && rejected.Count == 0 && duplicates.Count > 0)
        {
            rejected = duplicates.Distinct().ToList();
            error = DuplicateErrorText;
        }

        if (added.Count > 0)
        {
            Values ??= new();
            Values.AddRange(added);
            await InvokeValuesChanged();
        }

        if (clearText)
        {
            await SetTextCoreAsync(string.Join(Separators.FirstOrDefault(' '), rejected));
            if (RuntimeLocation.IsServerSide && focusAfter && added.Count > 0)
            {
                // the blur/focus round trip refreshes the input on the server; its blur event must not commit again
                _committing = true;
                try
                {
                    await BlurAsync();
                    await FocusAsync();
                }
                finally
                {
                    _committing = false;
                }
            }
        }

        // after the text update, which validates the field and would reset the error
        if (error == null)
            await SetErrorAsync();
        else
            await SetErrorWithStyle(error);
        _chipError = error != null;

        await InvokeAsync(StateHasChanged);
    }

    private async Task SetTextCoreAsync(string text)
    {
        _committing = true;
        _trackedText = text;
        try
        {
            await SetText(text);
            await base.SetTextAndUpdateValueAsync(text);
        }
        finally
        {
            _committing = false;
        }
    }

    private async Task SetErrorWithStyle(string text)
    {
        await SetErrorAsync(text);
        await AppearanceService.ApplyTemporarilyToAsync(StyleBuilder.Clear().WithAnimation(ErrorAnimation), this);    
    }

    private async Task SetErrorAsync(string error = null)
    {
        _chipError = false;
        var text = TryLocalize(error);
        await ErrorTextState.SetValueAsync(text);
        await ErrorState.SetValueAsync(!string.IsNullOrEmpty(text));
    }

    private string ChipError(T value) => ValidateChip?.Invoke(value);

    private bool IsClosable(T value) => IsChipClosable?.Invoke(value) ?? true;

    private EventCallback<MudChip<T>> CloseOf(T value) =>
        Disabled || ReadOnly || !Closeable || !IsClosable(value) ? default : EventCallback.Factory.Create<MudChip<T>>(this, Remove);

    private sealed record Entry(string Text, T Value, string ConversionError);

    private async Task Remove(MudChip<T> chip)
    {        
        if (Disabled || ReadOnly)        
            return;        
        Values.Remove(chip.Value);
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

    private async Task HandleOnChipMouseOver(MouseEventArgs arg, T value)
    {
        IsMouseOverChip = true;
        var args = ChipMouseEventArgs<T>.Create(arg, value);
        await IsMouseOverChipChanged.InvokeAsync(IsMouseOverChip);
        await OnChipMouseOver.InvokeAsync(args);
    }

    private async Task HandleOnChipMouseOut(MouseEventArgs arg, T value)
    {
        IsMouseOverChip = false;
        var args = ChipMouseEventArgs<T>.Create(arg, value);
        await IsMouseOverChipChanged.InvokeAsync(IsMouseOverChip);
        await OnChipMouseOut.InvokeAsync(args);
    }
}

public class ChipMouseEventArgs<T> : MouseEventArgs
{
    // the copy runs over reflection, and blazor only ever writes MouseEventArgs (it deserializes
    // the browser event into it). A trimmed application therefore drops the getters nobody calls -
    // ScreenX/ScreenY/MovementX/MovementY at the time of writing - and reading them throws.
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(MouseEventArgs))]
    public static ChipMouseEventArgs<T> Create(MouseEventArgs args, T value) => args.MapTo<ChipMouseEventArgs<T>>().SetProperties(a => a.Value = value);

    public T Value { get; set; }
}
