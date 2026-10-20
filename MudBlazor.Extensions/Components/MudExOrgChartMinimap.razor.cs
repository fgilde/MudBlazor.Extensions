using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The overview of a <see cref="MudExOrgChart{TItem}"/>: every card as a box, the visible area as a rectangle; click or drag to move the view.
/// </summary>
public partial class MudExOrgChartMinimap
{
    private bool _dragging;

    /// <summary>Shows the map, otherwise only the toggle button.</summary>
    [Parameter] public bool Open { get; set; } = true;

    /// <summary>Raised by the toggle button.</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>The corner of the chart.</summary>
    [Parameter] public Origin Position { get; set; } = Origin.BottomRight;

    /// <summary>The width of the map box in pixels; the tree is fitted in with its aspect ratio kept.</summary>
    [Parameter] public double Width { get; set; } = 220;

    /// <summary>The height of the map box in pixels.</summary>
    [Parameter] public double Height { get; set; } = 140;

    /// <summary>The unscaled width of the whole tree.</summary>
    [Parameter] public double StageWidth { get; set; }

    /// <summary>The unscaled height of the whole tree.</summary>
    [Parameter] public double StageHeight { get; set; }

    /// <summary>The cards in stage coordinates.</summary>
    [Parameter] public IEnumerable<OrgChartBox> Boxes { get; set; } = [];

    /// <summary>The connectors in stage coordinates.</summary>
    [Parameter] public IEnumerable<OrgChartLine> Lines { get; set; } = [];

    /// <summary>Keys of highlighted cards (selection, search matches).</summary>
    [Parameter] public ISet<int> Marked { get; set; } = new HashSet<int>();

    /// <summary>The visible area in stage coordinates.</summary>
    [Parameter] public OrgChartBox View { get; set; } = new(-1, 0, 0, 0, 0);

    /// <summary>Raised with the stage point the view should center on.</summary>
    [Parameter] public EventCallback<(double X, double Y)> OnNavigate { get; set; }

    /// <summary>The tooltip of the toggle button.</summary>
    [Parameter] public string ToggleTitle { get; set; } = "Map";

    private double Scale => StageWidth > 0 && StageHeight > 0 ? Math.Min(Width / StageWidth, Height / StageHeight) : 0;

    private string PositionClass => (Position.ToString().StartsWith("Top") ? "top" : "bottom") + (Position.ToString().EndsWith("Left") ? " left" : " right");

    private static string N(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    private Task Down(PointerEventArgs e)
    {
        _dragging = true;
        return Move(e);
    }

    // the children ignore the pointer (CSS), so the offset is relative to the svg; the tree is centered in the box (letterboxed)
    private Task Move(PointerEventArgs e) => _dragging
        ? OnNavigate.InvokeAsync(((e.OffsetX - ((Width - (StageWidth * Scale)) / 2)) / Scale, (e.OffsetY - ((Height - (StageHeight * Scale)) / 2)) / Scale))
        : Task.CompletedTask;
}
