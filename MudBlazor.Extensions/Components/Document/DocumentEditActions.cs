using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

internal static class DocumentEditActions
{
    public static IDocumentCommand<TextDocument> For(TextDocument document, string name, DocumentRange range, string text)
    {
        var empty = range.IsEmpty;
        return name switch
        {
            "split" => empty ? new SplitParagraphCommand(range.Start) : new ReplaceTextCommand(range, "\n"),
            "pageBreak" => empty ? new InsertBreakCommand(range.Start, BreakKind.Page) : new ReplaceTextCommand(range, "\f"),
            "deleteBackward" => empty ? Backward(document, range.Start) : new ReplaceTextCommand(range, ""),
            "deleteForward" => empty ? Forward(document, range.Start) : new ReplaceTextCommand(range, ""),
            "replace" => new ReplaceTextCommand(range, text),
            "tab" => Tab(document, range),
            "untab" => new ChangeListLevelCommand(Ids(document, range), -1),
            _ => null
        };
    }

    private static IDocumentCommand<TextDocument> Backward(TextDocument document, DocumentPosition caret)
    {
        var (paragraph, offset) = document.Resolve(caret);
        var text = paragraph.Text;
        if (offset > 0)
            return Delete(caret, offset - (offset > 1 && char.IsLowSurrogate(text[offset - 1]) && char.IsHighSurrogate(text[offset - 2]) ? 2 : 1), offset);
        if (Lists.Of(document, paragraph) != null) return new RemoveListCommand([paragraph.Id]);
        return document.PreviousSibling(paragraph.Id) switch
        {
            Paragraph { Inlines.Count: 0 } empty when document.Sections.All(s => s.LastBlockId != empty.Id) => new RemoveEmptyParagraphCommand(empty.Id, paragraph.Id),
            Paragraph previous => new MergeParagraphsCommand(previous.Id),
            _ => null
        };
    }

    private static IDocumentCommand<TextDocument> Forward(TextDocument document, DocumentPosition caret)
    {
        var (paragraph, offset) = document.Resolve(caret);
        var text = paragraph.Text;
        if (offset < text.Length)
            return Delete(caret, offset, offset + (offset + 1 < text.Length && char.IsHighSurrogate(text[offset]) && char.IsLowSurrogate(text[offset + 1]) ? 2 : 1));
        return document.NextSibling(paragraph.Id) is Paragraph ? new MergeParagraphsCommand(paragraph.Id) : null;
    }

    private static ReplaceTextCommand Delete(DocumentPosition caret, int start, int end) => new(new DocumentRange(caret with { Offset = start }, caret with { Offset = end }), "");

    private static IDocumentCommand<TextDocument> Tab(TextDocument document, DocumentRange range)
    {
        var paragraphs = document.ParagraphsIn(range).ToList();
        var start = range.Normalize(document).Start;
        var indent = paragraphs.Count > 1 || paragraphs.Count == 1 && start.Offset == 0 && Lists.Of(document, paragraphs[0]) != null;
        return indent ? new ChangeListLevelCommand(paragraphs.Select(p => p.Id).ToList(), 1) : new ReplaceTextCommand(range, "\t");
    }

    private static List<string> Ids(TextDocument document, DocumentRange range) => document.ParagraphsIn(range).Select(p => p.Id).ToList();
}
