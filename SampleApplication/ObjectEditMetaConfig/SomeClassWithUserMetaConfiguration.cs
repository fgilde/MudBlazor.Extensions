using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using SampleApplication.Client.Types;

namespace SampleApplication.Client.ObjectEditMetaConfig;

public class SomeClassWithUserMetaConfiguration : IObjectMetaConfiguration<SomeClassWithUser>
{
    public Task ConfigureAsync(ObjectEditMeta<SomeClassWithUser> meta)
    {
        meta.Property(m => m.MainUser).RenderWith<MudExObjectEditForm<UserModel>, UserModel>(mudExObjectEdit => mudExObjectEdit.Value);
        return Task.CompletedTask;
    }
}