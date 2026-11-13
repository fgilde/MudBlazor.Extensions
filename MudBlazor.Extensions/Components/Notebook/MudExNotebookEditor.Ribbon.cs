namespace MudBlazor.Extensions.Components;

public partial class MudExNotebookEditor
{
    private static readonly double[] ZoomLevels = [0.25, 0.5, 0.75, 1, 1.25, 1.5, 2, 3, 4];

    private static readonly (string Id, string Text, string Icon)[] ClipboardItems =
    [
        ("nb-paste", "Paste", Icons.Material.Filled.ContentPaste),
        ("nb-cut", "Cut", Icons.Material.Filled.ContentCut),
        ("nb-copy", "Copy", Icons.Material.Filled.ContentCopy)
    ];

    private static readonly (string Id, string Text, string Icon)[] FontItems =
    [
        ("nb-bold", "Bold", Icons.Material.Filled.FormatBold),
        ("nb-italic", "Italic", Icons.Material.Filled.FormatItalic),
        ("nb-underline", "Underline", Icons.Material.Filled.FormatUnderlined),
        ("nb-strikethrough", "Strikethrough", Icons.Material.Filled.FormatStrikethrough),
        ("nb-highlight", "Highlight", Icons.Material.Filled.BorderColor),
        ("nb-font-color", "Font Color", Icons.Material.Filled.FormatColorText)
    ];

    private static readonly (string Id, string Text, string Icon)[] ParagraphItems =
    [
        ("nb-bullets", "Bullets", Icons.Material.Filled.FormatListBulleted),
        ("nb-numbering", "Numbering", Icons.Material.Filled.FormatListNumbered),
        ("nb-todo", "To Do", Icons.Material.Filled.CheckBox),
        ("nb-outdent", "Decrease Indent", Icons.Material.Filled.FormatIndentDecrease),
        ("nb-indent", "Increase Indent", Icons.Material.Filled.FormatIndentIncrease),
        ("nb-align-left", "Align Left", Icons.Material.Filled.FormatAlignLeft),
        ("nb-align-center", "Center", Icons.Material.Filled.FormatAlignCenter),
        ("nb-align-right", "Align Right", Icons.Material.Filled.FormatAlignRight)
    ];

    private static readonly (string Id, string Text, string Icon)[] StyleItems =
    [
        ("nb-heading-1", "Heading 1", Icons.Material.Filled.Title),
        ("nb-heading-2", "Heading 2", Icons.Material.Filled.Title),
        ("nb-heading-3", "Heading 3", Icons.Material.Filled.Title),
        ("nb-normal", "Normal", Icons.Material.Filled.TextFields)
    ];

    private static readonly (string Id, string Text, string Icon)[] TableItems = [("nb-insert-table", "Table", Icons.Material.Filled.TableChart)];

    private static readonly (string Id, string Text, string Icon)[] ImageItems = [("nb-insert-picture", "Picture", Icons.Material.Filled.Image)];

    private static readonly (string Id, string Text, string Icon)[] ShapeItems = [("nb-insert-shapes", "Shapes", Icons.Material.Filled.Category)];

    private static readonly (string Id, string Text, string Icon)[] LinkItems = [("nb-insert-link", "Link", Icons.Material.Filled.Link)];

    private static readonly (string Id, string Text, string Icon, NotebookTool Tool)[] DrawItems =
    [
        ("nb-draw-type", "Type", Icons.Material.Filled.TextFields, NotebookTool.Text),
        ("nb-draw-select", "Select", Icons.Material.Filled.NearMe, NotebookTool.Select),
        ("nb-draw-lasso", "Lasso Select", Icons.Material.Filled.Gesture, NotebookTool.Lasso),
        ("nb-draw-eraser", "Eraser", Icons.Material.Filled.AutoFixNormal, NotebookTool.Eraser),
        ("nb-draw-pen", "Pen", Icons.Material.Filled.Edit, NotebookTool.Pen),
        ("nb-draw-highlighter", "Highlighter", Icons.Material.Filled.BorderColor, NotebookTool.Highlighter)
    ];

    private async Task SetToolAsync(NotebookTool tool)
    {
        await CommitTextAsync();
        _tool = tool;
    }

    private Task CommitTextAsync() => _canvas?.CommitTextAsync() ?? Task.CompletedTask;
}
