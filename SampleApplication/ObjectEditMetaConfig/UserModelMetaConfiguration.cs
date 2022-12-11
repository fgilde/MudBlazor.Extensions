using MudBlazor;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using SampleApplication.Client.Types;

namespace SampleApplication.Client.ObjectEditMetaConfig;

public class UserModelMetaConfiguration: IObjectMetaConfiguration<UserModel>
{
    public Task ConfigureAsync(ObjectEditMeta<UserModel> meta)
    {
        meta.Properties(m => m.FirstName, m => m.LastName)
            .WrapInMudItem(mudItem => mudItem.xs = 6);
        meta.Property(m => m.Password)
            .RenderWith<MudTextField<string>, string>(field => field.Value, field =>
            {
                field.InputType = InputType.Password;
            })
            .WrapInMudItem(mudItem => mudItem.xs = 12);
        return Task.CompletedTask;
    }
}