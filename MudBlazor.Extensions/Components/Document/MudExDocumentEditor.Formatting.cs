using System.Globalization;
using System.Text.RegularExpressions;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Document;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components;

public partial class MudExDocumentEditor
{
    private const int SpaceTwips = 240;
    private static readonly Regex ThemeColor = new("^theme:[A-Za-z0-9]+( shade=[0-9A-Fa-f]{2})?( tint=[0-9A-Fa-f]{2})?$", RegexOptions.CultureInvariant);
    private static readonly double[] FontSteps = [8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72];
    private static readonly IReadOnlyList<MudExColor> HighlightPalette = [.. HtmlReader.Highlights.Values.Select(v => (MudExColor)v)];
    private static readonly string[] SurfaceShortcuts = DocumentShortcuts.Keys();

    private static readonly Dictionary<string, (string Text, Func<RunFormat, bool> IsOn, Func<RunFormat, bool, RunFormat> Set)> Toggles = new()
    {
        ["bold"] = ("Bold", f => f.Bold == true, (f, on) => f with { Bold = on }),
        ["italic"] = ("Italic", f => f.Italic == true, (f, on) => f with { Italic = on }),
        ["underline"] = ("Underline", f => f.Underline is not (null or "none"), (f, on) => f with { Underline = on ? "single" : "none" }),
        ["doubleUnderline"] = ("Double underline", f => f.Underline == "double", (f, on) => f with { Underline = on ? "double" : "none" }),
        ["strike"] = ("Strikethrough", f => f.Strike == true, (f, on) => f with { Strike = on }),
        ["subscript"] = ("Subscript", f => f.VerticalAlign == VerticalAlign.Subscript, (f, on) => f with { VerticalAlign = on ? VerticalAlign.Subscript : VerticalAlign.Baseline }),
        ["superscript"] = ("Superscript", f => f.VerticalAlign == VerticalAlign.Superscript, (f, on) => f with { VerticalAlign = on ? VerticalAlign.Superscript : VerticalAlign.Baseline })
    };

    private static readonly (string Id, string Text, string Icon, string Shortcut, string Property)[] FontToggles =
    [
        ("wd-bold", "Bold", Icons.Material.Filled.FormatBold, "Ctrl+B", "bold"),
        ("wd-italic", "Italic", Icons.Material.Filled.FormatItalic, "Ctrl+I", "italic"),
        ("wd-underline", "Underline", Icons.Material.Filled.FormatUnderlined, "Ctrl+U", "underline"),
        ("wd-strikethrough", "Strikethrough", Icons.Material.Filled.FormatStrikethrough, null, "strike"),
        ("wd-subscript", "Subscript", Icons.Material.Filled.Subscript, "Ctrl+=", "subscript"),
        ("wd-superscript", "Superscript", Icons.Material.Filled.Superscript, "Ctrl+Shift+=", "superscript")
    ];

    private static readonly (string Id, string Text, string Icon, string Shortcut, ListTemplate List)[] ListToggles =
    [
        ("wd-bullets", "Bullets", Icons.Material.Filled.FormatListBulleted, "Ctrl+Shift+L", ListTemplate.Bullet),
        ("wd-numbering", "Numbering", Icons.Material.Filled.FormatListNumbered, null, ListTemplate.Decimal),
        ("wd-multilevel", "Multilevel List", Icons.Material.Filled.ViewList, null, ListTemplate.Multilevel)
    ];

    private static readonly (string Id, string Text, string Icon, string Shortcut, ParagraphAlignment Align)[] AlignToggles =
    [
        ("wd-align-left", "Align Left", Icons.Material.Filled.FormatAlignLeft, "Ctrl+L", ParagraphAlignment.Left),
        ("wd-align-center", "Center", Icons.Material.Filled.FormatAlignCenter, "Ctrl+E", ParagraphAlignment.Center),
        ("wd-align-right", "Align Right", Icons.Material.Filled.FormatAlignRight, "Ctrl+R", ParagraphAlignment.Right),
        ("wd-align-justify", "Justify", Icons.Material.Filled.FormatAlignJustify, "Ctrl+J", ParagraphAlignment.Justify)
    ];

