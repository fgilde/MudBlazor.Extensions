using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>Shows one shape of a slide at its resolved position. Groups render an empty frame; their children are shapes of their own.</summary>
public partial class MudExSlideShapeView
{
    /// <summary>The shape to show.</summary>
    [Parameter] public SlideShape Shape { get; set; }

    /// <summary>The presentation that defines the slide size.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    /// <summary>The image URLs of the presentation.</summary>
    [Parameter] public SlideImageUrls Images { get; set; }

    /// <summary>The master used to resolve theme colours if the shape has none of its own.</summary>
    [Parameter] public SlideMaster Master { get; set; }

    private SlideMaster ColorMaster => Shape.Master ?? Master;

    private string ShapeClass => Shape switch
    {
        OpaqueShape => "mud-ex-slide-shape mud-ex-slide-opaque",
        GroupShape => "mud-ex-slide-shape mud-ex-slide-group",
        _ => "mud-ex-slide-shape"
    };

    private string FrameStyle
    {
        get
        {
            var t = Shape.ResolvedTransform;
            var style = $"left:{Percent(t.X, Presentation.SlideWidth)}%;top:{Percent(t.Y, Presentation.SlideHeight)}%;" +
                        $"width:{Percent(t.Width, Presentation.SlideWidth)}%;height:{Percent(t.Height, Presentation.SlideHeight)}%;";
            var transform = $"{(t.Rotation != 0 ? $"rotate({Num(t.Rotation)}deg) " : "")}{(t.FlipH ? "scaleX(-1) " : "")}{(t.FlipV ? "scaleY(-1)" : "")}".Trim();
            return transform.Length == 0 ? style : $"{style}transform:{transform};";
        }
    }

    internal static string Num(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    private static string Percent(long part, long whole) => Num(whole == 0 ? 0 : (double)part / whole * 100);

    private static string GeometryPath(AutoShape shape)
    {
        var t = shape.ResolvedTransform;
        if (shape.Geometry == "custom" || t.Width <= 0 || t.Height <= 0) return null;
        var hasFill = shape.Fill.Kind != ShapeFillKind.None && shape.Fill.Color != null;
        if (!hasFill && shape.Line?.Color == null) return null;
        var preset = PresetGeometry.IsSupported(shape.Geometry) ? shape.Geometry : "rect";
        return PresetGeometry.Path(preset, t.Width, t.Height, shape.Adjustments);
    }

    private string FillOf(AutoShape shape) =>
        shape.Fill.Kind == ShapeFillKind.None || string.IsNullOrWhiteSpace(shape.Fill.Color) ? "none" : ColorResolver.Resolve(shape.Fill.Color, ColorMaster);

    private string StrokeOf(AutoShape shape) =>
        string.IsNullOrWhiteSpace(shape.Line?.Color) ? "none" : ColorResolver.Resolve(shape.Line.Color, ColorMaster);

    private static string DashOf(AutoShape shape)
    {
        if (shape.Line == null) return null;
        var unit = shape.Line.Width;
        return shape.Line.Dash switch
        {
            "dash" or "sysDash" => $"{unit * 4} {unit * 3}",
            "dot" or "sysDot" => $"{unit} {unit * 2}",
            "dashDot" or "sysDashDot" => $"{unit * 4} {unit * 3} {unit} {unit * 3}",
            _ => null
        };
    }
}
