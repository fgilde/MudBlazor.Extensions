using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options.MetaDataConfig;

public class DialogOptionsExMetaConfig : IObjectMetaConfiguration<DialogOptionsEx>
{
    public Task ConfigureAsync(ObjectEditMeta<DialogOptionsEx> meta)
    {
        DialogOptionsEx.ConfigureObjectEditMeta(meta);
        return Task.CompletedTask;
    }
}