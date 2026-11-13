using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>Shows one shape of a slide at its resolved position. Groups render an empty frame; their children are shapes of their own.</summary>
public partial class MudExSlideShapeView
{
    private readonly string _paintId = "mud-ex-slide-paint-" + Guid.NewGuid().ToString("N");

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

    /// <summary>For a table with <see cref="HideText"/>: the cell whose text is left out.</summary>
    [Parameter] public (int Row, int Column)? HiddenCell { get; set; }

    /// <summary>The prompt shown in an empty placeholder with a dashed outline, or null.</summary>
    [Parameter] public string Prompt { get; set; }

    /// <summary>The number shown by slide number fields.</summary>
    [Parameter] public int SlideNumber { get; set; }

    /// <summary>The visibility and build animation of the shape in the slide show, or null.</summary>
    [Parameter] public SlideTargetState Target { get; set; }

    /// <summary>The visibility and build animation of the animated paragraphs in the slide show, or null.</summary>
    [Parameter] public IReadOnlyDictionary<(uint Shape, int? Paragraph), SlideTargetState> Targets { get; set; }

    private SlideMaster ColorMaster => Shape.Master ?? Master;

    private string PaintUrl => $"url(#{_paintId})";

    private string ShapeClass => Shape switch
    {
        OpaqueShape => "mud-ex-slide-shape mud-ex-slide-opaque",
        TableShape => "mud-ex-slide-shape mud-ex-slide-table-shape",
        GroupShape => "mud-ex-slide-shape mud-ex-slide-group",
        _ => "mud-ex-slide-shape"
    } + (Inherited ? " mud-ex-slide-inherited" : "") + (Prompt != null ? " mud-ex-slide-placeholder-empty" : "") + (Target?.CssClass is { } animation ? " " + animation : "");

    internal static string BoxStyle(ShapeTransform t, Presentation presentation, bool flip = true)
    {
        var style = $"left:{Percent(t.X, presentation.SlideWidth)}%;top:{Percent(t.Y, presentation.SlideHeight)}%;" +
                    $"width:{Percent(t.Width, presentation.SlideWidth)}%;height:{Percent(t.Height, presentation.SlideHeight)}%;";
        var transform = $"{(t.Rotation != 0 ? $"rotate({Num(t.Rotation)}deg) " : "")}{(flip && t.FlipH ? "scaleX(-1) " : "")}{(flip && t.FlipV ? "scaleY(-1)" : "")}".Trim();
        return transform.Length == 0 ? style : $"{style}transform:{transform};";
    }

    internal static string Num(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    internal static (double X1, double Y1, double X2, double Y2) GradientVector(double? angle)
    {
        var radians = (angle ?? 90) * Math.PI / 180;
        var (cos, sin) = (Math.Cos(radians), Math.Sin(radians));
        var half = (Math.Abs(cos) + Math.Abs(sin)) / 2;
        return (Clean(0.5 - cos * half), Clean(0.5 - sin * half), Clean(0.5 + cos * half), Clean(0.5 + sin * half));
    }

    private static double Clean(double value) => Math.Round(value, 6) + 0d;

    private static string Percent(long part, long whole) => Num(whole == 0 ? 0 : (double)part / whole * 100);

    private static string GeometryPath(AutoShape shape, bool visible)
    {
        var t = shape.ResolvedTransform;
        if (!visible || shape.Geometry == "custom" || t.Width <= 0 || t.Height <= 0) return null;
        var preset = PresetGeometry.IsSupported(shape.Geometry) ? shape.Geometry : "rect";
        return PresetGeometry.Path(preset, t.Width, t.Height, shape.Adjustments);
    }

    private string PaintOf(ShapeFill fill) => fill.Kind switch
    {
        ShapeFillKind.None => "none",
        ShapeFillKind.Gradient when MudExSlideView.Gradient(fill.Stops, ColorMaster, Shape.ColorMap).Count > 0 => PaintUrl,
        ShapeFillKind.Picture when Images?.UrlFor(fill.ImagePart) != null => PaintUrl,
        _ => Resolve(fill.Color) ?? "none"
    };

    private string StrokeOf(ShapeLine line) => Resolve(line?.Color) ?? "none";

    private string Resolve(string color) => ColorResolver.Resolve(color, ColorMaster, Shape.ColorMap);

    private static string CropStyle(ImageCrop crop)
    {
        if (crop == null) return null;
        var (width, height) = (1 - crop.Left - crop.Right, 1 - crop.Top - crop.Bottom);
        if (width <= 0 || height <= 0) return null;
        return $"left:{Num(-crop.Left / width * 100)}%;top:{Num(-crop.Top / height * 100)}%;width:{Num(100 / width)}%;height:{Num(100 / height)}%;";
    }

    private static string DashOf(ShapeLine line)
    {
        if (line == null) return null;
        int[] pattern = line.Dash switch
        {
            "dash" => [4, 3],
            "dot" => [1, 3],
            "dashDot" => [4, 3, 1, 3],
            "lgDash" => [8, 3],
            "lgDashDot" => [8, 3, 1, 3],
            "lgDashDotDot" => [8, 3, 1, 3, 1, 3],
            "sysDash" => [3, 1],
            "sysDot" => [1, 1],
            "sysDashDot" => [3, 1, 1, 1],
            "sysDashDotDot" => [3, 1, 1, 1, 1, 1],
            _ => null
        };
        return pattern == null ? null : string.Join(" ", pattern.Select(p => p * line.Width));
    }
}
