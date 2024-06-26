using Microsoft.AspNetCore.Components;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeViewFlatList<T> 
    where T : IHierarchical<T>
{
    /// <summary>
    /// Set to true to show the item path under the item name.
    /// </summary>
    [Parameter] public bool ShowPath { get; set; } = true;

    /// <summary>
    /// Separator for the path.
    /// </summary>
    [Parameter] public string PathSeparator { get; set; } = "/";
}

