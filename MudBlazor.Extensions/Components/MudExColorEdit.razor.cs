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
public partial class MudExColorEdit
{
    [Inject] protected IServiceProvider ServiceProvider { get; set; }
    private IStringLocalizer<MudExColorEdit> _fallbackLocalizer => ServiceProvider.GetService<IStringLocalizer<MudExColorEdit>>();
    protected IStringLocalizer LocalizerToUse => Localizer ?? _fallbackLocalizer;

    [Parameter] public IStringLocalizer Localizer { get; set; }
    [Parameter] public Variant FilterVariant { get; set; }
    [Parameter] public MudExColor Value { get => _value; set => _value = value; }
    [Parameter] public bool ShowThemeColors { get; set; } = true;
    [Parameter] public bool ShowHtmlColors { get; set; } = true;
    [Parameter] public bool ShowCssColorVariables { get; set; } = true;
    [Parameter] public string Filter { get; set; }
    [Parameter] public ColorPreviewMode PreviewMode { get; set; } = ColorPreviewMode.Both;
    [Parameter] public EventCallback<MudExColor> ValueChanged { get; set; }

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

    private bool Matches(string value)
    {
        return string.IsNullOrEmpty(Filter)
               || value.Contains(Filter, StringComparison.InvariantCultureIgnoreCase);
    }

    protected override Task OnParametersSetAsync()
    {
        Text = Value.ToString();
        UpdatePreview();
        return base.OnParametersSetAsync();
    }

    private string OnSet(MudExColor color)
    {
        UpdatePreview();
        return Text = color.ToString();
    }

    private void UpdatePreview()
    {
        Style = MudExStyleBuilder.Empty()
            .WithColor(Value, PreviewMode is ColorPreviewMode.Text or ColorPreviewMode.Both)
            .WithFill(Value, PreviewMode is ColorPreviewMode.Icon or ColorPreviewMode.Both)
            .Build();
    }

    private MudExColor OnGet(string value) => Value = new MudExColor(value);

    protected override Task StringValueChanged(string value)
    {
        Touched = true;
        Value = Converter.Get(value);
        UpdatePreview();
        return ValueChanged.InvokeAsync(Value);
    }

    private void Select(string color)
    {
        ValueChanged.InvokeAsync(Value = color);
        OnSet(Value);
        Close();
    }

    private void Select(Color color)
    {
        ValueChanged.InvokeAsync(Value = color);
        OnSet(Value);
        Close();
    }

    private void Select(MudColor color)
    {
        ValueChanged.InvokeAsync(Value = color);
        OnSet(Value);
    }
}

public enum ColorPreviewMode
{
    None,
    Text,
    Icon,
    Both
}