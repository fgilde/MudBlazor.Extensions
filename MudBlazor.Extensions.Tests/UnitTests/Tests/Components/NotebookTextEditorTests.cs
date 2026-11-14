using System.Text.Json;
using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Notebook;

public class NotebookTextEditorTests : IAsyncLifetime
{
    private readonly BunitContext _context = new();

    public NotebookTextEditorTests()
    {
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        _context.Render<MudPopoverProvider>();
        _context.Render<MudDialogProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _context.DisposeAsync();

    private static readonly NoteRunFormat Bold = new(Bold: true);
    private static readonly NoteRunFormat RedItalic = new(Italic: true, Color: "#FF0000", SizePt: 14);

    private static (Notebook Notebook, NotebookPage Page) Page(params NoteParagraph[] paragraphs)
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        notebook.Execute(new AddElementCommand(page, new TextElement { Id = "text", X = 10, Y = 20, Width = 300, Paragraphs = paragraphs }));
        notebook.Undo.Clear();
        return (notebook, page);
    }

    private static NoteParagraph Para(NoteListKind list, int indent, bool done, params NoteRun[] runs) => new() { List = list, Indent = indent, Checked = done, Runs = runs };

    private static NotebookTextParagraphDto Dto(int? source, string? list, int indent, bool done, params NotebookTextRunDto[] runs) =>
        new() { Source = source, List = list, Indent = indent, Checked = done, Runs = runs };

    private static NotebookTextRunDto Run(int? source, string text, bool bold = false) => new() { Source = source, Text = text, B = bold };

    private static TextElement Text(NotebookPage page) => (TextElement)page.Elements.Single(e => e.Id == "text");

    private IRenderedComponent<MudExNotebookCanvas> Canvas(Notebook notebook, NotebookPage page) =>
        _context.Render<MudExNotebookCanvas>(p => p.Add(c => c.Notebook, notebook).Add(c => c.Page, page).Add(c => c.ViewportWidth, 800).Add(c => c.ViewportHeight, 600));

    private static async Task Commit(IRenderedComponent<MudExNotebookCanvas> cut, params NotebookTextParagraphDto[] paragraphs)
    {
        await cut.InvokeAsync(() => cut.Instance.OnPageClickAsync(20, 25));
        Assert.True(cut.Instance.IsEditingText);
        var editor = cut.FindComponent<MudExNotebookTextEditor>();
        await cut.InvokeAsync(() => editor.Instance.OnTextCommitAsync(paragraphs, null));
        Assert.False(cut.Instance.IsEditingText);
    }

    [Fact]
    public void TextDom_RoundTrip_KeepsRunsListsAndChecks()
    {
        var extra = new Dictionary<string, JsonElement> { ["future"] = JsonDocument.Parse("42").RootElement };
        IReadOnlyList<NoteParagraph> original =
        [
            new NoteParagraph { Runs = [new NoteRun("Title", new NoteRunFormat(Font: "Georgia", Highlight: "#FFFF00"))], Heading = NoteHeading.H2, Align = NoteAlign.Center, Extra = extra },
            Para(NoteListKind.Bullet, 1, false, new NoteRun("plain "), new NoteRun("bold", Bold), new NoteRun("link", new NoteRunFormat(Underline: true, Link: "https://mudex.org"))),
            Para(NoteListKind.Number, 0, false, new NoteRun("red", RedItalic)),
            Para(NoteListKind.Todo, 2, true, new NoteRun("done", new NoteRunFormat(Strike: true))),
            new NoteParagraph()
        ];

        var dom = NotebookTextDom.ToDom(original);
        Assert.Same(original, NotebookTextDom.FromDom(original, dom));

        var json = JsonSerializer.Deserialize<NotebookTextParagraphDto[]>(JsonSerializer.Serialize(dom, JsonSerializerOptions.Web), JsonSerializerOptions.Web)!;
        Assert.Equal(original, NotebookTextDom.FromDom([], json));

        json[0].Runs[0].Text = "New title";
        var changed = NotebookTextDom.FromDom(original, json);
        Assert.Equal("New title", changed[0].Runs[0].Text);
        Assert.Same(extra, changed[0].Extra);
        Assert.Equal(original.Skip(1), changed.Skip(1));
        Assert.Equal((NoteListKind.Todo, 2, true), (changed[3].List, changed[3].Indent, changed[3].Checked));
    }

