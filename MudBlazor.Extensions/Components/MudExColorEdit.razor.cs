using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component to Edit MudExColor
/// </summary>
public sealed partial class MudExColorEdit
{
    private MudColorOutputFormats? _suggestedFormat = null;
    private MudColor _initialMudColorValue;
    private IStringLocalizer<MudExColorEdit> _fallbackLocalizer => ServiceProvider.GetService<IStringLocalizer<MudExColorEdit>>();
    private bool rendered;

    /// <summary>
    /// Gets or sets the <see cref="IServiceProvider"/> to be used for dependency injection.
    /// </summary>
    [Inject]
    protected IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// Gets the <see cref="IStringLocalizer"/> to be used for localizing strings.
    /// </summary>
    protected IStringLocalizer LocalizerToUse => Localizer ?? _fallbackLocalizer;

    /// <summary>
    /// Gets or sets the <see cref="IStringLocalizer"/> to be used for localizing strings.
    /// </summary>
    [Parameter]
    public IStringLocalizer Localizer { get; set; }

    /// <summary>
    /// Gets or sets the variant filter.
    /// </summary>
    [Parameter]
    public Variant FilterVariant { get; set; }

    /// <summary>
    /// Gets or sets the value of the color picker.
    /// </summary>
    [Parameter]
    public MudExColor Value { get => _value; set => _value = value; }

    /// <summary>
    /// Gets or sets the callback method when the value of the color picker is changed.
    /// </summary>
    [Parameter]
    public EventCallback<MudExColor> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the Css Variables.
    /// </summary>
    [Parameter]
    public KeyValuePair<string, MudColor>[] CssVars
    {
        get => _cssVars;
        set => _cssVars = value;
    }

    /// <summary>
    /// Gets or sets the string value of the color picker selected by the user.
    /// </summary>
    [Parameter]
    public string ValueString
    {
        get
        {
            if (ForceSelectOfMudColor)
            {
                var res = _value.ToCssStringValue(_suggestedFormat ?? _value.SuggestedFormat);
                if (IsCssVarStr(res)) // This can happen is editable and user writes the name of a variable. So wee need to ensure color again. Otherwise it happens on selection
                {
                    res = FindFromCssVar(res)?.ToString(_suggestedFormat ?? _value.SuggestedFormat) ?? res;
                }

                return res;
            }
            return _value.ToString();
        }
        set => _value = value;
    }

