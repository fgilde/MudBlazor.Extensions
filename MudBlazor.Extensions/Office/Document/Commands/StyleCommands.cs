namespace MudBlazor.Extensions.Office.Document;

/// <summary>
/// Applies a paragraph style; only the style id of the paragraphs changes, the default paragraph style is stored as no style. A built-in style the file
/// does not define yet is added together with the styles it is based on and its linked character style.
/// </summary>
/// <param name="paragraphIds">The ids of the paragraphs, in the body, in table cells or in headers and footers.</param>
/// <param name="styleId">The id of a paragraph style of the file or of a built-in paragraph style.</param>
public sealed class ApplyParagraphStyleCommand(IReadOnlyList<string> paragraphIds, string styleId) : TextDocumentCommand
{
    private readonly IReadOnlyList<string> _paragraphIds = paragraphIds?.ToArray() ?? throw new ArgumentNullException(nameof(paragraphIds));

    /// <inheritdoc />
    public override string Description => "Apply style";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var styles = StyleDefinitions.With(document.Styles, styleId, StyleType.Paragraph);
        var id = StyleDefinitions.Reference(styles, styleId);
        var edits = document.MapParagraphs(_paragraphIds, p => p.Format.StyleId == id ? p : p with { Format = p.Format with { StyleId = id } });
        return (document.WithDefinitions(edits, styles, null), null);
    }
}

/// <summary>Applies a character style to a range or removes the character style from it. A built-in style the file does not define yet is added.</summary>
/// <param name="range">The range, in either direction.</param>
/// <param name="styleId">The id of a character style of the file or of a built-in character style; null removes the character style.</param>
public sealed class ApplyCharacterStyleCommand(DocumentRange range, string styleId) : TextDocumentCommand
{
    /// <inheritdoc />
    public override string Description => styleId == null ? "Clear character style" : "Apply style";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var styles = styleId == null ? null : StyleDefinitions.With(document.Styles, styleId, StyleType.Character);
        var (edits, after) = document.MapInlines(range, false, i => i.Format.StyleId == styleId ? i : i with { Format = i.Format with { StyleId = styleId } });
        return (document.WithDefinitions(edits, styles, null), after);
    }
}

/// <summary>
/// Updates a paragraph style to match a paragraph: the paragraph's own formatting (without style and list) and the own formatting of its first text run
/// move into the style. The paragraph gets the style and loses that formatting; other paragraphs keep theirs.
/// </summary>
/// <param name="styleId">The id of a paragraph style of the file or of a built-in paragraph style.</param>
/// <param name="paragraphId">The id of the paragraph.</param>
public sealed class UpdateStyleFromSelectionCommand(string styleId, string paragraphId) : TextDocumentCommand
{
    /// <inheritdoc />
    public override string Description => "Update style";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        if (paragraphId == null || document.FindBlock(paragraphId) is not Paragraph paragraph)
            throw new ArgumentException($"{paragraphId} is not a paragraph of the document.", nameof(paragraphId));
        var styles = StyleDefinitions.With(document.Styles, styleId, StyleType.Paragraph);
        var style = styles.Find(styleId);
        var moved = (paragraph.Inlines.OfType<TextRun>().FirstOrDefault()?.Format ?? new RunFormat()) with { StyleId = null };
        var updated = style with
        {
            ParagraphFormat = (paragraph.Format with { StyleId = null, Numbering = null }).Over(style.ParagraphFormat),
            RunFormat = moved.Over(style.RunFormat)
        };
        if (updated != style) styles = new DocumentStyles(styles.All.Select(s => ReferenceEquals(s, style) ? updated : s), styles.DefaultRunFormat, styles.DefaultParagraphFormat);

        var format = new ParagraphFormat { StyleId = StyleDefinitions.Reference(styles, styleId), Numbering = paragraph.Format.Numbering };
        var inlines = paragraph.Inlines.Select(i => i is TextRun or Tab or Break && StyleDefinitions.Without(i.Format, moved) is var f && f != i.Format ? i with { Format = f } : i).ToList();
        var edits = document.MapParagraphs([paragraphId],
            p => format == p.Format && TextDocument.SameItems(inlines, p.Inlines) ? p : p with { Format = format, Inlines = InlineEdit.Normalize(inlines) });
        return (document.WithDefinitions(edits, styles, null, true), null);
    }
}

