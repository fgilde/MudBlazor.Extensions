namespace MudBlazor.Extensions.Office.Document;

/// <summary>
/// Splits a paragraph at a position. The second half gets the same formatting, or the style's next style when the split is at the end (no style for
/// the default style), and takes over the end of a section. It continues a list only from a non-empty paragraph. On an empty list paragraph it
/// removes the numbering instead.
/// </summary>
/// <param name="at">Where to split.</param>
public sealed class SplitParagraphCommand(DocumentPosition at) : TextDocumentCommand
{
    /// <inheritdoc />
    public override string Description => "Split paragraph";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var (paragraph, offset) = document.Resolve(at);
        if (paragraph.Length == 0 && Lists.Of(document, paragraph) != null)
            return (document.MapParagraphs([paragraph.Id], p => Lists.Unnumbered(document, p)), d => Caret(d, paragraph.Id, 0));

        var style = document.Styles.Find(paragraph.Format.StyleId) ?? document.Styles.DefaultParagraphStyle;
        var nextStyle = offset == paragraph.Length && style?.Next is { } id && id != style.Id ? id : null;
        Func<Paragraph, Paragraph> next = nextStyle == null && paragraph.Length > 0
            ? null
            : p => p with
            {
                Format = p.Format with
                {
                    StyleId = nextStyle == null ? p.Format.StyleId : nextStyle == document.Styles.DefaultParagraphStyle?.Id ? null : nextStyle,
                    Numbering = paragraph.Length > 0 || p.Format.Numbering is { NumId: 0 } ? p.Format.Numbering : null
                }
            };
        return document.ReplaceRange(new DocumentRange(at, at), [[], []], next);
    }
}

/// <summary>
/// Merges a paragraph with the following paragraph of the same container, keeping the first paragraph's formatting. If the paragraph ends a
/// section, that section is removed and the following section's setup applies. Nothing happens if the next block is not a paragraph.
/// </summary>
/// <param name="paragraphId">The id of the first paragraph.</param>
public sealed class MergeParagraphsCommand(string paragraphId) : TextDocumentCommand
{
    /// <inheritdoc />
    public override string Description => "Merge paragraphs";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        if (document.FindBlock(paragraphId) is not Paragraph paragraph) throw new ArgumentException($"{paragraphId} is not a paragraph of the document.", nameof(paragraphId));
        var end = document.PositionOf(paragraphId, paragraph.Length);
        return document.NextSibling(paragraphId) is Paragraph next
            ? document.ReplaceRange(new DocumentRange(end, document.PositionOf(next.Id, 0)), [[]])
            : ([], _ => new DocumentRange(end, end));
    }
}

internal sealed class RemoveEmptyParagraphCommand(string paragraphId, string caretParagraphId) : TextDocumentCommand
{
    public override string Description => "Delete";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document) =>
        document.FindBlock(paragraphId) is Paragraph { Inlines.Count: 0 }
            ? (document.EditBlock(paragraphId, []), d => Caret(d, caretParagraphId, 0))
            : throw new ArgumentException($"{paragraphId} is not an empty paragraph of the document.", nameof(paragraphId));
}

/// <summary>Replaces the inlines of a paragraph; the typing primitive. Inline instances passed back unchanged keep their origin in the file.</summary>
public sealed class ReplaceParagraphContentCommand : TextDocumentCommand
{
    private readonly string _paragraphId;
    private readonly IReadOnlyList<Inline> _inlines;

    /// <summary>Creates the command.</summary>
    /// <param name="paragraphId">The id of the paragraph.</param>
    /// <param name="inlines">The new inlines.</param>
    public ReplaceParagraphContentCommand(string paragraphId, IReadOnlyList<Inline> inlines)
    {
        _paragraphId = paragraphId;
        _inlines = inlines?.ToArray() ?? throw new ArgumentNullException(nameof(inlines));
    }

    /// <inheritdoc />
    public override string Description => Typing ? "Typing" : "Edit paragraph";

    internal bool Typing { get; init; }

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document) =>
        (document.MapParagraphs([_paragraphId], p => TextDocument.SameItems(p.Inlines, _inlines) ? p : p with { Inlines = _inlines }), null);
}
