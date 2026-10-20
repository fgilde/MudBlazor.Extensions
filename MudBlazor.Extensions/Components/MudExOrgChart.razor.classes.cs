using System.ComponentModel;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Direction in which <see cref="MudExOrgChart{TItem}"/> draws its tree.
/// </summary>
public enum OrgChartOrientation
{
    /// <summary>
    /// The roots at the top, children below.
    /// </summary>
    [Description("mud-ex-org-chart-td")]
    TopDown,

    /// <summary>
    /// The roots at the left, children to the right.
    /// </summary>
    [Description("mud-ex-org-chart-lr")]
    LeftRight
}

/// <summary>
/// Raised when a node of <see cref="MudExOrgChart{TItem}"/> is dropped onto another node.
/// </summary>
/// <param name="Item">The dragged item.</param>
/// <param name="OldParent">The parent before the drop, default for a root.</param>
/// <param name="NewParent">The node it was dropped onto, default when dropped onto the empty canvas or the root drop zone.</param>
public record OrgChartReparentArgs<TItem>(TItem Item, TItem OldParent, TItem NewParent)
{
    /// <summary>Why the drop was refused, only set for <c>OnReparentRefused</c>.</summary>
    public OrgChartReparentRefusal? Refusal { get; init; }
}

/// <summary>
/// Why <see cref="MudExOrgChart{TItem}"/> refused a drop.
/// </summary>
public enum OrgChartReparentRefusal
{
    /// <summary>The new parent is below the dragged item.</summary>
    Cycle,

    /// <summary><c>CanReparent</c> returned false.</summary>
    NotAllowed
}

/// <summary>
/// A measured node in stage coordinates, used by the minimap.
/// </summary>
public record OrgChartBox(int Key, double X, double Y, double Width, double Height);

/// <summary>
/// A connector line in stage coordinates, used by the minimap.
/// </summary>
public record OrgChartLine(double X1, double Y1, double X2, double Y2);
