using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Notebook;

using MudBlazor.Extensions.Office.Notebook;

public class InkGeometryTests
{
    private static readonly (double X, double Y)[] Square = [(0, 0), (100, 0), (100, 100), (0, 100)];

    private static InkElement Stroke(params (double X, double Y)[] points) => new()
    {
        X = 0, Y = 0, StrokeWidth = 2, Points = points.Select(p => new InkPoint(p.X, p.Y, 0.5)).ToList()
    };

    [Fact]
    public void Eraser_HitsOnlyTouchedStrokes()
    {
        var crossed = Stroke((0, 10), (100, 10));
        var below = Stroke((0, 100), (100, 100));
        var near = Stroke((0, 40), (100, 40));
        var dot = Stroke((78, 24));
        (double X, double Y)[] path = [(50, 0), (50, 30), (77, 25)];

        Assert.True(InkGeometry.HitByEraser(crossed, path, 2));
        Assert.False(InkGeometry.HitByEraser(below, path, 2));
        Assert.False(InkGeometry.HitByEraser(near, path, 2));
        Assert.True(InkGeometry.HitByEraser(dot, path, 2));
        Assert.False(InkGeometry.HitByEraser(dot, [(90, 90)], 2));
    }

    [Fact]
    public void Eraser_UsesPagePositionOfStroke()
    {
        var stroke = Stroke((0, 0), (10, 0));
        stroke.X = 200;
        stroke.Y = 300;

        Assert.True(InkGeometry.HitByEraser(stroke, [(205, 301)], 1));
        Assert.False(InkGeometry.HitByEraser(stroke, [(5, 1)], 1));
    }

    [Fact]
    public void Lasso_SelectsMostlyInsideStrokesOnly()
    {
        var mostlyInside = Stroke((10, 10), (20, 20), (30, 30), (150, 150));
        var half = Stroke((10, 10), (20, 20), (150, 150), (160, 160));
        var mostlyOutside = Stroke((10, 10), (150, 150), (160, 160), (170, 170));
        var shapeInside = new ShapeElement { X = 80, Y = 80, Width = 30, Height = 30 };
        var shapeOutside = new ShapeElement { X = 90, Y = 90, Width = 40, Height = 40 };

        Assert.True(InkGeometry.InsideLasso(mostlyInside, Square));
        Assert.False(InkGeometry.InsideLasso(half, Square));
        Assert.False(InkGeometry.InsideLasso(mostlyOutside, Square));
        Assert.True(InkGeometry.InsideLasso(shapeInside, Square));
        Assert.False(InkGeometry.InsideLasso(shapeOutside, Square));
        Assert.False(InkGeometry.InsideLasso(mostlyInside, [(0, 0), (100, 100)]));
    }

    [Fact]
    public void Simplify_KeepsEndpoints()
    {
        var line = Enumerable.Range(0, 11).Select(i => new InkPoint(i, i * 0.01, 0.5)).ToList();
        InkPoint[] corner = [new(0, 0, 0.5), new(5, 0.1, 0.5), new(10, 0, 0.5), new(10, 5, 0.5), new(10, 10, 0.7)];

        Assert.Equal([line[0], line[^1]], InkGeometry.Simplify(line));
        Assert.Equal([corner[0], corner[2], corner[4]], InkGeometry.Simplify(corner));
        Assert.Equal([new InkPoint(1, 2, 0.5)], InkGeometry.Simplify([new InkPoint(1, 2, 0.5)]));
        Assert.Empty(InkGeometry.Simplify([]));
    }
}
