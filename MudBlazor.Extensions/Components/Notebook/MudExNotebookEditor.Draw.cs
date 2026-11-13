using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

internal sealed record NotebookInkPreset(string Name, NotebookTool Tool, string Color, double Width);

public partial class MudExNotebookEditor
{
    private static readonly NotebookInkPreset[] InkPresets =
    [
        new("Black Pen", NotebookTool.Pen, "#000000", 2),
        new("Blue Pen", NotebookTool.Pen, "#0070C0", 2),
        new("Red Pen", NotebookTool.Pen, "#C00000", 2),
        new("Green Pen", NotebookTool.Pen, "#00B050", 3),
        new("Yellow Highlighter", NotebookTool.Highlighter, "#FFFF00", 14),
        new("Green Highlighter", NotebookTool.Highlighter, "#92D050", 14)
    ];

    private (string Color, double Width) _pen = ("#000000", 2);
    private (string Color, double Width) _highlighter = ("#FFFF00", 14);

    private bool IsInkTool => _tool is NotebookTool.Pen or NotebookTool.Highlighter;

    private (string Color, double Width) Ink => _tool == NotebookTool.Highlighter ? _highlighter : _pen;

    private NotebookInkPreset CurrentPreset => IsInkTool ? InkPresets.FirstOrDefault(p => p.Tool == _tool && p.Color == Ink.Color && p.Width == Ink.Width) : null;

    private Task ChoosePresetAsync(NotebookInkPreset preset)
    {
        if (preset == null)
            return Task.CompletedTask;
        SetInk(preset.Tool, (preset.Color, preset.Width));
        return SetToolAsync(preset.Tool);
    }

    private void SetInkColor(MudExColor color)
    {
        if (IsInkTool && Hex(color) is { } hex)
            SetInk(_tool, (hex, Ink.Width));
    }

    private void SetInkWidth(double width)
    {
        if (IsInkTool && double.IsFinite(width) && width > 0)
            SetInk(_tool, (Ink.Color, Math.Min(width, 100)));
    }

    private void SetInk(NotebookTool tool, (string Color, double Width) ink)
    {
        if (tool == NotebookTool.Highlighter)
            _highlighter = ink;
        else
            _pen = ink;
    }
}
