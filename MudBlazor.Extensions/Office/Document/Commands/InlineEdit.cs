using System.Text;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Document;

internal static class InlineEdit
{
    internal const string HyperlinkStyle = "Hyperlink";

    public static (List<Inline> Left, List<Inline> Right) Split(IReadOnlyList<Inline> inlines, int offset)
    {
        var left = new List<Inline>();
        var right = new List<Inline>();
        var position = 0;
        foreach (var inline in inlines)
        {
            var length = inline.Length;
            if (position < offset && position + length <= offset)
                left.Add(inline);
            else if (position >= offset)
                right.Add(inline);
            else
            {
                var run = (TextRun)inline;
                left.Add(run with { Text = run.Text[..(offset - position)] });
                right.Add(run with { Text = run.Text[(offset - position)..] });
            }

            position += length;
        }

        return (left, right);
    }

    public static (List<Inline> Left, List<Inline> Middle, List<Inline> Right) Split(IReadOnlyList<Inline> inlines, int start, int end)
    {
        var (left, rest) = Split(inlines, start);
        var (middle, right) = Split(rest, end - start);
        return (left, middle, right);
    }

    public static Inline At(Paragraph paragraph, int offset)
    {
        var position = 0;
        foreach (var inline in paragraph.Inlines)
        {
            if (offset >= position && offset < position + inline.Length) return inline;
            position += inline.Length;
        }

        return null;
    }

    public static (RunFormat Format, Hyperlink Link, XElement Source) InsertionStyle(Paragraph paragraph, int offset)
    {
        var before = offset > 0 ? At(paragraph, offset - 1) : null;
        var after = At(paragraph, offset);
        var link = before?.Link != null && ReferenceEquals(before.Link, after?.Link) ? before.Link : null;
        var source = before ?? after;
        return (Unlinked(source?.Format ?? paragraph.MarkFormat ?? new RunFormat(), link, source), link, RunOf(source));
    }

    public static XElement RunOf(Inline inline) => inline?.Source is { } source && source.Name == FormatXml.W + "r" ? source : null;

    public static RunFormat Unlinked(RunFormat format, Hyperlink link, Inline origin) =>
        origin?.Link != null && link == null && format.StyleId == HyperlinkStyle ? format with { StyleId = null } : format;

    public static List<Inline> Normalize(IEnumerable<Inline> inlines)
    {
        var result = new List<Inline>();
        foreach (var inline in inlines)
            if (result.Count > 0 && result[^1] is TextRun left && inline is TextRun right && ReferenceEquals(left.Source, right.Source) && left.Format == right.Format &&
                ReferenceEquals(left.Link, right.Link))
                result[^1] = left with { Text = left.Text + right.Text };
            else
                result.Add(inline);
        return result;
    }

    public static IReadOnlyList<IReadOnlyList<Inline>> Lines(string text, RunFormat format, Hyperlink link, XElement source) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\uFFFC", "").Split('\n').Select(line => Line(line, format, link, source)).ToList();

    private static IReadOnlyList<Inline> Line(string line, RunFormat format, Hyperlink link, XElement source)
    {
        var inlines = new List<Inline>();
        var run = new StringBuilder();
        foreach (var c in line)
        {
            if (c is not ('\t' or '\v' or '\f'))
            {
                run.Append(c);
                continue;
            }

            Flush();
            inlines.Add(c == '\t'
                ? new Tab { Format = format, Link = link, Source = source }
                : new Break(c == '\v' ? BreakKind.Line : BreakKind.Page) { Format = format, Link = link, Source = source });
        }

        Flush();
        return inlines;

        void Flush()
        {
            if (run.Length > 0) inlines.Add(new TextRun(run.ToString()) { Format = format, Link = link, Source = source });
            run.Clear();
        }
    }
}
