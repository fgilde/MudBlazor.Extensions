using System.Globalization;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Presentation;

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

    internal static AutoShape Selection(AutoShape shape, IReadOnlyList<SlideTextParagraphDto> paragraphs)
    {
        var body = shape.Text ?? new TextBody();
        var runs = body.Paragraphs.SelectMany(p => p.Runs).ToList();
        var selected = paragraphs.Select(dto =>
        {
            var source = At(body.Paragraphs, dto.Source) ?? new TextParagraph();
            return source with
            {
                Align = ParseAlign(dto.Align), Bullet = Bool(dto.Bullet), Runs = (dto.Runs ?? []).Select(r => Run(At(runs, r.Source), r, " ", null, source.EndFormat)).ToList()
            };
        });
        return new AutoShape
        {
            Placeholder = shape.Placeholder, InheritsFrom = shape.InheritsFrom, Master = shape.Master, ColorMap = shape.ColorMap, Source = shape.Source,
            Text = body with { Paragraphs = selected.ToList() }
        };
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

    internal static SlideTextParagraphDto[] FromHtml(string html) => HtmlPasteReader.Read(html).Select(p => new SlideTextParagraphDto
    {
        Runs = p.Runs.Select(r => new SlideTextRunDto
        {
            Text = r.Text, B = r.Format.Bold ? "1" : null, I = r.Format.Italic ? "1" : null, U = r.Format.Underline ? "1" : null, Color = r.Format.Color
        }).ToArray()
    }).ToArray();

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
}
