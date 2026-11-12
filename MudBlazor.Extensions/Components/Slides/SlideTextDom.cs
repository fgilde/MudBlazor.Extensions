using System.Globalization;
using System.Net;
using System.Text;
using MudBlazor.Extensions.Office.Presentation;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>A paragraph of the text editing DOM exchanged between <see cref="MudExSlideTextEditor"/> and the browser.</summary>
public sealed class SlideTextParagraphDto
{
    /// <summary>Index of the original paragraph this one came from, or null for a new paragraph.</summary>
    public int? Source { get; set; }

    /// <summary>The paragraph's own alignment (<c>left</c>, <c>center</c>, <c>right</c>, <c>justify</c>), or null to inherit it.</summary>
    public string Align { get; set; }

    /// <summary><c>1</c> for a bullet, <c>0</c> for none, null to inherit.</summary>
    public string Bullet { get; set; }

    /// <summary>The CSS the paragraph is shown with; only sent to the browser.</summary>
    public string Style { get; set; }

    /// <summary>The bullet or number shown before the paragraph, or null; only sent to the browser, never part of the text.</summary>
    public string Marker { get; set; }

    /// <summary>The runs in order.</summary>
    public SlideTextRunDto[] Runs { get; set; } = [];
}

/// <summary>A run of the text editing DOM. Format values are the run's own values; null inherits them.</summary>
public sealed class SlideTextRunDto
{
    /// <summary>Index of the original run (counted over all paragraphs) this one came from, or null for new text.</summary>
    public int? Source { get; set; }

    /// <summary>The text; a newline is a line break.</summary>
    public string Text { get; set; } = "";

    /// <summary>Bold: <c>1</c>, <c>0</c> or null.</summary>
    public string B { get; set; }

    /// <summary>Italic: <c>1</c>, <c>0</c> or null.</summary>
    public string I { get; set; }

    /// <summary>Underline: <c>1</c>, <c>0</c> or null.</summary>
    public string U { get; set; }

    /// <summary>Strikethrough: <c>1</c>, <c>0</c> or null.</summary>
    public string S { get; set; }

    /// <summary>Font size in points, or null.</summary>
    public string Size { get; set; }

    /// <summary>Colour in the format of <see cref="RunFormat.Color"/>, or null.</summary>
    public string Color { get; set; }

    /// <summary>Typeface, or null.</summary>
    public string Font { get; set; }

    /// <summary>The CSS the run is shown with; only sent to the browser.</summary>
    public string Style { get; set; }
}

internal static class SlideTextDom
{
    private const char ZeroWidthSpace = '\u200B';

    internal static SlideTextParagraphDto[] ToDom(AutoShape shape, Presentation presentation)
    {
        var index = 0;
        var paragraphs = shape.Text?.Paragraphs is { Count: > 0 } list ? list : [new TextParagraph()];
        var markers = shape.Markers();
        return paragraphs.Select((p, i) => new SlideTextParagraphDto
        {
            Source = shape.Text?.Paragraphs.Count > 0 ? i : null,
            Align = p.Align?.ToString().ToLowerInvariant(),
            Bullet = Flag(p.Bullet),
            Style = MudExSlideText.ParagraphStyle(shape, p, presentation),
            Marker = i < markers.Count ? markers[i] : null,
            Runs = p.Runs.Count == 0
                ? [Run(new TextRun("", p.EndFormat), null, shape, presentation)]
                : p.Runs.Select(r => Run(r, index++, shape, presentation)).ToArray()
        }).ToArray();
    }

    internal static TextBody FromDom(TextBody original, IReadOnlyList<SlideTextParagraphDto> paragraphs)
    {
        var body = original ?? new TextBody();
        var runs = body.Paragraphs.SelectMany(p => p.Runs).ToList();
        var built = paragraphs.Select(p => Paragraph(At(body.Paragraphs, p.Source), runs, p)).ToList();
        return original != null && built.SequenceEqual(original.Paragraphs, ReferenceEqualityComparer.Instance) ? original : body with { Paragraphs = built };
    }

