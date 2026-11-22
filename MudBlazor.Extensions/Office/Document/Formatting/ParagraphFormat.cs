namespace MudBlazor.Extensions.Office.Document;

/// <summary>Horizontal alignment of a paragraph.</summary>
public enum ParagraphAlignment
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

/// <summary>How <see cref="LineSpacing.Value"/> is interpreted.</summary>
public enum LineRule
{
    /// <summary>A multiple of single spacing in 240ths (240 = single, 360 = 1.5 lines).</summary>
    Auto,

    /// <summary>Exactly the value in twips.</summary>
    Exact,

    /// <summary>At least the value in twips.</summary>
    AtLeast
}

/// <summary>The line spacing of a paragraph.</summary>
/// <param name="Value">The spacing in 240ths of a line for <see cref="LineRule.Auto"/>, otherwise in twips.</param>
/// <param name="Rule">How the value is interpreted.</param>
public readonly record struct LineSpacing(int Value, LineRule Rule);

/// <summary>The list a paragraph belongs to (<c>w:numPr</c>).</summary>
/// <param name="NumId">The numbering instance id; 0 explicitly removes an inherited numbering.</param>
/// <param name="Level">The list level, 0 to 8.</param>
public readonly record struct NumberingRef(int NumId, int Level);

/// <summary>Alignment of a tab stop.</summary>
public enum TabAlignment
{
    /// <summary>Text starts at the stop.</summary>
    Left,

    /// <summary>Text is centred on the stop.</summary>
    Center,

    /// <summary>Text ends at the stop.</summary>
    Right,

    /// <summary>The decimal separator sits on the stop.</summary>
    Decimal,

    /// <summary>A vertical bar is drawn at the stop.</summary>
    Bar,

    /// <summary>Removes an inherited stop at this position.</summary>
    Clear
}

/// <summary>A custom tab stop of a paragraph.</summary>
/// <param name="Position">The position from the left text margin in twips.</param>
/// <param name="Alignment">The alignment at the stop.</param>
/// <param name="Leader">The leader as the <c>w:leader</c> value, for example <c>dot</c>; null for none.</param>
public sealed record TabStop(int Position, TabAlignment Alignment, string Leader);

/// <summary>Paragraph formatting of a paragraph or a style (<c>w:pPr</c>). Null properties are inherited. Lengths are in twips.</summary>
public sealed record ParagraphFormat
{
    /// <summary>The paragraph style id (<c>w:pStyle</c>); null for the default paragraph style.</summary>
    public string StyleId { get; init; }

    /// <summary>The horizontal alignment; null inherits it.</summary>
    public ParagraphAlignment? Alignment { get; init; }

    /// <summary>The left indent; null inherits it.</summary>
    public int? IndentLeft { get; init; }

    /// <summary>The right indent; null inherits it.</summary>
    public int? IndentRight { get; init; }

    /// <summary>The first line indent relative to <see cref="IndentLeft"/>, negative for a hanging indent; null inherits it.</summary>
    public int? IndentFirstLine { get; init; }

    /// <summary>The space above the paragraph; null inherits it.</summary>
    public int? SpacingBefore { get; init; }

    /// <summary>The space below the paragraph; null inherits it.</summary>
    public int? SpacingAfter { get; init; }

    /// <summary>The line spacing; null inherits it.</summary>
    public LineSpacing? Line { get; init; }

    /// <summary>Kept on the same page as the next paragraph; null inherits it.</summary>
    public bool? KeepWithNext { get; init; }

    /// <summary>All lines kept on one page; null inherits it.</summary>
    public bool? KeepLines { get; init; }

    /// <summary>Starts on a new page; null inherits it.</summary>
    public bool? PageBreakBefore { get; init; }

    /// <summary>Prevents single lines at the top or bottom of a page; null inherits it.</summary>
    public bool? WidowControl { get; init; }

    /// <summary>The outline level, 0 to 8 (9 is body text); null inherits it.</summary>
    public int? OutlineLevel { get; init; }

    /// <summary>The list the paragraph belongs to; null inherits it.</summary>
    public NumberingRef? Numbering { get; init; }

    /// <summary>The custom tab stops; null inherits them.</summary>
    public IReadOnlyList<TabStop> Tabs { get; init; }

    /// <summary>The shading fill colour (<c>#RRGGBB</c>, <c>auto</c> or <c>theme:&lt;name&gt;</c>); null inherits it.</summary>
    public string Shading { get; init; }

    /// <summary>Combines this format with a basis: the own non-null values win, the others come from the basis.</summary>
    /// <param name="basis">The inherited format, or null.</param>
    /// <returns>The combined format.</returns>
    public ParagraphFormat Over(ParagraphFormat basis) => basis == null
        ? this
        : new ParagraphFormat
        {
            StyleId = StyleId ?? basis.StyleId,
            Alignment = Alignment ?? basis.Alignment,
            IndentLeft = IndentLeft ?? basis.IndentLeft,
            IndentRight = IndentRight ?? basis.IndentRight,
            IndentFirstLine = IndentFirstLine ?? basis.IndentFirstLine,
            SpacingBefore = SpacingBefore ?? basis.SpacingBefore,
            SpacingAfter = SpacingAfter ?? basis.SpacingAfter,
            Line = Line ?? basis.Line,
            KeepWithNext = KeepWithNext ?? basis.KeepWithNext,
            KeepLines = KeepLines ?? basis.KeepLines,
            PageBreakBefore = PageBreakBefore ?? basis.PageBreakBefore,
            WidowControl = WidowControl ?? basis.WidowControl,
            OutlineLevel = OutlineLevel ?? basis.OutlineLevel,
            Numbering = Numbering ?? basis.Numbering,
            Tabs = Tabs ?? basis.Tabs,
            Shading = Shading ?? basis.Shading
        };
}
