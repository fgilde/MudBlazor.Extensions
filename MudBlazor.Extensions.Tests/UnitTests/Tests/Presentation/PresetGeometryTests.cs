using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public class PresetGeometryTests
{
    private static readonly string[] Required =
    [
        "rect", "roundRect", "ellipse", "triangle", "rtTriangle", "diamond", "parallelogram", "trapezoid", "pentagon", "hexagon", "octagon",
        "star5", "star6", "plus", "rightArrow", "leftArrow", "upArrow", "downArrow", "leftRightArrow", "chevron", "homePlate", "line",
        "straightConnector1", "wedgeRectCallout", "wedgeRoundRectCallout", "cloud", "heart", "donut", "frame", "snip1Rect", "round2SameRect"
    ];

    [Fact]
    public void Path_Rect_IsBox() => Assert.Equal("M0,0 L100,0 L100,50 L0,50 Z", PresetGeometry.Path("rect", 100, 50));

    [Fact]
    public void Path_RoundRect_DefaultAdjustRadiusIsSixthOfShortSide() =>
        Assert.Equal("M0,10 A10,10 0 0 1 10,0 L90,0 A10,10 0 0 1 100,10 L100,50 A10,10 0 0 1 90,60 L10,60 A10,10 0 0 1 0,50 Z",
            PresetGeometry.Path("roundRect", 100, 60));

    [Fact]
    public void Path_RightArrow_UsesAdjustments() =>
        Assert.Equal("M0,40 L60,40 L60,0 L100,50 L60,100 L60,60 L0,60 Z",
            PresetGeometry.Path("rightArrow", 100, 100, new Dictionary<string, long> { ["adj1"] = 20000, ["adj2"] = 40000 }));

    [Fact]
    public void Path_UnknownPreset_FallsBackToRect()
    {
        Assert.Equal(PresetGeometry.Path("rect", 30, 20), PresetGeometry.Path("noSuchShape", 30, 20));
        Assert.False(PresetGeometry.IsSupported("noSuchShape"));
    }

    [Fact]
    public void Supported_ContainsAllRequiredPresets()
    {
        foreach (var preset in Required)
        {
            Assert.Contains(preset, PresetGeometry.Supported);
            Assert.True(PresetGeometry.IsSupported(preset));
            Assert.StartsWith("M", PresetGeometry.Path(preset, 120, 80));
        }
    }
}
