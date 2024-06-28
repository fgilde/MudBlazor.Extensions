using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using Nextended.Core.Extensions;

namespace MainSample.WebAssembly.ObjectEditMetaConfig;

public class ClientThemeMetaConfiguration : IObjectMetaConfiguration<ClientTheme>
{
    public Task ConfigureAsync(ObjectEditMeta<ClientTheme> meta)
    {
        meta.Property(c => c.PaletteLight).Children.Recursive(om => om.Children).WrapInMudItem(i => i.xs = 6);
        meta.Property(c => c.PaletteDark).Children.Recursive(om => om.Children).WrapInMudItem(i => i.xs = 6);
        return Task.CompletedTask;
    }
}