using System.Net;
using System.Text;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Office;

internal enum PastedList { None, Bullet, Number }

internal readonly record struct PastedFormat(bool Bold, bool Italic, bool Underline, string Color, string Link);

internal sealed class PastedRun(string text, PastedFormat format)
{
    public string Text { get; set; } = text;

    public PastedFormat Format { get; } = format;
}

internal sealed class PastedParagraph
{
    public List<PastedRun> Runs { get; } = [];

    public PastedList List { get; set; }

    public int Depth { get; set; }
}

internal sealed class HtmlPasteReader : IHtmlTokenSink
{
    private static readonly HashSet<string> Blocks =
        ["p", "div", "li", "ul", "ol", "h1", "h2", "h3", "h4", "h5", "h6", "tr", "table", "blockquote", "pre", "section", "article", "header", "footer", "dt", "dd"];

    private static readonly HashSet<string> Empty = ["br", "img", "hr", "meta", "link", "input", "col", "wbr", "area", "base", "source", "param", "embed"];

    private readonly List<PastedParagraph> _paragraphs = [new()];
    private readonly List<(string Name, PastedFormat Format)> _open = [];
    private readonly List<PastedList> _lists = [];
    private bool _space = true;

    public bool Done => false;

    private PastedFormat Current => _open.Count == 0 ? default : _open[^1].Format;

    public static IReadOnlyList<PastedParagraph> Read(string html)
    {
        if (string.IsNullOrEmpty(html) || html.Length > HtmlTableFormat.MaxLength) return [];
        var reader = new HtmlPasteReader();
        HtmlTableFormat.Tokenize(html, reader);
        foreach (var paragraph in reader._paragraphs.Where(p => p.Runs.Count > 0))
        {
            paragraph.Runs[^1].Text = paragraph.Runs[^1].Text.TrimEnd(' ');
            if (paragraph.Runs[^1].Text.Length == 0) paragraph.Runs.RemoveAt(paragraph.Runs.Count - 1);
        }

        return reader._paragraphs.Where(p => p.Runs.Count > 0).ToList();
    }

    public void StyleSheet(string css)
    {
    }

    public void Start(string name, Dictionary<string, string> attributes)
    {
        if (name == "br")
        {
            Add("\n");
            _space = true;
            return;
        }

        if (Blocks.Contains(name)) Break();
        if (name is "ul" or "ol") _lists.Add(name == "ul" ? PastedList.Bullet : PastedList.Number);
        if (name == "li") (_paragraphs[^1].List, _paragraphs[^1].Depth) = (_lists.Count == 0 ? PastedList.Bullet : _lists[^1], Math.Max(0, _lists.Count - 1));
        if (Empty.Contains(name)) return;
        var format = name switch
        {
            "b" or "strong" => Current with { Bold = true },
            "i" or "em" => Current with { Italic = true },
            "u" or "ins" => Current with { Underline = true },
            "a" when attributes.TryGetValue("href", out var href) => Current with { Link = href.Trim() },
            _ => Current
        };
        _open.Add((name, attributes.TryGetValue("style", out var css) ? Css(format, css) : format));
    }

    public void End(string name)
    {
        var at = _open.FindLastIndex(o => o.Name == name);
        if (at >= 0) _open.RemoveRange(at, _open.Count - at);
        if (name is "ul" or "ol" && _lists.Count > 0) _lists.RemoveAt(_lists.Count - 1);
        if (Blocks.Contains(name)) Break();
    }

    public void Text(string text)
    {
        var sb = new StringBuilder();
        foreach (var c in WebUtility.HtmlDecode(text))
        {
            if (c is ' ' or '\t' or '\n' or '\r' or '\f')
            {
                if (!_space) sb.Append(' ');
                _space = true;
            }
            else if (!char.IsControl(c))
            {
                sb.Append(c);
                _space = false;
            }
        }

        Add(sb.ToString());
    }

    private void Break()
    {
        if (_paragraphs[^1].Runs.Count > 0) _paragraphs.Add(new PastedParagraph());
        _space = true;
    }

    private void Add(string text)
    {
        if (text.Length == 0) return;
        var runs = _paragraphs[^1].Runs;
        if (runs.Count > 0 && runs[^1].Format == Current)
            runs[^1].Text += text;
        else
            runs.Add(new PastedRun(text, Current));
    }

    private static PastedFormat Css(PastedFormat format, string css)
    {
        foreach (var declaration in HtmlTableFormat.Split(css, ';'))
        {
            var colon = declaration.IndexOf(':');
            if (colon <= 0) continue;
            var value = declaration[(colon + 1)..].Replace("!important", "", StringComparison.OrdinalIgnoreCase).Trim().ToLowerInvariant();
            format = declaration[..colon].Trim().ToLowerInvariant() switch
            {
                "font-weight" => format with { Bold = value is "bold" or "bolder" || int.TryParse(value, out var weight) && weight >= 600 },
                "font-style" => format with { Italic = value is "italic" or "oblique" },
                "text-decoration" or "text-decoration-line" => format with { Underline = value.Contains("underline") },
                "color" => format with { Color = HtmlTableFormat.Color(value) ?? format.Color },
                _ => format
            };
        }

        return format;
    }
}
