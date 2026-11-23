using System.Text;

namespace MudBlazor.Extensions.Office.Document;

/// <summary>The typed state of one paragraph in a typing batch, see <see cref="TextDocument.ApplyTyping(IReadOnlyList{TypedParagraph}, DateTimeOffset)"/>.</summary>
/// <param name="ParagraphId">The id of the paragraph.</param>
/// <param name="Text">The full new <see cref="Paragraph.Text"/> of the paragraph.</param>
/// <param name="Caret">The caret offset in <paramref name="Text"/> when the caret is in this paragraph, otherwise null.</param>
/// <param name="Format">
/// The own format of the inserted text (a pending caret format), or null for the format of the text around it. Only the span the diff finds changed
/// gets it: text that is unchanged keeps its format, even if the user retyped identical characters over a selection.
/// </param>
public sealed record TypedParagraph(string ParagraphId, string Text, int? Caret = null, RunFormat Format = null);

internal sealed class TypeTextCommand : TextDocumentCommand
{
    private readonly IReadOnlyList<TypedParagraph> _paragraphs;

    internal TypeTextCommand(IReadOnlyList<TypedParagraph> paragraphs)
    {
        ArgumentNullException.ThrowIfNull(paragraphs);
        _paragraphs = paragraphs.Select(p => p?.Text == null ? throw new ArgumentNullException(nameof(paragraphs)) : p).ToArray();
        if (_paragraphs.Select(p => p.ParagraphId).Distinct().Count() < _paragraphs.Count)
            throw new ArgumentException("A paragraph appears more than once in the batch.", nameof(paragraphs));
    }

    public override string Description => "Typing";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var caret = _paragraphs.LastOrDefault(p => p.Caret != null);
        var dropped = 0;
        var entries = _paragraphs.Where(p => p.ParagraphId != null).ToDictionary(p => p.ParagraphId);
        var edits = document.MapParagraphs(_paragraphs.Select(p => p.ParagraphId), paragraph =>
        {
            var entry = entries[paragraph.Id];
            var (typed, removed) = Type(paragraph, entry);
            if (ReferenceEquals(entry, caret)) dropped = removed;
            return typed;
        });
        if (Before.Start == null && _paragraphs.Count > 0)
        {
            var first = _paragraphs[0];
            var start = TextDiff.Compute(((Paragraph)document.FindBlock(first.ParagraphId)).Text, first.Text, first.Caret).Start;
            Before = Caret(document, first.ParagraphId, start);
        }

        return (edits, caret == null ? null : d => Caret(d, caret.ParagraphId, CaretOffset(d, caret.ParagraphId, caret.Caret.Value - dropped)));
    }

    private static (Paragraph Paragraph, int DroppedBeforeCaret) Type(Paragraph paragraph, TypedParagraph entry)
    {
        var (start, oldEnd, newEnd) = TextDiff.Compute(paragraph.Text, entry.Text, entry.Caret);
        var (format, link, source) = InlineEdit.InsertionStyle(paragraph, start);
        if (entry.Format != null) format = InlineEdit.Unlinked(entry.Format, link, InlineEdit.At(paragraph, start));
        var (left, middle, right) = InlineEdit.Split(paragraph.Inlines, start, oldEnd);
        var objects = middle.Where(i => i is not (TextRun or Marker)).ToList();
        var inserted = new List<Inline>();
        var text = new StringBuilder();
        var dropped = 0;
        for (var k = start; k < newEnd; k++)
        {
            var c = entry.Text[k];
            var reused = objects.FindIndex(i => Paragraph.TextOf(i)[0] == c);
            if (reused >= 0)
            {
                Flush();
                inserted.Add(objects[reused]);
                objects.RemoveAt(reused);
            }
            else if (c is '\r' or '\n' or '\uFFFC')
                dropped += k < entry.Caret ? 1 : 0;
            else
                text.Append(c);
        }

        Flush();
        if (start == oldEnd && inserted.Count == 0) return (paragraph, dropped);
        var markers = middle.Where(i => i is Marker { Kind: not "proofErr" });
        return (paragraph with { Inlines = InlineEdit.Normalize([.. left, .. markers, .. inserted, .. right]) }, dropped);

        void Flush()
        {
            inserted.AddRange(InlineEdit.Lines(text.ToString(), format, link, source)[0]);
            text.Clear();
        }
    }

    private static int CaretOffset(TextDocument document, string paragraphId, int offset)
    {
        var text = ((Paragraph)document.FindBlock(paragraphId)).Text;
        return TextDocument.SnapOffset(text, Math.Clamp(offset, 0, text.Length), true);
    }
}
