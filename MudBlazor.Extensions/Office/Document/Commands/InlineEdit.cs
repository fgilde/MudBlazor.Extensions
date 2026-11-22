using System.Text;

namespace MudBlazor.Extensions.Office.Document;

internal static class InlineEdit
{
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

    public static (RunFormat Format, Hyperlink Link) InsertionStyle(Paragraph paragraph, int offset)
    {
        var before = offset > 0 ? At(paragraph, offset - 1) : null;
        var after = At(paragraph, offset);
        var link = before?.Link != null && ReferenceEquals(before.Link, after?.Link) ? before.Link : null;
        return ((before ?? after)?.Format ?? paragraph.MarkFormat ?? new RunFormat(), link);
    }

    public static void Append(List<Inline> target, IEnumerable<Inline> items)
    {
        var first = true;
        foreach (var item in items)
        {
            if (first && target.Count > 0 && target[^1] is TextRun left && item is TextRun right && left.Format == right.Format && ReferenceEquals(left.Link, right.Link))
                target[^1] = (left.Source != null || right.Source == null ? left : right) with { Text = left.Text + right.Text };
            else
                target.Add(item);
            first = false;
        }
    }

    public static IReadOnlyList<IReadOnlyList<Inline>> Lines(string text, RunFormat format, Hyperlink link) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("￼", "").Split('\n').Select(line => Line(line, format, link)).ToList();

    private static IReadOnlyList<Inline> Line(string line, RunFormat format, Hyperlink link)
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
            inlines.Add(c == '\t' ? new Tab { Format = format, Link = link } : new Break(c == '\v' ? BreakKind.Line : BreakKind.Page) { Format = format, Link = link });
        }

        Flush();
        return inlines;

        void Flush()
        {
            if (run.Length > 0) inlines.Add(new TextRun(run.ToString()) { Format = format, Link = link });
            run.Clear();
        }
    }
}
