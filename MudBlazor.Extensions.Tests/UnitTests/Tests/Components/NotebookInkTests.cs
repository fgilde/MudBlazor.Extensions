using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Notebook;

public class NotebookInkTests : IAsyncLifetime
{
    private readonly BunitContext _context = new();

    public NotebookInkTests()
    {
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        _context.Render<MudPopoverProvider>();
        _context.Render<MudDialogProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _context.DisposeAsync();

    private static (Notebook Notebook, NotebookPage Page) Page()
    {
        var notebook = Notebook.Create();
        return (notebook, notebook.Sections[0].Pages[0]);
    }

    private IRenderedComponent<MudExNotebookCanvas> Canvas(Notebook notebook, NotebookPage page, NotebookTool tool, bool readOnly = false, string color = "#000000", double width = 2) =>
        _context.Render<MudExNotebookCanvas>(p => p.Add(c => c.Notebook, notebook).Add(c => c.Page, page).Add(c => c.Tool, tool).Add(c => c.ReadOnly, readOnly)
            .Add(c => c.InkColor, color).Add(c => c.InkWidth, width).Add(c => c.ViewportWidth, 800).Add(c => c.ViewportHeight, 600));

    private static InkPointDto P(double x, double y, double pressure = 0.5) => new() { X = x, Y = y, Pressure = pressure };

    private static Task Stroke(IRenderedComponent<MudExNotebookCanvas> cut, params InkPointDto[] points) => cut.InvokeAsync(() => cut.Instance.OnStrokeAsync(points));

    private static string AddStroke(Notebook notebook, NotebookPage page, params (double X, double Y)[] points)
    {
        var command = new AddStrokeCommand(page, points.Select(p => new InkPoint(p.X, p.Y, 0.5)).ToList(), "#000000", 2, InkTool.Pen);
        notebook.Execute(command);
        return command.Stroke.Id;
    }

    [Fact]
    public async Task Ink_StrokeCallback_AddsInkElement_OneUndoStep()
    {
        var (notebook, page) = Page();
        var cut = Canvas(notebook, page, NotebookTool.Pen, color: "#C00000", width: 4);

        await Stroke(cut, P(10, 10), P(20, 10), P(30, 10, 0.8), P(double.NaN, 5), P(30, 40, 2));

        var ink = Assert.IsType<InkElement>(Assert.Single(page.Elements));
        Assert.Equal(("#C00000", 4d, InkTool.Pen), (ink.Color, ink.StrokeWidth, ink.Tool));
        Assert.Equal(3, ink.Points.Count);
        Assert.Equal(new NoteBounds(8, 8, 24, 34), ink.Bounds);
        Assert.Equal(1, ink.Points[^1].Pressure);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-notebook-ink")));

        notebook.Undo.Undo();
        Assert.Empty(page.Elements);
        Assert.False(notebook.Undo.CanUndo);

        await Stroke(cut);
        await Stroke(cut, P(double.NaN, 1));
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public async Task Ink_SinglePointStroke_IsDot()
    {
        var (notebook, page) = Page();
        var cut = Canvas(notebook, page, NotebookTool.Pen, width: 4);

        await Stroke(cut, P(50, 60));

        var ink = Assert.IsType<InkElement>(Assert.Single(page.Elements));
        Assert.Equal(new NoteBounds(48, 58, 4, 4), ink.Bounds);
        Assert.Single(ink.Points);
        cut.WaitForAssertion(() => Assert.Equal("M2 2L2 2", cut.Find(".mud-ex-notebook-ink path").GetAttribute("d")));
    }

    [Fact]
    public async Task Ink_EraseGesture_IsOneUndoStep()
    {
        var (notebook, page) = Page();
        var first = AddStroke(notebook, page, (10, 10), (50, 10));
        var second = AddStroke(notebook, page, (10, 30), (50, 30));
        var third = AddStroke(notebook, page, (10, 300), (50, 300));
        notebook.Execute(new AddElementCommand(page, new ShapeElement { Id = "box", X = 0, Y = 0, Width = 60, Height = 60, Kind = NoteShapeKind.Rectangle }));
        notebook.Undo.Clear();
        var cut = Canvas(notebook, page, NotebookTool.Eraser);

        await cut.InvokeAsync(() => cut.Instance.OnEraseAsync([first, second, "box", "unknown", first]));

        Assert.Equal([third, "box"], page.Elements.Select(e => e.Id));
        notebook.Undo.Undo();
        Assert.Equal([first, second, third, "box"], page.Elements.Select(e => e.Id));
        Assert.False(notebook.Undo.CanUndo);

        await cut.InvokeAsync(() => cut.Instance.OnEraseAsync(["box"]));
        await cut.InvokeAsync(() => cut.Instance.OnEraseAsync([]));
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public async Task Ink_Lasso_SelectsViaGeometry()
    {
        var (notebook, page) = Page();
        var inside = AddStroke(notebook, page, (10, 10), (50, 50), (90, 90));
        var mostlyOutside = AddStroke(notebook, page, (50, 50), (150, 150), (200, 200));
        notebook.Execute(new AddElementCommand(page, new ShapeElement { Id = "shape", X = 20, Y = 20, Width = 40, Height = 40 }));
        notebook.Execute(new AddElementCommand(page, new ShapeElement { Id = "far", X = 300, Y = 300, Width = 40, Height = 40 }));
        notebook.Execute(new AddElementCommand(page, new TextElement { Id = "tall", X = 10, Y = 90, Width = 80, Paragraphs = [new NoteParagraph { Runs = [new NoteRun("x")] }] }));
        var cut = Canvas(notebook, page, NotebookTool.Lasso);
        await cut.InvokeAsync(() => cut.Instance.OnHeightsMeasuredAsync(new Dictionary<string, double> { ["tall"] = 100 }));

        PointDto[] square = [new() { X = 0, Y = 0 }, new() { X = 100, Y = 0 }, new() { X = 100, Y = 100 }, new() { X = 0, Y = 100 }];
        await cut.InvokeAsync(() => cut.Instance.OnLassoAsync(square));

        Assert.Equal([inside, "shape"], cut.Instance.Selection);
        Assert.Equal(2, cut.FindAll(".mud-ex-notebook-selection").Count);
        Assert.Equal(8, cut.FindAll($".mud-ex-notebook-selection[data-id='{inside}'] [data-handle]").Count);
        Assert.DoesNotContain(mostlyOutside, cut.Instance.Selection);

        await cut.InvokeAsync(() => cut.Instance.OnLassoAsync([]));
        Assert.Empty(cut.Instance.Selection);
    }

    [Fact]
    public async Task Ink_ToolFromGallery_SetsColorAndWidth()
    {
        var cut = _context.Render<MudExNotebookEditor>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-notebook-canvas")));
        var gallery = cut.FindComponent<MudExRibbonGallery<NotebookInkPreset>>();
        MudExNotebookCanvas Canvas() => cut.FindComponent<MudExNotebookCanvas>().Instance;
        Task Choose(string name) => cut.InvokeAsync(() => gallery.Instance.SelectedItemChanged.InvokeAsync(gallery.Instance.Items.Single(p => p.Name == name)));

        Assert.Equal(6, gallery.Instance.Items.Count());
        Assert.Equal(NotebookTool.Text, Canvas().Tool);

        await Choose("Red Pen");
        cut.WaitForAssertion(() => Assert.Equal((NotebookTool.Pen, "#C00000", 2d), (Canvas().Tool, Canvas().InkColor, Canvas().InkWidth)));
        Assert.Equal("Red Pen", gallery.Instance.SelectedItem?.Name);

        await cut.InvokeAsync(() => cut.FindComponents<MudExRibbonNumberBox>().Single(b => b.Instance.Id == "nb-draw-width").Instance.ValueChanged.InvokeAsync(5));
        await cut.InvokeAsync(() => cut.FindComponents<MudExRibbonColorPicker>().Single(b => b.Instance.Id == "nb-draw-color").Instance.ValueChanged.InvokeAsync((MudExColor)"#0070C0"));
        cut.WaitForAssertion(() => Assert.Equal(("#0070C0", 5d), (Canvas().InkColor, Canvas().InkWidth)));
        Assert.Null(gallery.Instance.SelectedItem);

        await Choose("Yellow Highlighter");
        cut.WaitForAssertion(() => Assert.Equal((NotebookTool.Highlighter, "#FFFF00", 14d), (Canvas().Tool, Canvas().InkColor, Canvas().InkWidth)));

        await cut.InvokeAsync(() => cut.FindComponents<MudExRibbonToggleButton>().Single(b => b.Instance.Id == "nb-draw-type").Instance.CheckedChanged.InvokeAsync(true));
        cut.WaitForAssertion(() => Assert.Equal(NotebookTool.Text, Canvas().Tool));
        Assert.Null(gallery.Instance.SelectedItem);
        Assert.True(cut.FindComponents<MudExRibbonNumberBox>().Single(b => b.Instance.Id == "nb-draw-width").Instance.Disabled);
    }

    [Fact]
    public async Task Ink_ReadOnly_ToolsDisabled()
    {
        var (notebook, page) = Page();
        var stroke = AddStroke(notebook, page, (10, 10), (50, 50));
        notebook.Undo.Clear();
        var cut = Canvas(notebook, page, NotebookTool.Pen, readOnly: true);

        await Stroke(cut, P(1, 1), P(5, 5));
        await cut.InvokeAsync(() => cut.Instance.OnEraseAsync([stroke]));
        await cut.InvokeAsync(() => cut.Instance.OnLassoAsync([new() { X = 0, Y = 0 }, new() { X = 100, Y = 0 }, new() { X = 100, Y = 100 }]));

        Assert.Equal([stroke], page.Elements.Select(e => e.Id));
        Assert.Empty(cut.Instance.Selection);
        Assert.False(notebook.Undo.CanUndo);
        Assert.Equal("true", cut.Find(".mud-ex-notebook-canvas").GetAttribute("data-readonly"));

        var editor = _context.Render<MudExNotebookEditor>(p => p.Add(e => e.Notebook, notebook).Add(e => e.ReadOnly, true));
        editor.WaitForAssertion(() => Assert.NotEmpty(editor.FindAll(".mud-ex-notebook-canvas")));
        Assert.All(editor.FindComponents<MudExRibbonGallery<NotebookInkPreset>>(), g => Assert.True(g.Instance.Disabled));
        Assert.All(editor.FindComponents<MudExRibbonToggleButton>().Where(b => b.Instance.Id?.StartsWith("nb-draw-") == true), b => Assert.True(b.Instance.Disabled));
        Assert.Equal("true", editor.Find(".mud-ex-notebook-canvas").GetAttribute("data-readonly"));
    }

    [Fact]
    public async Task Ink_HighlighterStroke_HasHighlighterTool()
    {
        var (notebook, page) = Page();
        var cut = Canvas(notebook, page, NotebookTool.Highlighter, color: "#FFFF00", width: 14);

        await Stroke(cut, P(10, 10), P(100, 10));

        var ink = Assert.IsType<InkElement>(Assert.Single(page.Elements));
        Assert.Equal((InkTool.Highlighter, "#FFFF00", 14d), (ink.Tool, ink.Color, ink.StrokeWidth));
        cut.WaitForAssertion(() => Assert.Contains("mud-ex-notebook-highlighter", cut.Find(".mud-ex-notebook-ink").ClassName));
        Assert.Equal("Highlight", notebook.Undo.UndoDescription);
    }
}
