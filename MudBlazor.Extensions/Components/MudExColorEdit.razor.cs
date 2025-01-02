using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Utilities;
using System.Reflection;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component to Edit MudExColor
/// </summary>
public sealed partial class MudExColorEdit
{
    private MudColorOutputFormats? _suggestedFormat = null;
    private MudColor _initialMudColorValue;
    private KeyValuePair<string, MudColor>[] _cssVars;
    private MudColor[] _palette;
    private bool HasDefinedColors => ShowThemeColors || ShowHtmlColors || ShowCssColorVariables;
    private List<ColorItem> _colors = new();


    /// <summary>
    /// Gets or sets the variant filter.
    /// </summary>
    [Parameter, SafeCategory("Filtering")]
    public Variant FilterVariant { get; set; }


    /// <summary>
    /// Gets or sets the Css Variables.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public KeyValuePair<string, MudColor>[] CssVars
    {
        get => _cssVars;
        set => _cssVars = value;
    }

    /// <summary>
    /// Gets or sets the string value of the color picker selected by the user.
    /// </summary>
    [Parameter, SafeCategory("Data")]
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
    [Parameter, SafeCategory("Behavior")]
    public EventCallback<string> ValueStringChanged { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show theme colors.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public bool ShowThemeColors { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show HTML colors.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public bool ShowHtmlColors { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show CSS color variables.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public bool ShowCssColorVariables { get; set; } = true;

    /// <summary>
    /// Gets or sets the filter for the color picker.
    /// </summary>
    [Parameter, SafeCategory("Filtering")]
    public string Filter { get; set; }

    /// <summary>
    /// Gets or sets the color preview mode.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public ColorPreviewMode PreviewMode { get; set; } = ColorPreviewMode.Both;

    /// <summary>
    /// Gets or sets the auto close behavior.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public AutoCloseBehaviour AutoCloseBehaviour { get; set; } = AutoCloseBehaviour.OnDefinedSelect;

    /// <summary>
    /// Gets or sets the <see cref="MudColorOutputFormats"/>.
    /// </summary>
    [Parameter, SafeCategory("Misc")]
    public MudColorOutputFormats? MudColorStringFormat { get => _suggestedFormat; set => _suggestedFormat = value; }

    /// <summary>
    /// Gets or sets a value indicating whether to have always <see cref="MudColor"/> filled in <see cref="MudExColor"/> as the OneOf value.
    /// With this setting turned on, you can use this edit control for all of your Color Properties.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool ForceSelectOfMudColor { get; set; }

    /// <summary>
    /// True to use old render look and feel
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public bool LegacyRender { get; set; }

    /// <summary>
    /// Gets or sets the label for the custom tab.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string LabelCustomTab { get; set; } = "Custom";

    /// <summary>
    /// Gets or sets the label for the defined tab.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string LabelDefinedTab { get; set; } = "Defined";
    

    private async Task EnsureCssVarsAsync()
    {
        if (_cssVars == null || _cssVars.Length == 0)
        {            
            _cssVars = await JsRuntime.GetCssColorVariablesAsync();
            _palette = _cssVars.Select(x => x.Value).Distinct().ToArray();
            UpdateColors();
        }
    }

    private void UpdateColors()
    {
        _colors.Clear();
        if (ShowThemeColors)
        {
            foreach (Color color in Enum.GetValues(typeof(Color)))
                _colors.Add(new ColorItem(color, color.ToString(), TryLocalize("Theme Colors")));
        }
        if (ShowHtmlColors)
            _colors.AddRange(typeof(System.Drawing.Color).GetProperties(BindingFlags.Static | BindingFlags.Public).Select(c => new ColorItem(System.Drawing.Color.FromName(c.Name), c.Name, TryLocalize("HTML Colors"))));
        if (ShowCssColorVariables && _cssVars is not null && _cssVars.Length > 0)
            _colors.AddRange(_cssVars.Select(v => new ColorItem(v.Value, v.Key, TryLocalize("CSS Color Variables"))));
        StateHasChanged();
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
        if (PickerVariant is PickerVariant.Static)        
            await EnsureCssVarsAsync();        
        Text = ValueString;
        UpdatePreview();
        await base.OnParametersSetAsync();
        if (_colors.Count == 0)
            UpdateColors();
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var updateRequired = (parameters.TryGetValue<bool>(nameof(ShowThemeColors), out var showThemeColors) && ShowThemeColors != showThemeColors)
                             || (parameters.TryGetValue<bool>(nameof(ShowHtmlColors), out var showHtmlColors) && ShowHtmlColors != showHtmlColors)
                             || (parameters.TryGetValue<bool>(nameof(ShowCssColorVariables), out var showCssColorVariables) && ShowCssColorVariables != showCssColorVariables);
        await base.SetParametersAsync(parameters);
        if (updateRequired)
            UpdateColors();
    }

        
    /// <inheritdoc />
    protected override Task OnPickerOpenedAsync()
    {        
        _ = UpdateInitialMudColor();
        _ = EnsureCssVarsAsync();        
        return base.OnPickerOpenedAsync();
    }

    /// <inheritdoc />
    protected override async Task StringValueChangedAsync(string value)
    {
        SetSuggestedFormat();
        if (!Rendered)
            return;
        Touched = true;
        Value = Converter.Get(value);
        if (ForceSelectOfMudColor)
            await SetTextAsync(ValueString, false);

        UpdatePreview();
        RaiseChangedIf();
    }

    /// <inheritdoc />
    protected override void RaiseChanged()
    {
        base.RaiseChanged();
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
            CloseAsync();
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

    private MudColor FindFromCssVar(string var) => _cssVars?.FirstOrDefault(p => string.Equals($"var({p.Key})", var, StringComparison.CurrentCultureIgnoreCase) || string.Equals(p.Key, var, StringComparison.CurrentCultureIgnoreCase)).Value;

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
            _initialMudColorValue = (await Value.ToMudColorAsync(JsRuntime) ?? new MudColor(0,0,0,255));
        _initialMudColorValue = _initialMudColorValue.IsValid() ? _initialMudColorValue : new MudColor(0, 0, 0, 255);
    }

    private static bool IsCssVarStr(string s) => s.StartsWith("var(") || s.StartsWith("--");

    private bool GetIsOpen() => Open || PickerVariant == PickerVariant.Static;

    private string ColorItemStyle(ColorItem context)
    {
        var borderColor = MudExColor.Transparent;

        if(context.Name == ValueString || context.Color.ToCssStringValue(MudColorOutputFormats.HexA).Equals(Value.ToCssStringValue(MudColorOutputFormats.HexA)) || context.Color.ToCssStringValue(MudColorOutputFormats.RGBA) == ValueString)        
            borderColor = Color.Warning;
        
        return new MudExStyleBuilder()
            .WithBorder(1, Core.Css.BorderStyle.Solid, borderColor)
            .WithJustifyContent("flex-start")
            .Build();
    }
}