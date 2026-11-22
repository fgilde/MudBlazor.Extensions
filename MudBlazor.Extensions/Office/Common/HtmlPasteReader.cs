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
    private const string Monospace = "Consolas";

    private static readonly HashSet<string> Blocks =
        ["p", "div", "li", "ul", "ol", "h1", "h2", "h3", "h4", "h5", "h6", "tr", "table", "blockquote", "pre", "section", "article", "header", "footer", "dt", "dd"];

    private static readonly HashSet<string> Empty = ["br", "img", "hr", "meta", "link", "input", "col", "wbr", "area", "base", "source", "param", "embed"];

    private static readonly HashSet<string> TableParts = ["table", "tr", "td", "th", "caption"];

    private static readonly HashSet<string> Explicit = ["p", "li", "h1", "h2", "h3", "h4", "h5", "h6"];

    private static readonly HashSet<string> HeadContent = ["html", "head", "meta", "link", "base", "xml"];

    private static readonly HashSet<string> ImageTypes = ["image/png", "image/jpeg", "image/jpg", "image/gif", "image/bmp"];

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
    private int _head;
    private int _listRun;
    private string _wordList;
    private bool _pageBreak;
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

    private Open Top => _open.Count == 0 ? default : _open[^1];

    private PastedFormat Current => Top.Format;

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
        if (_head > 0 && !HeadContent.Contains(name) && !name.Contains(':')) Pop(_open.FindLastIndex(o => o.Name == "head"));
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

        if (_structured && TableParts.Contains(name)) StartTable(name, attributes);
        else if (Blocks.Contains(name) || _structured && name == "center") Break();
        if (name is "ul" or "ol")
        {
            if (_lists.Count == 0) _listRun++;
            _wordList = null;
            _lists.Add(name == "ul" ? PastedList.Bullet : PastedList.Number);
        }

        if (name == "li" && Paragraph() is var item)
            (item.List, item.Depth, item.ListRun, item.Continuation) = (_lists.Count == 0 ? PastedList.Bullet : _lists[^1], Math.Max(0, _lists.Count - 1), _listRun, false);
        if (_structured && Explicit.Contains(name) && Paragraph() is var opened) (opened.Explicit, opened.Ended) = (true, false);

        if (Empty.Contains(name) || _open.Count >= MaxOpen) return;
        var format = Format(name, attributes);
        var align = name != "table" && attributes.TryGetValue("align", out var alignment) ? HtmlPasteStyle.Align(alignment.Trim().ToLowerInvariant()) : null;
        if (_structured && name == "center") align = "center";
        var kind = _structured && name is "head" or "o:p" ? OpenKind.Skip : OpenKind.Normal;
        var keep = name == "pre" || Top.Keep;
        var lines = name == "pre" || Top.Lines;
        int? indent = null, firstLine = null;
        var breakAfter = false;
        if (attributes.TryGetValue("style", out var css))
        {
            var style = HtmlPasteStyle.Css(format, css);
            (format, align) = (_structured && name is "td" or "th" or "tr" or "table" ? style.Format with { Highlight = format.Highlight } : style.Format, style.Align ?? align);
            (keep, lines, indent, firstLine, breakAfter) = (style.KeepSpace ?? keep, style.KeepLines ?? lines, style.Indent, style.FirstLine, _structured && style.BreakAfter);
            _pageBreak |= _structured && style.BreakBefore;
            if (_structured && style.Label) kind = OpenKind.Label;
            else if (_structured && style.Level is { } level && Paragraph() is var paragraph)
            {
                if (style.ListId != _wordList) (_wordList, _listRun) = (style.ListId, _listRun + 1);
                (paragraph.List, paragraph.Depth, paragraph.ListRun) = (PastedList.Bullet, level, _listRun);
            }
        }

        _open.Add(new Open(name, format, align, kind, keep, lines, name == "pre" || Top.Pre, indent, firstLine, breakAfter));
        if (kind == OpenKind.Skip) _skip++;
        if (kind == OpenKind.Skip && name == "head") _head++;
        if (kind == OpenKind.Label) _labels++;
    }

    public void End(string name)
    {
        _preStart = false;
        var at = _open.Count > 0 && _open[^1].Name == name ? _open.Count - 1 : _open.FindLastIndex(o => o.Name == name);
        if (at >= 0) Pop(at);
        if (name is "ul" or "ol" && _lists.Count > 0) _lists.RemoveAt(_lists.Count - 1);
        if (_structured && Explicit.Contains(name) && _paragraph != null) _paragraph.Ended = true;
        if (_structured && TableParts.Contains(name)) EndTable(name);
        else if (Blocks.Contains(name) || _structured && name == "center") Break();
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

        var keep = _structured && Top.Keep;
        var lines = _structured && Top.Lines;
        if (lines) decoded = decoded.Replace("\r\n", "\n").Replace('\r', '\n');
        if (keep)
        {
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
            else if (keep && c is ' ' or '\t' or '\n')
            {
                sb.Append(c);
                _space = false;
            }
            else if (lines && c == '\n')
            {
                if (sb.Length > 0 && sb[^1] == ' ') sb.Length--;
                sb.Append(c);
                _space = true;
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

    private PastedFormat Format(string name, Dictionary<string, string> attributes)
    {
        var current = Current;
        var format = name switch
        {
            "b" or "strong" => current with { Bold = true },
            "i" or "em" => current with { Italic = true },
            "u" or "ins" => current with { Underline = true },
            "s" or "strike" or "del" => current with { Strike = true },
            "sup" => current with { VerticalAlign = "super" },
            "sub" => current with { VerticalAlign = "sub" },
            "a" when attributes.TryGetValue("href", out var href) => current with { Link = Clean(href.Trim()) },
            _ => current
        };
        if (!_structured) return format;
        return name switch
        {
            "mark" => format with { Highlight = "#FFFF00" },
            "code" or "kbd" or "samp" or "tt" => format with { Font = Monospace },
            "big" => format with { SizePt = Math.Round((format.SizePt ?? 11) * 1.2 * 2) / 2 },
            "small" => format with { SizePt = Math.Max(1, Math.Round((format.SizePt ?? 11) / 1.2 * 2) / 2) },
            "font" => format with
            {
                Color = attributes.TryGetValue("color", out var color) ? HtmlTableFormat.Color(color.Trim().ToLowerInvariant()) ?? format.Color : format.Color,
                Font = attributes.TryGetValue("face", out var face) ? HtmlPasteStyle.FontName(face) ?? format.Font : format.Font,
                SizePt = attributes.TryGetValue("size", out var size) ? HtmlPasteStyle.FontTagSize(size) ?? format.SizePt : format.SizePt
            },
            _ => format
        };
    }

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
                var trimmed = paragraph.Preformatted || paragraph.KeepSpace ? text : text.TrimEnd(' ');
                if ((structured || paragraph.Preformatted) && trimmed.EndsWith('\n')) trimmed = trimmed[..^1];
                if (trimmed.Length == 0) paragraph.Runs.RemoveAt(paragraph.Runs.Count - 1);
                else if (trimmed.Length != text.Length) last.Text = trimmed;
                if (structured || paragraph.Runs.Count > 0) kept.Add(paragraph);
            }
            else if (block is PastedParagraph { Runs.Count: > 0 })
                kept.Add(block);
            else if (block is PastedTable table)
            {
                foreach (var cell in table.Rows.SelectMany(r => r)) Finish(cell.Blocks, structured);
                table.Rows.RemoveAll(r => r.Count == 0);
                if (table.Rows.Count > 0) kept.Add(table);
            }

        var first = blocks.FindIndex(kept.Contains);
        var final = blocks.FindLastIndex(kept.Contains);
        for (var k = first + 1; structured && first >= 0 && k < final; k++)
            if (blocks[k] is PastedParagraph { Explicit: true }) kept.Add(blocks[k]);
        blocks.RemoveAll(b => !kept.Contains(b));
    }

    private void Pop(int at)
    {
        var label = false;
        for (var k = at; k < _open.Count; k++)
        {
            if (_open[k].Kind == OpenKind.Skip) _skip--;
            if (_open[k].Kind == OpenKind.Skip && _open[k].Name == "head") _head--;
            if (_open[k].Kind == OpenKind.Label) (_labels, label) = (_labels - 1, true);
            _pageBreak |= _open[k].BreakAfter;
        }

        _open.RemoveRange(at, _open.Count - at);
        if (!label || _labels > 0) return;
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

        var (current, outer) = _tables[^1];
        var rows = current.Rows;
        if (name == "caption")
        {
            _container = outer;
            outer.Insert(outer.LastIndexOf(current), _paragraph = new PastedParagraph { Align = "center" });
            _space = true;
            return;
        }

        if (name == "tr")
        {
            rows.Add([]);
            (_container, _paragraph) = (null, null);
            return;
        }

        if (rows.Count == 0) rows.Add([]);
        var shading = attributes.TryGetValue("style", out var css) ? HtmlPasteStyle.Css(default, css).Format.Highlight : null;
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
        var scope = _structured ? _open.FindLastIndex(o => TableParts.Contains(o.Name)) : -1;
        for (var k = _open.Count - 1; k > scope; k--)
        {
            var name = _open[k].Name;
            if (paragraph.Heading == 0 && name.Length == 2 && name[0] == 'h' && name[1] is >= '1' and <= '6') paragraph.Heading = name[1] - '0';
            paragraph.Align ??= _open[k].Align;
            paragraph.IndentLeft ??= _open[k].Indent;
            paragraph.IndentFirstLine ??= _open[k].FirstLine;
        }

        var item = _open.FindLastIndex(o => o.Name == "li");
        if (_structured && paragraph.List == PastedList.None && _lists.Count > 0 && item > scope && item > _open.FindLastIndex(o => o.Name is "ul" or "ol"))
            (paragraph.List, paragraph.Depth, paragraph.ListRun, paragraph.Continuation) = (_lists[^1], _lists.Count - 1, _listRun, true);
        paragraph.Preformatted = _structured && Top.Pre;
        paragraph.KeepSpace = _structured && Top.Keep;
        paragraph.PageBreakBefore |= _structured && _pageBreak;
        _pageBreak = false;
        return paragraph;
    }

    private void Break()
    {
        if (_paragraph is { Runs.Count: > 0 } or { Explicit: true, Ended: true }) _container.Add(_paragraph = new PastedParagraph());
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
        if (!uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || comma < 0 || comma > 256 || (uri.Length - comma - 1L) * 3 / 4 > MaxImageBytes) return default;
        var header = uri[5..comma].Split(';');
        if (!ImageTypes.Contains(header[0].Trim().ToLowerInvariant()) || !header.Skip(1).Any(p => p.Trim().Equals("base64", StringComparison.OrdinalIgnoreCase))) return default;
        try
        {
            var data = Convert.FromBase64String(uri[(comma + 1)..]);
            return data.Length is > 0 and <= MaxImageBytes && Sniff(data) is { } type ? (data, type) : default;
        }
        catch (FormatException)
        {
            return default;
        }
    }

    private static string Sniff(byte[] data) => data switch
    {
        [0x89, 0x50, 0x4E, 0x47, ..] => "image/png",
        [0xFF, 0xD8, 0xFF, ..] => "image/jpeg",
        [0x47, 0x49, 0x46, 0x38, ..] => "image/gif",
        [0x42, 0x4D, ..] => "image/bmp",
        _ => null
    };

    private static int? Pixels(Dictionary<string, string> attributes, string name)
    {
        if (!attributes.TryGetValue(name, out var value)) return null;
        value = value.Trim();
        if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase)) value = value[..^2];
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var pixels) && pixels is > 0 and <= 32767 ? pixels : null;
    }

    private readonly record struct Open(string Name, PastedFormat Format, string Align, OpenKind Kind, bool Keep, bool Lines, bool Pre, int? Indent, int? FirstLine, bool BreakAfter);
}
