namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>How a shape is filled.</summary>
public enum ShapeFillKind
{
    /// <summary>No fill (<c>a:noFill</c>).</summary>
    None,

    /// <summary>A solid colour (<c>a:solidFill</c>).</summary>
    Solid,

    /// <summary>No own fill; the shape style or placeholder decides.</summary>
    Inherited,

    /// <summary>A pattern or group fill; kept unchanged when the file is written.</summary>
    Other,

    /// <summary>A picture (<c>a:blipFill</c>); kept unchanged when the file is written.</summary>
    Picture,

    /// <summary>A gradient (<c>a:gradFill</c>); kept unchanged when the file is written.</summary>
    Gradient
}

/// <summary>A colour stop of a gradient.</summary>
/// <param name="Position">The position from 0 (start) to 1 (end).</param>
/// <param name="Color">The colour in the format of <see cref="RunFormat.Color"/>.</param>
public sealed record GradientStop(double Position, string Color);

/// <summary>The fill of a shape.</summary>
/// <param name="Kind">The kind of fill.</param>
/// <param name="Color">
/// The colour in the format of <see cref="RunFormat.Color"/>: the solid colour, the first gradient stop, the pattern foreground for
/// <see cref="ShapeFillKind.Other"/>, the shape style colour for <see cref="ShapeFillKind.Inherited"/>; null if there is none.
/// </param>
public sealed record ShapeFill(ShapeFillKind Kind, string Color = null)
{
    /// <summary>The colour stops of a gradient, ordered by position; empty for other fills.</summary>
    public IReadOnlyList<GradientStop> Stops { get; init; } = [];

    /// <summary>The direction of a linear gradient in degrees clockwise from left to right; null for path gradients and other fills.</summary>
    public double? Angle { get; init; }

    /// <summary>The image part of a picture fill, or null.</summary>
    public string ImagePart { get; init; }

    /// <inheritdoc />
    public bool Equals(ShapeFill other) =>
        other is not null && Kind == other.Kind && Color == other.Color && Angle == other.Angle && ImagePart == other.ImagePart && Stops.SequenceEqual(other.Stops);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Kind, Color, Angle, ImagePart, Stops.Count);

    internal static ShapeFill EffectiveFill(AutoShape shape) => shape.Fill switch
    {
        { Kind: not ShapeFillKind.Inherited } own => own,
        { Color: { } style } => new ShapeFill(ShapeFillKind.Solid, style),
        _ => shape.InheritsFrom is AutoShape parent ? EffectiveFill(parent) : new ShapeFill(ShapeFillKind.None)
    };
}
