using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Interface can implemented and used in object edit RenderWith to completely create an own property renderer
/// </summary>
public interface ICustomRenderer
{
    /// <summary>
    /// If attached as setting in ObjectEditPropertyMeta.RenderData this is called by MudExPropertyEdit
    /// </summary>
    void Render(RenderTreeBuilder builder, IHandleEvent eventTarget, ObjectEditPropertyMeta propertyMeta);
}