namespace MudBlazor.Extensions.Office.Document;

/// <summary>A paragraph (<c>w:p</c>).</summary>
public sealed record Paragraph : Block
{
    private readonly IReadOnlyList<Inline> _inlines = [];
    private readonly TextCache _cache = new();

    /// <summary>The paragraph's own formatting; properties not set are inherited from the style.</summary>
    public ParagraphFormat Format { get; init; } = new();

    /// <summary>Formatting of the paragraph mark (<c>w:pPr/w:rPr</c>), used for the caret in an empty paragraph; null if none.</summary>
    public RunFormat MarkFormat { get; init; }

    /// <summary>The inlines in order; null is stored as empty. The list is treated as immutable after assignment: replace the paragraph instead of changing it.</summary>
    public IReadOnlyList<Inline> Inlines
    {
        get => _inlines;
        init
        {
            _inlines = value ?? [];
            _cache = new TextCache();
        }
    }

    /// <summary>
    /// The text as the editor counts it: run text, <c>\t</c> for a tab, <c>\v</c> for a line break, <c>\f</c> for a page or column break,
    /// U+FFFC for an image or other token and nothing for a marker.
    /// </summary>
    public string Text => _cache.Text ??= string.Concat(Inlines.Select(TextOf));

    /// <summary>The length of <see cref="Text"/>.</summary>
    public int Length => _cache.Length ??= Inlines.Sum(i => i.Length);

    private static string TextOf(Inline inline) => inline switch
    {
        TextRun run => run.Text ?? "",
        Tab => "\t",
        Break { Kind: BreakKind.Line } => "\v",
        Break => "\f",
        Marker => "",
        _ => "\uFFFC"
    };

    private sealed class TextCache
    {
        public string Text;

        public int? Length;

        public override bool Equals(object obj) => obj is TextCache;

        public override int GetHashCode() => 0;
    }
}
