using MudBlazor.Extensions.Components.ObjectEdit.Options;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public interface IDefaultRenderDataProvider
{
    IRenderData GetRenderData(ObjectEditPropertyMeta propertyMeta);
}