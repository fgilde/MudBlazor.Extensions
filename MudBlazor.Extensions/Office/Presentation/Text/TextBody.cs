using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>Vertical anchoring of text in its shape.</summary>
public enum TextAnchor
{
    /// <summary>Top.</summary>
    Top,

    /// <summary>Middle.</summary>
    Middle,

    /// <summary>Bottom.</summary>
    Bottom
}

/// <summary>The text of a shape. Body properties the slide does not set are inherited from the layout and master placeholders.</summary>
public sealed record TextBody
{
    /// <summary>The paragraphs in order.</summary>
    public IReadOnlyList<TextParagraph> Paragraphs { get; init; } = [];

    /// <summary>Vertical anchoring.</summary>
    public TextAnchor Anchor { get; init; }

    /// <summary>Left inset in EMU.</summary>
    public long InsetLeft { get; init; } = 91440;

    /// <summary>Top inset in EMU.</summary>
    public long InsetTop { get; init; } = 45720;

    /// <summary>Right inset in EMU.</summary>
    public long InsetRight { get; init; } = 91440;

    /// <summary>Bottom inset in EMU.</summary>
    public long InsetBottom { get; init; } = 45720;

    /// <summary>True if lines wrap at the shape width.</summary>
    public bool Wrap { get; init; } = true;

    internal XElement Source { get; init; }
}
