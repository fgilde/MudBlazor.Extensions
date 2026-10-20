using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The roots of a <see cref="MudExOrgChart{TItem}"/>. Renders only when the tree changed, not on every pan or zoom.
/// </summary>
public partial class MudExOrgChartTree<TItem>
{
    private int _rendered = -1;

    /// <summary>The chart.</summary>
    [Parameter, EditorRequired] public MudExOrgChart<TItem> Chart { get; set; }

    /// <summary>Changes whenever the tree has to render again.</summary>
    [Parameter] public int Version { get; set; }

    /// <inheritdoc />
    protected override bool ShouldRender() => _rendered != Version;

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender) => _rendered = Version;
}
