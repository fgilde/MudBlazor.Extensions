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
}
