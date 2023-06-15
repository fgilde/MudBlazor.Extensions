using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;
using MudBlazor.Utilities;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

public partial class MudExThemeEdit<TTheme>
{
    [Parameter] public TTheme Theme { get; set; }
    [Parameter] public bool EditFull { get; set; }

    /// <summary>
    /// This bool represents a tri state. True to edit only dark color palette, false to edit only light color palette and null to edit both
    /// </summary>
    [Parameter] public bool? IsDark { get; set; }

    private ObjectEditMeta<TTheme> MetaInformation { get; set; }


    public override async Task SetParametersAsync(ParameterView parameters)
    {
        bool updateConditions = parameters.TryGetValue<bool?>(nameof(IsDark), out var isDark) && IsDark != isDark;
        await base.SetParametersAsync(parameters);
        if (updateConditions)
            UpdateConditions();

    }

    private void UpdateConditions()
    {
        MetaInformation?.UpdateAllConditionalSettings();
    }

    private Task OnThemeChanged(TTheme arg)
    {
        return Task.CompletedTask;
    }

    private void ThemeEditMetaConfiguration(ObjectEditMeta<TTheme> meta)
    {
        MetaInformation = meta;
        meta.Properties<MudColor>()
            .RenderWith<MudExColorEdit, MudColor, MudExColor>(edit => edit.Value)
            .WithAdditionalAttributes(RenderDataDefaults.ColorPickerOptions());

        ConfigurePalette(meta.Property(c => c.Palette), false);
        ConfigurePalette(meta.Property(c => c.PaletteDark), true);
    }

    private void ConfigurePalette(ObjectEditPropertyMetaOf<TTheme> paletteProperty, bool isDarkPalette)
    {
        paletteProperty.Children.Recursive(om => om.Children)
            .OrderBy(p => p.PropertyName).Apply((i, p) => p.WithOrder(i))
            .IgnoreIf<TTheme>(m => (IsDark.HasValue && isDarkPalette && !IsDark.Value) || (IsDark.HasValue && !isDarkPalette && IsDark.Value))
            .WrapInMudItem(i => i.xs = 6);

    }
}