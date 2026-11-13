using System.Globalization;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Notebook;
using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Components;

public partial class MudExNotebookEditor
{
    private static readonly string[] Fonts =
        ["Calibri", "Arial", "Cambria", "Consolas", "Georgia", "Segoe UI", "Tahoma", "Times New Roman", "Trebuchet MS", "Verdana"];

    private static readonly IReadOnlyList<MudExColor> Palette =
    [
        "#000000", "#FFFFFF", "#E7E6E6", "#44546A", "#4472C4", "#ED7D31", "#A5A5A5", "#FFC000", "#5B9BD5", "#70AD47",
        "#C00000", "#FF0000", "#FFFF00", "#92D050", "#00B050", "#00B0F0", "#0070C0", "#002060", "#7030A0", "#C43E1C"
    ];

    private static readonly IReadOnlyList<MudExColor> HighlightPalette =
        ["#FFFF00", "#00FF00", "#00FFFF", "#FF00FF", "#FFC000", "#FF0000", "#92D050", "#00B0F0", "#C0C0C0", "#FFFFFF"];

    private static readonly (string Id, string Text, string Icon, string Shortcut, string Key)[] FontToggles =
    [
        ("nb-bold", "Bold", Icons.Material.Filled.FormatBold, "Ctrl+B", "b"),
        ("nb-italic", "Italic", Icons.Material.Filled.FormatItalic, "Ctrl+I", "i"),
        ("nb-underline", "Underline", Icons.Material.Filled.FormatUnderlined, "Ctrl+U", "u"),
        ("nb-strikethrough", "Strikethrough", Icons.Material.Filled.FormatStrikethrough, null, "s")
    ];

    private static readonly (string Id, string Text, string Icon, string Shortcut, NoteListKind List)[] ListToggles =
    [
        ("nb-bullets", "Bullets", Icons.Material.Filled.FormatListBulleted, null, NoteListKind.Bullet),
        ("nb-numbering", "Numbering", Icons.Material.Filled.FormatListNumbered, null, NoteListKind.Number),
        ("nb-todo", "To Do", Icons.Material.Filled.CheckBox, null, NoteListKind.Todo)
    ];

    private static readonly (string Id, string Text, string Icon, string Shortcut, NoteAlign Align)[] AlignToggles =
    [
        ("nb-align-left", "Align Left", Icons.Material.Filled.FormatAlignLeft, "Ctrl+L", NoteAlign.Left),
        ("nb-align-center", "Center", Icons.Material.Filled.FormatAlignCenter, "Ctrl+E", NoteAlign.Center),
        ("nb-align-right", "Align Right", Icons.Material.Filled.FormatAlignRight, "Ctrl+R", NoteAlign.Right)
    ];

    private static readonly (string Id, string Text, NoteHeading Heading)[] HeadingToggles =
    [
        ("nb-heading-1", "Heading 1", NoteHeading.H1),
        ("nb-heading-2", "Heading 2", NoteHeading.H2),
        ("nb-heading-3", "Heading 3", NoteHeading.H3),
        ("nb-normal", "Normal", NoteHeading.None)
    ];

    private NotebookTextState TextState => IsReadOnly ? null : _canvas?.TextState;

    private bool CanFormat => !IsReadOnly && _canvas?.CanFormatText == true;

    private bool CanLink => !IsReadOnly && _canvas?.IsEditingText == true;

    private bool IsOn(string key) => TextState is { } state && key switch
    {
        "b" => state.Bold,
        "i" => state.Italic,
        "u" => state.Underline,
        _ => state.Strike
    };

    private async Task FormatAsync(string key, string value)
    {
        if (!CanFormat)
            return;
        try
        {
            await _canvas.FormatTextAsync(key, value);
        }
        catch (Exception e)
        {
            await ShowErrorAsync(e.Message);
        }
    }

    private Task SetListAsync(NoteListKind list) => FormatAsync("list", NotebookTextDom.ListName(list));

    private Task SetFontAsync(string font) => NoteValues.IsFont(font?.Trim()) ? FormatAsync("font", font.Trim()) : Task.CompletedTask;

    private Task SetFontSizeAsync(double size) => size is > 0 and <= 1000 ? FormatAsync("size", size.ToString(CultureInfo.InvariantCulture)) : Task.CompletedTask;

    private Task SetFontColorAsync(MudExColor color) => Hex(color) is { } hex ? FormatAsync("color", hex) : Task.CompletedTask;

    private Task SetHighlightAsync(MudExColor color) => Hex(color) is { } hex ? FormatAsync("highlight", hex == "#FFFFFF" ? "" : hex) : Task.CompletedTask;

    private Task InsertLinkAsync() => CanLink ? _canvas.RequestLinkAsync() : Task.CompletedTask;

    private async Task OpenLinkDialogAsync((string Text, string Link) request)
    {
        var reference = await DialogService.ShowAsync<MudExNotebookLinkDialog>(TryLocalize("Link"), new DialogParameters
        {
            { nameof(MudExNotebookLinkDialog.Text), request.Text },
            { nameof(MudExNotebookLinkDialog.Address), request.Link },
            { nameof(MudExNotebookLinkDialog.Localizer), Localizer }
        }, new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true });
        if (await reference.Result is { Canceled: false, Data: (string text, string address) } && _canvas != null)
            await _canvas.SetLinkAsync(text, address);
    }

    private void OnCanvasSelectionChanged(IReadOnlyList<string> ids) => StateHasChanged();

    private Task OnCanvasFailedAsync(Exception e) => ShowErrorAsync(e.Message);

    private static string Hex(MudExColor color) => color.IsSet() && color.ToMudColor() is { } c ? c.ToString(MudColorOutputFormats.Hex).ToUpperInvariant() : null;
}
