using MudBlazor.Extensions.Components.ObjectEdit;
using Nextended.Core.Types;
using static MudBlazor.CategoryTypes;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeViewBreadcrumb<T> 
    where T : IHierarchical<T>
{
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