    /// <summary>
    /// Gets or sets the callback method when the string value of the color picker is changed.
    /// </summary>
    [Parameter]
    public EventCallback<string> ValueStringChanged { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show theme colors.
    /// </summary>
    [Parameter]
    public bool ShowThemeColors { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show HTML colors.
    /// </summary>
    [Parameter]
    public bool ShowHtmlColors { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show CSS color variables.
    /// </summary>
    [Parameter]
    public bool ShowCssColorVariables { get; set; } = true;

    /// <summary>
    /// Gets or sets the filter for the color picker.
    /// </summary>
    [Parameter]
    public string Filter { get; set; }

    /// <summary>
    /// Gets or sets the color preview mode.
    /// </summary>
    [Parameter]
    public ColorPreviewMode PreviewMode { get; set; } = ColorPreviewMode.Both;

    /// <summary>
    /// Gets or sets a value indicating whether to delay the value change of the color picker until the picker is closed.
    /// </summary>
    [Parameter]
    public bool DelayValueChangeToPickerClose { get; set; }

    /// <summary>
    /// Gets or sets the auto close behavior.
    /// </summary>
    [Parameter]
    public AutoCloseBehaviour AutoCloseBehaviour { get; set; } = AutoCloseBehaviour.OnDefinedSelect;

    /// <summary>
    /// Gets or sets the <see cref="MudColorOutputFormats"/>.
    /// </summary>
    [Parameter]
    public MudColorOutputFormats? MudColorStringFormat { get => _suggestedFormat; set => _suggestedFormat = value; }

    /// <summary>
    /// Gets or sets a value indicating whether to have always <see cref="MudColor"/> filled in <see cref="MudExColor"/> as the OneOf value.
    /// With this setting turned on, you can use this edit control for all of your Color Properties.
    /// </summary>
    [Parameter]
    public bool ForceSelectOfMudColor { get; set; }

    /// <summary>
    /// Gets or sets the label for the custom tab.
    /// </summary>
    [Parameter]
    public string LabelCustomTab { get; set; } = "Custom";

    /// <summary>
    /// Gets or sets the label for the defined tab.
    /// </summary>
    [Parameter]
    public string LabelDefinedTab { get; set; } = "Defined";

    private KeyValuePair<string, MudColor>[] _cssVars;
    private MudColor[] _palette ;

    private bool HasDefinedColors => ShowThemeColors || ShowHtmlColors || ShowCssColorVariables;


    public string TryLocalize(string text, params object[] args) => LocalizerToUse.TryLocalize(text, args);

    
    private async Task EnsureCssVarsAsync()
    {
        if (_cssVars == null || _cssVars.Length == 0)
        {
            _cssVars = await MudExCss.GetCssColorVariablesAsync();
            _palette = _cssVars.Select(x => x.Value).Distinct().ToArray();
        }
    }


    /// <inheritdoc />
    protected override void OnInitialized()
    {
        AdornmentIcon = Icons.Material.Filled.ColorLens;
        Editable = true;
        Converter = new DefaultConverter<MudExColor>();
        Converter.GetFunc = OnGet;
        Converter.SetFunc = OnSet;
        Class = string.IsNullOrEmpty(Class) || !Class.Contains("mud-ex-color-edit") ? $"{Class} mud-ex-color-edit" : Class;
        base.OnInitialized();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (PickerVariant == PickerVariant.Static || PickerVariant == PickerVariant.Dialog)
        {
            await EnsureCssVarsAsync();
        }
        Text = ValueString;
        UpdatePreview();
        await base.OnParametersSetAsync();
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender || !rendered)
            rendered = true;
        base.OnAfterRender(firstRender);
    }

    /// <inheritdoc />
    protected override void OnPickerClosed()
    {
        if (DelayValueChangeToPickerClose)
            RaiseChanged();
    }
    
    /// <inheritdoc />
    protected override void OnPickerOpened()
    {
        _ = UpdateInitialMudColor();
        _ = EnsureCssVarsAsync();
        base.OnPickerOpened();
    }

    /// <inheritdoc />
    protected override async Task StringValueChanged(string value)
    {
        SetSuggestedFormat();
        if (!rendered)
            return;
        Touched = true;
        Value = Converter.Get(value);
        if (ForceSelectOfMudColor)
            await SetTextAsync(ValueString, false);

        UpdatePreview();
        RaiseChangedIf();
    }

    private void RaiseChanged()
    {
        ValueChanged.InvokeAsync(Value);
        ValueStringChanged.InvokeAsync(ValueString);
    }

    private void SetSuggestedFormat()
    {
        _suggestedFormat ??= string.IsNullOrEmpty(Text) ? _value.SuggestedFormat :MudExColor.GetSuggestedFormat(Text);
    }

    private bool Matches(string value)
    {
        return string.IsNullOrEmpty(Filter)
               || value.Contains(Filter, StringComparison.InvariantCultureIgnoreCase);
    }


    private string OnSet(MudExColor color)
    {
        UpdatePreview();
        RaiseChangedIf();
        return Text = ValueString;
    }

    private void RaiseChangedIf()
    {
        if (DelayValueChangeToPickerClose && IsOpen)
            return;
        RaiseChanged();
    }


    private void UpdatePreview()
    {
        Style = MudExStyleBuilder.Empty()
            .WithColor(Value, PreviewMode is ColorPreviewMode.Text or ColorPreviewMode.Both)
            .WithFill(Value, PreviewMode is ColorPreviewMode.Icon or ColorPreviewMode.Both)
            .Build();
    }

    private MudExColor OnGet(string value) => Value = value;


    private void CloseIf(AutoCloseBehaviour behaviour)
    {
        if (AutoCloseBehaviour == behaviour || AutoCloseBehaviour == AutoCloseBehaviour.Always)
            Close();
    }

    private void Select(string color)
    {
        if (ForceSelectOfMudColor && TryFindMudColorFor(color, out var mudColor))
            Select(mudColor, false);
        else
            OnSet(Value = color);
        CloseIf(AutoCloseBehaviour.OnDefinedSelect);
    }


    private void Select(MudColor color) => Select(color, true);
    
    private void Select(MudColor color, bool tryClose)
    {
        OnSet(Value = color);
        if (tryClose)
            CloseIf(AutoCloseBehaviour.OnCustomSelect);
    }

    private void Select(Color color)
    {
        if (ForceSelectOfMudColor && TryFindMudColorFor(color, out var mudColor))
            Select(mudColor, false);
        else
            OnSet(Value = color);
        CloseIf(AutoCloseBehaviour.OnDefinedSelect);
    }

    private bool TryFindMudColorFor(string s, out MudColor color)
    {
        color = default;
        try
        {
            color = new MudColor(s);
            return color.IsValid();
        }
        catch (Exception )
        {
            return false;
        }
    }

    private bool TryFindMudColorFor(Color colorEnum, out MudColor color)
    {
        color = default;
        try
        {
            color = FindFromCssVar(colorEnum.CssVarName());
            return color.IsValid();
        }
        catch (Exception)
        {
            return false;
        }
    }

    private MudColor? FindFromCssVar(string var) => _cssVars?.FirstOrDefault(p => string.Equals($"var({p.Key})", var, StringComparison.CurrentCultureIgnoreCase) || string.Equals(p.Key, var, StringComparison.CurrentCultureIgnoreCase)).Value;

    private void SelectCssVar(KeyValuePair<string, MudColor> cssVar)
    {
        if (ForceSelectOfMudColor)
        {
            Select(cssVar.Value, false);
            CloseIf(AutoCloseBehaviour.OnDefinedSelect);
        }
        else
        {
            Select($"var({cssVar.Key})");
        }
    }

    private async Task CustomTabClick()
    {
        await UpdateInitialMudColor();
    }

    private async Task UpdateInitialMudColor()
    {
        if (Value.IsString && IsCssVarStr(Value.AsString))
            _initialMudColorValue = FindFromCssVar(Value.AsString);
        else if (Value.IsColor && TryFindMudColorFor(Value.AsColor, out var color))
            _initialMudColorValue = color;
        else 
            _initialMudColorValue = (await Value.ToMudColorAsync() ?? new MudColor(0,0,0,255));
        _initialMudColorValue = _initialMudColorValue.IsValid() ? _initialMudColorValue : new MudColor(0, 0, 0, 255);
    }

    private static bool IsCssVarStr(string s) => s.StartsWith("var(") || s.StartsWith("--");

    private bool GetIsOpen() => IsOpen || PickerVariant == PickerVariant.Static || PickerVariant == PickerVariant.Dialog;
}

public enum ColorPreviewMode
{
    None,
    Text,
    Icon,
    Both
}

public enum AutoCloseBehaviour
{
    Never,
    Always,
    OnDefinedSelect,
    OnCustomSelect
}