    private static readonly (string Id, double Lines)[] LineSpacings =
        [("wd-line-spacing-100", 1), ("wd-line-spacing-115", 1.15), ("wd-line-spacing-150", 1.5), ("wd-line-spacing-200", 2), ("wd-line-spacing-250", 2.5), ("wd-line-spacing-300", 3)];

    private static readonly RunFormat RunDefaults = new()
    {
        Bold = false, Italic = false, Strike = false, DoubleStrike = false, Caps = false, SmallCaps = false, Hidden = false, Underline = "none", VerticalAlign = VerticalAlign.Baseline
    };

    private static readonly ParagraphFormat ParagraphDefaults = new()
    {
        Alignment = ParagraphAlignment.Left, IndentLeft = 0, IndentRight = 0, IndentFirstLine = 0, SpacingBefore = 0, SpacingAfter = 0, Line = new LineSpacing(240, LineRule.Auto),
        KeepWithNext = false, KeepLines = false, PageBreakBefore = false, WidowControl = false
    };

    private (TextDocument Document, int Version, DocumentRange Selection, RunFormat Caret) _formatKey;
    private SelectionFormat _format;
    private (DocumentStyles Styles, DocumentTheme Theme, IReadOnlyList<StyleItem> Items) _gallery;
    private MudExColor _fontColor = "#FF0000";
    private string _highlight = "yellow";

    internal SelectionFormat Format
    {
        get
        {
            if (!ShowRibbon || IsReadOnly || _surface == null || Selection.Start == null)
                return null;
            var key = (_document, _document.Version, Selection, _surface.CaretFormat);
            if (key != _formatKey)
                (_formatKey, _format) = (key, FormatAt(Selection));
            return _format;
        }
    }

    private RibbonView View => !_collapsed && Format is { Run: var r, Paragraph: var p } f
        ? new(r.Bold, r.Italic, r.Underline, r.Strike, r.VerticalAlign, r.Font, r.SizePt, r.Color, r.Highlight, p.Alignment, f.List, p.Line, f.StyleId, r.StyleId, p.SpacingBefore,
            p.SpacingAfter, _document.Styles, ObjectsOf(Selection))
        : default;

    private IReadOnlyList<string> FontItems => Format?.Run.Font is { } font && !MudExNotebookEditor.Fonts.Contains(font) ? [font, .. MudExNotebookEditor.Fonts] : MudExNotebookEditor.Fonts;

    internal Task ToggleRunAsync(string property) =>
        Toggles.TryGetValue(property, out var toggle)
            ? FormatRunAsync(range => FormatAt(range) is not { } format ? null : toggle.IsOn(format.Run) ? f => toggle.Set(f, false) : f => toggle.Set(f, true), toggle.Text)
            : Task.CompletedTask;

    internal Task SetRunAsync(Func<RunFormat, RunFormat> change, string description) => FormatRunAsync(_ => change, description);

    internal Task SetFontAsync(string font) =>
        font?.Trim() is { Length: > 0 and <= 64 } name ? SetRunAsync(f => f with { Font = name }, "Font") : Task.CompletedTask;

    internal Task SetSizeAsync(double size) => double.IsFinite(size) && size > 0 ? SetRunAsync(f => f with { SizePt = ClampSize(size) }, "Font size") : Task.CompletedTask;

    internal Task GrowFontAsync(int direction) => ResizeAsync(size => direction >= 0
        ? FontSteps.Where(s => s > size).DefaultIfEmpty(Math.Floor(size / 10) * 10 + 10).First()
        : FontSteps.Where(s => s < size).DefaultIfEmpty(Math.Ceiling(size) - 1).Last(), direction >= 0 ? "Grow font" : "Shrink font");

