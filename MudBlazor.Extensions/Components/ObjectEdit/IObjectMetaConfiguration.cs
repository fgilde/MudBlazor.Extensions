using MudBlazor.Extensions.Components.ObjectEdit.Options;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public interface IObjectMetaConfiguration<TModel>
{
    Task ConfigureAsync(ObjectEditMeta<TModel> meta);
}