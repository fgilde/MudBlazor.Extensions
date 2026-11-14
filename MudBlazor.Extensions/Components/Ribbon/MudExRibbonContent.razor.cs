using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Hosts arbitrary content inside a ribbon group, for example a picker.
/// </summary>
public partial class MudExRibbonContent
{
    /// <summary>
    /// Content drawn in the ribbon.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <inheritdoc />
    public override bool IsCommand => false;
}
