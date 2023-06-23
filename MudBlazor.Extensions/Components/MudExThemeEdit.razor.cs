using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Helper;
using MudBlazor.Utilities;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// MudExThemeEdit is a powerful component to edit one ore more themes
/// </summary>
/// <typeparam name="TTheme"></typeparam>
public partial class MudExThemeEdit<TTheme>
{
    private const string FontFamilyEditPath = $"{nameof(Typography)}.{nameof(Typography.Default)}.{nameof(Typography.Default.FontFamily)}";
    private const int ExtraDelay = 500; // Currently needed because of timing issues in MudExObjectEdit. But will fixed later

    private KeyValuePair<string, MudColor>[] _cssVars;
    private bool _isLoading = true;
    private ThemePreset<TTheme> _selectedPreset;
    private TTheme _theme;
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
        FontFamilyEditPath,
    };

    /// <summary>
    /// if true user can click on cancel
    /// </summary>
    [Parameter] public bool ShowCancelButton { get; set; }

    /// <summary>
    /// if true user can click on save
    /// </summary>
    [Parameter] public bool ShowSaveButton { get; set; }

    /// <summary>
    /// If true user can import json themes
    /// </summary>
    [Parameter] public bool AllowImport { get; set; } = true;

    /// <summary>
    /// If true user can export theme as json
    /// </summary>
    [Parameter] public bool AllowExport { get; set; } = true;

    /// <summary>
    /// If true the state of current theme and edit values are stored in storage and restored automatically
    /// </summary>
    [Parameter] public bool AutoSaveRestoreState { get; set; } = true;

    /// <summary>
    /// The theme to edit or current theme from preset
    /// </summary>
    [Parameter]
    public TTheme Theme
    {
        get => _theme;
        set
        {
            if (_theme != value)
            {
                _theme = value;
                UpdateInitialTheme();
            }
        }
    }
    
    /// <summary>
    /// The theme that will used for reset
    /// </summary>
    public TTheme InitialTheme { get; private set; }

    /// <summary>
    /// Edit mode (default simple)
    /// </summary>
    [Parameter] public ThemeEditMode EditMode { get; set; } = ThemeEditMode.Simple;
    

    /// <summary>
    /// If true user can switch between simple and full edit mode
    /// </summary>
    [Parameter] public bool AllowModeToggle { get; set; } = true;

    /// <summary>
    /// This bool represents a tri state. True to edit only dark color palette, false to edit only light color palette and null to edit both
    /// </summary>
    [Parameter] public bool? IsDark { get; set; }

    /// <summary>
    /// Object edit Meta 
    /// </summary>
    [Parameter] public ObjectEditMeta<TTheme> MetaInformation { get; set; }

    /// <summary>
    /// This collection of presets will be used to populate the dropdown for preset selection
    /// If its allowed user can also add or delete from this collection
    /// </summary>
    [Parameter] public ICollection<ThemePreset<TTheme>> Presets { get; set; }

    /// <summary>
    /// If true you can add or delete themes
    /// </summary>
    [Parameter] public bool AllowPresetsEdit { get; set; } = true;

    /// <summary>
    /// If function returns true user can delete theme from param
    /// </summary>
    [Parameter] public Func<ThemePreset<TTheme>, bool> CanDelete { get; set; } = _ => true;
    
    /// <summary>
    /// Raised when new theme is created
    /// </summary>
    [Parameter] public EventCallback<ThemePreset<TTheme>> ThemeCreated { get; set; }

    /// <summary>
    /// Raised when Theme is deleted
    /// </summary>
    [Parameter] public EventCallback<ThemePreset<TTheme>> ThemeDeleted { get; set; }

    /// <summary>
    /// Raised when something in Theme or whole theme has changed
    /// </summary>
    [Parameter] public EventCallback<TTheme> ThemeChanged { get; set; }

    /// <summary>
    /// Raised when edit mode changed
    /// </summary>
    [Parameter] public EventCallback<ThemeEditMode> EditModeChanged { get; set; }

    /// <summary>
    /// Raised when user clicks on save
    /// </summary>
    [Parameter] public EventCallback<(TTheme Theme, ThemePreset<TTheme> Preset)> ThemeSaved { get; set; }

    /// <summary>
    /// Raised when user clicks on cancel
    /// </summary>
    [Parameter] public EventCallback<(TTheme Theme, ThemePreset<TTheme> Preset)> EditCanceled { get; set; }


    /// <summary>
    /// Reference to MudExObjectEdit
    /// </summary>
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
            InitialTheme = Theme.CloneTheme();
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
        await Task.Delay(ExtraDelay);
        Loading(false);
    }

    private void UpdateInitialTheme()
    {
        if (Theme != null)
            InitialTheme = Theme.CloneTheme();
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
        await Task.Delay(ExtraDelay);
        Loading(false);
    }


    private Task OnThemeChanged(TTheme arg)
    {
        return ThemeChanged.InvokeAsync(arg);
    }

    private Task OnPropertyChanged(ObjectEditPropertyMeta arg)
    {
        if (arg.PropertyName == FontFamilyEditPath && EditMode == ThemeEditMode.Simple)
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
            .RenderWith<MudExFontSelect, string[], IEnumerable<string>>(edit => edit.Selected)
            .WithOrder(0);

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
        if (typeof(TTheme) != typeof(MudTheme) && typeof(TTheme).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Contains(objectEditPropertyMeta.PropertyInfo))
            return true;

        var isLightPalette = objectEditPropertyMeta.PropertyName.Contains($"{nameof(MudTheme.Palette)}.");
        var isDarkPalette = objectEditPropertyMeta.PropertyName.Contains(nameof(MudTheme.PaletteDark));

        if ((IsDark.HasValue && isDarkPalette && !IsDark.Value) || (IsDark.HasValue && isLightPalette && IsDark.Value))
            return false;

        return EditMode == ThemeEditMode.Full
               || _propertiesForSimpleMode.Contains(objectEditPropertyMeta.PropertyName)  // Full path like Object.Data.Id
               || _propertiesForSimpleMode.Contains(objectEditPropertyMeta.PropertyInfo.Name); // Name only.. like Id

    }

    private Task BeforeImport(ImportData<TTheme> arg) => Task.FromResult(arg.Json = JsonHelper.SimplifyMudColorInJson(arg.Json));
    private void BeforeExport(ExportData<TTheme> obj) => obj.Json = JsonHelper.SimplifyMudColorInJson(obj.Json);
    
    private async Task AfterImport(ImportedData<TTheme> arg) => await SetTheme(arg.Value);
    private Task OnCancel() => EditCanceled.InvokeAsync((Theme, _selectedPreset));
    private Task OnValidSubmit(EditContext arg) => ThemeSaved.InvokeAsync((Theme, _selectedPreset));

    private async Task Reset()
    {
        if (InitialTheme is not null)
        {
            var cloneTheme = InitialTheme.CloneTheme();
            Loading(true);
            await ObjectEditor.DeleteState();
            if (_selectedPreset is not null)
                _selectedPreset.Theme = cloneTheme;
            await SetTheme(cloneTheme);
        }
    }


    private async Task OnSelectedPresetChange(ThemePreset<TTheme> arg)
    {
        _selectedPreset = arg;
        await SetTheme(arg.Theme);
    }

    private ThemePreviewMode GetPreviewMode() => IsDark switch {
        true => ThemePreviewMode.DarkOnly,
        false => ThemePreviewMode.LightOnly,
        _ => ThemePreviewMode.BothDiagonal
    };

    private bool CanDeletePreset(ThemePreset<TTheme> preset)
    {
        return preset is not null && CanDelete(preset);
    }

    private async Task OnDeleteThemeClick()
    {
        if (CanDeletePreset(_selectedPreset))
            await ThemeDeleted.InvokeAsync(_selectedPreset);
    }

    private async Task OnAddThemeClick()
    {
        var name = await DialogService.PromptAsync("Enter name", "Please enter name", Icons.Material.Filled.Add, s => !string.IsNullOrEmpty(s));
        if (name != null)
        {
            var themePreset = new ThemePreset<TTheme>(name, Theme.CloneTheme());
            await ThemeCreated.InvokeAsync(themePreset);
        }
    }
}