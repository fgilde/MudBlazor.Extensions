using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Helper;
using MudBlazor.Utilities;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

public partial class MudExThemeEdit<TTheme>
{
    private bool _isLoading = true;
    private bool _isRendered;

    private static readonly string[] _propertiesForSimpleMode = { 
        nameof(MudTheme.Palette.AppbarBackground),
        nameof(MudTheme.Palette.Primary)
    };

    private KeyValuePair<string, MudColor>[] _cssVars;

    [Parameter] public TTheme Theme { get; set; }
    [Parameter] public EventCallback<TTheme> ThemeChanged { get; set; }
    [Parameter] public IEnumerable<TTheme> Presets { get; set; }
    [Parameter] public ThemeEditMode EditMode { get; set; } = ThemeEditMode.Simple;
    [Parameter] public EventCallback<ThemeEditMode> EditModeChanged { get; set; }
    [Parameter] public bool AllowModeToggle { get; set; } = true;


    /// <summary>
    /// This bool represents a tri state. True to edit only dark color palette, false to edit only light color palette and null to edit both
    /// </summary>
    [Parameter] public bool? IsDark { get; set; }

    [Parameter] public ObjectEditMeta<TTheme> MetaInformation { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _cssVars = await MudExCss.GetCssColorVariablesAsync(); 
        await base.OnInitializedAsync();
    }

    private Task EditModeChangedInternally(ThemeEditMode arg)
    {
        EditMode = arg;
        _ = UpdateConditions();
        return EditModeChanged.InvokeAsync(EditMode);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        _isRendered = true;
    }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        bool updateConditions = (parameters.TryGetValue<bool?>(nameof(IsDark), out var isDark) && IsDark != isDark)
                             || (parameters.TryGetValue<ThemeEditMode>(nameof(EditMode), out var editMode) && EditMode != editMode);
        await base.SetParametersAsync(parameters);
        if (updateConditions)
        {
            await UpdateConditions();
        }
    }

    private void Loading(bool isLoading)
    {
        _isLoading = isLoading;
        StateHasChanged();
    }

    private async Task UpdateConditions()
    {
        if (!_isRendered)
            return;
        Loading(true);
        MetaInformation?.UpdateAllConditionalSettings();
        await Task.Delay(500);
        Loading(false);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        await Task.Delay(2000);
        _isLoading = false;
    }


    private Task OnThemeChanged(TTheme arg)
    {
        return ThemeChanged.InvokeAsync(arg);
    }


    private void ThemeEditMetaConfiguration(ObjectEditMeta<TTheme> meta)
    {
        MetaInformation ??= meta;
        meta.Properties<MudColor>()
            .RenderWith<MudExColorEdit, MudColor, string>(edit => edit.ValueString)
            .WithAdditionalAttributes(OptionsForColorEdit());
        meta.Properties<string>().Where(p => p?.Value?.ToString()?.StartsWith("rgb") == true || p?.Value?.ToString()?.StartsWith("#") == true)
            .RenderWith<MudExColorEdit, string, string>(edit => edit.ValueString)
            .WithAdditionalAttributes(RenderDataDefaults.ColorPickerOptions());

        ConfigurePalette(meta.Property(c => c.Palette));
        ConfigurePalette(meta.Property(c => c.PaletteDark));
        meta.AllProperties.IgnoreIf<ObjectEditPropertyMeta>(IsNotAllowed);
    }

    private IDictionary<string, object> OptionsForColorEdit()
    {
        return RenderDataDefaults.ColorPickerOptions().AddOrUpdate(nameof(MudExColorEdit.CssVars), _cssVars);
    }

    private void ConfigurePalette(ObjectEditPropertyMetaOf<TTheme> paletteProperty)
    {
        paletteProperty.Children.Recursive(om => om.Children)
            .OrderBy(p => p.PropertyName).Apply((i, p) => p.WithOrder(i))
            .WrapInMudItem(i => i.xs = 6);

    }

    private bool IsNotAllowed(ObjectEditPropertyMeta objectEditPropertyMeta) => !IsAllowed(objectEditPropertyMeta);
    private bool IsAllowed(ObjectEditPropertyMeta objectEditPropertyMeta)
    {
        var isLightPalette = objectEditPropertyMeta.PropertyName.Contains($"{nameof(MudTheme.Palette)}.");
        var isDarkPalette = objectEditPropertyMeta.PropertyName.Contains(nameof(MudTheme.PaletteDark));
            
        if( (IsDark.HasValue && isDarkPalette && !IsDark.Value) || (IsDark.HasValue && isLightPalette && IsDark.Value) )
            return false;
        
        return EditMode == ThemeEditMode.Full
               || _propertiesForSimpleMode.Contains(objectEditPropertyMeta.PropertyName)  // Full path like Object.Data.Id
               || _propertiesForSimpleMode.Contains(objectEditPropertyMeta.PropertyInfo.Name); // Name only.. like Id

    }

}

public enum ThemeEditMode
{
    Simple,
    Full,
}