    internal Task SetColorAsync(string color) =>
        (color == "auto" || ThemeColor.IsMatch(color ?? "") ? color : DocumentCss.Color(color)) is { } valid ? SetRunAsync(f => f with { Color = valid }, "Font color") : Task.CompletedTask;

    internal Task SetHighlightAsync(string name) =>
        name == "none" || HtmlReader.Highlights.ContainsKey(name ?? "") ? SetRunAsync(f => f with { Highlight = name }, "Highlight") : Task.CompletedTask;

    internal Task ClearFormattingAsync(bool paragraphToo) => RunAsync(range =>
    {
        if (!Live(range))
            return;
        Func<RunFormat, RunFormat> clear = paragraphToo ? f => new RunFormat { StyleId = f.StyleId == InlineEdit.HyperlinkStyle ? f.StyleId : null } : f => new RunFormat { StyleId = f.StyleId };
        using (_document.Undo.BeginTransaction("Clear formatting"))
        {
            ChangeRuns(range, clear, "Clear formatting");
            if (!paragraphToo)
                return;
            var ids = ParagraphIds(range);
            _document.Execute(new ApplyParagraphStyleCommand(ids, "Normal"), range);
            _document.Execute(new SetParagraphFormatCommand(ids, p => p with
            {
                Alignment = null, IndentLeft = null, IndentRight = null, IndentFirstLine = null, SpacingBefore = null, SpacingAfter = null, Line = null
            }, "Clear formatting"), range);
            if (ids.Where(id => _document.FindBlock(id) is Paragraph { Length: 0 }).ToList() is { Count: > 0 } empty)
                _document.Execute(new SetMarkFormatCommand(empty, clear, "Clear formatting"), range);
        }
    });

    internal Task SetParagraphAsync(Func<ParagraphFormat, ParagraphFormat> change, string description) => FormatParagraphsAsync(_ => change, description);

    internal Task ToggleListAsync(ListTemplate template) => ParagraphCommandAsync(ids => new ApplyListCommand(ids, template));

    internal Task IndentAsync(int delta) => ParagraphCommandAsync(ids => new ChangeListLevelCommand(ids, delta));

    internal Task ApplyStyleAsync(string styleId) => ParagraphCommandAsync(ids => new ApplyParagraphStyleCommand(ids, styleId));

    internal IReadOnlyList<StyleItem> GalleryItems
    {
        get
        {
            if (_document == null)
                return [];
            if (!ReferenceEquals(_document.Styles, _gallery.Styles) || !ReferenceEquals(_document.Theme, _gallery.Theme))
                _gallery = (_document.Styles, _document.Theme, [.. _document.Styles.Gallery.Select(s => new StyleItem(s.Id, DisplayName(s), s.Type, PreviewCss(s)))]);
            return _gallery.Items;
        }
    }

    private StyleItem SelectedStyle => Format is { } format
        ? GalleryItems.FirstOrDefault(i => i.Id == format.Run.StyleId) ?? GalleryItems.FirstOrDefault(i => i.Id == format.StyleId)
        : null;

    private string UpdateStyleText => TryLocalize("Update {0} to Match Selection", Format?.StyleId is { } id ? GalleryItems.FirstOrDefault(i => i.Id == id)?.Name ?? id : TryLocalize("Style"));

    internal Task PickStyleAsync(StyleItem item) => item?.Type switch
    {
        StyleType.Paragraph => ApplyStyleAsync(item.Id),
        StyleType.Character => ApplyCharacterStyleAsync(item.Id),
        _ => Task.CompletedTask
    };

    internal Task ApplyCharacterStyleAsync(string styleId) => RunAsync(range =>
    {
        if (!Live(range))
            return;
        _document.Execute(new ApplyCharacterStyleCommand(range, styleId), range);
        if (range.IsEmpty)
            _surface.SetCaretFormat(InsertionFormat(range) with { StyleId = styleId });
    });

    internal Task UpdateStyleFromSelectionAsync() => RunAsync(range =>
    {
        if (Live(range) && FormatAt(range)?.StyleId is { } styleId)
            _document.Execute(new UpdateStyleFromSelectionCommand(ParagraphIds(range)[0], styleId), range);
    });

