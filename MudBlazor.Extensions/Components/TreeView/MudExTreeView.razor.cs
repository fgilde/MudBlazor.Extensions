using Microsoft.AspNetCore.Components;
using Nextended.Core.Types;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Helper;
using Nextended.Blazor.Helper;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// TreeView component that renders a tree structure of items.
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class MudExTreeView<T>
    where T : IHierarchical<T>
{
    private TreeViewMode _viewMode = TreeViewMode.Horizontal;
    private DynamicComponent _renderedTree;
    private MudExTreeViewBase<T> _renderedTreeComponent => _renderedTree?.Instance as MudExTreeViewBase<T>;

    /// <summary>
    /// Mode controls how the tree view will be rendered.
    /// </summary>
    [Parameter]
    public TreeViewMode ViewMode    
    {
        get => _viewMode;
        set => Set(ref _viewMode, value, mode => ViewModeChanged.InvokeAsync(mode));
    }

    /// <summary>
    /// Invoked when the view mode changes.
    /// </summary>
    [Parameter] public EventCallback<TreeViewMode> ViewModeChanged { get; set; }

    /// <summary>
    /// Here you can specify between which view modes the user can toggle.
    /// </summary>
    [Parameter] public TreeViewMode[] ToggleableViewModes { get; set; } = Enum<TreeViewMode>.ToArray();

    /// <summary>
    /// Specify if and how the view mode can be toggled.
    /// </summary>
    [Parameter] public TreeViewModeToggleComponent TreeViewModeToggleComponent { get; set; } = TreeViewModeToggleComponent.None;

    /// <summary>
    /// Parameters for the component based on ViewMode. 
    /// </summary>
    [Parameter] public IDictionary<string, object> Parameters { get; set; }

    #region Delegating methods

    /// <inheritdoc />
    public override void ExpandAll()
    {
        _renderedTreeComponent?.ExpandAll();
        base.ExpandAll();
    }

    /// <inheritdoc />
    public override void ExpandAllLoaded()
    {
        _renderedTreeComponent?.ExpandAllLoaded();
        base.ExpandAllLoaded();
    }

    /// <inheritdoc />
    public override void ExpandAllNotLoaded()
    {
        _renderedTreeComponent?.ExpandAllNotLoaded();
        base.ExpandAllNotLoaded();
    }

    /// <inheritdoc />
    public override void ToggleExpand(T node)
    {
        _renderedTreeComponent?.ToggleExpand(node);
        base.ToggleExpand(node);
    }

    /// <inheritdoc />
    public override void SetExpanded(T context, bool expanded)
    {
        _renderedTreeComponent?.SetExpanded(context, expanded);
        base.SetExpanded(context, expanded);
    }


    #endregion


    private Type GetComponentForViewMode()
        => (GetRenderWithAttribute(ViewMode)?.ComponentType ?? typeof(MudExTreeViewDefault<>)).MakeGenericType(typeof(T));

    private IDictionary<string, object> GetParameters()
    {
        var componentType = GetComponentForViewMode();
        var res = ComponentRenderHelper.GetCompatibleParameters(this, componentType)
            .Where(p => IsOverwritten(p.Key)).ToDictionary(p => p.Key, p => p.Value);

        if (!FiltersChanged.HasDelegate)
            res = res.Where(p => p.Key != nameof(Filters)).ToDictionary(p => p.Key, p => p.Value);
        if (!FilterChanged.HasDelegate)
            res = res.Where(p => p.Key != nameof(Filter)).ToDictionary(p => p.Key, p => p.Value);

        foreach (var parameter in Parameters ?? new Dictionary<string, object>())
        {
            if (ComponentRenderHelper.IsValidParameter(componentType, parameter.Key, parameter.Value))
                res.AddOrUpdate(parameter.Key, parameter.Value);
        }

        res.AddOrUpdate(nameof(ToolBarContent), ToggleComponent().CombineWith(ToolBarContent));
        return res.ToDictionary(p => p.Key, p => p.Value);
    }

    private static RenderWithAttribute GetRenderWithAttribute(Enum val)
    {
        var customAttributes = (RenderWithAttribute[])val.GetType().GetField(val.ToString()).GetCustomAttributes(typeof(RenderWithAttribute), false);
        return customAttributes.FirstOrDefault();
    }

    public static string IconFor(TreeViewMode mode)
    {
        return mode switch
        {
            TreeViewMode.Default => Icons.Material.Filled.AccountTree,
            TreeViewMode.Horizontal => Icons.Material.Filled.AlignHorizontalLeft,
            TreeViewMode.Breadcrumb => MudExSvg.RotateSvg(Icons.Material.Filled.MenuOpen, 180),
            TreeViewMode.List => Icons.Material.Filled.ViewList,
            TreeViewMode.FlatList => Icons.Material.Filled.List,
            TreeViewMode.CardGrid => Icons.Material.Filled.ViewModule,
            _ => string.Empty
        };
    }
}

public enum TreeViewModeToggleComponent
{
    None,
    ToggleButton,
    ButtonGroup
}