/// <summary>
/// Sets the document default font and size (<c>w:docDefaults/w:rPrDefault</c>); a theme font is replaced by the named font unless the theme font
/// already resolves to it.
/// </summary>
public sealed class SetDefaultFontCommand : TextDocumentCommand
{
    private readonly string _font;
    private readonly double _sizePt;

    /// <summary>Creates the command.</summary>
    /// <param name="font">The typeface name, for example <c>Arial</c>.</param>
    /// <param name="sizePt">The size in points.</param>
    /// <exception cref="ArgumentException">The font is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The size is not positive.</exception>
    public SetDefaultFontCommand(string font, double sizePt)
    {
        if (string.IsNullOrWhiteSpace(font)) throw new ArgumentException("A font name is required.", nameof(font));
        if (!(sizePt > 0)) throw new ArgumentOutOfRangeException(nameof(sizePt), sizePt, "The size must be positive.");
        (_font, _sizePt) = (font, sizePt);
    }

    /// <inheritdoc />
    public override string Description => "Default font";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var styles = document.Styles;
        var current = styles.DefaultRunFormat.Font;
        var run = styles.DefaultRunFormat with { Font = current != null && document.Theme.Font(current) == _font ? current : _font, SizePt = _sizePt };
        return (document.WithDefinitions([], run == styles.DefaultRunFormat ? null : new DocumentStyles(styles.All, run, styles.DefaultParagraphFormat), null, true), null);
    }
}

internal static class StyleDefinitions
{
    internal static DocumentStyles With(DocumentStyles styles, string styleId, StyleType type)
    {
        var style = styles.Find(styleId) ?? BuiltInStyles.All.FirstOrDefault(s => s.Id == styleId) ?? throw new ArgumentException($"There is no style {styleId}.", nameof(styleId));
        if (style.Type != type) throw new ArgumentException($"{styleId} is not a {type.ToString().ToLowerInvariant()} style.", nameof(styleId));
        var added = new List<DocumentStyle>();
        Add(styleId);
        return added.Count == 0 ? styles : new DocumentStyles([.. styles.All, .. added], styles.DefaultRunFormat, styles.DefaultParagraphFormat);

        void Add(string id)
        {
            if (styles.Find(id) != null || added.Any(s => s.Id == id) || BuiltInStyles.All.FirstOrDefault(s => s.Id == id) is not { } builtIn) return;
            added.Add(builtIn with { BuiltIn = false, IsDefault = builtIn.IsDefault && !styles.All.Any(s => s.IsDefault && s.Type == builtIn.Type) });
            Add(builtIn.BasedOn);
            Add(builtIn.Link);
        }
    }

    internal static string Reference(DocumentStyles styles, string styleId) => styleId == styles.DefaultParagraphStyle?.Id ? null : styleId;

    internal static RunFormat Without(RunFormat format, RunFormat moved) => new()
    {
        Bold = format.Bold == moved.Bold ? null : format.Bold,
        Italic = format.Italic == moved.Italic ? null : format.Italic,
        Strike = format.Strike == moved.Strike ? null : format.Strike,
        DoubleStrike = format.DoubleStrike == moved.DoubleStrike ? null : format.DoubleStrike,
        Caps = format.Caps == moved.Caps ? null : format.Caps,
        SmallCaps = format.SmallCaps == moved.SmallCaps ? null : format.SmallCaps,
        Hidden = format.Hidden == moved.Hidden ? null : format.Hidden,
        Underline = format.Underline == moved.Underline ? null : format.Underline,
        SizePt = format.SizePt == moved.SizePt ? null : format.SizePt,
        Font = format.Font == moved.Font ? null : format.Font,
        Color = format.Color == moved.Color ? null : format.Color,
        Highlight = format.Highlight == moved.Highlight ? null : format.Highlight,
        Shading = format.Shading == moved.Shading ? null : format.Shading,
        VerticalAlign = format.VerticalAlign == moved.VerticalAlign ? null : format.VerticalAlign,
        StyleId = format.StyleId
    };
}
