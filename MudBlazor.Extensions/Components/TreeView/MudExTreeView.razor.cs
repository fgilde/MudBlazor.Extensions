using Microsoft.AspNetCore.Components;
using Nextended.Core.Types;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core.Enums;
using Nextended.Blazor.Helper;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeView<T>
    where T : IHierarchical<T>
{
    /// <summary>
    /// Mode controls how the tree view will be rendered.
    /// </summary>
    [Parameter] public TreeViewMode ViewMode { get; set; } = TreeViewMode.Horizontal;

    /// <summary>
    /// Parameters for the component based on ViewMode. 
    /// </summary>
    [Parameter] public IDictionary<string, object> Parameters { get; set; }

    private Type GetComponentForViewMode()
        => (GetRenderWithAttribute(ViewMode)?.ComponentType ?? typeof(MudExTreeViewDefault<>)).MakeGenericType(typeof(T));

    private IDictionary<string, object> GetParameters()
    {
        var componentType = GetComponentForViewMode();
        var res = ComponentRenderHelper.GetCompatibleParameters(this, componentType)
            .Where(p => IsOverwritten(p.Key)).ToDictionary();

        if(!FiltersChanged.HasDelegate)
            res = res.Where(p => p.Key != nameof(Filters)).ToDictionary();
        if (!FilterChanged.HasDelegate)
            res = res.Where(p => p.Key != nameof(Filter)).ToDictionary();
        
        foreach (var parameter in Parameters ?? new Dictionary<string, object>())
        {
            if (ComponentRenderHelper.IsValidParameter(componentType, parameter.Key, parameter.Value))
                res.AddOrUpdate(parameter.Key, parameter.Value);
        }

        return res.ToDictionary();
    }

    private static RenderWithAttribute GetRenderWithAttribute(Enum val)
    {
        var customAttributes = (RenderWithAttribute[])val.GetType().GetField(val.ToString()).GetCustomAttributes(typeof(RenderWithAttribute), false);
        return customAttributes.FirstOrDefault();
    }
}
