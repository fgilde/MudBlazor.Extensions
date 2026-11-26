namespace MudBlazor.Extensions.Office.Document;

/// <summary>Vertical position of a run relative to the baseline.</summary>
public enum VerticalAlign
{
    /// <summary>On the baseline.</summary>
    Baseline,

    /// <summary>Raised and smaller.</summary>
    Superscript,

    /// <summary>Lowered and smaller.</summary>
    Subscript
}

/// <summary>Character formatting of a run, a paragraph mark or a style (<c>w:rPr</c>). Null properties are inherited.</summary>
public sealed record RunFormat
{
    /// <summary>Bold; null inherits it.</summary>
    public bool? Bold { get; init; }

    /// <summary>Italic; null inherits it.</summary>
    public bool? Italic { get; init; }

    /// <summary>Struck through with a single line; null inherits it.</summary>
    public bool? Strike { get; init; }

    /// <summary>Struck through with a double line; null inherits it.</summary>
    public bool? DoubleStrike { get; init; }

    /// <summary>Shown in capital letters; null inherits it.</summary>
    public bool? Caps { get; init; }

    /// <summary>Lower case letters shown as small capitals; null inherits it.</summary>
    public bool? SmallCaps { get; init; }

    /// <summary>Hidden text; null inherits it.</summary>
    public bool? Hidden { get; init; }

    /// <summary>The underline style as the <c>w:u</c> value, for example <c>single</c>, <c>double</c> or <c>none</c>; null inherits it.</summary>
    public string Underline { get; init; }

    /// <summary>Font size in points; null inherits it.</summary>
    public double? SizePt { get; init; }

    /// <summary>The ASCII typeface name, or <c>theme:</c> with the <c>w:asciiTheme</c> value, for example <c>theme:minorHAnsi</c>; null inherits it.</summary>
    public string Font { get; init; }

    /// <summary>Text colour: <c>#RRGGBB</c>, <c>auto</c> or <c>theme:&lt;name&gt;</c> with optional <c>shade=XX</c>/<c>tint=XX</c>; null inherits it.</summary>
    public string Color { get; init; }

    /// <summary>The highlight colour name as the <c>w:highlight</c> value, for example <c>yellow</c>; null inherits it.</summary>
    public string Highlight { get; init; }

    /// <summary>The shading fill colour in the same notation as <see cref="Color"/>; null inherits it.</summary>
    public string Shading { get; init; }

    /// <summary>Superscript or subscript; null inherits it.</summary>
    public VerticalAlign? VerticalAlign { get; init; }

    /// <summary>The character style id (<c>w:rStyle</c>); null for none.</summary>
    public string StyleId { get; init; }

    /// <summary>Combines this format with a basis: the own non-null values win, the others come from the basis.</summary>
    /// <param name="basis">The inherited format, or null.</param>
    /// <returns>The combined format.</returns>
    public RunFormat Over(RunFormat basis) => basis == null
        ? this
        : new RunFormat
        {
            Bold = Bold ?? basis.Bold,
            Italic = Italic ?? basis.Italic,
            Strike = Strike ?? basis.Strike,
            DoubleStrike = DoubleStrike ?? basis.DoubleStrike,
            Caps = Caps ?? basis.Caps,
            SmallCaps = SmallCaps ?? basis.SmallCaps,
            Hidden = Hidden ?? basis.Hidden,
            Underline = Underline ?? basis.Underline,
            SizePt = SizePt ?? basis.SizePt,
            Font = Font ?? basis.Font,
            Color = Color ?? basis.Color,
            Highlight = Highlight ?? basis.Highlight,
            Shading = Shading ?? basis.Shading,
            VerticalAlign = VerticalAlign ?? basis.VerticalAlign,
            StyleId = StyleId ?? basis.StyleId
        };
}
