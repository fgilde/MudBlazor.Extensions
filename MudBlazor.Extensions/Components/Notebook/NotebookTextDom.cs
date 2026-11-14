using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

/// <summary>A paragraph of the text editing DOM exchanged between <see cref="MudExNotebookTextEditor"/> and the browser.</summary>
public sealed class NotebookTextParagraphDto
{
    /// <summary>Index of the original paragraph this one came from, or null for a new paragraph.</summary>
    public int? Source { get; set; }

    /// <summary>The list kind: <c>bullet</c>, <c>number</c>, <c>todo</c> or null for none.</summary>
    public string List { get; set; }

    /// <summary>The indent level, 0 to 8.</summary>
    public int Indent { get; set; }

    /// <summary>True if the check box of a to-do item is ticked.</summary>
    public bool Checked { get; set; }

    /// <summary>The alignment: <c>left</c>, <c>center</c> or <c>right</c>.</summary>
    public string Align { get; set; }

    /// <summary>The heading level, 0 for normal text to 3.</summary>
    public int Heading { get; set; }

    /// <summary>The runs in order.</summary>
    public NotebookTextRunDto[] Runs { get; set; } = [];
}

/// <summary>A run of the text editing DOM of <see cref="MudExNotebookTextEditor"/>.</summary>
public sealed class NotebookTextRunDto
{
    /// <summary>Index of the original run (counted over all paragraphs) this one came from, or null for new text.</summary>
    public int? Source { get; set; }

    /// <summary>The text; a newline is a line break.</summary>
    public string Text { get; set; } = "";

    /// <summary>Bold.</summary>
    public bool B { get; set; }

    /// <summary>Italic.</summary>
    public bool I { get; set; }

    /// <summary>Underlined.</summary>
    public bool U { get; set; }

    /// <summary>Struck through.</summary>
    public bool S { get; set; }

    /// <summary>The font size in points, or null for the default.</summary>
    public double? Size { get; set; }

    /// <summary>The text colour as <c>#RRGGBB</c>, or null.</summary>
    public string Color { get; set; }

    /// <summary>The highlight colour as <c>#RRGGBB</c>, or null.</summary>
    public string Highlight { get; set; }

    /// <summary>The font family, or null.</summary>
    public string Font { get; set; }

    /// <summary>The link target, or null; only <c>http</c>, <c>https</c> and <c>mailto</c> are kept.</summary>
    public string Link { get; set; }
}

internal sealed record NotebookTextState(
    bool Bold, bool Italic, bool Underline, bool Strike, NoteListKind? List, NoteAlign? Align, NoteHeading? Heading, string Font, double? SizePt);

internal static class NotebookTextDom
{
    private static readonly (string Prefix, NoteListKind List)[] AutoLists = [("* ", NoteListKind.Bullet), ("1. ", NoteListKind.Number), ("[] ", NoteListKind.Todo)];

    internal static NotebookTextParagraphDto[] ToDom(IReadOnlyList<NoteParagraph> paragraphs)
    {
        var index = 0;
        var list = paragraphs is { Count: > 0 } ? paragraphs : [new NoteParagraph()];
        return list.Select((p, i) => new NotebookTextParagraphDto
        {
            Source = ReferenceEquals(list, paragraphs) ? i : null,
            List = ListName(p.List),
            Indent = p.Indent,
            Checked = p.Checked,
            Align = p.Align.ToString().ToLowerInvariant(),
            Heading = (int)p.Heading,
            Runs = p.Runs.Select(r => new NotebookTextRunDto
            {
                Source = index++, Text = r.Text, B = r.Format.Bold, I = r.Format.Italic, U = r.Format.Underline, S = r.Format.Strike, Size = r.Format.SizePt,
                Color = r.Format.Color, Highlight = r.Format.Highlight, Font = r.Format.Font, Link = r.Format.Link
            }).ToArray()
        }).ToArray();
    }

    internal static IReadOnlyList<NoteParagraph> FromDom(IReadOnlyList<NoteParagraph> original, IReadOnlyList<NotebookTextParagraphDto> paragraphs)
    {
        original ??= [];
        var runs = original.SelectMany(p => p.Runs).ToList();
        var built = (paragraphs ?? []).Where(p => p != null).Select(p => Paragraph(At(original, p.Source), runs, p)).ToList();
        return built.SequenceEqual(original) ? original : built;
    }

    internal static IReadOnlyList<NoteParagraph> Format(IReadOnlyList<NoteParagraph> paragraphs, string key, string value)
    {
        var runs = paragraphs.SelectMany(p => p.Runs).Where(r => r.Text.Length > 0).ToList();
        value ??= key is "b" or "i" or "u" or "s" ? (runs.Count > 0 && runs.All(r => Flag(r.Format, key)) ? "0" : "1") : null;
        if (value == null) return paragraphs;
        var changed = paragraphs.Select(p => Format(p, key, value)).ToList();
        return changed.SequenceEqual(paragraphs) ? paragraphs : changed;
    }

    internal static NotebookTextState State(IReadOnlyList<NoteParagraph> paragraphs)
    {
        if (paragraphs.Count == 0) return null;
        var formats = paragraphs.SelectMany(p => p.Runs).Where(r => r.Text.Length > 0).Select(r => r.Format).ToList();
        if (formats.Count == 0) formats = paragraphs.SelectMany(p => p.Runs).Select(r => r.Format).ToList();
        bool All(Func<NoteRunFormat, bool> flag) => formats.Count > 0 && formats.All(flag);
        T? Same<T>(IEnumerable<T> values) where T : struct => values.Distinct().ToList() is [var single] ? single : null;
        return new NotebookTextState(All(f => f.Bold), All(f => f.Italic), All(f => f.Underline), All(f => f.Strike), Same(paragraphs.Select(p => p.List)),
            Same(paragraphs.Select(p => p.Align)), Same(paragraphs.Select(p => p.Heading)), formats.Select(f => f.Font).Distinct().ToList() is [var font] ? font : null,
            Same(formats.Select(f => f.SizePt ?? 0)) is { } size && size > 0 ? size : null);
    }