    internal async Task OpenFontDialogAsync()
    {
        var (range, format) = await SelectionFormatAsync();
        if (format == null)
            return;
        var reference = await DialogService.ShowExAsync<MudExDocumentFontDialog>(TryLocalize("Font"), new DialogParameters
        {
            { nameof(MudExDocumentFontDialog.Format), format.Run },
            { nameof(MudExDocumentFontDialog.Localizer), Localizer }
        }, DialogOptionsEx.EditorDialogOptions(DialogOptions));
        var result = await reference.Result;
        if (await ReselectAsync(range) && result is { Canceled: false, Data: Func<RunFormat, RunFormat> change })
            await SetRunAsync(change, "Font");
        await FocusSurfaceAsync();
    }

    internal async Task OpenParagraphDialogAsync()
    {
        var (range, format) = await SelectionFormatAsync();
        if (format == null)
            return;
        var reference = await DialogService.ShowExAsync<MudExDocumentParagraphDialog>(TryLocalize("Paragraph"), new DialogParameters
        {
            { nameof(MudExDocumentParagraphDialog.Format), format.Paragraph },
            { nameof(MudExDocumentParagraphDialog.Metric), DocumentUnits.Metric(CultureInfo.CurrentCulture) },
            { nameof(MudExDocumentParagraphDialog.Localizer), Localizer }
        }, DialogOptionsEx.EditorDialogOptions(DialogOptions));
        var result = await reference.Result;
        if (await ReselectAsync(range) && result is { Canceled: false, Data: Func<ParagraphFormat, ParagraphFormat> change })
            await SetParagraphAsync(change, "Paragraph");
        await FocusSurfaceAsync();
    }

    private async Task<(DocumentRange Range, SelectionFormat Format)> SelectionFormatAsync()
    {
        (DocumentRange, SelectionFormat) selection = default;
        await RunAsync(range => selection = Live(range) && FormatAt(range) is { } format ? (range, DialogFormat(range, format)) : default);
        return selection;
    }

    private SelectionFormat DialogFormat(DocumentRange range, SelectionFormat format)
    {
        var paragraphs = new FormatMerge<ParagraphFormat>();
        foreach (var id in ParagraphIds(range))
            paragraphs.Add(_document.EffectiveParagraphFormat((Paragraph)_document.FindBlock(id)).Over(ParagraphDefaults));
        var runs = new FormatMerge<RunFormat>();
        var (start, end) = range.Normalize(_document);
        var (first, from) = _document.Resolve(start);
        var (last, to) = _document.Resolve(end);
        var counted = false;
        foreach (var paragraph in range.IsEmpty ? Enumerable.Empty<Paragraph>() : _document.ParagraphsIn(range))
            foreach (var inline in InlineEdit.Split(paragraph.Inlines, paragraph.Id == first.Id ? from : 0, paragraph.Id == last.Id ? to : paragraph.Length).Middle.Where(i => i is TextRun or Tab))
            {
                runs.Add(_document.EffectiveRunFormat(paragraph, inline).Over(RunDefaults));
                counted = true;
            }

        return format with { Run = counted ? runs.Result() : format.Run.Over(RunDefaults), Paragraph = paragraphs.Result() };
    }

    private async Task<bool> ReselectAsync(DocumentRange range)
    {
        if (IsReadOnly || _surface == null || _document == null || !Live(range))
            return false;
        await _surface.SelectAsync(range, false);
        return true;
    }

    private Task FocusSurfaceAsync() => _surface?.FocusAsync() ?? Task.CompletedTask;

    private static string DisplayName(DocumentStyle style) =>
        string.Join(' ', (style.Name ?? style.Id ?? "").Split(' ').Select(w => w.Length > 0 ? char.ToUpperInvariant(w[0]) + w[1..] : w));

