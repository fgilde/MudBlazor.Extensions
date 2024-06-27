using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Components.ObjectEdit.Options;

namespace MudBlazor.Extensions.Core.Enums;

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
    List,

    /// <summary>
    /// Render tree view as flat list.
    /// </summary>
    [RenderWith(typeof(MudExTreeViewFlatList<>))]
    FlatList,


    /// <summary>
    /// Render tree view as flat list.
    /// </summary>
    [RenderWith(typeof(MudExTreeViewCardGrid<>))]
    CardGrid
}