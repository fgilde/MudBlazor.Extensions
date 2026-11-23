namespace MudBlazor.Extensions.Office.Document;

/// <summary>Changes the character formatting of a range; runs are split at the range edges and both parts keep their origin in the file.</summary>
/// <param name="range">The range, in either direction.</param>
/// <param name="change">Gets the new format of an inline from its own format.</param>
/// <param name="description">The description of the undo step, for example <c>Bold</c>.</param>
public sealed class SetRunFormatCommand(DocumentRange range, Func<RunFormat, RunFormat> change, string description) : TextDocumentCommand
{
    private readonly Func<RunFormat, RunFormat> _change = change ?? throw new ArgumentNullException(nameof(change));

    /// <inheritdoc />
    public override string Description => description;

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document) =>
        document.MapInlines(range, false, inline => _change(inline.Format) is var format && format == inline.Format ? inline : inline with { Format = format });
}

/// <summary>Changes the formatting of paragraphs.</summary>
/// <param name="paragraphIds">The ids of the paragraphs, in the body, in table cells or in headers and footers.</param>
/// <param name="change">Gets the new format of a paragraph from its own format.</param>
/// <param name="description">The description of the undo step, for example <c>Center</c>.</param>
public sealed class SetParagraphFormatCommand(IReadOnlyList<string> paragraphIds, Func<ParagraphFormat, ParagraphFormat> change, string description) : TextDocumentCommand
{
    private readonly IReadOnlyList<string> _paragraphIds = paragraphIds?.ToArray() ?? throw new ArgumentNullException(nameof(paragraphIds));
    private readonly Func<ParagraphFormat, ParagraphFormat> _change = change ?? throw new ArgumentNullException(nameof(change));

    /// <inheritdoc />
    public override string Description => description;

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document) =>
        (document.MapParagraphs(_paragraphIds, p => _change(p.Format) is var format && format == p.Format ? p : p with { Format = format }), null);
}

internal sealed class SetMarkFormatCommand(IReadOnlyList<string> paragraphIds, Func<RunFormat, RunFormat> change, string description) : TextDocumentCommand
{
    private readonly IReadOnlyList<string> _paragraphIds = paragraphIds?.ToArray() ?? throw new ArgumentNullException(nameof(paragraphIds));

    public override string Description => description;

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document) =>
        (document.MapParagraphs(_paragraphIds, p => change(p.MarkFormat ?? new RunFormat()) is var format && format != (p.MarkFormat ?? new RunFormat()) ? p with { MarkFormat = format } : p), null);
}
