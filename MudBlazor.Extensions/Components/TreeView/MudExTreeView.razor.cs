using Microsoft.AspNetCore.Components;
using Nextended.Core.Types;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
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

    private Type GetComponentForViewMode()
        => (GetRenderWithAttribute(ViewMode)?.ComponentType ?? typeof(MudExTreeViewDefault<>)).MakeGenericType(typeof(T));

    private IDictionary<string, object> GetParameters()
    {
        var res = ComponentRenderHelper.GetCompatibleParameters(this, GetComponentForViewMode())
            .Where(p => IsOverwritten(p.Key));

        if(!FiltersChanged.HasDelegate)
            res = res.Where(p => p.Key != nameof(Filters));
        if (!FilterChanged.HasDelegate)
            res = res.Where(p => p.Key != nameof(Filter));

        return res.ToDictionary();
    }

    private static RenderWithAttribute GetRenderWithAttribute(Enum val)
    {
        var customAttributes = (RenderWithAttribute[])val.GetType().GetField(val.ToString()).GetCustomAttributes(typeof(RenderWithAttribute), false);
        return customAttributes.FirstOrDefault();
    }
}




/// <summary>
/// TreeView mode determines how the tree view will be rendered.
/// </summary>
public enum TreeViewMode
{
    /// <summary>
    /// Render tree view in default mode.
    /// </summary>
    [RenderWith(typeof(MudExTreeViewDefault<>))]
    Default,

    /// <summary>
    /// Render tree view in horizontal mode.
    /// </summary>
    [RenderWith(typeof(MudExTreeViewHorizontal<>))]
    Horizontal,

    /// <summary>
    /// Render tree view as breadcrumb.
    /// </summary>
    [RenderWith(typeof(MudExTreeViewBreadcrumb<>))]
    Breadcrumb,

    /// <summary>
    /// Render tree view as list.
    /// </summary>
    [RenderWith(typeof(MudExTreeViewList<>))]
    List
}
