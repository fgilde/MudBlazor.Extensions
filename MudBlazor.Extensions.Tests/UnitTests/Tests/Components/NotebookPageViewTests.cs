using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Notebook;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Notebook;

public class NotebookPageViewTests
{
    private static IRenderedComponent<MudExNotebookPageView> Render(TestContext context, Notebook notebook, NotebookPage? page = null,
        IReadOnlyDictionary<string, double>? measured = null, double viewportWidth = 0, double viewportHeight = 0, double zoom = 1)
    {
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context.Render<MudExNotebookPageView>(p => p
            .Add(c => c.Notebook, notebook)
            .Add(c => c.Page, page ?? notebook.Sections[0].Pages[0])
            .Add(c => c.Zoom, zoom)
            .Add(c => c.ViewportWidth, viewportWidth)
            .Add(c => c.ViewportHeight, viewportHeight)
            .Add(c => c.MeasuredHeights, measured));
    }

    private static (Notebook Notebook, NotebookPage Page) With(params NoteElement[] elements)
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        foreach (var element in elements)
            notebook.Execute(new AddElementCommand(page, element));
        return (notebook, page);
    }

    private static TextElement Text(string id, params NoteParagraph[] paragraphs) => new() { Id = id, X = 10, Y = 20, Width = 300, Paragraphs = paragraphs };

    private static NoteParagraph Paragraph(string text, NoteListKind list = NoteListKind.None, int indent = 0, bool done = false, NoteRunFormat? format = null) =>
        new() { Runs = [new NoteRun(text, format)], List = list, Indent = indent, Checked = done };

    private static ShapeElement Box(string id, double x, double y) => new() { Id = id, X = x, Y = y, Width = 50, Height = 50, Kind = NoteShapeKind.Rectangle };

    private static string RootStyle(IRenderedComponent<MudExNotebookPageView> cut) => cut.Find(".mud-ex-notebook-page").GetAttribute("style")!;

    [Fact]
    public void PageView_ElementAtAbsolutePosition()
    {
        var (notebook, _) = With(Box("box", 120, 340));
        using var context = new TestContext();

        var cut = Render(context, notebook);

        var style = cut.Find("[data-element-id='box']").GetAttribute("style");
        Assert.Contains("left:120px", style);
        Assert.Contains("top:340px", style);
        Assert.Contains("width:50px", style);
        Assert.Contains("height:50px", style);
    }

    [Fact]
    public void PageView_SizeFollowsContentBounds_GrowsAndShrinks()
    {
        var (notebook, page) = With(Box("box", 100, 100));
        using var context = new TestContext();
        var cut = Render(context, notebook, viewportWidth: 800, viewportHeight: 600);
        Assert.Contains("width:800px;height:600px", RootStyle(cut));

        notebook.Execute(new MoveElementsCommand(page, new Dictionary<string, (double X, double Y)> { ["box"] = (1000, 2000) }));
        cut.Render(p => p.Add(c => c.Page, page));
        Assert.Contains("width:1450px;height:2450px", RootStyle(cut));

        notebook.Execute(new DeleteElementsCommand(page, ["box"]));
        cut.Render(p => p.Add(c => c.Page, page));
        Assert.Contains("width:800px;height:600px", RootStyle(cut));

        cut.Render(p => p.Add(c => c.ViewportWidth, 0).Add(c => c.ViewportHeight, 0));
        Assert.Contains("width:400px;height:400px", RootStyle(cut));

        cut.Render(p => p.Add(c => c.Zoom, 2).Add(c => c.ViewportWidth, 1000).Add(c => c.ViewportHeight, 1000));
        Assert.Contains("width:1000px;height:1000px", RootStyle(cut));
        Assert.Contains("width:500px;height:500px", cut.Find(".mud-ex-notebook-page-content").GetAttribute("style"));
    }

    [Fact]
    public void PageView_TextHeight_UsesMeasuredHeight_ElseEstimate()
    {
        var (notebook, page) = With(Text("text", Paragraph("one"), Paragraph("two"), Paragraph("three")));
        using var context = new TestContext();

        var estimated = Render(context, notebook);
        var style = RootStyle(estimated);
        Assert.DoesNotContain("height:420px", style);
        Assert.Matches(@"height:(4[3-9]\d|5\d\d)(\.\d+)?px", style);

        using var second = new TestContext();
        var measured = Render(second, notebook, page, new Dictionary<string, double> { ["text"] = 300 });
        Assert.Contains("width:710px;height:720px", RootStyle(measured));
    }

    [Fact]
    public void PageView_ScriptTextRendersAsText()
    {
        const string script = "<script>alert(1)</script><b>x</b>";
        var (notebook, _) = With(Text("text", Paragraph(script)));
        using var context = new TestContext();

        var cut = Render(context, notebook);

        Assert.Empty(cut.FindAll(".mud-ex-notebook-page script"));
        Assert.Empty(cut.FindAll(".mud-ex-notebook-page b"));
        Assert.Equal(script, cut.Find("[data-element-id='text'] .mud-ex-notebook-run").TextContent);
    }

    [Fact]
    public void PageView_NumberedListAcrossIndent()
    {
        var (notebook, _) = With(Text("text", Paragraph("a", NoteListKind.Number), Paragraph("b", NoteListKind.Number, 1), Paragraph("c", NoteListKind.Number, 1),
            Paragraph("d", NoteListKind.Number), Paragraph("e", NoteListKind.Bullet)));
        using var context = new TestContext();

        var cut = Render(context, notebook);

        var markers = cut.FindAll(".mud-ex-notebook-marker");
        Assert.Equal(["1.", "a.", "b.", "2.", "•"], markers.Select(m => m.TextContent.Trim()));
        Assert.All(markers, m => Assert.Equal("false", m.GetAttribute("contenteditable")));
        Assert.Contains("padding-left:24px", cut.FindAll(".mud-ex-notebook-paragraph")[1].GetAttribute("style"));
    }

    [Fact]
    public void PageView_TodoShowsCheckedState()
    {
        var (notebook, _) = With(Text("text", Paragraph("done", NoteListKind.Todo, done: true), Paragraph("open", NoteListKind.Todo)));
        using var context = new TestContext();

        var cut = Render(context, notebook);

        var boxes = cut.FindAll(".mud-ex-notebook-todo");
        Assert.Equal(["true", "false"], boxes.Select(b => b.GetAttribute("aria-checked")));
        Assert.Contains("mud-ex-notebook-todo-checked", boxes[0].ClassName);
        Assert.DoesNotContain("mud-ex-notebook-todo-checked", boxes[1].ClassName);
    }

    [Fact]
    public void PageView_InkRendersPathWithStrokeWidth()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        var stroke = new AddStrokeCommand(page, [new InkPoint(10, 10, 0.5), new InkPoint(30, 20, 0.5), new InkPoint(50, 10, 0.5)], "#FF0000", 4, InkTool.Pen);
        notebook.Execute(stroke);
        using var context = new TestContext();

        var cut = Render(context, notebook);

        var element = cut.Find($"[data-element-id='{stroke.Stroke.Id}']");
        Assert.Equal("svg", element.QuerySelector("svg")!.TagName.ToLowerInvariant());
        var path = element.QuerySelector("path")!;
        Assert.Equal("M2 2L22 12L42 2", path.GetAttribute("d"));
        Assert.Equal("4", path.GetAttribute("stroke-width"));
        Assert.Equal("#FF0000", path.GetAttribute("stroke"));
        Assert.DoesNotContain("mud-ex-notebook-highlighter", element.ClassName);
    }

    [Fact]
    public void PageView_HighlighterIsTranslucent()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        var stroke = new AddStrokeCommand(page, [new InkPoint(10, 10, 0.5)], "#FFFF00", 12, InkTool.Highlighter);
        notebook.Execute(stroke);
        using var context = new TestContext();

        var cut = Render(context, notebook);

        var svg = cut.Find($"[data-element-id='{stroke.Stroke.Id}'] svg");
        Assert.Contains("mud-ex-notebook-highlighter", svg.ClassName);
        Assert.Equal("M6 6L6 6", svg.QuerySelector("path")!.GetAttribute("d"));
    }

    [Fact]
    public void PageView_UnchangedElement_DoesNotRerender()
    {
        var (notebook, page) = With(Box("a", 0, 0), Box("b", 100, 0));
        using var context = new TestContext();
        var cut = Render(context, notebook);
        var views = cut.FindComponents<MudExNotebookElementView>().ToDictionary(v => v.Instance.Element.Id);
        var (a, b) = (views["a"].RenderCount, views["b"].RenderCount);

        cut.Render(p => p.Add(c => c.Page, page));
        Assert.Equal(a, views["a"].RenderCount);

        notebook.Execute(new MoveElementsCommand(page, new Dictionary<string, (double X, double Y)> { ["b"] = (200, 50) }));
        cut.Render(p => p.Add(c => c.Page, page));

        Assert.Equal(a, views["a"].RenderCount);
        Assert.True(views["b"].RenderCount > b);
        Assert.Contains("left:200px", cut.Find("[data-element-id='b']").GetAttribute("style"));
    }

    [Fact]
    public void PageView_LoadError_ShowsAlert()
    {
        var notebook = new NotebookBuilder().RawPage("p1", "{ broken").Load();
        using var context = new TestContext();

        var cut = Render(context, notebook);

        Assert.NotNull(cut.Find(".mud-alert"));
        Assert.Empty(cut.FindAll(".mud-ex-notebook-page-content"));
    }

    [Fact]
    public void PageView_LinkWithoutAllowedScheme_IsPlainText()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        page.ElementList.Add(Text("text",
            new NoteParagraph { Runs = [new NoteRun("safe", new NoteRunFormat(Link: "https://example.test/")), new NoteRun("evil", new NoteRunFormat(Link: "javascript:alert(1)"))] }));
        using var context = new TestContext();

        var cut = Render(context, notebook);

        var link = cut.Find("a");
        Assert.Equal("https://example.test/", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
        Assert.Single(cut.FindAll("a"));
        Assert.Equal("evil", cut.FindAll(".mud-ex-notebook-run").Single(r => r.TextContent == "evil").TextContent);
        Assert.DoesNotContain("javascript", cut.Markup);
    }

    [Fact]
    public void PageView_RunStyle_SanitisesFontAndColors()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        page.ElementList.Add(Text("text", Paragraph("x", format: new NoteRunFormat(Bold: true, SizePt: 12, Color: "red;background:url(x)", Highlight: "#FFFF00",
            Font: "Arial\";}body{x:url(y)"))));
        using var context = new TestContext();

        var style = Render(context, notebook).Find(".mud-ex-notebook-run").GetAttribute("style");

        Assert.Contains("font-size:16px", style);
        Assert.Contains("font-weight:bold", style);
        Assert.Contains("background-color:#FFFF00", style);
        Assert.DoesNotContain("url", style);
        Assert.DoesNotContain("red", style);
        Assert.DoesNotContain("}", style);
    }

    [Fact]
    public void PageView_ImageUsesDataUri_OpaqueShowsType()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        var image = new AddImageCommand(page, NotebookBuilder.Png, "image/png", 0, 0, 100);
        notebook.Execute(image);
        page.ElementList.Add(new OpaqueElement { Id = "future", Type = "hologram", X = 300, Y = 0, Width = 40, Height = 40 });
        using var context = new TestContext();

        var cut = Render(context, notebook);

        var src = cut.Find($"[data-element-id='{image.Image.Id}'] img").GetAttribute("src");
        Assert.StartsWith("data:image/png;base64,", src);
        Assert.Contains("hologram", cut.Find("[data-element-id='future']").TextContent);

        var urls = new NotebookImageUrls(notebook);
        Assert.True(urls.NeedsBlob(image.Image.MediaId));
        urls.Register(image.Image.MediaId, "blob:x");
        Assert.Equal("blob:x", urls.UrlFor(image.Image.MediaId));
        Assert.False(urls.NeedsBlob(image.Image.MediaId));
    }
}
