namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>The background of a slide, layout or master.</summary>
/// <param name="Kind">
/// <see cref="ShapeFillKind.Solid"/>, <see cref="ShapeFillKind.Other"/> for a gradient or pattern, <see cref="ShapeFillKind.Picture"/> or
/// <see cref="ShapeFillKind.None"/>.
/// </param>
/// <param name="Color">The colour in the format of <see cref="RunFormat.Color"/>; the first stop of a gradient; null for a picture or no fill.</param>
/// <param name="ImagePart">The image part of a picture background, or null.</param>
public sealed record SlideBackground(ShapeFillKind Kind, string Color, string ImagePart)
{
    private static readonly SlideBackground Default = new(ShapeFillKind.Solid, "scheme:bg1", null);

    internal static SlideBackground EffectiveBackground(Slide slide) =>
        slide.Background ?? slide.Layout?.Background ?? slide.Layout?.Master?.Background ?? Default;
}