    internal static bool All(IEnumerable<AutoShape> shapes, string key) => key switch
    {
        "bullet" => shapes.SelectMany(s => s.Text.Paragraphs).All(p => p.Bullet == true),
        "align" => false,
        _ => shapes.SelectMany(s => s.Text.Paragraphs.SelectMany(p => p.Runs).Where(r => r.Text != "\n").Select(r => s.EffectiveFormat(r)))
            .ToList() is { Count: > 0 } formats && formats.All(f => Get(f, key) == true)
    };

    internal static TextBody Format(TextBody body, string key, string value)
    {
        var paragraphs = body.Paragraphs.Select(p =>
        {
            var changed = key switch
            {
                "align" => p with { Align = ParseAlign(value) },
                "bullet" => p with { Bullet = value == "1" },
                _ => p with
                {
                    Runs = p.Runs.Select(r => Reuse(r, r with { Format = Set(r.Format, key, value) })).ToList(),
                    EndFormat = p.EndFormat == null && p.Runs.Count > 0 ? null : Set(p.EndFormat ?? new RunFormat(), key, value)
                }
            };
            return changed.Align == p.Align && changed.Bullet == p.Bullet && changed.EndFormat == p.EndFormat && changed.Runs.SequenceEqual(p.Runs, ReferenceEqualityComparer.Instance)
                ? p
                : changed;
        }).ToList();
        return paragraphs.SequenceEqual(body.Paragraphs, ReferenceEqualityComparer.Instance) ? body : body with { Paragraphs = paragraphs };
    }

    internal static SlideTextParagraphDto[] FromHtml(string html)
    {
        if (string.IsNullOrEmpty(html) || html.Length > HtmlTableFormat.MaxLength) return [];
        var reader = new PasteReader();
        HtmlTableFormat.Tokenize(html, reader);
        return reader.Result();
    }

    private static SlideTextRunDto Run(TextRun run, int? source, AutoShape shape, Presentation presentation) => new()
    {
        Source = source,
        Text = run.Text,
        B = Flag(run.Format.Bold),
        I = Flag(run.Format.Italic),
        U = Flag(run.Format.Underline),
        S = Flag(run.Format.Strike),
        Size = run.Format.SizePt?.ToString(CultureInfo.InvariantCulture),
        Color = run.Format.Color,
        Font = run.Format.Font,
        Style = MudExSlideText.RunStyle(shape, presentation, run)
    };

    private static TextParagraph Paragraph(TextParagraph source, List<TextRun> runs, SlideTextParagraphDto dto)
    {
        var template = source ?? new TextParagraph();
        var built = new List<TextRun>();
        foreach (var run in dto.Runs ?? [])
            foreach (var text in Lines((run.Text ?? "").Replace(ZeroWidthSpace.ToString(), "")))
                built.Add(Run(At(runs, run.Source), run, text, built.LastOrDefault(), template.EndFormat));

        var paragraph = template with { Align = ParseAlign(dto.Align), Bullet = Bool(dto.Bullet), Runs = built };
        return source != null && paragraph.Align == source.Align && paragraph.Bullet == source.Bullet && built.SequenceEqual(source.Runs, ReferenceEqualityComparer.Instance)
            ? source
            : paragraph;
    }

    private static TextRun Run(TextRun source, SlideTextRunDto dto, string text, TextRun previous, RunFormat end)
    {
        var format = new RunFormat(Bool(dto.B), Bool(dto.I), Bool(dto.U), Bool(dto.S), Size(dto.Size), Blank(dto.Color), Blank(dto.Font));
        if (source != null)
            return Reuse(source, source with { Text = text, Format = format, FieldType = text == source.Text ? source.FieldType : null });
        if (format == new RunFormat())
            return previous != null ? previous with { Text = text, FieldType = null } : new TextRun(text, end);
        return new TextRun(text, format);
    }

    private static TextRun Reuse(TextRun original, TextRun changed) => changed == original ? original : changed;

    private static IEnumerable<string> Lines(string text)
    {
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n') continue;
            if (i > start) yield return text[start..i];
            yield return "\n";
            start = i + 1;
        }

