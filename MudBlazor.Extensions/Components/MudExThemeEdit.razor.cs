using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Helper;
using MudBlazor.Utilities;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

public partial class MudExThemeEdit<TTheme>
{
    private const int extraDelay = 500;
    private bool _isLoading = true;
    private const string fontFamilyEditPath = $"{nameof(Typography)}.{nameof(Typography.Default)}.{nameof(Typography.Default.FontFamily)}";
    
    private static readonly string[] _propertiesForSimpleMode = { 
        nameof(MudTheme.Palette.AppbarBackground),
        nameof(MudTheme.Palette.Surface),
        nameof(MudTheme.Palette.DrawerBackground),
        nameof(MudTheme.Palette.DrawerIcon),
        nameof(MudTheme.Palette.Background),
        nameof(MudTheme.Palette.Primary),
        nameof(MudTheme.Palette.Secondary),
        nameof(MudTheme.Palette.Tertiary),
        nameof(MudTheme.Palette.Info),
        nameof(MudTheme.Palette.Success),
        nameof(MudTheme.Palette.Warning),
        nameof(MudTheme.Palette.Error),
        nameof(MudTheme.LayoutProperties.AppbarHeight),
        nameof(MudTheme.LayoutProperties.DefaultBorderRadius),
        nameof(MudTheme.LayoutProperties.DrawerWidthLeft),
        nameof(MudTheme.LayoutProperties.DrawerWidthRight),
        nameof(MudTheme.LayoutProperties.DrawerWidthRight),
        fontFamilyEditPath,
    };

    private KeyValuePair<string, MudColor>[] _cssVars;
    [Inject] public IDialogService DialogService { get; set; }
    [Parameter] public TTheme Theme { get; set; }
    public TTheme InitialTheme { get; private set; }
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

    public MudExObjectEditForm<TTheme> ObjectEditor { get; private set; }

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

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Theme != null && InitialTheme == null)
        {
            InitialTheme = Theme.CloneTheme();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _cssVars = await MudExCss.GetCssColorVariablesAsync();
        }
        await base.OnAfterRenderAsync(firstRender);
        await Task.Delay(2000);
        _isLoading = false;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }
    

    private Task EditModeChangedInternally(ThemeEditMode arg)
    {
        EditMode = arg;
        _ = UpdateConditions();
        return EditModeChanged.InvokeAsync(EditMode);
    }

    private async Task SetTheme(TTheme theme)
    {
        Theme = null;
        Loading(true);
        Theme = theme;
        await OnThemeChanged(Theme);
        StateHasChanged();
        await Task.Delay(extraDelay);
        Loading(false);
    }

    private void Loading(bool isLoading)
    {
        _isLoading = isLoading;
        StateHasChanged();
    }

    private async Task UpdateConditions()
    {
        if (!IsRendered)
            return;
        Loading(true);
        MetaInformation?.UpdateAllConditionalSettings();
        await Task.Delay(extraDelay);
        Loading(false);
    }

    
    private Task OnThemeChanged(TTheme arg)
    {
        return ThemeChanged.InvokeAsync(arg);
    }

    private Task OnPropertyChanged(ObjectEditPropertyMeta arg)
    {
        if (arg.PropertyName == fontFamilyEditPath && EditMode == ThemeEditMode.Simple)
        {
            Theme.Typography.Body1.FontFamily = Theme.Typography.Body2.FontFamily = Theme.Typography.Caption.FontFamily = 
            Theme.Typography.Button.FontFamily = Theme.Typography.H1.FontFamily = Theme.Typography.H2.FontFamily = 
            Theme.Typography.H3.FontFamily = Theme.Typography.H4.FontFamily = Theme.Typography.H5.FontFamily = 
            Theme.Typography.H6.FontFamily = Theme.Typography.Subtitle1.FontFamily = Theme.Typography.Subtitle2.FontFamily = 
            Theme.Typography.Overline.FontFamily = arg.Value as string[];
        }
        return Task.CompletedTask;
    }


    private void ThemeEditMetaConfiguration(ObjectEditMeta<TTheme> meta)
    {
        MetaInformation ??= meta;
        meta.Properties<MudColor>()
            .RenderWith<MudExColorEdit, MudColor, string>(edit => edit.ValueString)
            .WithAdditionalAttributes(OptionsForColorEdit());
        

        meta.Properties<string>().Where(p => p?.Value?.ToString()?.StartsWith("rgb") == true || p?.Value?.ToString()?.StartsWith("#") == true)
            .RenderWith<MudExColorEdit, string, string>(edit => edit.ValueString)
            .WithAdditionalAttributes(OptionsForColorEdit());

        ConfigurePalette(meta.Property(c => c.Palette));
        ConfigurePalette(meta.Property(c => c.PaletteDark));

        //meta.Properties(theme => theme.Typography.Default.FontFamily)
        meta.Properties().Where(p => p.PropertyInfo.Name == nameof(MudTheme.Typography.Default.FontFamily))
            .RenderWith<MudExFontSelect, string[], IEnumerable<string>>(edit => edit.Selected);
        
        meta.AllProperties.IgnoreIf<ObjectEditPropertyMeta>(IsNotAllowed);
    }

    private IDictionary<string, object> OptionsForColorEdit() => RenderDataDefaults.ColorPickerOptions().AddOrUpdate(nameof(MudExColorEdit.CssVars), _cssVars);

    
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

    private Task BeforeImport(ImportData<TTheme> arg) 
        => Task.FromResult(arg.Json = JsonHelper.SimplifyMudColorInJson(arg.Json));
    private void BeforeExport(ExportData<TTheme> obj) => 
        obj.Json = JsonHelper.SimplifyMudColorInJson(obj.Json);


    private async Task AfterImport(ImportedData<TTheme> arg) => await SetTheme(arg.Value);

    private async Task OnCancel()
    {
        await ObjectEditor.Reset();
    }

    private Task OnValidSubmit(EditContext arg)
    {
        return Task.CompletedTask;
    }

}

public enum ThemeEditMode
{
    Simple,
    Full,
}