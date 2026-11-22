using System.Text;
using System.Xml;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Office.Document;

internal static class TextFormat
{
    internal static TextDocument Read(Stream stream) => FromText(Decode(stream));

    internal static string Decode(Stream stream)
    {
        var bytes = HtmlEncoding.ReadLimited(stream, HtmlReader.MaxLength, b => HtmlEncoding.Bom(b)?.Encoding ?? Encoding.UTF8);
        var text = CsvFormat.Decode(new MemoryStream(bytes));
        return text.Length > HtmlReader.MaxLength ? throw new InvalidDataException("The text is too large to read.") : text;
    }

    internal static void Write(TextDocument document, Stream target)
    {
        var text = new StringBuilder();
        Blocks(document, document.Blocks, text);
        if (text.Length >= 2) text.Length -= 2;
        target.Write(new UTF8Encoding(false).GetBytes(text.ToString()));
    }

    internal static bool Hidden(TextDocument document, Paragraph paragraph, Inline inline) => document.EffectiveRunFormat(paragraph, inline).Hidden == true;

    internal static TextDocument FromText(string text)
    {
        var document = TextDocument.Create();
        var paragraphs = InlineEdit.Lines(Clean(text), new RunFormat(), null, null).Select(line => (Block)new Paragraph { Inlines = line }).ToList();
        document.BlockList.Clear();
        document.BlockList.AddRange(paragraphs.Select(document.Identify));
        document.InvalidateIndex();
        return document;
    }

    private static string Clean(string text)
    {
        var sb = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                sb.Append(text, i++, 2);
            else if (text[i] is '\t' or '\f' or '\r' or '\n' || !char.IsControl(text[i]) && !char.IsSurrogate(text[i]) && XmlConvert.IsXmlChar(text[i]))
                sb.Append(text[i]);
        return sb.ToString();
    }

    private static void Blocks(TextDocument document, IEnumerable<Block> blocks, StringBuilder text)
    {
        foreach (var block in blocks)
            switch (block)
            {
                case Paragraph paragraph:
                    if (document.EffectiveParagraphFormat(paragraph).PageBreakBefore == true) text.Append('\f');
                    if (document.ListLabel(paragraph) is { Length: > 0 } label) text.Append(label).Append('\t');
                    text.Append(Line(document, paragraph, false)).Append("\r\n");
                    break;
                case Table table:
                    foreach (var row in table.Rows)
                    {
                        var cells = Enumerable.Repeat("", row.GridBefore)
                            .Concat(row.Cells.SelectMany(c => Enumerable.Repeat("", c.GridSpan - 1).Prepend(c.VMerge == VerticalMerge.Continue ? "" : CellText(document, c.Blocks))));
                        text.Append(string.Join('\t', cells)).Append("\r\n");
                    }

                    break;
                case OpaqueBlock { Visible: true, Preview: { } preview }:
                    Blocks(document, preview, text);
                    break;
            }
    }

    private static string CellText(TextDocument document, IEnumerable<Block> blocks) =>
        string.Join(' ', blocks.SelectMany(b => b switch
        {
            Paragraph p => [Line(document, p, true)],
            Table t => t.Rows.SelectMany(r => r.Cells).Select(c => CellText(document, c.Blocks)),
            OpaqueBlock { Visible: true, Preview: { } preview } => [CellText(document, preview)],
            _ => []
        }).Where(s => s.Length > 0));

    private static string Line(TextDocument document, Paragraph paragraph, bool cell)
    {
        var line = new StringBuilder();
        foreach (var inline in paragraph.Inlines)
        {
            if (Hidden(document, paragraph, inline)) continue;
            line.Append(inline switch
            {
                TextRun run => run.Text,
                Tab => cell ? " " : "\t",
                Break { Kind: BreakKind.Page } => cell ? " " : "\f",
                Break => cell ? " " : "\r\n",
                OpaqueInline token => token.DisplayText,
                _ => ""
            });
        }

        return line.ToString();
    }
}
