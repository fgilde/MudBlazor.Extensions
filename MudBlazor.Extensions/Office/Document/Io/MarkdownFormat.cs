using System.Text;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MudBlazor.Extensions.Office.Document;

internal static class MarkdownFormat
{
    private const string Escaped = "\\*_[]#|<>`~&";
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UsePipeTables().UseEmphasisExtras().UseAutoLinks().UseYamlFrontMatter().Build();

    internal static TextDocument Read(Stream stream)
    {
        var text = TextFormat.Decode(stream);
        string html, title;
        try
        {
            var markdown = Markdig.Markdown.Parse(text, Pipeline);
            title = Title(markdown.Descendants<YamlFrontMatterBlock>().FirstOrDefault());
            foreach (var image in markdown.Descendants<LinkInline>().Where(l => l.IsImage && l.Url?.TrimStart().StartsWith("data:", StringComparison.OrdinalIgnoreCase) != true).ToList())
                image.ReplaceBy(new LiteralInline(string.Concat(image.Descendants<LiteralInline>().Select(l => l.Content.ToString()))), false);
            html = Markdig.Markdown.ToHtml(markdown, Pipeline);
        }
        catch (ArgumentException)
        {
            return TextFormat.FromText(text);
        }

        var document = HtmlReader.Read(html);
        if (title != null) document.Properties = document.Properties with { Title = title };
        return document;
    }

    internal static void Write(TextDocument document, Stream target) => target.Write(new UTF8Encoding(false).GetBytes(new Writer(document).Run()));

    internal static string Escape(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (Escaped.Contains(c)) sb.Append('\\');
            sb.Append(c);
        }

        return sb.ToString();
    }

    internal static string Url(string url)
    {
        var sb = new StringBuilder(url.Length);
        foreach (var c in url)
            if (c is '\\' or '(' or ')') sb.Append('\\').Append(c);
            else if (c <= ' ' || c is '<' or '>') sb.Append('%').Append(((int)c).ToString("X2"));
            else sb.Append(c);
        return sb.ToString();
    }

    private static string Title(YamlFrontMatterBlock frontMatter)
    {
        if (frontMatter == null) return null;
        var lines = frontMatter.Lines;
        for (var i = 0; i < lines.Count; i++)
        {
            var text = lines.Lines[i].Slice.ToString();
            if (!text.StartsWith("title:", StringComparison.Ordinal)) continue;
            var title = HtmlPasteReader.Clean(text[6..].Trim().Trim('"', '\''));
            return title.Length == 0 ? null : title.Length > 1024 ? title[..1024] : title;
        }

        return null;
    }

    private static string LineStart(string line)
    {
        if (line.Length > 0 && line[0] is '+' or '-' or '=') return "\\" + line;
        var digits = 0;
        while (digits < line.Length && char.IsAsciiDigit(line[digits])) digits++;
        return digits is > 0 and < 10 && digits < line.Length && line[digits] is '.' or ')' ? line.Insert(digits, "\\") : line;
    }

    private sealed class Writer(TextDocument document)
    {
        private readonly StringBuilder _md = new();
        private readonly MarkdownInline _inline = new(document);
        private readonly List<int> _items = [];
        private bool _item;

        public string Run()
        {
            Blocks(document.Blocks);
            return _md.Length == 0 ? "" : _md.Append('\n').ToString();
        }

        private void Blocks(IEnumerable<Block> blocks)
        {
            foreach (var block in blocks)
                if (block is Paragraph paragraph) Paragraph(paragraph);
                else if (block is Table table) Table(table);
                else if (block is OpaqueBlock { Visible: true, Preview: { } preview }) Blocks(preview);
        }

        private void Start(bool item)
        {
            if (_md.Length > 0) _md.Append(item && _item ? "\n" : "\n\n");
            if (!item) _items.Clear();
            _item = item;
        }

        private void Paragraph(Paragraph paragraph)
        {
            var format = document.EffectiveParagraphFormat(paragraph);
            if (format.OutlineLevel is { } outline and < 9)
            {
                var heading = _inline.Write(paragraph, true).Trim(' ', '\t');
                if (heading.Length == 0) return;
                Start(false);
                _md.Append('#', Math.Min(outline + 1, 6)).Append(' ').Append(heading);
                return;
            }

            var level = format.Numbering is { } numbering ? document.Numbering.Level(numbering) : null;
            if (level == null && HtmlWriter.IsPreformatted(document, paragraph) && (paragraph.Inlines.Any(i => i is Break or Tab) || paragraph.Text.StartsWith(' ') ||
                                                                      paragraph.Text.EndsWith(' ') || paragraph.Text.Contains("  ", StringComparison.Ordinal)))
            {
                Code(paragraph);
                return;
            }

            var lines = Lines(_inline.Write(paragraph, false));
            if (level == null)
            {
                if (lines.Count == 0) return;
                Start(false);
                _md.AppendJoin("\\\n", lines);
                return;
            }

            var depth = Math.Min(format.Numbering!.Value.Level, _item ? _items.Count : 0);
            Start(true);
            _items.RemoveRange(depth, _items.Count - depth);
            var marker = level.Format is NumberFormat.Bullet or NumberFormat.None ? "- " : "1. ";
            var indent = new string(' ', _items.Sum());
            _items.Add(marker.Length);
            _md.Append(indent).Append(lines.Count == 0 ? marker.TrimEnd() : marker).AppendJoin("\\\n" + indent + new string(' ', marker.Length), lines);
        }

        private static List<string> Lines(string text)
        {
            var lines = text.Split('\n').Select(line => LineStart(line.Trim(' ', '\t'))).ToList();
            while (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
            return lines;
        }

        private void Code(Paragraph paragraph)
        {
            var text = string.Concat(paragraph.Inlines.Where(i => !TextFormat.Hidden(document, paragraph, i)).Select(i => i switch
            {
                TextRun run => run.Text,
                Tab => "\t",
                Break => "\n",
                OpaqueInline token => token.DisplayText,
                _ => ""
            }));
            int longest = 0, run = 0;
            foreach (var c in text) longest = Math.Max(longest, run = c == '`' ? run + 1 : 0);
            var fence = new string('`', Math.Max(3, longest + 1));
            Start(false);
            _md.Append(fence).Append('\n').Append(text).Append('\n').Append(fence);
        }

        private void Table(Table table)
        {
            if (table.Rows.Count == 0) return;
            var rows = table.Rows.Select(row => TextFormat.Cells(row, c => Cell(c.Blocks)).ToList()).ToList();
            var columns = rows.Max(r => r.Count);
            Start(false);
            for (var r = 0; r < rows.Count; r++)
            {
                if (r > 0) _md.Append('\n');
                _md.Append("| ").AppendJoin(" | ", rows[r].Concat(Enumerable.Repeat("", columns - rows[r].Count))).Append(" |");
                if (r == 0) _md.Append("\n|").Append(string.Concat(Enumerable.Repeat(" --- |", columns)));
            }
        }

        private string Cell(IEnumerable<Block> blocks) =>
            string.Join(' ', blocks.SelectMany(b => b switch
            {
                Paragraph p => [_inline.Write(p, true).Trim(' ', '\t')],
                Table t => t.Rows.SelectMany(r => r.Cells).Select(c => Cell(c.Blocks)),
                OpaqueBlock { Visible: true, Preview: { } preview } => [Cell(preview)],
                _ => []
            }).Where(s => s.Length > 0));
    }
}
