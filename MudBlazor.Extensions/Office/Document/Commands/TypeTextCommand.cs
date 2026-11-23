namespace MudBlazor.Extensions.Office.Document;

/// <summary>The typed state of one paragraph in a typing batch, see <see cref="TextDocument.ApplyTyping(IReadOnlyList{TypedParagraph}, DateTimeOffset)"/>.</summary>
/// <param name="ParagraphId">The id of the paragraph.</param>
/// <param name="Text">The full new <see cref="Paragraph.Text"/> of the paragraph.</param>
/// <param name="Caret">The caret offset in <paramref name="Text"/> when the caret is in this paragraph, otherwise null.</param>
/// <param name="Format">The own format of the inserted text (a pending caret format), or null for the format of the text around it.</param>
public sealed record TypedParagraph(string ParagraphId, string Text, int? Caret = null, RunFormat Format = null);

internal sealed class TypeTextCommand : TextDocumentCommand
{
    private readonly IReadOnlyList<TypedParagraph> _paragraphs;

    internal TypeTextCommand(IReadOnlyList<TypedParagraph> paragraphs)
    {
        ArgumentNullException.ThrowIfNull(paragraphs);
        _paragraphs = paragraphs.Select(p => p?.Text == null ? throw new ArgumentNullException(nameof(paragraphs)) : p).ToArray();
    }

    public override string Description => "Typing";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var entries = new Dictionary<string, TypedParagraph>();
        foreach (var entry in _paragraphs) entries[entry.ParagraphId ?? ""] = entry;
        var edits = document.MapParagraphs(_paragraphs.Select(p => p.ParagraphId), paragraph => Type(paragraph, entries[paragraph.Id]));
        var caret = _paragraphs.LastOrDefault(p => p.Caret != null);
        return (edits, caret == null ? null : d => Caret(d, caret.ParagraphId, CaretOffset(d, caret)));
    }

    private static Paragraph Type(Paragraph paragraph, TypedParagraph entry)
    {
        var (start, oldEnd, newEnd) = TextDiff.Compute(paragraph.Text, entry.Text, entry.Caret);
        var inserted = string.Concat(entry.Text[start..newEnd].Where(c => c is not ('\r' or '\n' or '\uFFFC')));
        if (start == oldEnd && inserted.Length == 0) return paragraph;

        var (format, link, source) = InlineEdit.InsertionStyle(paragraph, start);
        if (entry.Format != null) format = InlineEdit.Unlinked(entry.Format, link, InlineEdit.At(paragraph, start));
        var (left, middle, right) = InlineEdit.Split(paragraph.Inlines, start, oldEnd);
        var markers = middle.Where(i => i is Marker { Kind: not "proofErr" });
        return paragraph with { Inlines = InlineEdit.Normalize([.. left, .. markers, .. InlineEdit.Lines(inserted, format, link, source)[0], .. right]) };
    }

    private static int CaretOffset(TextDocument document, TypedParagraph entry)
    {
        var text = ((Paragraph)document.FindBlock(entry.ParagraphId)).Text;
        return TextDocument.SnapOffset(text, Math.Clamp(entry.Caret.Value, 0, text.Length), true);
    }
}
