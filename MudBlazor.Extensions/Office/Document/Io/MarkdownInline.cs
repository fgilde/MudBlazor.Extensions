using System.Net;
using System.Text;

namespace MudBlazor.Extensions.Office.Document;

internal sealed class MarkdownInline(TextDocument document)
{
    private static readonly char[] Space = [' ', '\t', '\n'];
    private static readonly (string Markdown, string Tag)[] Kinds = [("~~", "del"), ("**", "strong"), ("*", "em")];
    private static readonly HashSet<char> RawCodeEscapes = ['*', '_', '`', '[', ']', '\\', '~', '|'];

    private readonly List<object> _tokens = [];
    private readonly List<Pair> _open = [];

    public string Write(Paragraph paragraph, bool single)
    {
        _tokens.Clear();
        _open.Clear();
        var baseline = document.EffectiveRunFormat(new Paragraph { Format = paragraph.Format }, null);
        var inlines = paragraph.Inlines.Where(i => i is not Marker && !TextFormat.Hidden(document, paragraph, i))
            .Select(i => (Inline: i, Format: document.EffectiveRunFormat(paragraph, i))).ToList();
        var pending = "";
        Hyperlink link = null;
        for (var n = 0; n < inlines.Count; n++)
        {
            var (inline, format) = inlines[n];
            var target = Target(inline);
            if (!ReferenceEquals(target, link))
            {
                Close(0);
                if (link != null) _tokens.Add("](" + MarkdownFormat.Url(link.SafeTarget) + ")");
                _tokens.Add(pending);
                pending = "";
                if (target != null) _tokens.Add("[");
                link = target;
            }

            var kinds = Wanted(format, baseline);
            string text, core;
            if (IsCode(inline, format))
            {
                var code = new StringBuilder(CodeText(inline, single));
                while (n + 1 < inlines.Count && IsCode(inlines[n + 1].Inline, inlines[n + 1].Format) && ReferenceEquals(Target(inlines[n + 1].Inline), link) &&
                       Wanted(inlines[n + 1].Format, baseline) == kinds)
                    code.Append(CodeText(inlines[++n].Inline, single));
                text = core = CodeSpan(code.ToString(), single);
            }
            else
            {
                text = inline switch
                {
                    TextRun run => MarkdownFormat.Escape(run.Text),
                    Tab => single ? " " : "\t",
                    Break => single ? " " : "\n",
                    OpaqueInline token => MarkdownFormat.Escape(token.DisplayText),
                    InlineImage image => Image(image),
                    _ => ""
                };
                core = text.Trim(Space);
            }

            if (core.Length == 0)
            {
                pending += text;
                continue;
            }

            var wanted = Enumerable.Range(0, Kinds.Length).Where(kind => (kinds & 1 << kind) != 0).ToList();
            var keep = 0;
            while (keep < _open.Count && keep < wanted.Count && _open[keep].Kind == wanted[keep]) keep++;
            Close(keep);
            var lead = text.IndexOf(core, StringComparison.Ordinal);
            _tokens.Add(pending + text[..lead]);
            pending = text[(lead + core.Length)..];
            foreach (var kind in wanted.Skip(keep))
            {
                var pair = new Pair(kind, _tokens.Count);
                _open.Add(pair);
                _tokens.Add(new Mark(pair, true));
            }

            _tokens.Add(core);
        }

        Close(0);
        if (link != null) _tokens.Add("](" + MarkdownFormat.Url(link.SafeTarget) + ")");
        _tokens.Add(pending);
        return string.Concat(_tokens.Select(t => t switch
        {
            Mark { Pair.Empty: true } => "",
            Mark { Pair.Raw: true } mark => mark.Open ? $"<{Kinds[mark.Pair.Kind].Tag}>" : $"</{Kinds[mark.Pair.Kind].Tag}>",
            Mark mark => Kinds[mark.Pair.Kind].Markdown,
            _ => (string)t
        }));
    }

    private static int Wanted(RunFormat format, RunFormat baseline) =>
        ((format.Strike == true || format.DoubleStrike == true) && baseline.Strike != true ? 1 : 0) |
        (format.Bold == true && baseline.Bold != true ? 2 : 0) |
        (format.Italic == true && baseline.Italic != true ? 4 : 0);

    private static Hyperlink Target(Inline inline) => inline.Link?.SafeTarget != null ? inline.Link : null;

    private static bool IsCode(Inline inline, RunFormat format) => inline is TextRun or Tab && format.Font == HtmlReader.Monospace;

    private static string CodeText(Inline inline, bool single) => inline is Tab ? single ? " " : "\t" : ((TextRun)inline).Text;

    private static string CodeSpan(string code, bool single)
    {
        if (code.Length == 0) return "";
        if (single && code.Contains('|')) return "<code>" + string.Concat(WebUtility.HtmlEncode(code).Select(c => RawCodeEscapes.Contains(c) ? $"&#{(int)c};" : c.ToString())) + "</code>";
        int longest = 0, run = 0;
        foreach (var c in code) longest = Math.Max(longest, run = c == '`' ? run + 1 : 0);
        var fence = new string('`', longest + 1);
        var pad = code.StartsWith('`') || code.EndsWith('`') || code.StartsWith(' ') && code.EndsWith(' ') && code.Trim(' ').Length > 0 ? " " : "";
        return fence + pad + code + pad + fence;
    }

    private void Close(int keep)
    {
        for (var k = _open.Count - 1; k >= keep; k--)
        {
            _tokens.Add(new Mark(_open[k], false));
            Decide(_open[k]);
        }

        _open.RemoveRange(keep, _open.Count - keep);
    }

    private void Decide(Pair pair)
    {
        var start = pair.Start;
        char? first = null, last = null;
        for (var i = start + 1; i < _tokens.Count - 1 && first == null; i++) first = Edge(_tokens[i], true);
        for (var i = _tokens.Count - 2; i > start && last == null; i--) last = Edge(_tokens[i], false);
        pair.Empty = first == null;
        pair.Raw = !(first is { } f && char.IsLetterOrDigit(f) && last is { } l && char.IsLetterOrDigit(l));
    }

    private static char? Edge(object token, bool start) => token switch
    {
        string { Length: > 0 } text => start ? text[0] : text[^1],
        Mark { Pair.Empty: false, Pair.Raw: true } => start ? '<' : '>',
        _ => null
    };

    private string Image(InlineImage image)
    {
        var data = document.GetMedia(image.MediaPart);
        var type = document.MediaContentType(image.MediaPart)?.ToLowerInvariant();
        return data == null || type == null || !HtmlWriter.ImageTypes.Contains(type)
            ? ""
            : $"![{MarkdownFormat.Escape((image.AltText ?? "").ReplaceLineEndings(" "))}](data:{type};base64,{Convert.ToBase64String(data)})";
    }

    private sealed class Pair(int kind, int start)
    {
        public int Kind { get; } = kind;

        public int Start { get; } = start;

        public bool Raw { get; set; }

        public bool Empty { get; set; }
    }

    private readonly record struct Mark(Pair Pair, bool Open);
}
