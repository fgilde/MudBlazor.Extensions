namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A slide of a presentation.</summary>
public sealed class Slide
{
    internal Slide()
    {
    }

    /// <summary>A key that identifies the slide while the presentation is in memory.</summary>
    public string Key { get; } = Guid.NewGuid().ToString("N");

    /// <summary>The layout the slide is based on, or null if the file references none.</summary>
    public SlideLayout Layout { get; internal set; }

    /// <summary>The top-level shapes; the list order is the z-order.</summary>
    public IReadOnlyList<SlideShape> Shapes => ShapeList;

    /// <summary>The speaker notes, paragraphs separated by newlines; empty if there are none.</summary>
    public string Notes { get; internal set; } = "";

    /// <summary>True if the slide has a notes slide part.</summary>
    public bool HasNotesPart { get; internal set; }

    /// <summary>True if the slide is hidden in the slide show.</summary>
    public bool Hidden { get; internal set; }

    /// <summary>True if the slide changed since it was loaded.</summary>
    public bool Modified { get; internal set; }

    /// <summary>Incremented by every change of the slide; use it as render key.</summary>
    public int Version { get; internal set; }

    internal List<SlideShape> ShapeList { get; } = [];
}
