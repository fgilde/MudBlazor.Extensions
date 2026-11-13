using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Notebook;

public class NotebookCanvasTests : IDisposable
{
    private readonly BunitContext _context = new();

    public NotebookCanvasTests()
    {
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _context.Dispose();

    private static (Notebook Notebook, NotebookPage Page) Page()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        notebook.Execute(new AddElementCommand(page, new TextElement { Id = "text", X = 10, Y = 20, Width = 300, Paragraphs = [new NoteParagraph { Runs = [new NoteRun("Hello")] }] }));
        notebook.Execute(new AddElementCommand(page, new ImageElement { Id = "image", X = 100, Y = 200, Width = 200, Height = 100, MediaId = "missing" }));
        notebook.Execute(new AddElementCommand(page, new ShapeElement { Id = "box", X = 400, Y = 50, Width = 50, Height = 50, Kind = NoteShapeKind.Rectangle }));
        notebook.Execute(new AddElementCommand(page, OpaqueElement.From(JsonDocument.Parse("""{"type":"hologram","id":"future","x":500,"y":300,"width":40,"height":40}""").RootElement)));
        notebook.Undo.Clear();
        return (notebook, page);
    }

    private IRenderedComponent<MudExNotebookCanvas> Render(Notebook notebook, NotebookPage page, bool readOnly = false, NotebookTool tool = NotebookTool.Text,
        Action<NotebookTextEditRequest>? requested = null) =>
        _context.Render<MudExNotebookCanvas>(p => p
            .Add(c => c.Notebook, notebook)
            .Add(c => c.Page, page)
            .Add(c => c.ReadOnly, readOnly)
            .Add(c => c.Tool, tool)
            .Add(c => c.ViewportWidth, 800)
            .Add(c => c.ViewportHeight, 600)
            .Add(c => c.TextEditRequested, r => requested?.Invoke(r)));

    private static NoteElement Element(NotebookPage page, string id) => page.Elements.Single(e => e.Id == id);

    private static Task Select(IRenderedComponent<MudExNotebookCanvas> cut, params string[] ids) => cut.InvokeAsync(() => cut.Instance.OnSelectionAsync(ids));

