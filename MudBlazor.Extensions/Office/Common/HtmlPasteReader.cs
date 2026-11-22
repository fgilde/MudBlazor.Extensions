using System.Globalization;
using System.Net;
using System.Text;
using System.Xml;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Office;

internal sealed class HtmlPasteReader : IHtmlTokenSink
{
    internal const int MaxImageBytes = 32 * 1024 * 1024;
    internal const int MaxTableDepth = 32;
    private const int MaxOpen = 256;
    private const int MaxSpan = 1000;

    private static readonly HashSet<string> Blocks =
        ["p", "div", "li", "ul", "ol", "h1", "h2", "h3", "h4", "h5", "h6", "tr", "table", "blockquote", "pre", "section", "article", "header", "footer", "dt", "dd"];

    private static readonly HashSet<string> Empty = ["br", "img", "hr", "meta", "link", "input", "col", "wbr", "area", "base", "source", "param", "embed"];

    private static readonly Dictionary<string, string> ImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = "image/png", ["image/jpeg"] = "image/jpeg", ["image/jpg"] = "image/jpeg", ["image/gif"] = "image/gif", ["image/bmp"] = "image/bmp"
    };

    private readonly bool _structured;
    private readonly List<PastedBlock> _root = [];
    private readonly List<Open> _open = [];
    private readonly List<PastedList> _lists = [];
    private readonly List<(PastedTable Table, List<PastedBlock> Outer)> _tables = [];
    private readonly StringBuilder _label = new();
    private List<PastedBlock> _container;
    private PastedParagraph _paragraph;
    private int _flatTables;
    private int _skip;
    private int _labels;
    private int _pre;
    private bool _preStart;
    private bool _space = true;
    private string _title;

    private HtmlPasteReader(bool structured)
    {
        _structured = structured;
        _container = _root;
        _container.Add(_paragraph = new PastedParagraph());
    }

    private enum OpenKind { Normal, Skip, Label }

    public bool Done => false;

    private PastedFormat Current => _open.Count == 0 ? default : _open[^1].Format;

    public static IReadOnlyList<PastedParagraph> Read(string html)
    {
        if (string.IsNullOrEmpty(html) || html.Length > HtmlTableFormat.MaxLength) return [];
        var reader = new HtmlPasteReader(false);
        HtmlTableFormat.Tokenize(html, reader);
        foreach (var paragraph in reader._root.OfType<PastedParagraph>()) Project(paragraph);
        Finish(reader._root, false);
        return reader._root.OfType<PastedParagraph>().ToList();
    }

    public static IReadOnlyList<PastedBlock> ReadBlocks(string html) => ReadBlocks(html, out _);

    public static IReadOnlyList<PastedBlock> ReadBlocks(string html, out string title)
    {
        title = null;
        if (string.IsNullOrEmpty(html) || html.Length > HtmlTableFormat.MaxLength) return [];
        var reader = new HtmlPasteReader(true);
        HtmlTableFormat.Tokenize(html, reader);
        Finish(reader._root, true);
        title = reader._title;
        return reader._root;
    }

    internal static string Clean(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var sb = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                sb.Append(text, i++, 2);
            else if (Valid(text[i]))
                sb.Append(text[i]);
        return sb.ToString();
    }

    public void StyleSheet(string css)
    {
    }

    public void Title(string text)
    {
        if (!_structured || _title != null) return;
        var title = string.Join(' ', Clean(WebUtility.HtmlDecode(text)).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (title.Length > 0) _title = title.Length > 1024 ? title[..1024] : title;
    }

    public void Start(string name, Dictionary<string, string> attributes)
    {
        _preStart = name == "pre";
        if (name == "br")
        {
            Add("\n");
            _space = true;
            return;
        }

        if (_structured && name == "img")
        {
            Image(attributes);
            return;
        }

        if (_structured && name is "table" or "tr" or "td" or "th") StartTable(name, attributes);
        else if (Blocks.Contains(name)) Break();
        if (name is "ul" or "ol") _lists.Add(name == "ul" ? PastedList.Bullet : PastedList.Number);
        if (name == "li" && Paragraph() is var item)
            (item.List, item.Depth) = (_lists.Count == 0 ? PastedList.Bullet : _lists[^1], Math.Max(0, _lists.Count - 1));

        if (Empty.Contains(name) || _open.Count >= MaxOpen) return;
        var format = name switch
        {
            "b" or "strong" => Current with { Bold = true },
            "i" or "em" => Current with { Italic = true },
            "u" or "ins" => Current with { Underline = true },
            "s" or "strike" or "del" => Current with { Strike = true },
            "sup" => Current with { VerticalAlign = "super" },
            "sub" => Current with { VerticalAlign = "sub" },
            "a" when attributes.TryGetValue("href", out var href) => Current with { Link = Clean(href.Trim()) },
            _ => Current
        };
        var align = attributes.TryGetValue("align", out var alignment) ? Align(alignment.Trim().ToLowerInvariant()) : null;
        var kind = _structured && name is "head" or "o:p" ? OpenKind.Skip : OpenKind.Normal;
        if (attributes.TryGetValue("style", out var css))
        {
            var style = Css(format, css);
            (format, align) = (_structured && name is "td" or "th" ? style.Format with { Highlight = format.Highlight } : style.Format, style.Align ?? align);
            if (_structured && style.Label) kind = OpenKind.Label;
            else if (_structured && style.Level is { } level && Paragraph() is var paragraph) (paragraph.List, paragraph.Depth) = (PastedList.Bullet, level);
        }

        _open.Add(new Open(name, format, align, kind));
        if (kind == OpenKind.Skip) _skip++;
        if (kind == OpenKind.Label) _labels++;
        if (name == "pre") _pre++;
    }

    public void End(string name)
    {
        _preStart = false;
        var at = _open.FindLastIndex(o => o.Name == name);
        if (at >= 0) Pop(at);
        if (name is "ul" or "ol" && _lists.Count > 0) _lists.RemoveAt(_lists.Count - 1);
        if (_structured && name is "table" or "tr" or "td" or "th") EndTable(name);
        else if (Blocks.Contains(name)) Break();
    }

    public void Text(string text)
    {
        if (_skip > 0) return;
        var decoded = WebUtility.HtmlDecode(text);
        if (_labels > 0)
        {
            _label.Append(decoded);
            return;
        }

        var pre = _structured && _pre > 0;
        if (pre)
        {
            decoded = decoded.Replace("\r\n", "\n").Replace('\r', '\n');
            if (_preStart && decoded.StartsWith('\n')) decoded = decoded[1..];
            _preStart = false;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < decoded.Length; i++)
        {
            var c = decoded[i];
            if (char.IsHighSurrogate(c) && i + 1 < decoded.Length && char.IsLowSurrogate(decoded[i + 1]))
            {
                sb.Append(decoded, i++, 2);
                _space = false;
            }
            else if (pre && c is ' ' or '\t' or '\n')
            {
                sb.Append(c);
                _space = false;
            }
            else if (c is ' ' or '\t' or '\n' or '\r' or '\f')
            {
                if (!_space) sb.Append(' ');
                _space = true;
            }
            else if (Valid(c))
            {
                sb.Append(c);
                _space = false;
            }
        }

        Add(sb.ToString());
    }

    private static bool Valid(char c) => !char.IsControl(c) && !char.IsSurrogate(c) && c != '\uFFFC' && XmlConvert.IsXmlChar(c);

    private static void Project(PastedParagraph paragraph)
    {
        var runs = new List<PastedRun>();
        foreach (var run in paragraph.Runs.Where(r => r.ImageData == null))
        {
            var format = new PastedFormat(run.Format.Bold, run.Format.Italic, run.Format.Underline, run.Format.Color, run.Format.Link);
            if (runs.Count > 0 && runs[^1].Format == format)
                runs[^1].Append(run.Text);
            else
                runs.Add(new PastedRun(run.Text, format));
        }

        paragraph.Runs.Clear();
        paragraph.Runs.AddRange(runs);
    }

    private static void Finish(List<PastedBlock> blocks, bool structured)
    {
        var kept = new HashSet<PastedBlock>();
        foreach (var block in blocks)
            if (block is PastedParagraph { Runs.Count: > 0 } paragraph && paragraph.Runs[^1] is { ImageData: null } last)
            {
                var text = last.Text;
                var trimmed = paragraph.Preformatted ? text : text.TrimEnd(' ');
                if ((structured || paragraph.Preformatted) && trimmed.EndsWith('\n')) trimmed = trimmed[..^1];
                if (trimmed.Length == 0) paragraph.Runs.RemoveAt(paragraph.Runs.Count - 1);
                else if (trimmed.Length != text.Length) last.Text = trimmed;
                if (structured) kept.Add(paragraph);
            }
            else if (block is PastedTable table)
            {
                foreach (var cell in table.Rows.SelectMany(r => r)) Finish(cell.Blocks, structured);
                table.Rows.RemoveAll(r => r.Count == 0);
            }

        blocks.RemoveAll(b => b is PastedParagraph { Runs.Count: 0 } && !kept.Contains(b) || b is PastedTable { Rows.Count: 0 });
    }

    private void Pop(int at)
    {
        for (var k = at; k < _open.Count; k++)
        {
            if (_open[k].Kind == OpenKind.Skip) _skip--;
            if (_open[k].Kind == OpenKind.Label) _labels--;
            if (_open[k].Name == "pre") _pre--;
        }

        var label = _open.Skip(at).Any(o => o.Kind == OpenKind.Label) && _labels == 0;
        _open.RemoveRange(at, _open.Count - at);
        if (!label) return;
        var text = _label.ToString().Trim();
        _label.Clear();
        if (_paragraph is { List: not PastedList.None } paragraph)
            paragraph.List = text.Any(char.IsDigit) || text.Length > 1 && text[^1] is '.' or ')' ? PastedList.Number : PastedList.Bullet;
    }

    private void StartTable(string name, Dictionary<string, string> attributes)
    {
        if (name == "table")
        {
            Break();
            if (_flatTables > 0 || _tables.Count >= MaxTableDepth)
            {
                _flatTables++;
                return;
            }

            var table = new PastedTable();
            Paragraph();
            _container.Add(table);
            _tables.Add((table, _container));
            (_container, _paragraph) = (null, null);
            return;
        }

        if (_tables.Count == 0 || _flatTables > 0)
        {
            if (name == "tr") Break();
            return;
        }

        var rows = _tables[^1].Table.Rows;
        if (name == "tr")
        {
            rows.Add([]);
            (_container, _paragraph) = (null, null);
            return;
        }

        if (rows.Count == 0) rows.Add([]);
        var shading = attributes.TryGetValue("style", out var css) ? Css(default, css).Format.Highlight : null;
        if (shading == null && attributes.TryGetValue("bgcolor", out var color)) shading = HtmlTableFormat.Color(color.Trim().ToLowerInvariant());
        var cell = new PastedCell { ColSpan = Span(attributes, "colspan"), RowSpan = Span(attributes, "rowspan"), Shading = shading };
        rows[^1].Add(cell);
        _container = cell.Blocks;
        _container.Add(_paragraph = new PastedParagraph());
        _space = true;
    }

    private void EndTable(string name)
    {
        if (_tables.Count == 0 || _flatTables > 0)
        {
            if (name == "table" && _flatTables > 0) _flatTables--;
            if (name is "table" or "tr") Break();
            return;
        }

        _space = true;
        if (name != "table")
        {
            (_container, _paragraph) = (null, null);
            return;
        }

        _container = _tables[^1].Outer;
        _tables.RemoveAt(_tables.Count - 1);
        _container.Add(_paragraph = new PastedParagraph());
    }

    private static int Span(Dictionary<string, string> attributes, string name) =>
        attributes.TryGetValue(name, out var value) && int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var span) ? Math.Clamp(span, 1, MaxSpan) : 1;

    private PastedParagraph Paragraph()
    {
        if (_paragraph != null) return _paragraph;
        var rows = _tables[^1].Table.Rows;
        if (rows.Count == 0) rows.Add([]);
        var cell = new PastedCell();
        rows[^1].Add(cell);
        _container = cell.Blocks;
        _container.Add(_paragraph = new PastedParagraph());
        return _paragraph;
    }

    private PastedParagraph Content()
    {
        var paragraph = Paragraph();
        if (paragraph.Runs.Count > 0) return paragraph;
        for (var k = _open.Count - 1; k >= 0; k--)
        {
            var name = _open[k].Name;
            if (paragraph.Heading == 0 && name.Length == 2 && name[0] == 'h' && name[1] is >= '1' and <= '6') paragraph.Heading = name[1] - '0';
            paragraph.Align ??= _open[k].Align;
        }

        paragraph.Preformatted = _structured && _pre > 0;
        return paragraph;
    }

    private void Break()
    {
        if (_paragraph is { Runs.Count: > 0 }) _container.Add(_paragraph = new PastedParagraph());
        _space = true;
    }

    private void Add(string text)
    {
        if (text.Length == 0 || _skip > 0 || _labels > 0) return;
        var runs = Content().Runs;
        if (runs.Count > 0 && runs[^1].ImageData == null && runs[^1].Format == Current)
            runs[^1].Append(text);
        else
            runs.Add(new PastedRun(text, Current));
    }

    private void Image(Dictionary<string, string> attributes)
    {
        if (_skip > 0 || _labels > 0 || !attributes.TryGetValue("src", out var src) || DataUri(src) is not ({ } data, { } type)) return;
        Content().Runs.Add(new PastedRun("", Current)
        {
            ImageData = data, ImageType = type, Alt = attributes.TryGetValue("alt", out var alt) ? Clean(alt) : null,
            Width = Pixels(attributes, "width"), Height = Pixels(attributes, "height")
        });
        _space = false;
    }

    private static (byte[] Data, string Type) DataUri(string src)
    {
        var uri = src.Trim();
        var comma = uri.IndexOf(',');
        if (!uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || comma < 0 || comma > 256 || uri.Length - comma > 2L * MaxImageBytes) return default;
        var header = uri[5..comma].Split(';');
        if (!ImageTypes.TryGetValue(header[0].Trim(), out var type) || !header.Skip(1).Any(p => p.Trim().Equals("base64", StringComparison.OrdinalIgnoreCase))) return default;
        try
        {
            var data = Convert.FromBase64String(uri[(comma + 1)..]);
            return data.Length is > 0 and <= MaxImageBytes ? (data, type) : default;
        }
        catch (FormatException)
        {
            return default;
        }
    }

    private static int? Pixels(Dictionary<string, string> attributes, string name)
    {
        if (!attributes.TryGetValue(name, out var value)) return null;
        value = value.Trim();
        if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase)) value = value[..^2];
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var pixels) && pixels is > 0 and <= 32767 ? pixels : null;
    }

    private static (PastedFormat Format, string Align, int? Level, bool Label) Css(PastedFormat format, string css)
    {
        string align = null;
        int? level = null;
        var label = false;
        foreach (var declaration in HtmlTableFormat.Split(css, ';'))
        {
            var colon = declaration.IndexOf(':');
            if (colon <= 0) continue;
            var raw = declaration[(colon + 1)..].Replace("!important", "", StringComparison.OrdinalIgnoreCase).Trim();
            var value = raw.ToLowerInvariant();
            switch (declaration[..colon].Trim().ToLowerInvariant())
            {
                case "font-weight":
                    format = format with { Bold = value is "bold" or "bolder" || int.TryParse(value, out var weight) && weight >= 600 };
                    break;
                case "font-style":
                    format = format with { Italic = value is "italic" or "oblique" };
                    break;
                case "text-decoration" or "text-decoration-line":
                    format = format with { Underline = value.Contains("underline"), Strike = value.Contains("line-through") };
                    break;
                case "color":
                    format = format with { Color = HtmlTableFormat.Color(value) ?? format.Color };
                    break;
                case "font-size":
                    format = format with { SizePt = FontSize(value) ?? format.SizePt };
                    break;
                case "font-family":
                    format = format with { Font = FontName(raw) ?? format.Font };
                    break;
                case "background" or "background-color":
                    format = format with { Highlight = HtmlTableFormat.Split(value, ' ').Select(HtmlTableFormat.Color).FirstOrDefault(c => c != null) ?? format.Highlight };
                    break;
                case "vertical-align":
                    format = format with { VerticalAlign = value switch { "super" => "super", "sub" => "sub", "baseline" => null, _ => format.VerticalAlign } };
                    break;
                case "text-align":
                    align = Align(value) ?? align;
                    break;
                case "mso-list":
                    label |= value == "ignore";
                    foreach (var token in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        if (token.StartsWith("level", StringComparison.Ordinal) && int.TryParse(token[5..], NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n >= 1)
                            level = Math.Min(n - 1, 8);
                    break;
            }
        }

        return (format, align, level, label);
    }

    private static string Align(string value) => value switch
    {
        "left" or "start" => "left",
        "center" => "center",
        "right" or "end" => "right",
        "justify" => "justify",
        _ => null
    };

    private static double? FontSize(string value)
    {
        var factor = value.EndsWith("pt", StringComparison.Ordinal) ? 1 : value.EndsWith("px", StringComparison.Ordinal) ? 0.75 : 0;
        if (factor == 0 || !double.TryParse(value[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var size) || !(size > 0)) return null;
        return Math.Clamp(Math.Round(size * factor * 2) / 2, 1, 1638);
    }

    private static string FontName(string value)
    {
        var first = HtmlTableFormat.Split(value, ',').FirstOrDefault()?.Trim().Trim('"', '\'') ?? "";
        var name = new string(first.Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-').ToArray()).Trim();
        return name.Length is > 0 and <= 31 && !(name.ToLowerInvariant() is "serif" or "sans-serif" or "monospace" or "cursive" or "fantasy" or "inherit" or "initial")
            ? name
            : null;
    }

    private readonly record struct Open(string Name, PastedFormat Format, string Align, OpenKind Kind);
}
