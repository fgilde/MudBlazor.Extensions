using System.ComponentModel;
using System.Globalization;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office;
using MudBlazor.Utilities;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlideEditor
{
    private static readonly string[] Fonts =
        ["Aptos", "Arial", "Calibri", "Calibri Light", "Cambria", "Century Gothic", "Consolas", "Georgia", "Segoe UI", "Tahoma", "Times New Roman", "Trebuchet MS", "Verdana"];

    private static readonly double[] ZoomLevels = [0.25, 0.5, 0.66, 0.75, 1, 1.5, 2, 3, 4];

    private static readonly IReadOnlyList<MudExColor> Palette =
    [
        "#000000", "#FFFFFF", "#E7E6E6", "#44546A", "#4472C4", "#ED7D31", "#A5A5A5", "#FFC000", "#5B9BD5", "#70AD47",
        "#C00000", "#FF0000", "#FFFF00", "#92D050", "#00B050", "#00B0F0", "#0070C0", "#002060", "#7030A0", "#C43E1C"
    ];

    private static readonly (string Id, string Text, string Icon, string Shortcut, string Key)[] FontToggles =
    [
        ("pp-bold", "Bold", Icons.Material.Filled.FormatBold, "Ctrl+B", "b"),
        ("pp-italic", "Italic", Icons.Material.Filled.FormatItalic, "Ctrl+I", "i"),
        ("pp-underline", "Underline", Icons.Material.Filled.FormatUnderlined, "Ctrl+U", "u"),
        ("pp-strikethrough", "Strikethrough", Icons.Material.Filled.FormatStrikethrough, null, "s")
    ];

    private static readonly (string Id, string Text, string Icon, string Shortcut, TextAlign Align)[] AlignToggles =
    [
        ("pp-align-left", "Align Left", Icons.Material.Filled.FormatAlignLeft, "Ctrl+L", TextAlign.Left),
        ("pp-align-center", "Center", Icons.Material.Filled.FormatAlignCenter, "Ctrl+E", TextAlign.Center),
        ("pp-align-right", "Align Right", Icons.Material.Filled.FormatAlignRight, "Ctrl+R", TextAlign.Right),
        ("pp-align-justify", "Justify", Icons.Material.Filled.FormatAlignJustify, "Ctrl+J", TextAlign.Justify)
    ];

    private static readonly (string Id, string Text, string Icon, ZOrderChange Change)[] ArrangeItems =
    [
        ("pp-bring-to-front", "Bring to Front", Icons.Material.Filled.FlipToFront, ZOrderChange.BringToFront),
        ("pp-bring-forward", "Bring Forward", Icons.Material.Filled.ArrowUpward, ZOrderChange.BringForward),
        ("pp-send-backward", "Send Backward", Icons.Material.Filled.ArrowDownward, ZOrderChange.SendBackward),
        ("pp-send-to-back", "Send to Back", Icons.Material.Filled.FlipToBack, ZOrderChange.SendToBack)
    ];

    private IReadOnlyList<AutoShape> SelectedTextShapes =>
        SelectedAutoShapes.Where(s => s.Text != null).Concat(SelectedTable == null ? [] : _canvas.SelectedCellShapes.Select(c => c.Shape)).ToList();

    private RunFormat CurrentFormat => SelectedTextShapes.Select(s => s.Text.Paragraphs.SelectMany(p => p.Runs).FirstOrDefault() is { } run ? s.EffectiveFormat(run) : null)
        .FirstOrDefault(f => f != null);

    private TextAlign? CurrentAlign => SelectedTextShapes.FirstOrDefault() is { Text.Paragraphs: [var paragraph, ..] } shape ? shape.EffectiveAlign(paragraph) : null;

    private SlideMaster CurrentMaster => _current?.Layout?.Master ?? _presentation?.Masters.FirstOrDefault();

    private IReadOnlyList<SlideLayout> AvailableLayouts => _presentation?.Layouts.Where(l => l.Master == CurrentMaster).ToList() ?? [];

    private SlideLayout DefaultLayout
    {
        get
        {
            if (_current?.Layout is { Type: not "title" } layout)
                return layout;
            var layouts = AvailableLayouts;
            return layouts.FirstOrDefault(l => l.Type == "obj") ?? _current?.Layout ?? layouts.FirstOrDefault();
        }
    }

    private bool IsOn(string key) => SelectedTextShapes is { Count: > 0 } shapes && SlideTextDom.All(shapes, key);

    private Task FormatAsync(string key, string value) => IsReadOnly || _canvas == null ? Task.CompletedTask : _canvas.FormatTextAsync(key, value);

    private Task SetFontAsync(string font) => string.IsNullOrWhiteSpace(font) ? Task.CompletedTask : FormatAsync("font", font.Trim());

    private Task SetFontSizeAsync(double size) => size > 0 ? FormatAsync("size", size.ToString(CultureInfo.InvariantCulture)) : Task.CompletedTask;

    private Task SetFontColorAsync(MudExColor color) => Hex(color) is { } hex ? FormatAsync("color", hex) : Task.CompletedTask;

    private Task InsertTextBoxAsync() => IsReadOnly || _canvas == null ? Task.CompletedTask : _canvas.InsertTextBoxAsync();

    private PresentationSection CurrentSection => _current == null ? null : _presentation?.SectionOf(_current);

    private bool HasSections => _presentation?.Sections.Count > 0;

    private Task RunCommandAsync(IDocumentCommand<Presentation> command) => RunAsync(() => _presentation.Execute(command));

    private async Task AddSectionAsync()
    {
        if (_current == null)
            return;
        var command = new AddSectionCommand(_current, TryLocalize("Untitled Section"), TryLocalize(PresentationSection.DefaultName));
        await RunCommandAsync(command);
        if (command.Section != null)
            _panel?.StartRename(command.Section);
    }

    /// <summary>Called by the browser when a section header was dragged to a new position in the slide panel.</summary>
    /// <param name="from">The index of the dragged section.</param>
    /// <param name="target">The insertion index, from 0 to the section count.</param>
    /// <returns>A task that completes when the section moved.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnSectionDropped(int from, int target) => InvokeAsync(async () =>
    {
        var sections = _presentation?.Sections ?? [];
        var index = target > from ? target - 1 : target;
        if (from < 0 || from >= sections.Count || target < 0 || target > sections.Count || index == from)
            return;
        await RunCommandAsync(new MoveSectionCommand(sections[from], index));
        StateHasChanged();
    });

    private Task SetLayoutAsync(SlideLayout layout) => _selection.Count == 0 ? Task.CompletedTask : RunCommandAsync(new SetSlideLayoutCommand(_selection, layout));

    private Task ResetSlidesAsync() => _selection.Count == 0 ? Task.CompletedTask : RunCommandAsync(new ResetSlideCommand(_selection));

    private async Task HeaderFooterAsync()
    {
        if (IsReadOnly || _current == null)
            return;
        if (_canvas != null)
            await _canvas.CommitTextAsync();
        var dialog = await DialogService.ShowAsync<MudExSlideHeaderFooterDialog>(TryLocalize("Header and Footer"), new DialogParameters
        {
            { nameof(MudExSlideHeaderFooterDialog.Options), HeaderFooterOptions.From(_current) },
            { nameof(MudExSlideHeaderFooterDialog.Localizer), Localizer }
        }, new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true });
        if (await dialog.Result is { Canceled: false, Data: (HeaderFooterOptions options, bool all) })
            await RunCommandAsync(new SetHeaderFooterCommand(all ? _presentation.Slides : _selection, options));
    }
}
