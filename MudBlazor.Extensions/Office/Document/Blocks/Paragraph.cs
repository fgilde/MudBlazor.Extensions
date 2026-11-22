namespace MudBlazor.Extensions.Office.Document;

/// <summary>A paragraph (<c>w:p</c>).</summary>
public sealed record Paragraph : Block
{
    /// <summary>The paragraph's own formatting; properties not set are inherited from the style.</summary>
    public ParagraphFormat Format { get; init; } = new();

    /// <summary>Formatting of the paragraph mark (<c>w:pPr/w:rPr</c>), used for the caret in an empty paragraph; null if none.</summary>
    public RunFormat MarkFormat { get; init; }

    /// <summary>The inlines in order.</summary>
    public IReadOnlyList<Inline> Inlines { get; init; } = [];

    /// <summary>
    /// The text as the editor counts it: run text, <c>\t</c> for a tab, <c>\v</c> for a line break, <c>\f</c> for a page or column break,
    /// <c>￼</c> for an image or other token and nothing for a marker.
    /// </summary>
    public string Text => string.Concat(Inlines.Select(TextOf));

    /// <summary>The length of <see cref="Text"/>.</summary>
    public int Length => Inlines.Sum(i => i.Length);

    private static string TextOf(Inline inline) => inline switch
    {
        TextRun run => run.Text,
        Tab => "\t",
        Break { Kind: BreakKind.Line } => "\v",
        Break => "\f",
        Marker => "",
        _ => "￼"
    };
}
