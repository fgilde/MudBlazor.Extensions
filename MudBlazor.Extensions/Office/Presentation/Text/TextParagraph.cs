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

/// <summary>The kind of bullet of a paragraph.</summary>
public enum BulletKind
{
    /// <summary>No bullet (<c>a:buNone</c>).</summary>
    None,

    /// <summary>A bullet character (<c>a:buChar</c> or a picture bullet).</summary>
    Char,

    /// <summary>A number that counts up over the paragraphs of the same level (<c>a:buAutoNum</c>).</summary>
    AutoNumber,

    /// <summary>No own bullet; the list styles decide.</summary>
    Inherited
}

/// <summary>A paragraph of a text body. Spacing, indent and bullet values hold the paragraph's own values; null inherits them from the list styles.</summary>
public sealed record TextParagraph
{
    /// <summary>The paragraph's own alignment; null inherits it from the list styles.</summary>
    public TextAlign? Align { get; init; }

    /// <summary>The outline level, 0 to 8.</summary>
    public int Level { get; init; }

    /// <summary>True for a bullet, false for an explicit <c>a:buNone</c>, null to inherit.</summary>
    public bool? Bullet { get; init; }

    /// <summary>The left margin of the text in EMU; null inherits it.</summary>
    public long? MarginLeft { get; init; }

    /// <summary>The indent of the first line relative to <see cref="MarginLeft"/> in EMU, negative for a hanging indent; null inherits it.</summary>
    public long? Indent { get; init; }

    /// <summary>The line spacing in percent of single spacing; null inherits it.</summary>
    public double? LineSpacingPercent { get; init; }

    /// <summary>The space above the paragraph in points; null inherits it.</summary>
    public double? SpaceBeforePt { get; init; }

    /// <summary>The space below the paragraph in points; null inherits it.</summary>
    public double? SpaceAfterPt { get; init; }

    /// <summary>The paragraph's own kind of bullet as read from the file; <see cref="Bullet"/> takes precedence when a command changed it.</summary>
    public BulletKind BulletKind { get; init; } = BulletKind.Inherited;

    /// <summary>The bullet character of <see cref="BulletKind.Char"/>, or null.</summary>
    public string BulletChar { get; init; }

    /// <summary>The numbering scheme of <see cref="BulletKind.AutoNumber"/>, for example <c>arabicPeriod</c> or <c>alphaLcParenR</c>, or null.</summary>
    public string AutoNumberType { get; init; }

    /// <summary>The first number of <see cref="BulletKind.AutoNumber"/>, or null for 1.</summary>
    public int? AutoNumberStart { get; init; }

    /// <summary>The runs in order.</summary>
    public IReadOnlyList<TextRun> Runs { get; init; } = [];

    /// <summary>Formatting of the paragraph end (<c>a:endParaRPr</c>), used for the caret in an empty paragraph; null if none.</summary>
    public RunFormat EndFormat { get; init; }

    internal string BulletFont { get; init; }

    internal XElement Source { get; init; }
}
