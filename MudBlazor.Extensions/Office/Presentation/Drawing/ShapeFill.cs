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

    /// <summary>A gradient, pattern, picture or group fill; kept unchanged when the file is written.</summary>
    Other,

    /// <summary>A picture (<c>a:blipFill</c>); only used by <see cref="SlideBackground"/>, picture-filled shapes report <see cref="Other"/>.</summary>
    Picture
}

/// <summary>The fill of a shape.</summary>
/// <param name="Kind">The kind of fill.</param>
/// <param name="Color">
/// The colour in the format of <see cref="RunFormat.Color"/>: the solid colour, the first gradient stop or pattern foreground for
/// <see cref="ShapeFillKind.Other"/>, the shape style colour for <see cref="ShapeFillKind.Inherited"/>; null if there is none.
/// </param>
public sealed record ShapeFill(ShapeFillKind Kind, string Color = null);
