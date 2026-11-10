using MudBlazor.Extensions.Core;
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

    private static readonly (string Id, string Text, string Icon, string Shortcut)[] FontToggles =
    [
        ("pp-bold", "Bold", Icons.Material.Filled.FormatBold, "Ctrl+B"),
        ("pp-italic", "Italic", Icons.Material.Filled.FormatItalic, "Ctrl+I"),
        ("pp-underline", "Underline", Icons.Material.Filled.FormatUnderlined, "Ctrl+U"),
        ("pp-strikethrough", "Strikethrough", Icons.Material.Filled.FormatStrikethrough, null)
    ];

    private static readonly (string Id, string Text, string Icon, string Shortcut)[] AlignToggles =
    [
        ("pp-align-left", "Align Left", Icons.Material.Filled.FormatAlignLeft, "Ctrl+L"),
        ("pp-align-center", "Center", Icons.Material.Filled.FormatAlignCenter, "Ctrl+E"),
        ("pp-align-right", "Align Right", Icons.Material.Filled.FormatAlignRight, "Ctrl+R"),
        ("pp-align-justify", "Justify", Icons.Material.Filled.FormatAlignJustify, "Ctrl+J")
    ];

    private static readonly (string Id, string Text, string Icon, ZOrderChange Change)[] ArrangeItems =
    [
        ("pp-bring-to-front", "Bring to Front", Icons.Material.Filled.FlipToFront, ZOrderChange.BringToFront),
        ("pp-bring-forward", "Bring Forward", Icons.Material.Filled.ArrowUpward, ZOrderChange.BringForward),
        ("pp-send-backward", "Send Backward", Icons.Material.Filled.ArrowDownward, ZOrderChange.SendBackward),
        ("pp-send-to-back", "Send to Back", Icons.Material.Filled.FlipToBack, ZOrderChange.SendToBack)
    ];

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
}
