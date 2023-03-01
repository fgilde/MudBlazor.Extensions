using MainSample.WebAssembly.Types;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;

namespace MainSample.WebAssembly.ObjectEditMetaConfig;

public class SomeClassWithUserMetaConfiguration : IObjectMetaConfiguration<SomeClassWithUser>
{
    public Task ConfigureAsync(ObjectEditMeta<SomeClassWithUser> meta)
    {
        meta.Property(m => m.MainUser).RenderWith<MudExObjectEditForm<UserModel>, UserModel>(mudExObjectEdit => mudExObjectEdit.Value);
        return Task.CompletedTask;
    }
}