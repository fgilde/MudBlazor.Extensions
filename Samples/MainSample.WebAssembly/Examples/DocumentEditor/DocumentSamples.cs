using System.Text;
using MudBlazor.Extensions.Office.Document;

namespace MainSample.WebAssembly.Examples.DocumentEditor;

public static class DocumentSamples
{
    private static readonly string[] Words =
        ["document", "editor", "paragraph", "model", "browser", "surface", "format", "style", "table", "list", "heading", "selection", "caret", "undo",
         "page", "section", "font", "color", "text", "word", "line", "save", "open", "ribbon", "zoom", "view", "status", "command", "change", "export"];

    public static TextDocument Showcase()
    {
        var document = TextDocument.Create();
        var title = Add(document, "MudExDocumentEditor");
        var intro = Add(document, "This document was built in code with the commands of the document model and opens in the editor like any Word file.");
        var formattingHeading = Add(document, "Formatted text");
        var formatted = Add(document, "Bold, italic, underlined, colored and highlighted text in one paragraph.");
        var listsHeading = Add(document, "Lists");
        string[] bullets = [Add(document, "Bulleted item"), Add(document, "Another bulleted item"), Add(document, "A third one")];
        string[] steps = [Add(document, "First step"), Add(document, "Second step"), Add(document, "Third step")];
        var tableHeading = Add(document, "Table");
        var before = Add(document, "Quarterly figures:");

        document.Execute(new ApplyParagraphStyleCommand([title], "Title"));
        document.Execute(new ApplyParagraphStyleCommand([formattingHeading, listsHeading, tableHeading], "Heading1"));
        Format(document, intro, "Word file", f => f with { Italic = true });
        Format(document, formatted, "Bold", f => f with { Bold = true });
        Format(document, formatted, "italic", f => f with { Italic = true });
        Format(document, formatted, "underlined", f => f with { Underline = "single" });
        Format(document, formatted, "colored", f => f with { Color = "#2B579A" });
        Format(document, formatted, "highlighted", f => f with { Highlight = "yellow" });
        document.Execute(new ApplyListCommand(bullets, ListTemplate.Bullet));
        document.Execute(new ApplyListCommand(steps, ListTemplate.Decimal));

        document.Execute(new InsertTableCommand(document.PositionOf(before, ((Paragraph)document.FindBlock(before)).Length), 3, 3));
        var table = document.Blocks.OfType<Table>().Single();
        string[][] cells = [["Quarter", "Revenue", "Growth"], ["Q1", "120", "4 %"], ["Q2", "135", "12 %"]];
        for (var r = 0; r < cells.Length; r++)
            for (var c = 0; c < cells[r].Length; c++)
                Type(document, table.Rows[r].Cells[c].Blocks[0].Id, cells[r][c]);
        Type(document, document.Blocks[^1].Id, "Edit anything above, then save the document from the File menu.");

        using var stream = new MemoryStream();
        document.Save(stream);
        stream.Position = 0;
        return TextDocument.Load(stream);
    }

    public static TextDocument Large(int paragraphs = 3000)
    {
        var text = new StringBuilder();
        var random = new Random(7);
        for (var p = 0; p < paragraphs; p++)
        {
            text.Append("Paragraph ").Append(p + 1).Append(':');
            for (var w = 0; w < 60; w++)
                text.Append(' ').Append(Words[random.Next(Words.Length)]);
            text.Append(".\n");
        }

        return TextDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(text.ToString())), DocumentFileFormat.Text);
    }

    private static string Add(TextDocument document, string text)
    {
        var last = (Paragraph)document.Blocks[^1];
        var id = last.Id;
        if (last.Length > 0)
        {
            document.Execute(new SplitParagraphCommand(document.PositionOf(id, last.Length)));
            id = document.Blocks[^1].Id;
        }

        Type(document, id, text);
        return id;
    }

    private static void Type(TextDocument document, string paragraphId, string text)
    {
        var at = document.PositionOf(paragraphId, 0);
        document.Execute(new ReplaceTextCommand(new DocumentRange(at, at), text));
    }

    private static void Format(TextDocument document, string paragraphId, string word, Func<RunFormat, RunFormat> change)
    {
        var start = ((Paragraph)document.FindBlock(paragraphId)).Text.IndexOf(word, StringComparison.Ordinal);
        document.Execute(new SetRunFormatCommand(new DocumentRange(document.PositionOf(paragraphId, start), document.PositionOf(paragraphId, start + word.Length)), change, "Format"));
    }
}