    private string PreviewCss(DocumentStyle style)
    {
        try
        {
            var styles = StyleDefinitions.With(_document.Styles, style.Id, style.Type);
            var paragraph = new Paragraph { Format = new ParagraphFormat { StyleId = style.Type == StyleType.Paragraph ? style.Id : null } };
            var run = new TextRun("") { Format = new RunFormat { StyleId = style.Type == StyleType.Character ? style.Id : null } };
            var format = EffectiveFormat.Run(styles, _document.Theme, null, paragraph, run);
            return DocumentCss.Run(format with { SizePt = Math.Min(format.SizePt ?? 11, 16) }, new RunFormat(), true);
        }
        catch (ArgumentException)
        {
            return "";
        }
    }

    private Task AlignAsync(ParagraphAlignment alignment) => FormatParagraphsAsync(range =>
    {
        var target = FormatAt(range)?.Paragraph.Alignment == alignment && alignment != ParagraphAlignment.Left ? ParagraphAlignment.Left : alignment;
        return p => p with { Alignment = target };
    }, "Align");

    private Task SetLineSpacingAsync(double lines) => SetParagraphAsync(p => p with { Line = Spacing(lines) }, "Line spacing");

    private static LineSpacing Spacing(double lines) => new((int)Math.Round(lines * 240), LineRule.Auto);

    private Task ToggleSpaceAsync(bool before) => FormatParagraphsAsync(range =>
    {
        var current = before ? FormatAt(range)?.Paragraph.SpacingBefore : FormatAt(range)?.Paragraph.SpacingAfter;
        var value = (current ?? 0) == 0 ? SpaceTwips : 0;
        return before ? p => p with { SpacingBefore = value } : p => p with { SpacingAfter = value };
    }, before ? "Space before" : "Space after");

    private Task ResizeAsync(Func<double, double> next, string description) => FormatRunAsync(range =>
        (FormatAt(range)?.Run.SizePt ?? FirstSize(range)) is { } size ? f => f with { SizePt = ClampSize(next(size)) } : null, description);

    private Task PickColorAsync(MudExColor color)
    {
        _fontColor = color;
        return SetColorAsync(MudExNotebookEditor.Hex(color));
    }

    private Task PickHighlightAsync(MudExColor color)
    {
        var hex = MudExNotebookEditor.Hex(color);
        if (HtmlReader.Highlights.FirstOrDefault(h => h.Value == hex).Key is not { } name)
            return Task.CompletedTask;
        _highlight = name;
        return SetHighlightAsync(name);
    }

    private bool IsOn(string property) => Format is { } format && Toggles[property].IsOn(format.Run);

    private bool IsAligned(ParagraphAlignment alignment) => Format is { } format && (format.Paragraph.Alignment ?? ParagraphAlignment.Left) == alignment;

    private bool SpaceIsSet(bool before) => (before ? Format?.Paragraph.SpacingBefore : Format?.Paragraph.SpacingAfter) is not (null or 0);

    private Task OnShortcutAsync(string key) => DocumentShortcuts.ActionOf(key) is { } action ? InvokeAsync(() => RunShortcutAsync(action)) : Task.CompletedTask;

    private Task RunShortcutAsync(string action) => action switch
    {
        "save" => SaveFromKeyAsync(),
        "find" or "replace" => OpenFindAsync(action == "replace"),
        "clearCharacter" => ClearFormattingAsync(false),
        "sizeUp" => ResizeAsync(size => size + 1, "Font size"),
        "sizeDown" => ResizeAsync(size => size - 1, "Font size"),
        "grow" => GrowFontAsync(1),
        "shrink" => GrowFontAsync(-1),
        "alignLeft" => AlignAsync(ParagraphAlignment.Left),
        "alignCenter" => AlignAsync(ParagraphAlignment.Center),
        "alignRight" => AlignAsync(ParagraphAlignment.Right),
        "alignJustify" => AlignAsync(ParagraphAlignment.Justify),
        "indent" => IndentAsync(1),
        "outdent" => IndentAsync(-1),
        "bullets" => ToggleListAsync(ListTemplate.Bullet),
        "lineSpacing1" => SetLineSpacingAsync(1),
        "lineSpacing15" => SetLineSpacingAsync(1.5),
        "lineSpacing2" => SetLineSpacingAsync(2),
        "spaceBefore" => ToggleSpaceAsync(true),
        "heading1" or "heading2" or "heading3" => ApplyStyleAsync("Heading" + action[^1]),
        "normal" => ApplyStyleAsync("Normal"),
        "fontDialog" => OpenFontDialogAsync(),
        "link" => OpenLinkDialogAsync(),
        _ => ToggleRunAsync(action)
    };

