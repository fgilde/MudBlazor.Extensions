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

    [Inject] protected IServiceProvider ServiceProvider { get; set; }
    protected IStringLocalizer LocalizerToUse => Localizer ?? _fallbackLocalizer;

    [Parameter] public IStringLocalizer Localizer { get; set; }
    [Parameter] public Variant FilterVariant { get; set; }
    [Parameter] public MudExColor Value { get => _value; set => _value = value; }
    [Parameter] public string ValueString
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

    [Parameter] public bool ShowThemeColors { get; set; } = true;
    [Parameter] public bool ShowHtmlColors { get; set; } = true;
    [Parameter] public bool ShowCssColorVariables { get; set; } = true;
    [Parameter] public string Filter { get; set; }
    [Parameter] public ColorPreviewMode PreviewMode { get; set; } = ColorPreviewMode.Both;
    [Parameter] public EventCallback<MudExColor> ValueChanged { get; set; }
    [Parameter] public bool DelayValueChangeToPickerClose { get; set; }
    [Parameter] public AutoCloseBehaviour AutoCloseBehaviour { get; set; } = AutoCloseBehaviour.OnDefinedSelect;
    [Parameter] public MudColorOutputFormats? MudColorStringFormat { get => _suggestedFormat; set => _suggestedFormat = value; }

    /// <summary>
    /// Set to true to have always MudColor filled in MudExColor as the OneOf value.
    /// With this setting turned on, you can use this edit control for all of your Color Properties
    /// </summary>
    [Parameter] public bool ForceSelectOfMudColor { get; set; }

    private KeyValuePair<string, MudColor>[] _cssVars;
    private MudColor[] _palette;

    private bool HasDefinedColors => ShowThemeColors || ShowHtmlColors || ShowCssColorVariables;

    public string TryLocalize(string text, params object[] args) => LocalizerToUse.TryLocalize(text, args);


    protected override async Task OnInitializedAsync()
    {
        _cssVars = await MudExCss.GetCssColorVariablesAsync();
        _palette = _cssVars.Select(x => x.Value).Distinct().ToArray();
        await base.OnInitializedAsync();
    }
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

    protected override async Task OnParametersSetAsync()
    {
        //await UpdateInitialMudColor();
        Text = ValueString;
        UpdatePreview();
        await base.OnParametersSetAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender || !rendered)
            rendered = true;
        base.OnAfterRender(firstRender);
    }

    protected override void OnPickerClosed()
    {
        if (DelayValueChangeToPickerClose)
            ValueChanged.InvokeAsync(Value);
    }

    protected override void OnPickerOpened()
    {
        _ = UpdateInitialMudColor();
        base.OnPickerOpened();
    }
    
    protected override async Task StringValueChanged(string value)
    {
        if(!rendered)
            return;
        SetSuggestedFormat();
        Touched = true;
        Value = Converter.Get(value);
        if (ForceSelectOfMudColor)
            await SetTextAsync(ValueString, false);

        UpdatePreview();
        RaiseChangedIf();
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
        ValueChanged.InvokeAsync(Value);
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