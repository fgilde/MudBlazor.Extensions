namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A preset geometry shape, text box, placeholder or connector, optionally with text.</summary>
public sealed class AutoShape : SlideShape
{
    internal AutoShape()
    {
    }

    /// <summary>The preset geometry name, <c>rect</c> by default; <c>custom</c> for a custom geometry.</summary>
    public string Geometry { get; internal set; } = "rect";

    /// <summary>The adjust values of the preset geometry by guide name.</summary>
    public IReadOnlyDictionary<string, long> Adjustments { get; internal set; } = new Dictionary<string, long>();

    /// <summary>The fill.</summary>
    public ShapeFill Fill { get; internal set; } = new(ShapeFillKind.Inherited);

    /// <summary>The outline, or null if it is inherited.</summary>
    public ShapeLine Line { get; internal set; }

    /// <summary>The text, or null if the shape has no text body.</summary>
    public TextBody Text { get; internal set; }

    /// <summary>True if the shape is a text box.</summary>
    public bool IsTextBox { get; internal set; }

    /// <summary>True if the shape is a connector; connectors have no text.</summary>
    public bool IsConnector { get; internal set; }

    /// <summary>Creates a shape of a preset geometry to add with <see cref="InsertShapeCommand"/>.</summary>
    /// <param name="preset">A preset name from <see cref="PresetGeometry.Supported"/>.</param>
    /// <param name="transform">The position and size in slide coordinates.</param>
    /// <param name="fill">The fill, or null to inherit it.</param>
    /// <param name="line">The outline, or null to inherit it.</param>
    /// <param name="text">The text, or null for a shape without text.</param>
    /// <returns>The new shape.</returns>
    public static AutoShape Create(string preset, ShapeTransform transform, ShapeFill fill = null, ShapeLine line = null, TextBody text = null) =>
        PresetGeometry.IsSupported(preset)
            ? new AutoShape { Geometry = preset, Transform = transform, Fill = fill ?? new ShapeFill(ShapeFillKind.Inherited), Line = line, Text = text }
            : throw new ArgumentException($"The preset geometry '{preset}' is not supported.", nameof(preset));

    /// <summary>
    /// Creates a shape of a preset geometry in the theme's shape style, as PowerPoint inserts it: accent 1 fill, a darker accent 1 outline and an empty
    /// centred text body whose text is light (<c>lt1</c>) in the minor font. Add it with <see cref="InsertShapeCommand"/>.
    /// </summary>
    /// <param name="preset">A preset name from <see cref="PresetGeometry.Supported"/>.</param>
    /// <param name="transform">The position and size in slide coordinates.</param>
    /// <returns>The new shape.</returns>
    public static AutoShape CreateStyled(string preset, ShapeTransform transform)
    {
        var shape = Create(preset, transform);
        shape.Source = SlideXml.StyledShape(preset);
        ShapeReader.ReadStyle(shape, shape.Source);
        shape.Text = ShapeReader.ReadText(shape.Source.Element(PptxReader.P + "txBody"));
        return shape;
    }

    /// <summary>Creates a text box without fill to add with <see cref="InsertShapeCommand"/>.</summary>
    /// <param name="transform">The position and size in slide coordinates.</param>
    /// <param name="text">The text, or null for one empty paragraph.</param>
    /// <returns>The new text box.</returns>
    public static AutoShape CreateTextBox(ShapeTransform transform, TextBody text = null) => new()
    {
        IsTextBox = true, Fill = new ShapeFill(ShapeFillKind.None), Transform = transform, Text = text ?? new TextBody { Wrap = true, Paragraphs = [new TextParagraph()] }
    };
}
