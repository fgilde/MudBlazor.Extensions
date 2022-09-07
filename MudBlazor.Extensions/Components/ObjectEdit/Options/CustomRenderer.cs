using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

public interface ICustomRenderer
{
    void Render(RenderTreeBuilder builder, IHandleEvent eventTarget, ObjectEditPropertyMeta propertyMeta);
}