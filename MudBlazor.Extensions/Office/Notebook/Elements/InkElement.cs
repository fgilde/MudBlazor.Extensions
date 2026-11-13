namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>The tool a stroke was drawn with.</summary>
public enum InkTool
{
    /// <summary>An opaque pen.</summary>
    Pen,

    /// <summary>A translucent highlighter.</summary>
    Highlighter
}

/// <summary>A point of a stroke.</summary>
/// <param name="X">The horizontal position relative to the element.</param>
/// <param name="Y">The vertical position relative to the element.</param>
/// <param name="Pressure">The pen pressure from 0 to 1.</param>
public readonly record struct InkPoint(double X, double Y, double Pressure);

/// <summary>A freehand stroke. A stroke with a single point is a dot.</summary>
public sealed class InkElement : NoteElement
{
    internal InkElement()
    {
    }

    /// <summary>The points in drawing order, relative to the element's top left corner.</summary>
    public IReadOnlyList<InkPoint> Points { get; internal set; } = [];

    /// <summary>The colour as <c>#RRGGBB</c>.</summary>
    public string Color { get; internal set; } = "#000000";

    /// <summary>The stroke width in pixels.</summary>
    public double StrokeWidth { get; internal set; } = 2;

    /// <summary>The tool the stroke was drawn with.</summary>
    public InkTool Tool { get; internal set; }
}
