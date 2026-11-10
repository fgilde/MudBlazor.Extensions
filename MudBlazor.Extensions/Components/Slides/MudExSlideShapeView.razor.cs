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

    /// <summary>True for a shape of the layout or master: it is drawn behind the slide's shapes and takes no pointer events.</summary>
    [Parameter] public bool Inherited { get; set; }

    /// <summary>True to leave out the text, for example while it is edited.</summary>
    [Parameter] public bool HideText { get; set; }

    private SlideMaster ColorMaster => Shape.Master ?? Master;

    private string ShapeClass => Shape switch
    {
        OpaqueShape => "mud-ex-slide-shape mud-ex-slide-opaque",
        GroupShape => "mud-ex-slide-shape mud-ex-slide-group",
        _ => "mud-ex-slide-shape"
    } + (Inherited ? " mud-ex-slide-inherited" : "");

    internal static string BoxStyle(ShapeTransform t, Presentation presentation, bool flip = true)
    {
        var style = $"left:{Percent(t.X, presentation.SlideWidth)}%;top:{Percent(t.Y, presentation.SlideHeight)}%;" +
                    $"width:{Percent(t.Width, presentation.SlideWidth)}%;height:{Percent(t.Height, presentation.SlideHeight)}%;";
        var transform = $"{(t.Rotation != 0 ? $"rotate({Num(t.Rotation)}deg) " : "")}{(flip && t.FlipH ? "scaleX(-1) " : "")}{(flip && t.FlipV ? "scaleY(-1)" : "")}".Trim();
        return transform.Length == 0 ? style : $"{style}transform:{transform};";
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
