using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Options;

namespace MainSample.WebAssembly.ObjectEditMetaConfig;

public class DialogOptionsExMetaConfig : IObjectMetaConfiguration<DialogOptionsEx>
{
    public Task ConfigureAsync(ObjectEditMeta<DialogOptionsEx> meta)
    {
        DialogOptionsEx.ConfigureObjectEditMeta(meta);
        return Task.CompletedTask;
    }
}