namespace MudBlazor.Extensions.Office.Document;

public sealed partial class TextDocument
{
    private readonly Dictionary<Paragraph, ParagraphFormat> _paragraphFormats = new(ReferenceEqualityComparer.Instance);
    private (int Version, DocumentStyles Styles, Numbering Numbering, DocumentTheme Theme) _formatKey;
    private IReadOnlyDictionary<string, string> _labels;

    /// <summary>The styles and document defaults.</summary>
    public DocumentStyles Styles { get; internal set; } = new([], null, null);

    /// <summary>The list definitions.</summary>
    public Numbering Numbering { get; internal set; } = new([], []);

    internal DocumentTheme Theme { get; set; } = DocumentTheme.Default;

    /// <summary>
    /// Resolves the formatting of a paragraph from the document defaults, the table style, the list level, the paragraph style chain and the
    /// paragraph's own formatting. Theme colours are resolved to <c>#RRGGBB</c>; inherited tab stops are merged.
    /// </summary>
    /// <param name="paragraph">A paragraph of the document.</param>
    /// <returns>The effective formatting.</returns>
    /// <exception cref="ArgumentNullException">The paragraph is null.</exception>
    public ParagraphFormat EffectiveParagraphFormat(Paragraph paragraph)
    {
        ArgumentNullException.ThrowIfNull(paragraph);
        RefreshFormats();
        if (!_paragraphFormats.TryGetValue(paragraph, out var format))
            _paragraphFormats[paragraph] = format = EffectiveFormat.Paragraph(Styles, Numbering, Theme, TableOf(paragraph), paragraph);
        return format;
    }

    /// <summary>
    /// Resolves the character formatting of an inline from the document defaults, the table style, the paragraph style chain, the character style
    /// chain and its own formatting. Bold, italic, caps, small caps, strike, double strike and hidden toggle across style levels; theme fonts and
    /// colours are resolved.
    /// </summary>
    /// <param name="paragraph">The paragraph that contains the inline.</param>
    /// <param name="inline">The inline, or null for the paragraph mark.</param>
    /// <returns>The effective formatting.</returns>
    /// <exception cref="ArgumentNullException">The paragraph is null.</exception>
    public RunFormat EffectiveRunFormat(Paragraph paragraph, Inline inline)
    {
        ArgumentNullException.ThrowIfNull(paragraph);
        return EffectiveFormat.Run(Styles, Theme, TableOf(paragraph), paragraph, inline);
    }

    /// <summary>Gets the list label of a paragraph of the body, for example <c>1.</c>, <c>a)</c> or <c>&#x2022;</c>.</summary>
    /// <param name="paragraph">A paragraph of the body or of a table cell.</param>
    /// <returns>The label, or null if the paragraph is not a list item.</returns>
    /// <exception cref="ArgumentNullException">The paragraph is null.</exception>
    public string ListLabel(Paragraph paragraph)
    {
        ArgumentNullException.ThrowIfNull(paragraph);
        RefreshFormats();
        _labels ??= NumberingLabels.Compute(Numbering, AllParagraphs().Select(WithStyleNumbering));
        return paragraph.Id != null && _labels.TryGetValue(paragraph.Id, out var label) ? label : null;
    }

    private Paragraph WithStyleNumbering(Paragraph paragraph) =>
        paragraph.Format.Numbering == null && EffectiveFormat.NumberingOf(Styles, paragraph) is { } numbering
            ? paragraph with { Format = paragraph.Format with { Numbering = numbering } }
            : paragraph;

    private Table TableOf(Paragraph paragraph) =>
        TryLocate(paragraph.Id, out var location) && location.Path.Length > 0
            ? Walk(Container(location.Container)[location.Index], location.Path[..^1]) as Table
            : null;

    private void RefreshFormats()
    {
        var key = (Version, Styles, Numbering, Theme);
        if (key == _formatKey) return;
        _formatKey = key;
        _labels = null;
        _paragraphFormats.Clear();
    }
}
