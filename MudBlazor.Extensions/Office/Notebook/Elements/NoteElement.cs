using System.Text.Json;

namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>A rectangle on a page in CSS pixels at 100 % zoom, origin at the top left of the page.</summary>
/// <param name="X">The left edge.</param>
/// <param name="Y">The top edge.</param>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct NoteBounds(double X, double Y, double Width, double Height)
{
    /// <summary>The right edge.</summary>
    public double Right => X + Width;

    /// <summary>The bottom edge.</summary>
    public double Bottom => Y + Height;

    /// <summary>The smallest rectangle containing both rectangles.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns>The hull.</returns>
    public NoteBounds Union(NoteBounds other)
    {
        var x = Math.Min(X, other.X);
        var y = Math.Min(Y, other.Y);
        return new NoteBounds(x, y, Math.Max(Right, other.Right) - x, Math.Max(Bottom, other.Bottom) - y);
    }
}

/// <summary>An element on a notebook page. Coordinates are CSS pixels at 100 % zoom, origin at the top left of the page.</summary>
public abstract class NoteElement
{
    private protected NoteElement()
    {
    }

    /// <summary>The element id, unique on its page.</summary>
    public string Id { get; internal set; } = NoteValues.NewId();

    /// <summary>The left edge.</summary>
    public double X { get; internal set; }

    /// <summary>The top edge.</summary>
    public double Y { get; internal set; }

    /// <summary>The width.</summary>
    public double Width { get; internal set; }

    /// <summary>The height; 0 for text and tables means the height follows the content.</summary>
    public double Height { get; internal set; }

    /// <summary>The element's rectangle.</summary>
    public NoteBounds Bounds => new(X, Y, Width, Height);

    internal Dictionary<string, JsonElement> Extra { get; set; }
}