    internal static NotebookTextParagraphDto[] FromHtml(string html) => HtmlPasteReader.Read(html).Select(p => new NotebookTextParagraphDto
    {
        List = p.List switch { PastedList.Bullet => "bullet", PastedList.Number => "number", _ => null },
        Indent = Math.Min(p.Depth, NoteParagraph.MaxIndent),
        Runs = p.Runs.Select(r => new NotebookTextRunDto
        {
            Text = r.Text, B = r.Format.Bold, I = r.Format.Italic, U = r.Format.Underline, Color = Color(r.Format.Color),
            Link = NoteValues.IsSafeLink(r.Format.Link) ? r.Format.Link : null
        }).ToArray()
    }).ToArray();

    internal static string ListName(NoteListKind list) => list == NoteListKind.None ? null : list.ToString().ToLowerInvariant();

    private static NoteParagraph Paragraph(NoteParagraph source, List<NoteRun> runs, NotebookTextParagraphDto dto)
    {
        var built = Merge((dto.Runs ?? []).Where(r => r != null).Select(r => Run(At(runs, r.Source), r)).Where(r => r.Text.Length > 0));
        var paragraph = (source ?? new NoteParagraph()) with
        {
            Runs = built,
            List = ParseList(dto.List),
            Indent = Math.Clamp(dto.Indent, 0, NoteParagraph.MaxIndent),
            Checked = dto.Checked,
            Align = ParseAlign(dto.Align),
            Heading = (NoteHeading)Math.Clamp(dto.Heading, 0, 3)
        };
        if (source != null && paragraph with { Runs = [] } == source with { Runs = [] } && built.SequenceEqual(Merge(source.Runs)))
            return source;
        if (paragraph.List != NoteListKind.None || built is not [{ } first, ..]
            || AutoLists.FirstOrDefault(a => first.Text.StartsWith(a.Prefix, StringComparison.Ordinal)) is not ({ } prefix, var kind))
            return paragraph;
        var rest = first.Text[prefix.Length..];
        return paragraph with { List = kind, Runs = rest.Length > 0 ? [first with { Text = rest }, .. built.Skip(1)] : built.Skip(1).ToList() };
    }

    private static NoteRun Run(NoteRun source, NotebookTextRunDto dto)
    {
        var format = new NoteRunFormat(dto.B, dto.I, dto.U, dto.S, Size(dto.Size), Color(dto.Color), Color(dto.Highlight), NoteValues.IsFont(dto.Font) ? dto.Font : null,
            NoteValues.IsSafeLink(dto.Link) ? dto.Link : null);
        var text = (dto.Text ?? "").Replace("​", "").Replace("\r", "");
        return source != null ? source with { Text = text, Format = format } : new NoteRun(text, format);
    }

    private static List<NoteRun> Merge(IEnumerable<NoteRun> runs)
    {
        var merged = new List<NoteRun>();
        foreach (var run in runs)
            if (merged.Count > 0 && merged[^1].Format == run.Format && run.Extra == null)
                merged[^1] = merged[^1] with { Text = merged[^1].Text + run.Text };
            else
                merged.Add(run);
        return merged;
    }

    private static NoteParagraph Format(NoteParagraph paragraph, string key, string value) => key switch
    {
        "align" => paragraph with { Align = ParseAlign(value) },
        "heading" => int.TryParse(value, out var level) ? paragraph with { Heading = (NoteHeading)Math.Clamp(level, 0, 3) } : paragraph,
        "list" => paragraph with { List = ParseList(value) },
        "indent" => paragraph with { Indent = Math.Clamp(paragraph.Indent + (value == "-1" ? -1 : 1), 0, NoteParagraph.MaxIndent) },
        _ => paragraph with { Runs = paragraph.Runs.Select(r => r with { Format = Format(r.Format, key, value) }).ToList() }
    };

    private static NoteRunFormat Format(NoteRunFormat format, string key, string value) => key switch
    {
        "b" => format with { Bold = value == "1" },
        "i" => format with { Italic = value == "1" },
        "u" => format with { Underline = value == "1" },
        "s" => format with { Strike = value == "1" },
        "size" when double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var size) && Size(size) != null
            => format with { SizePt = size },
        "color" when NoteValues.IsColor(value) => format with { Color = value },
        "highlight" when value == "" || NoteValues.IsColor(value) => format with { Highlight = Color(value) },
        "font" when NoteValues.IsFont(value) => format with { Font = value },
        _ => format
    };

    private static bool Flag(NoteRunFormat format, string key) => key switch
    {
        "b" => format.Bold,
        "i" => format.Italic,
        "u" => format.Underline,
        _ => format.Strike
    };

    private static NoteListKind ParseList(string value) =>
        Enum.TryParse<NoteListKind>(value, true, out var list) && Enum.IsDefined(list) && !int.TryParse(value, out _) ? list : NoteListKind.None;

    private static NoteAlign ParseAlign(string value) =>
        Enum.TryParse<NoteAlign>(value, true, out var align) && Enum.IsDefined(align) && !int.TryParse(value, out _) ? align : NoteAlign.Left;

    private static double? Size(double? size) => size is { } value && double.IsFinite(value) && value > 0 && value <= 1000 ? value : null;

    private static string Color(string value) => NoteValues.IsColor(value) ? value : null;

    private static T At<T>(IReadOnlyList<T> list, int? index) where T : class => index is { } i && i >= 0 && i < list.Count ? list[i] : null;
}
