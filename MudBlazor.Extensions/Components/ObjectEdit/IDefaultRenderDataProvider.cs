using MudBlazor.Extensions.Components.ObjectEdit.Options;

namespace MudBlazor.Extensions.Components.ObjectEdit;

/// <summary>
/// Interface for providing default render data for mudex object edit property
/// </summary>
public interface IDefaultRenderDataProvider
{
    /// <summary>
    /// Returns the render data 
    /// </summary>
    IRenderData GetRenderData(ObjectEditPropertyMeta propertyMeta);
}


/// <summary>
/// Interface for providing default render data for mudex object only for a specific type
/// This renderdata is only used when that type a property of the current edited object
/// </summary>
public interface IDefaultRenderDataProviderFor<T> : IDefaultRenderDataProvider
{ }