        if (start < text.Length) yield return text[start..];
    }

    private static RunFormat Set(RunFormat format, string key, string value) => key switch
    {
        "b" => format with { Bold = Bool(value) },
        "i" => format with { Italic = Bool(value) },
        "u" => format with { Underline = Bool(value) },
        "s" => format with { Strike = Bool(value) },
        "size" => format with { SizePt = Size(value) },
        "color" => format with { Color = Blank(value) },
        "font" => format with { Font = Blank(value) },
        _ => format
    };

    private static bool? Get(RunFormat format, string key) => key switch
    {
        "b" => format.Bold,
        "i" => format.Italic,
        "u" => format.Underline,
        "s" => format.Strike,
        _ => null
    };

    private static T At<T>(IReadOnlyList<T> list, int? index) where T : class => index is { } i && i >= 0 && i < list.Count ? list[i] : null;

    private static TextAlign? ParseAlign(string value) => Enum.TryParse<TextAlign>(value, true, out var align) && Enum.IsDefined(align) ? align : null;

    private static bool? Bool(string value) => value switch
    {
        "1" => true,
        "0" => false,
        _ => null
    };

    private static string Flag(bool? value) => value switch
    {
        true => "1",
        false => "0",
        _ => null
    };

    private static double? Size(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var size) && size is >= 1 and <= 4000 ? size : null;

    private static string Blank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private sealed class PasteReader : IHtmlTokenSink
    {
        private static readonly HashSet<string> Blocks =
            ["p", "div", "li", "ul", "ol", "h1", "h2", "h3", "h4", "h5", "h6", "tr", "table", "blockquote", "pre", "section", "article", "header", "footer", "dt", "dd"];

        private static readonly HashSet<string> Empty = ["br", "img", "hr", "meta", "link", "input", "col", "wbr", "area", "base", "source", "param", "embed"];

        private readonly List<List<SlideTextRunDto>> _paragraphs = [[]];
        private readonly List<(string Name, PasteFormat Format)> _open = [];
        private bool _space = true;

        public bool Done => false;

        private PasteFormat Current => _open.Count == 0 ? default : _open[^1].Format;

        public SlideTextParagraphDto[] Result()
        {
            foreach (var paragraph in _paragraphs.Where(p => p.Count > 0))
            {
                paragraph[^1].Text = paragraph[^1].Text.TrimEnd(' ');
                if (paragraph[^1].Text.Length == 0) paragraph.RemoveAt(paragraph.Count - 1);
            }

            return _paragraphs.Where(p => p.Count > 0).Select(p => new SlideTextParagraphDto { Runs = p.ToArray() }).ToArray();
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
            if (Empty.Contains(name)) return;
            var format = name switch
            {
                "b" or "strong" => Current with { Bold = true },
                "i" or "em" => Current with { Italic = true },
                "u" or "ins" => Current with { Underline = true },
                _ => Current
            };
            _open.Add((name, attributes.TryGetValue("style", out var css) ? Css(format, css) : format));
        }

        public void End(string name)
        {
            var at = _open.FindLastIndex(o => o.Name == name);
            if (at >= 0) _open.RemoveRange(at, _open.Count - at);
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
            if (_paragraphs[^1].Count > 0) _paragraphs.Add([]);
            _space = true;
        }

        private void Add(string text)
        {
            if (text.Length == 0) return;
            var f = Current;
            var runs = _paragraphs[^1];
            var run = new SlideTextRunDto { Text = text, B = f.Bold ? "1" : null, I = f.Italic ? "1" : null, U = f.Underline ? "1" : null, Color = f.Color };
            if (runs.Count > 0 && runs[^1] is { } last && last.B == run.B && last.I == run.I && last.U == run.U && last.Color == run.Color)
                last.Text += text;
            else
                runs.Add(run);
        }

        private static PasteFormat Css(PasteFormat format, string css)
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

    private readonly record struct PasteFormat(bool Bold, bool Italic, bool Underline, string Color);
}
