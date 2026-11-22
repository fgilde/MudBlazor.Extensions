using System.Text;
using Markdig;

namespace MudBlazor.Extensions.Office.Document;

internal static class MarkdownFormat
{
    private const string Escaped = "\\*_[]#|<>`~&";
    private static readonly char[] Space = [' ', '\t', '\n'];
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UsePipeTables().UseEmphasisExtras().UseAutoLinks().Build();

    internal static TextDocument Read(Stream stream)
    {
        var text = TextFormat.Decode(stream);
        string html;
        try
        {
            html = Markdig.Markdown.ToHtml(text, Pipeline);
        }
        catch (ArgumentException)
        {
            return TextFormat.FromText(text);
        }

        return HtmlReader.Read(html);
    }

    internal static void Write(TextDocument document, Stream target) => target.Write(new UTF8Encoding(false).GetBytes(new Writer(document).Run()));

    private static string Escape(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (Escaped.Contains(c)) sb.Append('\\');
            sb.Append(c);
        }

        return sb.ToString();
    }

    private static string LineStart(string line)
    {
        if (line.Length > 0 && line[0] is '+' or '-' or '=') return "\\" + line;
        var digits = 0;
        while (digits < line.Length && char.IsAsciiDigit(line[digits])) digits++;
        return digits is > 0 and < 10 && digits < line.Length && line[digits] is '.' or ')' ? line.Insert(digits, "\\") : line;
    }

    private static string Url(string url)
    {
        var sb = new StringBuilder(url.Length);
        foreach (var c in url)
            if (c is '\\' or '(' or ')') sb.Append('\\').Append(c);
            else if (c <= ' ' || c is '<' or '>') sb.Append('%').Append(((int)c).ToString("X2"));
            else sb.Append(c);
        return sb.ToString();
    }

    private sealed class Writer(TextDocument document)
    {
        private readonly StringBuilder _md = new();
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
                var heading = Inline(paragraph, true).Trim(' ', '\t');
                if (heading.Length == 0) return;
                Start(false);
                _md.Append('#', Math.Min(outline + 1, 6)).Append(' ').Append(heading);
                return;
            }

            var level = format.Numbering is { } numbering ? document.Numbering.Level(numbering) : null;
            if (level == null && HtmlWriter.IsPreformatted(document, paragraph))
            {
                Code(paragraph);
                return;
            }

            var lines = Lines(Inline(paragraph, false));
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
                Paragraph p => [Inline(p, true).Trim(' ', '\t')],
                Table t => t.Rows.SelectMany(r => r.Cells).Select(c => Cell(c.Blocks)),
                OpaqueBlock { Visible: true, Preview: { } preview } => [Cell(preview)],
                _ => []
            }).Where(s => s.Length > 0));

        private string Inline(Paragraph paragraph, bool single)
        {
            var baseline = document.EffectiveRunFormat(new Paragraph { Format = paragraph.Format }, null);
            var sb = new StringBuilder();
            var open = new List<string>();
            var pending = "";
            Hyperlink link = null;
            foreach (var inline in paragraph.Inlines)
            {
                if (inline is Marker || TextFormat.Hidden(document, paragraph, inline)) continue;
                var target = inline.Link?.SafeTarget != null ? inline.Link : null;
                if (!ReferenceEquals(target, link))
                {
                    Close(sb, open, 0);
                    if (link != null) sb.Append("](").Append(Url(link.SafeTarget)).Append(')');
                    sb.Append(pending);
                    pending = "";
                    if (target != null) sb.Append('[');
                    link = target;
                }

                var text = inline switch
                {
                    TextRun run => Escape(run.Text),
                    Tab => single ? " " : "\t",
                    Break => single ? " " : "\n",
                    OpaqueInline token => Escape(token.DisplayText),
                    InlineImage image => Image(image),
                    _ => ""
                };
                var core = text.Trim(Space);
                if (core.Length == 0)
                {
                    pending += text;
                    continue;
                }

                var format = document.EffectiveRunFormat(paragraph, inline);
                var wanted = new List<string>(3);
                if ((format.Strike == true || format.DoubleStrike == true) && baseline.Strike != true) wanted.Add("~~");
                if (format.Bold == true && baseline.Bold != true) wanted.Add("**");
                if (format.Italic == true && baseline.Italic != true) wanted.Add("*");
                var keep = 0;
                while (keep < open.Count && keep < wanted.Count && open[keep] == wanted[keep]) keep++;
                Close(sb, open, keep);
                var lead = text.Length - text.TrimStart(Space).Length;
                sb.Append(pending).Append(text, 0, lead);
                pending = text[(lead + core.Length)..];
                foreach (var delimiter in wanted.Skip(keep))
                {
                    sb.Append(delimiter);
                    open.Add(delimiter);
                }

                sb.Append(core);
            }

            Close(sb, open, 0);
            if (link != null) sb.Append("](").Append(Url(link.SafeTarget)).Append(')');
            return sb.Append(pending).ToString();
        }

        private static void Close(StringBuilder sb, List<string> open, int keep)
        {
            for (var k = open.Count - 1; k >= keep; k--) sb.Append(open[k]);
            open.RemoveRange(keep, open.Count - keep);
        }

        private string Image(InlineImage image)
        {
            var data = document.GetMedia(image.MediaPart);
            var type = document.MediaContentType(image.MediaPart)?.ToLowerInvariant();
            return data == null || type == null || !HtmlWriter.ImageTypes.Contains(type)
                ? ""
                : $"![{Escape(image.AltText ?? "")}](data:{type};base64,{Convert.ToBase64String(data)})";
        }
    }
}
