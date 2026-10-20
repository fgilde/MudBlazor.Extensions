using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A card of a <see cref="MudExOrgChart{TItem}"/> and, unless collapsed, its children.
/// </summary>
public partial class MudExOrgChartNode<TItem>
{
    private bool _over;

    [CascadingParameter] private MudExOrgChart<TItem> Chart { get; set; }

    /// <summary>The item of this card.</summary>
    [Parameter, EditorRequired] public TItem Item { get; set; }

    /// <summary>The tree version of the chart, so a card renders again when the tree changed.</summary>
    [Parameter] public int Version { get; set; }

    private string NodeClass(int key) => "mud-ex-org-chart-node"
        + (Chart.IsSelected(key) ? " mud-ex-org-chart-selected" : string.Empty)
        + (Chart.IsMatch(key) ? " mud-ex-org-chart-match" : string.Empty)
        + (Chart.IsCurrentMatch(key) ? " mud-ex-org-chart-current" : string.Empty)
        + (_over && Chart.CanDropOn(key) ? " mud-ex-org-chart-drop" : string.Empty);

    private Task DropAsync()
    {
        _over = false;
        return Chart.DropAsync(Chart.KeyOf(Item));
    }
}
