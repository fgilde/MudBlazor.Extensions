namespace MudBlazor.Extensions.Components;

public partial class MudExNotebookEditor
{
    private static readonly double[] ZoomLevels = [0.25, 0.5, 0.75, 1, 1.25, 1.5, 2, 3, 4];

    private static readonly (string Id, string Text, string Icon, NotebookTool Tool)[] DrawItems =
    [
        ("nb-draw-type", "Type", Icons.Material.Filled.TextFields, NotebookTool.Text),
        ("nb-draw-select", "Select", Icons.Material.Filled.NearMe, NotebookTool.Select),
        ("nb-draw-lasso", "Lasso Select", Icons.Material.Filled.Gesture, NotebookTool.Lasso),
        ("nb-draw-eraser", "Eraser", Icons.Material.Filled.AutoFixNormal, NotebookTool.Eraser)
    ];

    private async Task SetToolAsync(NotebookTool tool)
    {
        await CommitTextAsync();
        _tool = tool;
    }

    private Task CommitTextAsync() => _canvas?.CommitTextAsync() ?? Task.CompletedTask;
}
