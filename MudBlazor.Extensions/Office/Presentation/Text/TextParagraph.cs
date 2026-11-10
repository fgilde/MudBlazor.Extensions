using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>Horizontal alignment of a paragraph.</summary>
public enum TextAlign
{
    /// <summary>Left aligned.</summary>
    Left,

    /// <summary>Centred.</summary>
    Center,

    /// <summary>Right aligned.</summary>
    Right,

    /// <summary>Justified.</summary>
    Justify
}

/// <summary>A paragraph of a text body.</summary>
public sealed record TextParagraph
{
    /// <summary>The alignment, inherited from the list styles when the paragraph sets none.</summary>
    public TextAlign Align { get; init; }

    /// <summary>The outline level, 0 to 8.</summary>
    public int Level { get; init; }

    /// <summary>True for a bullet, false for an explicit <c>a:buNone</c>, null to inherit.</summary>
    public bool? Bullet { get; init; }

    /// <summary>The runs in order.</summary>
    public IReadOnlyList<TextRun> Runs { get; init; } = [];

    /// <summary>Formatting of the paragraph end (<c>a:endParaRPr</c>), used for the caret in an empty paragraph; null if none.</summary>
    public RunFormat EndFormat { get; init; }

    internal XElement Source { get; init; }
}
