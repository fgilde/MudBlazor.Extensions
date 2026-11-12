namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>The background of a slide, layout or master.</summary>
/// <param name="Kind">
/// <see cref="ShapeFillKind.Solid"/>, <see cref="ShapeFillKind.Gradient"/>, <see cref="ShapeFillKind.Other"/> for a pattern,
/// <see cref="ShapeFillKind.Picture"/> or <see cref="ShapeFillKind.None"/>.
/// </param>
/// <param name="Color">The colour in the format of <see cref="RunFormat.Color"/>; the first stop of a gradient; null for a picture or no fill.</param>
/// <param name="ImagePart">The image part of a picture background, or null.</param>
public sealed record SlideBackground(ShapeFillKind Kind, string Color, string ImagePart)
{
    private static readonly SlideBackground Default = new(ShapeFillKind.Solid, "scheme:bg1", null);

    /// <summary>The colour stops of a gradient background, ordered by position; empty for other backgrounds.</summary>
    public IReadOnlyList<GradientStop> Stops { get; init; } = [];

    /// <summary>The direction of a linear gradient in degrees clockwise from left to right; null for path gradients and other backgrounds.</summary>
    public double? Angle { get; init; }

    /// <inheritdoc />
    public bool Equals(SlideBackground other) =>
        other is not null && Kind == other.Kind && Color == other.Color && ImagePart == other.ImagePart && Angle == other.Angle && Stops.SequenceEqual(other.Stops);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Kind, Color, ImagePart, Angle, Stops.Count);

    internal static SlideBackground EffectiveBackground(Slide slide) =>
        slide.Background ?? slide.Layout?.Background ?? slide.Layout?.Master?.Background ?? Default;
}
