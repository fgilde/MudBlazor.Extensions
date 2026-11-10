namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>Position, size, rotation and flip of a shape in EMU (914400 per inch, 12700 per point).</summary>
/// <param name="X">Left edge of the unrotated box.</param>
/// <param name="Y">Top edge of the unrotated box.</param>
/// <param name="Width">Width of the box.</param>
/// <param name="Height">Height of the box.</param>
/// <param name="Rotation">Clockwise rotation around the box centre in degrees.</param>
/// <param name="FlipH">True if the shape is mirrored horizontally.</param>
/// <param name="FlipV">True if the shape is mirrored vertically.</param>
public readonly record struct ShapeTransform(long X, long Y, long Width, long Height, double Rotation = 0, bool FlipH = false, bool FlipV = false);