    [Fact]
    public async Task TextEdit_SplitAndJoinParagraph_KeepsFormat()
    {
        var (notebook, page) = Page(Para(NoteListKind.Bullet, 1, false, new NoteRun("Hello ", Bold), new NoteRun("World", RedItalic)));
        var cut = Canvas(notebook, page);
        var red = new NotebookTextRunDto { Source = 1, Text = "World", I = true, Color = "#FF0000", Size = 14 };

        await Commit(cut, Dto(0, "bullet", 1, false, Run(0, "Hello ", true)), Dto(null, "bullet", 1, false, red));

        Assert.Equal([Para(NoteListKind.Bullet, 1, false, new NoteRun("Hello ", Bold)), Para(NoteListKind.Bullet, 1, false, new NoteRun("World", RedItalic))], Text(page).Paragraphs);

        await Commit(cut, Dto(0, "bullet", 1, false, Run(0, "Hello ", true), red));

        Assert.Equal([Para(NoteListKind.Bullet, 1, false, new NoteRun("Hello ", Bold), new NoteRun("World", RedItalic))], Text(page).Paragraphs);
        notebook.Undo.Undo();
        Assert.Equal(2, Text(page).Paragraphs.Count);
        notebook.Undo.Undo();
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public async Task TextEdit_AutoList_FromAsteriskSpace()
    {
        var (notebook, page) = Page(new NoteParagraph { Runs = [new NoteRun("* kept literally")] });
        var cut = Canvas(notebook, page);

        await Commit(cut, Dto(0, null, 0, false, Run(0, "* kept literally")), Dto(null, null, 0, false, Run(null, "* Item")),
            Dto(null, null, 0, false, Run(null, "1. Step")), Dto(null, null, 0, false, Run(null, "[] Task")), Dto(null, null, 0, false, Run(null, "*no space")));

        Assert.Equal([NoteListKind.None, NoteListKind.Bullet, NoteListKind.Number, NoteListKind.Todo, NoteListKind.None], Text(page).Paragraphs.Select(p => p.List));
        Assert.Equal(["* kept literally", "Item", "Step", "Task", "*no space"], Text(page).Paragraphs.Select(p => string.Concat(p.Runs.Select(r => r.Text))));
    }

    [Fact]
    public async Task TextEdit_TabChangesIndent_NumbersFollow()
    {
        var (notebook, page) = Page(Para(NoteListKind.Number, 0, false, new NoteRun("one")), Para(NoteListKind.Number, 0, false, new NoteRun("two")),
            Para(NoteListKind.Number, 0, false, new NoteRun("three")));
        var cut = Canvas(notebook, page);
        string[] Markers() => cut.FindAll(".mud-ex-notebook-marker").Select(m => m.TextContent).ToArray();
        Assert.Equal(["1.", "2.", "3."], Markers());

        await Commit(cut, Dto(0, "number", 0, false, Run(0, "one")), Dto(1, "number", 1, false, Run(1, "two")), Dto(2, "number", 0, false, Run(2, "three")));

        Assert.Equal([0, 1, 0], Text(page).Paragraphs.Select(p => p.Indent));
        cut.WaitForAssertion(() => Assert.Equal(["1.", "a.", "2."], Markers()));
    }

    [Fact]
    public async Task TextEdit_ToggleTodo_OutsideEdit_IsOneUndoStep()
    {
        var (notebook, page) = Page(Para(NoteListKind.Todo, 0, false, new NoteRun("first")), Para(NoteListKind.Todo, 0, false, new NoteRun("second")));
        var cut = Canvas(notebook, page);

        await cut.InvokeAsync(() => cut.Instance.OnTodoToggleAsync("text", 1));

        Assert.Equal([false, true], Text(page).Paragraphs.Select(p => p.Checked));
        cut.WaitForAssertion(() => Assert.Equal(["false", "true"], cut.FindAll(".mud-ex-notebook-todo").Select(t => t.GetAttribute("aria-checked"))));
        notebook.Undo.Undo();
        Assert.Equal([false, false], Text(page).Paragraphs.Select(p => p.Checked));
        Assert.False(notebook.Undo.CanUndo);

        await cut.InvokeAsync(() => cut.Instance.OnTodoToggleAsync("text", 5));
        await cut.InvokeAsync(() => cut.Instance.OnTodoToggleAsync("missing", 0));
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public async Task TextEdit_MoveTodoParagraph_KeepsChecked()
    {
        var (notebook, page) = Page(Para(NoteListKind.Todo, 0, true, new NoteRun("done")), Para(NoteListKind.Todo, 0, false, new NoteRun("open")));
        var cut = Canvas(notebook, page);

        await Commit(cut, Dto(1, "todo", 0, false, Run(1, "open")), Dto(0, "todo", 0, true, Run(0, "done")));

        Assert.Equal([("open", false), ("done", true)], Text(page).Paragraphs.Select(p => (p.Runs[0].Text, p.Checked)));
    }

    [Fact]
    public async Task TextEdit_PasteHtml_KeepsBoldAndList_DropsScript()
    {
        var editor = _context.Render<MudExNotebookTextEditor>(p => p.Add(e => e.Paragraphs, [new NoteParagraph()]));

        var pasted = await editor.Instance.OnPasteHtml(
            "<html><head><style>b{color:red}</style></head><body><ul><li><b onclick=\"alert(1)\">bold</b> item</li><li>two<ul><li>nested</li></ul></li></ul>" +
            "<ol><li>first</li></ol><script>alert('x')</script><p>&lt;script&gt;x</p><p><a href=\"javascript:alert(1)\">bad</a> <a href=\"https://mudex.org\">good</a></p>" +
            "<img src=x onerror=alert(1)></body></html>", null);

        Assert.Equal(["bullet", "bullet", "bullet", "number", null, null], pasted.Select(p => p.List));
        Assert.Equal([0, 0, 1, 0, 0, 0], pasted.Select(p => p.Indent));
        Assert.Equal(["bold", " item"], pasted[0].Runs.Select(r => r.Text));
        Assert.Equal([true, false], pasted[0].Runs.Select(r => r.B));
        Assert.Equal("<script>x", Assert.Single(pasted[4].Runs).Text);
        Assert.Equal([null, null, "https://mudex.org"], pasted[5].Runs.Select(r => r.Link));
        Assert.DoesNotContain(pasted.SelectMany(p => p.Runs), r => r.Text.Contains("alert"));
        Assert.Empty(await editor.Instance.OnPasteHtml("<script>alert(1)</script>", null));

        var paragraphs = NotebookTextDom.FromDom([], pasted);
        Assert.Equal(NoteListKind.Bullet, paragraphs[0].List);
        Assert.True(paragraphs[0].Runs[0].Format.Bold);
        Assert.Equal("<script>x", paragraphs[4].Runs[0].Text);
    }

    [Fact]
    public async Task TextEdit_Session_IsOneUndoStep()
    {
        var (notebook, page) = Page(new NoteParagraph { Runs = [new NoteRun("Hello")] });
        var cut = Canvas(notebook, page);

        await Commit(cut, Dto(0, null, 0, false, Run(0, "Hello")));
        Assert.False(notebook.Undo.CanUndo);

        await Commit(cut, Dto(0, null, 0, false, Run(0, "Hello world")), Dto(null, null, 0, false, Run(null, "Second line", true)));
        Assert.Equal(["Hello world", "Second line"], Text(page).Paragraphs.Select(p => p.Runs[0].Text));
        notebook.Undo.Undo();
        Assert.Equal("Hello", Text(page).Paragraphs.Single().Runs[0].Text);
        Assert.False(notebook.Undo.CanUndo);

        await Commit(cut, Dto(0, null, 0, false, Run(0, "​")));
        Assert.DoesNotContain(page.Elements, e => e.Id == "text");
        notebook.Undo.Undo();
        Assert.Equal("Hello", Text(page).Paragraphs.Single().Runs[0].Text);
        Assert.False(notebook.Undo.CanUndo);

        var editor = _context.Render<MudExNotebookEditor>(p => p.Add(e => e.Notebook, notebook));
        var canvas = editor.FindComponent<MudExNotebookCanvas>();
        await editor.InvokeAsync(() => canvas.Instance.OnPageClickAsync(20, 25));
        Assert.True(canvas.Instance.IsEditingText);
        await editor.InvokeAsync(() => editor.Instance.ExportAsync());
        Assert.False(canvas.Instance.IsEditingText);
    }

    [Fact]
    public async Task TextEdit_LinkWithBadScheme_Rejected()
    {
        Assert.False(MudExNotebookLinkDialog.IsAllowed("javascript:alert(1)"));
        Assert.False(MudExNotebookLinkDialog.IsAllowed("data:text/html,x"));
        Assert.False(MudExNotebookLinkDialog.IsAllowed(""));
        Assert.True(MudExNotebookLinkDialog.IsAllowed(" https://mudex.org "));
        Assert.True(MudExNotebookLinkDialog.IsAllowed("mailto:a@b.org"));

        var (notebook, page) = Page(new NoteParagraph { Runs = [new NoteRun("Hello")] });
        var cut = Canvas(notebook, page);
        await Commit(cut, Dto(0, null, 0, false, new NotebookTextRunDto { Source = 0, Text = "bad", Link = "javascript:alert(1)" },
            new NotebookTextRunDto { Text = "good", Link = "https://mudex.org" }));

        Assert.Equal([null, "https://mudex.org"], Text(page).Paragraphs[0].Runs.Select(r => r.Format.Link));
        cut.WaitForAssertion(() => Assert.Equal("https://mudex.org", cut.Find("a.mud-ex-notebook-link").GetAttribute("href")));
        Assert.Single(cut.FindAll("a"));
    }

    [Fact]
    public async Task RibbonBold_WithoutEdit_AppliesToWholeBlock()
    {
        var (notebook, page) = Page(new NoteParagraph { Runs = [new NoteRun("plain "), new NoteRun("bold", Bold)] }, Para(NoteListKind.Bullet, 0, false, new NoteRun("item")));
        notebook.Execute(new AddElementCommand(page, new TextElement { Id = "other", X = 10, Y = 300, Width = 300, Paragraphs = [new NoteParagraph { Runs = [new NoteRun("x")] }] }));
        notebook.Undo.Clear();
        var cut = _context.Render<MudExNotebookEditor>(p => p.Add(e => e.Notebook, notebook));
        bool Disabled(string id) => cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == id).Disabled;
        Task Run(string id) => cut.InvokeAsync(() => cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == id).ExecuteAsync());
        Assert.True(Disabled("nb-bold"));

        var canvas = cut.FindComponent<MudExNotebookCanvas>();
        await cut.InvokeAsync(() => canvas.Instance.OnSelectionAsync(["text", "other"]));
        cut.WaitForAssertion(() => Assert.False(Disabled("nb-bold")));

        await Run("nb-bold");

        Assert.All(page.Elements.OfType<TextElement>().SelectMany(t => t.Paragraphs).SelectMany(p => p.Runs), r => Assert.True(r.Format.Bold));
        notebook.Undo.Undo();
        Assert.False(notebook.Undo.CanUndo);
        Assert.Equal([false, true, false, false], page.Elements.OfType<TextElement>().SelectMany(t => t.Paragraphs).SelectMany(p => p.Runs).Select(r => r.Format.Bold));

        await cut.InvokeAsync(() => canvas.Instance.OnSelectionAsync(["text"]));
        await Run("nb-numbering");
        Assert.All(Text(page).Paragraphs, p => Assert.Equal(NoteListKind.Number, p.List));
        await Run("nb-numbering");
        Assert.All(Text(page).Paragraphs, p => Assert.Equal(NoteListKind.None, p.List));
        await Run("nb-heading-2");
        Assert.All(Text(page).Paragraphs, p => Assert.Equal(NoteHeading.H2, p.Heading));
    }
}
