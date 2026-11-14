using Bunit;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Notebook;

public class NotebookInteropTests : IAsyncLifetime
{
    private readonly BunitContext _context = new();

    public NotebookInteropTests()
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
        var page = notebook.Sections[0].Pages[0];
        notebook.Execute(new AddTextCommand(page, 10, 20, 300, [new NoteParagraph { Runs = [new NoteRun("Hello")] }]));
        notebook.Execute(new AddShapeCommand(page, NoteShapeKind.Rectangle, new NoteBounds(400, 50, 50, 50)));
        notebook.Execute(new AddShapeCommand(page, NoteShapeKind.Ellipse, new NoteBounds(500, 300, 40, 40)));
        notebook.Undo.Clear();
        return (notebook, page);
    }

    private IRenderedComponent<MudExNotebookCanvas> Canvas(Notebook notebook, NotebookPage page, NotebookTool tool = NotebookTool.Text, Action<Exception>? failed = null) =>
        _context.Render<MudExNotebookCanvas>(p =>
        {
            p.Add(c => c.Notebook, notebook).Add(c => c.Page, page).Add(c => c.Tool, tool).Add(c => c.ViewportWidth, 800).Add(c => c.ViewportHeight, 600);
            if (failed != null)
                p.Add(c => c.CommandFailed, failed);
        });

    private static string Id(NotebookPage page, int index) => page.Elements[index].Id;

    [Fact]
    public async Task Stroke_LongPointListAsStream_AddsOneStroke_TooManyValuesIgnored()
    {
        var (notebook, page) = Page();
        var cut = Canvas(notebook, page, NotebookTool.Pen);
        var values = Enumerable.Range(0, 2000).SelectMany(i => new[] { 10 + i * 0.37, 400 + Math.Sin(i / 20.0) * 30, 0.5 }).ToArray();
        var stream = new JsonStreamReference(values);

        await cut.InvokeAsync(() => cut.Instance.OnStrokeAsync(null!, stream));

        Assert.True(stream.Disposed);
        var ink = Assert.IsType<InkElement>(page.Elements[^1]);
        Assert.InRange(ink.Points.Count, 2, 2000);
        await cut.InvokeAsync(() => cut.Instance.OnStrokeAsync(null!, new JsonStreamReference(new double[300_003])));
        Assert.Equal(4, page.Elements.Count);
    }

    [Fact]
    public async Task Marquee_SelectsElementsCompletelyInside_UsingMeasuredHeights()
    {
        var (notebook, page) = Page();
        var cut = Canvas(notebook, page, NotebookTool.Select);
        await cut.InvokeAsync(() => cut.Instance.OnHeightsMeasuredAsync(new Dictionary<string, double> { [Id(page, 0)] = 200 }));

        await cut.InvokeAsync(() => cut.Instance.OnMarqueeAsync(0, 0, 460, 200, false));
        Assert.Equal([Id(page, 1)], cut.Instance.Selection);

        await cut.InvokeAsync(() => cut.Instance.OnMarqueeAsync(0, 0, 320, 230, true));
        Assert.Equal([Id(page, 1), Id(page, 0)], cut.Instance.Selection);
    }

    [Fact]
    public async Task ElementClick_SelectsOrToggles()
    {
        var (notebook, page) = Page();
        var cut = Canvas(notebook, page, NotebookTool.Select);

        await cut.InvokeAsync(() => cut.Instance.OnElementClickAsync(Id(page, 1), false));
        await cut.InvokeAsync(() => cut.Instance.OnElementClickAsync(Id(page, 2), true));
        Assert.Equal([Id(page, 1), Id(page, 2)], cut.Instance.Selection);
        await cut.InvokeAsync(() => cut.Instance.OnElementClickAsync(Id(page, 2), false));
        Assert.Equal([Id(page, 1), Id(page, 2)], cut.Instance.Selection);
        await cut.InvokeAsync(() => cut.Instance.OnElementClickAsync(Id(page, 1), true));
        Assert.Equal([Id(page, 2)], cut.Instance.Selection);
    }

    [Fact]
    public async Task Move_ByOffset_MovesTheSelection_OrTheGivenIds()
    {
        var (notebook, page) = Page();
        var cut = Canvas(notebook, page, NotebookTool.Select);
        await cut.InvokeAsync(() => cut.Instance.OnSelectionAsync([Id(page, 1), Id(page, 2)]));

        await cut.InvokeAsync(() => cut.Instance.OnMoveAsync(10, -60, null));
        Assert.Equal([(410d, 0d), (510d, 240d)], page.Elements.Skip(1).Select(e => (e.X, e.Y)));

        await cut.InvokeAsync(() => cut.Instance.OnMoveAsync(5, 5, [Id(page, 0)]));
        Assert.Equal((15d, 25d), (page.Elements[0].X, page.Elements[0].Y));
    }

    [Fact]
    public async Task CommandFailure_IsReportedInsteadOfThrown()
    {
        var (notebook, page) = Page();
        Exception? failure = null;
        var cut = Canvas(notebook, page, NotebookTool.Shape, e => failure = e);
        notebook.Execute(new AddPageCommand(notebook.Sections[0], 1));
        notebook.Execute(new DeletePagesCommand([page]));

        await cut.InvokeAsync(() => cut.Instance.OnShapeAsync(10, 10, 60, 60));

        Assert.IsType<ArgumentException>(failure);
    }

    [Fact]
    public async Task SwitchingToReadOnly_CommitsTheRunningTextSession()
    {
        var (notebook, page) = Page();
        _context.JSInterop.Setup<IJSStreamReference>("readData", _ => true)
            .SetResult(new JsonStreamReference(new[] { new NotebookTextParagraphDto { Runs = [new NotebookTextRunDto { Text = "Typed" }] } }));
        var cut = Canvas(notebook, page);
        await cut.InvokeAsync(() => cut.Instance.OnPageClickAsync(50, 500));
        Assert.True(cut.Instance.IsEditingText);

        cut.Render(p => p.Add(c => c.ReadOnly, true));

        Assert.False(cut.Instance.IsEditingText);
        var text = Assert.IsType<TextElement>(page.Elements[^1]);
        Assert.Equal("Typed", text.Paragraphs[0].Runs[0].Text);
    }

    [Fact]
    public async Task DrawingTool_ClearsTheSelection()
    {
        var (notebook, page) = Page();
        var cut = Canvas(notebook, page, NotebookTool.Select);
        await cut.InvokeAsync(() => cut.Instance.OnSelectionAsync([Id(page, 1)]));

        cut.Render(p => p.Add(c => c.Tool, NotebookTool.Lasso));
        Assert.Single(cut.Instance.Selection);
        cut.Render(p => p.Add(c => c.Tool, NotebookTool.Pen));
        Assert.Empty(cut.Instance.Selection);
    }

    [Fact]
    public async Task TextCommit_LargeTextAsStream_IsStored()
    {
        var (notebook, page) = Page();
        var cut = Canvas(notebook, page);
        await cut.InvokeAsync(() => cut.Instance.OnPageClickAsync(20, 25));
        var editor = cut.FindComponent<MudExNotebookTextEditor>();
        var large = new string('x', 100_000);

        await cut.InvokeAsync(() => editor.Instance.OnTextCommitAsync(null!, new JsonStreamReference(new[] { new NotebookTextParagraphDto { Source = 0, Runs = [new NotebookTextRunDto { Source = 0, Text = large }] } })));

        Assert.Equal(large, ((TextElement)page.Elements[0]).Paragraphs[0].Runs[0].Text);
    }
}