    private Task FormatRunAsync(Func<DocumentRange, Func<RunFormat, RunFormat>> changeAt, string description) => RunAsync(range =>
    {
        if (Live(range) && changeAt(range) is { } change)
            ChangeRuns(range, change, description);
    });

    private void ChangeRuns(DocumentRange range, Func<RunFormat, RunFormat> change, string description)
    {
        if (range.IsEmpty)
            _surface.SetCaretFormat(change(InsertionFormat(range)));
        else
            _document.Execute(new SetRunFormatCommand(range, change, description), range);
    }

    private Task FormatParagraphsAsync(Func<DocumentRange, Func<ParagraphFormat, ParagraphFormat>> changeAt, string description) =>
        ParagraphCommandAsync((ids, range) => new SetParagraphFormatCommand(ids, changeAt(range), description));

    private Task ParagraphCommandAsync(Func<IReadOnlyList<string>, TextDocumentCommand> command) => ParagraphCommandAsync((ids, _) => command(ids));

    private Task ParagraphCommandAsync(Func<IReadOnlyList<string>, DocumentRange, TextDocumentCommand> command) => RunAsync(range =>
    {
        if (Live(range))
            _document.Execute(command(ParagraphIds(range), range), range);
    });

    private SelectionFormat FormatAt(DocumentRange range)
    {
        try
        {
            var format = _document.FormatOf(range);
            return range.IsEmpty && _surface?.CaretFormat is { } caret
                ? format with { Run = _document.EffectiveRunFormat(_document.Resolve(range.End).Paragraph, new TextRun("") { Format = caret }) }
                : format;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private bool Live(DocumentRange range)
    {
        if (range.Start == null || range.End == null)
            return false;
        try
        {
            _document.Resolve(range.Start);
            _document.Resolve(range.End);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private RunFormat InsertionFormat(DocumentRange range)
    {
        var (paragraph, offset) = _document.Resolve(range.End);
        return _surface.CaretFormat ?? InlineEdit.InsertionStyle(paragraph, offset).Format;
    }

    private double? FirstSize(DocumentRange range)
    {
        var (paragraph, offset) = _document.Resolve(range.Normalize(_document).Start);
        return _document.EffectiveRunFormat(paragraph, InlineEdit.At(paragraph, offset)).SizePt;
    }

    private List<string> ParagraphIds(DocumentRange range)
    {
        var (start, end) = range.Normalize(_document);
        var first = _document.Resolve(start).Paragraph.Id;
        var last = _document.Resolve(end);
        return [.. _document.ParagraphsIn(range).Select(p => p.Id).Where(id => id == first || id != last.Paragraph.Id || last.Offset > 0)];
    }

    private static double ClampSize(double size) => Math.Clamp(Math.Round(size * 2) / 2, 1, 1638);

    private readonly record struct RibbonView(bool? Bold, bool? Italic, string Underline, bool? Strike, VerticalAlign? VerticalAlign, string Font, double? Size, string Color,
        string Highlight, ParagraphAlignment? Alignment, ListTemplate? List, LineSpacing? Line, string StyleId, string CharacterStyle, int? SpacingBefore, int? SpacingAfter,
        DocumentStyles Styles, ObjectState Objects);

    internal sealed record StyleItem(string Id, string Name, StyleType Type, string Css);
}