    private static Task Key(IRenderedComponent<MudExNotebookCanvas> cut, string key, bool ctrl = false, bool shift = false) =>
        cut.Find(".mud-ex-notebook-canvas").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = key, CtrlKey = ctrl, ShiftKey = shift });

    private static string PageStyle(IRenderedComponent<MudExNotebookCanvas> cut) => cut.Find(".mud-ex-notebook-page").GetAttribute("style")!;

    [Fact]
    public async Task Canvas_ClickEmptySpace_RequestsTextAtPosition()
    {
        var (notebook, page) = Page();
        NotebookTextEditRequest? request = null;
        var cut = Render(notebook, page, requested: r => request = r);

        await cut.InvokeAsync(() => cut.Instance.OnPageClickAsync(120, 480));

        Assert.NotNull(request);
        Assert.True(request!.IsNew);
        Assert.Equal((120d, 480d), (request.X, request.Y));
        Assert.True(NoteValues.IsId(request.ElementId));
        Assert.DoesNotContain(page.Elements, e => e.Id == request.ElementId);
        Assert.Equal(request, cut.Instance.TextEdit);
        Assert.Equal(request.ElementId, cut.Find(".mud-ex-notebook-canvas").GetAttribute("data-editing"));
        Assert.False(notebook.Undo.CanUndo);

        request = null;
        await cut.InvokeAsync(() => cut.Instance.OnPageClickAsync(20, 25));
        Assert.Equal(("text", false), (request!.ElementId, request.IsNew));
        Assert.Equal(["text"], cut.Instance.Selection);

        request = null;
        cut.Render(p => p.Add(c => c.Tool, NotebookTool.Select));
        await cut.InvokeAsync(() => cut.Instance.OnPageClickAsync(700, 500));
        Assert.Null(request);
        Assert.Null(cut.Instance.TextEdit);
    }

    [Fact]
    public async Task Canvas_EmptyNewTextBlock_LeavesNoUndoStep()
    {
        var (notebook, page) = Page();
        var cut = Render(notebook, page);
        var count = page.Elements.Count;

        await cut.InvokeAsync(() => cut.Instance.OnPageClickAsync(50, 500));
        await cut.InvokeAsync(() => cut.Instance.EndTextEditAsync([new NoteParagraph { Runs = [new NoteRun("")] }]));
        Assert.Equal(count, page.Elements.Count);
        Assert.False(notebook.Undo.CanUndo);
        Assert.Null(cut.Instance.TextEdit);

        await cut.InvokeAsync(() => cut.Instance.OnPageClickAsync(50, 500));
        await cut.InvokeAsync(() => cut.Instance.OnPageClickAsync(60, 520));
        await Key(cut, "Escape");
        Assert.Null(cut.Instance.TextEdit);
        Assert.Equal(count, page.Elements.Count);
        Assert.False(notebook.Undo.CanUndo);

        await cut.InvokeAsync(() => cut.Instance.OnPageClickAsync(50, 500));
        var id = cut.Instance.TextEdit!.ElementId;
        await cut.InvokeAsync(() => cut.Instance.EndTextEditAsync([new NoteParagraph { Runs = [new NoteRun("Note")] }]));
        var added = Assert.IsType<TextElement>(Element(page, id));
        Assert.Equal((50d, 500d, 0d), (added.X, added.Y, added.Height));
        notebook.Undo.Undo();
        Assert.DoesNotContain(page.Elements, e => e.Id == id);
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public async Task Canvas_MoveCallback_IsOneUndoStep()
    {
        var (notebook, page) = Page();
        var cut = Render(notebook, page);
        await Select(cut, "image", "box");

        await cut.InvokeAsync(() => cut.Instance.OnMoveAsync(new Dictionary<string, PointDto>
        {
            ["image"] = new() { X = 150, Y = 260 },
            ["box"] = new() { X = -30, Y = -5 },
            ["unknown"] = new() { X = 1, Y = 1 }
        }));

        Assert.Equal((150d, 260d), (Element(page, "image").X, Element(page, "image").Y));
        Assert.Equal((0d, 0d), (Element(page, "box").X, Element(page, "box").Y));
        Assert.Equal(["image", "box"], cut.Instance.Selection);
        cut.WaitForAssertion(() => Assert.Contains("left:150px", cut.Find(".mud-ex-notebook-selection[data-id='image']").GetAttribute("style")));

        await cut.InvokeAsync(() => cut.Instance.OnMoveAsync(new Dictionary<string, PointDto> { ["future"] = new() { X = 520, Y = 310 } }));
        notebook.Undo.Undo();
        notebook.Undo.Undo();
        Assert.Equal((100d, 200d), (Element(page, "image").X, Element(page, "image").Y));
        Assert.Equal((400d, 50d), (Element(page, "box").X, Element(page, "box").Y));
        Assert.False(notebook.Undo.CanUndo);

        await cut.InvokeAsync(() => cut.Instance.OnMoveAsync(new Dictionary<string, PointDto> { ["box"] = new() { X = 400, Y = 50 }, ["image"] = new() { X = double.NaN, Y = 1 } }));
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public async Task Canvas_ResizeImage_KeepsAspect()
    {
        var (notebook, page) = Page();
        var cut = Render(notebook, page);

        await cut.InvokeAsync(() => cut.Instance.OnResizeAsync(new Dictionary<string, BoundsDto> { ["image"] = new() { X = 100, Y = 200, Width = 300, Height = 300 } }));
        Assert.Equal(new NoteBounds(100, 200, 300, 150), Element(page, "image").Bounds);

        await cut.InvokeAsync(() => cut.Instance.OnResizeAsync(new Dictionary<string, BoundsDto> { ["image"] = new() { X = 100, Y = 200, Width = 300, Height = 300, Free = true } }));
        Assert.Equal(new NoteBounds(100, 200, 300, 300), Element(page, "image").Bounds);

        await cut.InvokeAsync(() => cut.Instance.OnResizeAsync(new Dictionary<string, BoundsDto>
        {
            ["text"] = new() { X = 10, Y = 20, Width = 500, Height = 80 },
            ["box"] = new() { X = 400, Y = 50, Width = 80, Height = 20 },
            ["future"] = new() { X = 500, Y = 300, Width = 400, Height = 400 }
        }));
        Assert.Equal(new NoteBounds(10, 20, 500, 0), Element(page, "text").Bounds);
        Assert.Equal(new NoteBounds(400, 50, 80, 20), Element(page, "box").Bounds);
        Assert.Equal(new NoteBounds(500, 300, 40, 40), Element(page, "future").Bounds);

        notebook.Undo.Undo();
        Assert.Equal(new NoteBounds(10, 20, 300, 0), Element(page, "text").Bounds);
        Assert.Equal(new NoteBounds(400, 50, 50, 50), Element(page, "box").Bounds);
    }

    [Fact]
    public async Task Canvas_Handles_FollowElementType()
    {
        var (notebook, page) = Page();
        var cut = Render(notebook, page);

        await Key(cut, "a", ctrl: true);

        Assert.Equal(["text", "image", "box", "future"], cut.Instance.Selection);
        string[] Handles(string id) => cut.FindAll($".mud-ex-notebook-selection[data-id='{id}'] [data-handle]").Select(h => h.GetAttribute("data-handle")!).ToArray();
        Assert.Equal(["move", "e"], Handles("text"));
        Assert.Equal(["nw", "n", "ne", "e", "se", "s", "sw", "w"], Handles("image"));
        Assert.Equal(8, Handles("box").Length);
        Assert.Empty(Handles("future"));
        Assert.Equal("text,image,box,future", cut.Find(".mud-ex-notebook-canvas").GetAttribute("data-selection"));
    }

    [Fact]
    public async Task Canvas_Delete_RemovesSelected()
    {
        var (notebook, page) = Page();
        var cut = Render(notebook, page);
        IReadOnlyList<string>? raised = null;
        cut.Render(p => p.Add(c => c.SelectionChanged, ids => raised = ids));
        await Select(cut, "image", "future");
        Assert.Equal(["image", "future"], raised);

        await Key(cut, "Delete");

        Assert.Equal(["text", "box"], page.Elements.Select(e => e.Id));
        Assert.Empty(cut.Instance.Selection);
        Assert.Empty(raised!);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-notebook-selection")));
        notebook.Undo.Undo();
        Assert.Equal(4, page.Elements.Count);
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public async Task Canvas_Nudge_MovesByOnePixel()
    {
        var (notebook, page) = Page();
        var cut = Render(notebook, page);

        await Key(cut, "ArrowRight");
        Assert.False(notebook.Undo.CanUndo);

        await Select(cut, "image", "text");
        await Key(cut, "ArrowRight");
        Assert.Equal(101, Element(page, "image").X);
        Assert.Equal(11, Element(page, "text").X);

        await Key(cut, "ArrowUp", shift: true);
        Assert.Equal(190, Element(page, "image").Y);
        Assert.Equal(10, Element(page, "text").Y);

        await Key(cut, "ArrowUp", shift: true);
        await Key(cut, "ArrowUp", shift: true);
        Assert.Equal(0, Element(page, "text").Y);
        Assert.Equal(170, Element(page, "image").Y);

        notebook.Undo.Undo();
        notebook.Undo.Undo();
        notebook.Undo.Undo();
        notebook.Undo.Undo();
        Assert.Equal((100d, 200d), (Element(page, "image").X, Element(page, "image").Y));
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public async Task Canvas_Duplicate_SelectAll_Tab_Escape()
    {
        var (notebook, page) = Page();
        var cut = Render(notebook, page);

        await Key(cut, "Tab");
        Assert.Equal(["text"], cut.Instance.Selection);
        await Key(cut, "Tab", shift: true);
        Assert.Empty(cut.Instance.Selection);
        foreach (var id in new[] { "text", "image", "box", "future" })
        {
            await Key(cut, "Tab");
            Assert.Equal([id], cut.Instance.Selection);
        }

        await Key(cut, "Tab");
        Assert.Empty(cut.Instance.Selection);

        await Select(cut, "box");
        await Key(cut, "d", ctrl: true);
        var copy = Assert.Single(cut.Instance.Selection);
        Assert.NotEqual("box", copy);
        Assert.Equal((410d, 60d), (Element(page, copy).X, Element(page, copy).Y));

        await Key(cut, "Escape");
        Assert.Empty(cut.Instance.Selection);
    }

    [Fact]
    public async Task Canvas_MoveBeyondRightEdge_PageGrows()
    {
        var (notebook, page) = Page();
        var cut = Render(notebook, page);
        Assert.Contains("width:940px;height:740px", PageStyle(cut));

        await cut.InvokeAsync(() => cut.Instance.OnMoveAsync(new Dictionary<string, PointDto> { ["box"] = new() { X = 2000, Y = 3000 } }));
        cut.WaitForAssertion(() => Assert.Contains("width:2450px;height:3450px", PageStyle(cut)));

        await Select(cut, "box");
        await Key(cut, "Delete");
        cut.WaitForAssertion(() => Assert.Contains("width:940px;height:740px", PageStyle(cut)));
    }

    [Fact]
    public async Task Canvas_MeasuredHeights_SizeSelectionAndPage()
    {
        var (notebook, page) = Page();
        var cut = Render(notebook, page);
        await Select(cut, "text");

        await cut.InvokeAsync(() => cut.Instance.OnHeightsMeasuredAsync(new Dictionary<string, double> { ["text"] = 1000, ["image"] = double.NaN }));

        cut.WaitForAssertion(() => Assert.Contains("height:1000px", cut.Find(".mud-ex-notebook-selection[data-id='text']").GetAttribute("style")));
        Assert.Contains("width:940px;height:1420px", PageStyle(cut));
        Assert.Equal(1000, cut.Instance.MeasuredHeights["text"]);
        Assert.False(cut.Instance.MeasuredHeights.ContainsKey("image"));
    }

    [Fact]
    public async Task Canvas_ReadOnly_HasNoHandles()
    {
        var (notebook, page) = Page();
        NotebookTextEditRequest? request = null;
        var cut = Render(notebook, page, readOnly: true, requested: r => request = r);

        await Select(cut, "image");
        await Key(cut, "a", ctrl: true);
        await cut.InvokeAsync(() => cut.Instance.OnPageClickAsync(50, 500));
        await cut.InvokeAsync(() => cut.Instance.OnMoveAsync(new Dictionary<string, PointDto> { ["image"] = new() { X = 0, Y = 0 } }));
        await cut.InvokeAsync(() => cut.Instance.OnResizeAsync(new Dictionary<string, BoundsDto> { ["image"] = new() { X = 0, Y = 0, Width = 9, Height = 9 } }));

        Assert.Empty(cut.Instance.Selection);
        Assert.Empty(cut.FindAll(".mud-ex-notebook-selection"));
        Assert.Empty(cut.FindAll("[data-handle]"));
        Assert.Null(request);
        Assert.False(notebook.Undo.CanUndo);
        Assert.Equal("true", cut.Find(".mud-ex-notebook-canvas").GetAttribute("data-readonly"));
    }
}
