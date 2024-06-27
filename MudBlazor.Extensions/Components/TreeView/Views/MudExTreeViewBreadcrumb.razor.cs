using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.ObjectEdit;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeViewBreadcrumb<T> 
    where T : IHierarchical<T>
{

    /// <summary>
    /// Set the anchor origin point to determine where the popover will open from.
    /// </summary>
    [Parameter] public Origin MenuAnchorOrigin { get; set; } = Origin.BottomRight;

    /// <summary>
    /// Sets the transform origin point for the popover.
    /// </summary>
    [Parameter] public Origin MenuTransformOrigin { get; set; } = Origin.TopLeft;

    /// <summary>
    /// Max height of the menu when this is reached overflow will be scrollable
    /// </summary>
    [Parameter] public int MenuMaxHeight { get; set; } = 500;

    /// <summary>
    /// Class for use in the filter box
    /// </summary>
    protected override string GetFilterClassStr()
    {
        return string.Empty;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (!IsOverwritten(nameof(FilterMode)))
            FilterMode = PropertyFilterMode.Toggleable;
        base.OnInitialized();
    }

    private void NodeItemClick(T node)
    {
        if(IsAllowedToSelect(node))
            base.NodeClick(node);
    }
}

