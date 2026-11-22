namespace MudBlazor.Extensions.Office.Document;

/// <summary>
/// Replaces a range with text. Paragraphs in the range merge and keep the first paragraph's formatting; blocks fully inside the range are removed;
/// a table only partly covered keeps its structure and only the text of the covered cells is cleared. Markers in the range move to its start,
/// proofing marks are dropped. In the new text <c>\n</c> splits the paragraph, <c>\t</c> becomes a tab and <c>\v</c> a line break.
/// </summary>
public sealed class ReplaceTextCommand : TextDocumentCommand
{
    private readonly DocumentRange _range;
    private readonly string _text;
    private readonly RunFormat _format;

    /// <summary>Creates the command.</summary>
    /// <param name="range">The range to replace, in either direction; it must not span the body and a header or footer.</param>
    /// <param name="text">The new text; null or empty deletes the range.</param>
    /// <param name="format">
    /// The format of the new text; null for the format of the character before the start, or at the start of a paragraph the character after it.
    /// </param>
    public ReplaceTextCommand(DocumentRange range, string text, RunFormat format = null)
    {
        _range = range;
        _text = text ?? "";
        _format = format;
    }

    /// <inheritdoc />
    public override string Description => _text.Length == 0 ? "Delete" : "Replace text";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var (paragraph, offset) = document.Resolve(document.Snap(_range).Start);
        var (format, link, source) = InlineEdit.InsertionStyle(paragraph, offset);
        if (_format != null)
        {
            var origin = InlineEdit.At(paragraph, offset);
            format = InlineEdit.Unlinked(_format, link, origin);
            source = InlineEdit.RunOf(origin) ?? source;
        }

        return document.ReplaceRange(_range, InlineEdit.Lines(_text, format, link, source));
    }
}

/// <summary>Inserts a line, page or column break.</summary>
/// <param name="at">Where to insert it.</param>
/// <param name="kind">The kind of break.</param>
public sealed class InsertBreakCommand(DocumentPosition at, BreakKind kind) : TextDocumentCommand
{
    /// <inheritdoc />
    public override string Description => "Insert break";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var position = document.Snap(new DocumentRange(at, at)).Start;
        var (paragraph, offset) = document.Resolve(position);
        var (format, link, source) = InlineEdit.InsertionStyle(paragraph, offset);
        return document.ReplaceRange(new DocumentRange(position, position), [[new Break(kind) { Format = format, Link = link, Source = source }]]);
    }
}

/// <summary>Links a range to a target or removes the links in it.</summary>
public sealed class SetHyperlinkCommand : TextDocumentCommand
{
    private readonly DocumentRange _range;
    private readonly Hyperlink _link;

    /// <summary>Creates the command.</summary>
    /// <param name="range">The range, in either direction.</param>
    /// <param name="target">An <c>http</c>, <c>https</c> or <c>mailto</c> URL or a <c>#bookmark</c>; null removes the links.</param>
    /// <exception cref="ArgumentException">The target is of another kind, for example <c>javascript:</c> or <c>file:</c>.</exception>
    public SetHyperlinkCommand(DocumentRange range, string target)
    {
        if (target != null && new Hyperlink(target, null, null).SafeTarget == null)
            throw new ArgumentException($"'{target}' is not an http, https, mailto or #bookmark link.", nameof(target));
        _range = range;
        _link = target == null ? null : target.StartsWith('#') ? new Hyperlink(null, target[1..], null) : new Hyperlink(target, null, null);
    }

    /// <inheritdoc />
    public override string Description => _link == null ? "Remove link" : "Insert link";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document) =>
        document.MapInlines(_range, true, inline => ReferenceEquals(inline.Link, _link) ? inline : inline with { Link = _link });
}
