using System.Buffers.Binary;
using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Notebook;

public class NotebookFeaturesTests : IAsyncLifetime
{
    private readonly BunitContext _context = new();
    private readonly IRenderedComponent<MudPopoverProvider> _popovers;
    private readonly IRenderedComponent<MudDialogProvider> _dialogs;

    public NotebookFeaturesTests()
    {
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        _popovers = _context.Render<MudPopoverProvider>();
        _dialogs = _context.Render<MudDialogProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _context.DisposeAsync();

    private static NoteParagraph Para(string text) => new() { Runs = [new NoteRun(text)] };

    private static string TextOf(IReadOnlyList<NoteParagraph> paragraphs) => string.Join("|", paragraphs.Select(p => string.Concat(p.Runs.Select(r => r.Text))));

    private static byte[] Png(int width, int height)
    {
        var png = new byte[24];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(png, 0);
        "IHDR"u8.CopyTo(png.AsSpan(12));
        BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(16), width);
        BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(20), height);
        return png;
    }

    private IRenderedComponent<MudExNotebookEditor> Editor(Notebook notebook)
    {
        var cut = _context.Render<MudExNotebookEditor>(p => p.Add(e => e.Notebook, notebook));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-notebook-canvas")));
        return cut;
    }

    private IRenderedComponent<MudExNotebookCanvas> Canvas(Notebook notebook, NotebookPage page) =>
        _context.Render<MudExNotebookCanvas>(p => p.Add(c => c.Notebook, notebook).Add(c => c.Page, page).Add(c => c.ViewportWidth, 800).Add(c => c.ViewportHeight, 600));

    private static MudExNotebookCanvas CanvasOf(IRenderedComponent<MudExNotebookEditor> cut) => cut.FindComponent<MudExNotebookCanvas>().Instance;

    private static Task Run(IRenderedComponent<MudExNotebookEditor> cut, string id) =>
        cut.InvokeAsync(() => cut.FindComponent<MudExRibbon>().Instance.Commands.First(c => c.Id == id).ExecuteAsync());

    private static NotebookTextParagraphDto Dto(int? source, string text) => new() { Source = source, Runs = [new NotebookTextRunDto { Source = source, Text = text }] };

    [Fact]
    public async Task Shape_InsertArrow_AddsShapeElement()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        var cut = Editor(notebook);

        await Run(cut, "nb-shape-arrow");
        cut.WaitForAssertion(() => Assert.Equal((NotebookTool.Shape, NoteShapeKind.Arrow), (CanvasOf(cut).Tool, CanvasOf(cut).ShapeKind)));
        Assert.Equal("arrow", cut.Find(".mud-ex-notebook-canvas").GetAttribute("data-shape"));
        await cut.InvokeAsync(() => CanvasOf(cut).OnShapeAsync(200, 100, 100, 160));

        var shape = Assert.IsType<ShapeElement>(Assert.Single(page.Elements));
        Assert.Equal((NoteShapeKind.Arrow, new NoteBounds(100, 100, 100, 60), true, false), (shape.Kind, shape.Bounds, shape.FlipX, shape.FlipY));
        Assert.Equal([shape.Id], CanvasOf(cut).Selection);
        cut.WaitForAssertion(() => Assert.Equal(NotebookTool.Select, CanvasOf(cut).Tool));
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll($"[data-element-id='{shape.Id}'] path:not(.mud-ex-notebook-hit)").Count));

        await cut.InvokeAsync(() => cut.FindComponents<MudExRibbonColorPicker>().Single(p => p.Instance.Id == "nb-shape-outline").Instance.ValueChanged.InvokeAsync((MudExColor)"#FF0000"));
        Assert.Equal("#FF0000", ((ShapeElement)page.Elements[0]).Stroke);

        notebook.Undo.Undo();
        notebook.Undo.Undo();
        Assert.Empty(page.Elements);
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public async Task Image_Insert_LimitsWidthKeepsAspect()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        var cut = Editor(notebook);

        await cut.InvokeAsync(() => cut.Instance.InsertPictureAsync(Png(1200, 900), "image/png", 30, 40));
        await cut.InvokeAsync(() => cut.Instance.InsertPictureAsync(Encoding.UTF8.GetBytes("<html></html>"), "text/html", 0, 0));

        var image = Assert.IsType<ImageElement>(Assert.Single(page.Elements));
        Assert.Equal(new NoteBounds(30, 40, 600, 450), image.Bounds);
        Assert.Equal([image.Id], CanvasOf(cut).Selection);
        Assert.Contains(_context.JSInterop.Invocations, i => i.Identifier == "register" && Equals(i.Arguments[1], "image/png"));

        await cut.InvokeAsync(() => CanvasOf(cut).OnContextMenuAsync(image.Id, -1, -1, 50, 50));
        _popovers.WaitForAssertion(() => _popovers.Find(".mud-ex-notebook-menu-alt-text").Click());
        _dialogs.WaitForAssertion(() => _dialogs.Find(".mud-ex-nbalt-text textarea").Input("A chart "));
        _dialogs.Find(".mud-ex-nbalt-ok").Click();
        cut.WaitForAssertion(() => Assert.Equal("A chart", ((ImageElement)page.Elements[0]).AltText));
        notebook.Undo.Undo();
        Assert.Equal("", ((ImageElement)page.Elements[0]).AltText);
    }

    [Fact]
    public async Task Image_SvgWithScript_ShownOnlyAsImgDataUri()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        var cut = Editor(notebook);
        var svg = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"50\"><script>alert(1)</script><rect width=\"100\" height=\"50\"/></svg>");

        await cut.InvokeAsync(() => cut.Instance.InsertPictureAsync(svg, "image/svg+xml", 0, 0));

        var image = Assert.IsType<ImageElement>(Assert.Single(page.Elements));
        cut.WaitForAssertion(() => Assert.StartsWith("data:image/svg+xml;base64,", cut.Find($"[data-element-id='{image.Id}'] img").GetAttribute("src")));
        Assert.DoesNotContain("<script", cut.Find(".mud-ex-notebook-page").InnerHtml);
        Assert.Empty(cut.FindAll($"[data-element-id='{image.Id}'] svg"));
        Assert.DoesNotContain(_context.JSInterop.Invocations, i => i.Identifier == "register");
    }

    [Fact]
    public async Task Table_InsertFromGrid()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        var cut = Editor(notebook);

        await cut.InvokeAsync(() => cut.Instance.InsertTableAsync((3, 4)));

        var table = Assert.IsType<TableElement>(Assert.Single(page.Elements));
        Assert.Equal((3, 4), (table.Rows.Count, table.ColumnWidths.Count));
        Assert.Equal((40d, 40d), (table.X, table.Y));
        var edit = CanvasOf(cut).TextEdit!;
        Assert.Equal((table.Id, 0, 0), (edit.ElementId, edit.Row, edit.Column));
        var editor = cut.FindComponent<MudExNotebookTextEditor>();
        Assert.Equal("0,0", editor.Instance.Cell);
        Assert.False(editor.Instance.TableTab);

        await cut.InvokeAsync(() => editor.Instance.OnTextCommitAsync([Dto(null, "Cell text")], null));
        Assert.Equal("Cell text", TextOf(((TableElement)page.Elements[0]).Rows[0][0].Paragraphs));
        cut.WaitForAssertion(() => Assert.Contains("Cell text", cut.Find("td[data-cell='0,0']").TextContent));

        notebook.Undo.Undo();
        notebook.Undo.Undo();
        Assert.Empty(page.Elements);
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public async Task Table_TabInTextBlock_BecomesTable()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        notebook.Execute(new AddElementCommand(page, new TextElement { Id = "text", X = 10, Y = 20, Width = 300, Paragraphs = [Para("Hello")] }));
        notebook.Execute(new AddElementCommand(page, new TextElement { Id = "list", X = 10, Y = 400, Width = 300, Paragraphs = [Para("Item") with { List = NoteListKind.Bullet }] }));
        notebook.Undo.Clear();
        var cut = Canvas(notebook, page);

        await cut.InvokeAsync(() => cut.Instance.OnPageClickAsync(20, 25));
        var editor = cut.FindComponent<MudExNotebookTextEditor>();
        Assert.True(editor.Instance.TableTab);
        await cut.InvokeAsync(() => editor.Instance.OnTabAsync([Dto(0, "Hello")], null, false));

        var table = Assert.IsType<TableElement>(page.Elements.Single(e => e.Id != "list"));
        Assert.Equal((10d, 20d, 1, 2), (table.X, table.Y, table.Rows.Count, table.ColumnWidths.Count));
        Assert.Equal("Hello", TextOf(table.Rows[0][0].Paragraphs));
        Assert.Equal((table.Id, 0, 1), (cut.Instance.TextEdit!.ElementId, cut.Instance.TextEdit.Row, cut.Instance.TextEdit.Column));
        cut.WaitForAssertion(() => Assert.Equal("0,1", cut.FindComponent<MudExNotebookTextEditor>().Instance.Cell));

        notebook.Undo.Undo();
        Assert.Equal("Hello", TextOf(((TextElement)page.Elements.Single(e => e.Id == "text")).Paragraphs));
        Assert.False(notebook.Undo.CanUndo);

        await cut.InvokeAsync(() => cut.Instance.OnPageClickAsync(20, 405));
        await cut.InvokeAsync(() => cut.FindComponent<MudExNotebookTextEditor>().Instance.OnTabAsync([new NotebookTextParagraphDto { Source = 0, List = "bullet", Runs = [new NotebookTextRunDto { Source = 0, Text = "Item" }] }], null, false));
        Assert.IsType<TextElement>(page.Elements.Single(e => e.Id == "list"));
        Assert.Equal(2, page.Elements.Count);
    }

    [Fact]
    public async Task Table_TabInLastCell_AddsRow()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        var add = new AddTableCommand(page, 1, 2, 10, 10);
        notebook.Execute(add);
        notebook.Undo.Clear();
        var id = add.Table.Id;
        var cut = Canvas(notebook, page);

        await cut.InvokeAsync(() => cut.Instance.OnCellClickAsync(id, 0, 1));
        Assert.Equal((0, 1), (cut.Instance.TextEdit!.Row, cut.Instance.TextEdit.Column));
        await cut.InvokeAsync(() => cut.FindComponent<MudExNotebookTextEditor>().Instance.OnTabAsync([Dto(null, "b")], null, false));

        var table = (TableElement)page.Elements.Single();
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("b", TextOf(table.Rows[0][1].Paragraphs));
        Assert.Equal((1, 0), (cut.Instance.TextEdit!.Row, cut.Instance.TextEdit.Column));

        await cut.InvokeAsync(() => cut.FindComponent<MudExNotebookTextEditor>().Instance.OnTabAsync([Dto(null, "c")], null, true));
        Assert.Equal((0, 1), (cut.Instance.TextEdit!.Row, cut.Instance.TextEdit.Column));
        Assert.Equal("c", TextOf(((TableElement)page.Elements.Single()).Rows[1][0].Paragraphs));

        notebook.Undo.Undo();
        notebook.Undo.Undo();
        table = (TableElement)page.Elements.Single();
        Assert.Single(table.Rows);
        Assert.Equal("", TextOf(table.Rows[0][1].Paragraphs));
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public async Task Clipboard_PasteForeignText_AddsTextBlockAtLastClick()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        var cut = Editor(notebook);
        await Run(cut, "nb-draw-select");
        cut.WaitForAssertion(() => Assert.Equal(NotebookTool.Select, CanvasOf(cut).Tool));

        await cut.InvokeAsync(() => CanvasOf(cut).OnPageClickAsync(300, 400));
        await cut.InvokeAsync(() => cut.Instance.PasteAsync("first\r\nsecond\r\n", null, null));

        var text = Assert.IsType<TextElement>(Assert.Single(page.Elements));
        Assert.Equal((300d, 400d), (text.X, text.Y));
        Assert.Equal("first|second", TextOf(text.Paragraphs));
        Assert.Equal([text.Id], CanvasOf(cut).Selection);
        notebook.Undo.Undo();
        Assert.Empty(page.Elements);
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public async Task Clipboard_CopyPasteElements_KeepsContent()
    {
        var notebook = Notebook.Create();
        var first = notebook.Sections[0].Pages[0];
        var second = new AddPageCommand(notebook.Sections[0], 1, "Second");
        notebook.Execute(second);
        notebook.Execute(new AddElementCommand(first, new TextElement { Id = "text", X = 10, Y = 20, Width = 300, Paragraphs = [Para("Hello"), Para("World")] }));
        var stroke = new AddStrokeCommand(first, [new InkPoint(50, 50, 0.5), new InkPoint(80, 90, 0.7)], "#C00000", 3, InkTool.Pen);
        notebook.Execute(stroke);
        var image = new AddImageCommand(first, Png(20, 10), "image/png", 200, 200, 600);
        notebook.Execute(image);
        notebook.Undo.Clear();
        var cut = Editor(notebook);
        var ids = new[] { "text", stroke.Stroke.Id, image.Image.Id };
        await cut.InvokeAsync(() => CanvasOf(cut).SelectAsync(ids));

        await cut.InvokeAsync(() => cut.Instance.CopyElementsAsync(false));
        await cut.InvokeAsync(() => cut.Instance.PasteAsync(null, null, null));

        Assert.Equal(6, first.Elements.Count);
        var copies = first.Elements.Skip(3).ToList();
        Assert.Equal(copies.Select(c => c.Id), CanvasOf(cut).Selection);
        Assert.DoesNotContain(copies, c => ids.Contains(c.Id));
        Assert.Equal("Hello|World", TextOf(((TextElement)copies[0]).Paragraphs));
        Assert.Equal((20d, 30d), (copies[0].X, copies[0].Y));
        Assert.Equal(stroke.Stroke.Points, ((InkElement)copies[1]).Points);
        Assert.Equal(("#C00000", 3d), (((InkElement)copies[1]).Color, ((InkElement)copies[1]).StrokeWidth));
        Assert.Equal(image.Image.MediaId, ((ImageElement)copies[2]).MediaId);
        Assert.Contains(_context.JSInterop.Invocations, i => i.Identifier == "writeClipboard" && Equals(i.Arguments[0], "Hello\r\nWorld"));
        notebook.Undo.Undo();
        Assert.Equal(3, first.Elements.Count);
        Assert.False(notebook.Undo.CanUndo);

        await cut.InvokeAsync(() => CanvasOf(cut).SelectAsync(ids));
        await cut.InvokeAsync(() => cut.Instance.CopyElementsAsync(true));
        Assert.Empty(first.Elements);
        cut.FindAll(".mud-ex-notebook-page-item")[1].Click();
        cut.WaitForAssertion(() => Assert.Same(second.Page, cut.Instance.CurrentPage));
        await cut.InvokeAsync(() => cut.Instance.PasteAsync(null, null, null));
        Assert.Equal([(10d, 20d), (stroke.Stroke.X, stroke.Stroke.Y), (200d, 200d)], second.Page.Elements.Select(e => (e.X, e.Y)));
    }

    [Fact]
    public void Search_Notebook_FindsInTitlesTextAndCells()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        notebook.Execute(new RenamePageCommand(page, "Apple pie"));
        notebook.Execute(new AddElementCommand(page, new TextElement { Id = "text", Width = 100, Paragraphs = [Para("Pears"), Para("I like apples")] }));
        notebook.Execute(new AddElementCommand(page, new TextElement { Id = "other", Width = 100, Paragraphs = [Para("banana")] }));
        var table = new AddTableCommand(page, 2, 2, 0, 300);
        notebook.Execute(table);
        notebook.Execute(new SetTableCellCommand(page, table.Table.Id, 1, 1, [Para("green apple")]));
        var section = new AddSectionCommand(1, "Garden", "#00FF00");
        notebook.Execute(section);
        notebook.Execute(new AddElementCommand(section.Section.Pages[0], new TextElement { Id = "tree", Width = 100, Paragraphs = [Para("Apple tree")] }));

        var cut = _context.Render<MudExNotebookSearchPanel>(p => p.Add(x => x.Notebook, notebook).Add(x => x.Page, page).Add(x => x.WholeNotebook, true).Add(x => x.Query, "APPLE"));

        Assert.Equal(["Apple pie", "I like apples", "green apple", "Apple tree"], cut.FindAll(".mud-ex-notebook-search-result").Select(r => r.TextContent));
        Assert.Equal(2, cut.FindAll(".mud-ex-notebook-search-section").Count);
        Assert.Equal([null, "text", table.Table.Id, "tree"], MudExNotebookSearchPanel.Search(notebook, null, "apple").Select(r => r.ElementId));

        cut.Render(p => p.Add(x => x.WholeNotebook, false));
        Assert.Equal(3, cut.FindAll(".mud-ex-notebook-search-result").Count);
        cut.Render(p => p.Add(x => x.Query, "kiwi"));
        Assert.Single(cut.FindAll(".mud-ex-notebook-search-empty"));
    }

    [Fact]
    public async Task Search_Select_JumpsToPage()
    {
        var notebook = Notebook.Create();
        var second = new AddPageCommand(notebook.Sections[0], 1, "Second");
        notebook.Execute(second);
        notebook.Execute(new AddElementCommand(second.Page, new TextElement { Id = "hit", X = 10, Y = 10, Width = 200, Paragraphs = [Para("a needle here")] }));
        var cut = Editor(notebook);

        await cut.Find(".mud-ex-notebook-editor-surface").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "e", CtrlKey = true });
        cut.WaitForAssertion(() => cut.Find(".mud-ex-notebook-search-input input").Input("needle"));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-notebook-search-result")));
        cut.Find(".mud-ex-notebook-search-result").Click();

        cut.WaitForAssertion(() => Assert.Same(second.Page, cut.Instance.CurrentPage));
        cut.WaitForAssertion(() => Assert.Equal(["hit"], CanvasOf(cut).Selection));
        Assert.Single(cut.FindAll(".mud-ex-notebook-selection[data-id='hit']"));
    }
}
