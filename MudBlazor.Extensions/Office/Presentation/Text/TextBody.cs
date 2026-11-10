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

/// <summary>The text of a shape. Body properties hold the shape's own values; null inherits them from the layout and master placeholders.</summary>
public sealed record TextBody
{
    /// <summary>The paragraphs in order.</summary>
    public IReadOnlyList<TextParagraph> Paragraphs { get; init; } = [];

    /// <summary>Vertical anchoring; null inherits it.</summary>
    public TextAnchor? Anchor { get; init; }

    /// <summary>Left inset in EMU; null inherits it.</summary>
    public long? InsetLeft { get; init; }

    /// <summary>Top inset in EMU; null inherits it.</summary>
    public long? InsetTop { get; init; }

    /// <summary>Right inset in EMU; null inherits it.</summary>
    public long? InsetRight { get; init; }

    /// <summary>Bottom inset in EMU; null inherits it.</summary>
    public long? InsetBottom { get; init; }

    /// <summary>True if lines wrap at the shape width; null inherits it.</summary>
    public bool? Wrap { get; init; }

    internal XElement Source { get; init; }
}
