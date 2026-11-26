using System.Globalization;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

internal static class DocumentEditActions
{
    private const long EmuPerPixel = 9525;
    private const int MaxPixels = 20000;
    private const int TwipsPerPixel = 15;
    private const int MinColumnTwips = 300;

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
            "imageSize" => ImageSize(document, range, text),
            "columnWidths" => ColumnWidths(document, text),
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

    private static IDocumentCommand<TextDocument> ImageSize(TextDocument document, DocumentRange range, string text)
    {
        var parts = (text ?? "").Split(',');
        if (range.IsEmpty || parts.Length != 2 || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var width)
            || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var height) || !double.IsFinite(width) || !double.IsFinite(height))
            return null;
        var start = range.Normalize(document).Start;
        var (paragraph, offset) = document.Resolve(start);
        return InlineEdit.At(paragraph, offset) is InlineImage
            ? new ResizeImageCommand(start, (long)Math.Clamp(Math.Round(width), 1, MaxPixels) * EmuPerPixel, (long)Math.Clamp(Math.Round(height), 1, MaxPixels) * EmuPerPixel)
            : null;
    }

    private static IDocumentCommand<TextDocument> ColumnWidths(TextDocument document, string text)
    {
        var parts = (text ?? "").Split(';');
        if (parts.Length < 2 || document.FindBlock(parts[0]) is not Table table || table.Grid.Count != parts.Length - 1)
            return null;
        var widths = new List<int>();
        foreach (var part in parts.Skip(1))
        {
            if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var pixels) || !double.IsFinite(pixels))
                return null;
            widths.Add((int)Math.Clamp(Math.Round(pixels * TwipsPerPixel), MinColumnTwips, DocumentCss.MaxTwips));
        }

        return new SetTableColumnWidthsCommand(table.Id, widths);
    }

    private static List<string> Ids(TextDocument document, DocumentRange range) => document.ParagraphsIn(range).Select(p => p.Id).ToList();
}
