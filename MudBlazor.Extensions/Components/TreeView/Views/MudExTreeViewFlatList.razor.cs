using Microsoft.AspNetCore.Components;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

public partial class MudExTreeViewFlatList<T> 
    where T : IHierarchical<T>
{
    [Parameter] public bool ShowPath { get; set; } = true;
    [Parameter] public string PathSeparator { get; set; } = "/";
}

