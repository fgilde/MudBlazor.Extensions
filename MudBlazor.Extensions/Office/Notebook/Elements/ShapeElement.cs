namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>The kind of a shape.</summary>
public enum NoteShapeKind
{
    /// <summary>A straight line.</summary>
    Line,

    /// <summary>A straight line with an arrow head at its end.</summary>
    Arrow,

    /// <summary>A rectangle.</summary>
    Rectangle,

    /// <summary>An ellipse.</summary>
    Ellipse,

    /// <summary>A triangle pointing up.</summary>
    Triangle,

    /// <summary>A diamond.</summary>
    Diamond
}

/// <summary>A geometric shape. A line or arrow runs from the top left to the bottom right corner of its bounds unless flipped.</summary>
public sealed class ShapeElement : NoteElement
{
    internal ShapeElement()
    {
    }

    /// <summary>The kind of shape.</summary>
    public NoteShapeKind Kind { get; internal set; }

    /// <summary>The outline colour as <c>#RRGGBB</c>.</summary>
    public string Stroke { get; internal set; } = "#000000";

    /// <summary>The outline width in pixels.</summary>
    public double StrokeWidth { get; internal set; } = 2;

    /// <summary>The fill colour as <c>#RRGGBB</c>, or null for no fill.</summary>
    public string Fill { get; internal set; }

    /// <summary>True if a line or arrow starts at the right edge instead of the left.</summary>
    public bool FlipX { get; internal set; }

    /// <summary>True if a line or arrow starts at the bottom edge instead of the top.</summary>
    public bool FlipY { get; internal set; }
}
