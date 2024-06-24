using Microsoft.AspNetCore.Components;
using Nextended.Core.Types;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using Nextended.Blazor.Helper;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeView<T> 
    where T : IHierarchical<T>
{
    [Parameter] public TreeViewMode ViewMode { get; set; } = TreeViewMode.Horizontal;

    private Type GetComponentForViewMode() 
        => (GetRenderWithAttribute(ViewMode)?.ComponentType ?? typeof(MudExTreeViewDefault<>)).MakeGenericType(typeof(T));

    private IDictionary<string, object> GetParameters()
    {
        var res = ComponentRenderHelper.GetCompatibleParameters(this, GetComponentForViewMode());
        //res.Add("TItem", typeof(T));
        //res.Add("TComponent", GetComponentForViewMode());
        //res.AddOrUpdate(nameof(ItemContentTemplate), ItemContentTemplate);
        //res.AddOrUpdate(nameof(ItemTemplate), ItemContentTemplate);
        return res;
    }

    private static RenderWithAttribute GetRenderWithAttribute(Enum val)
    {
        var customAttributes = (RenderWithAttribute[])val.GetType().GetField(val.ToString()).GetCustomAttributes(typeof(RenderWithAttribute), false);
        return customAttributes.FirstOrDefault();
    }
}





public enum TreeViewMode
{
    [RenderWith(typeof(MudExTreeViewDefault<>))]
    Default,
    [RenderWith(typeof(MudExTreeViewHorizontal<>))]
    Horizontal,
    [RenderWith(typeof(MudExTreeViewBreadcrumb<>))]
    Breadcrumb,
    [RenderWith(typeof(MudExTreeViewList<>))]
    List
}
