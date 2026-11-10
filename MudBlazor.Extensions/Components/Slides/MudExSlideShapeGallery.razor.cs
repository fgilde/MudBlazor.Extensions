using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>The preset shapes of <see cref="PresetGeometry.Supported"/> as a grouped grid of previews, as in the Shapes gallery of PowerPoint.</summary>
public partial class MudExSlideShapeGallery
{
    internal static readonly (string Group, (string Preset, string Name)[] Shapes)[] Groups =
    [
        ("Lines", [("line", "Line"), ("straightConnector1", "Straight Connector")]),
        ("Rectangles", [("rect", "Rectangle"), ("roundRect", "Rounded Rectangle"), ("round2SameRect", "Round Same Side Corner Rectangle"), ("snip1Rect", "Snip Single Corner Rectangle")]),
        ("Basic shapes",
        [
            ("ellipse", "Oval"), ("triangle", "Isosceles Triangle"), ("rtTriangle", "Right Triangle"), ("parallelogram", "Parallelogram"), ("trapezoid", "Trapezoid"),
            ("diamond", "Diamond"), ("pentagon", "Regular Pentagon"), ("hexagon", "Hexagon"), ("octagon", "Octagon"), ("plus", "Cross"), ("donut", "Donut"),
            ("frame", "Frame"), ("heart", "Heart"), ("cloud", "Cloud")
        ]),
        ("Arrows",
        [
            ("rightArrow", "Right Arrow"), ("leftArrow", "Left Arrow"), ("upArrow", "Up Arrow"), ("downArrow", "Down Arrow"), ("leftRightArrow", "Left-Right Arrow"),
            ("homePlate", "Pentagon Arrow"), ("chevron", "Chevron")
        ]),
        ("Callouts", [("wedgeRectCallout", "Rectangular Callout"), ("wedgeRoundRectCallout", "Rounded Rectangular Callout")]),
        ("Stars", [("star5", "Star: 5 Points"), ("star6", "Star: 6 Points")])
    ];

    /// <summary>Raised with the preset name when the user picks a shape.</summary>
    [Parameter] public EventCallback<string> ShapeChosen { get; set; }

    internal static bool IsLine(string preset) => preset is "line" or "straightConnector1";

    internal static string NameOf(string preset) => Groups.SelectMany(g => g.Shapes).FirstOrDefault(s => s.Preset == preset).Name ?? preset;
}
