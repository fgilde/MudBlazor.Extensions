using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Notebook;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Notebook;

public class NotebookEditorTests : IAsyncLifetime
{
    private const string MxnbType = "application/vnd.mudex.notebook+zip";
    private readonly List<BunitContext> _contexts = [];
    private IRenderedComponent<MudPopoverProvider> _popovers = null!;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
            await context.DisposeAsync();
    }

    private sealed record Infos(string FileName, string ContentType, byte[] Bytes) : IMudExFileDisplayInfos
    {
        public string Url => null;
        public Stream ContentStream { get; } = new MemoryStream(Bytes);
    }

    private static byte[] Bytes(Notebook notebook)
    {
        var stream = new MemoryStream();
        notebook.Save(stream);
        return stream.ToArray();
    }

    private static Notebook TwoPages()
    {
        var notebook = Notebook.Create();
        notebook.Execute(new RenamePageCommand(notebook.Sections[0].Pages[0], "First"));
        notebook.Execute(new AddPageCommand(notebook.Sections[0], 1, "Second"));
        return notebook;
    }

    private BunitContext NewContext()
    {
        var context = new BunitContext();
        _contexts.Add(context);
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        _popovers = context.Render<MudPopoverProvider>();
        context.Render<MudDialogProvider>();
        return context;
    }

    private IRenderedComponent<MudExNotebookEditor> Render(Action<ComponentParameterCollectionBuilder<MudExNotebookEditor>> configure)
    {
        var cut = NewContext().Render(configure);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-notebook-page-item")));
        return cut;
    }

    private IRenderedComponent<MudExNotebookEditor> Render(Notebook notebook, bool readOnly = false)
        => Render(p => p.Add(x => x.Notebook, notebook).Add(x => x.ReadOnly, readOnly));

    private static Task Run(IRenderedComponent<MudExNotebookEditor> cut, string id)
        => cut.InvokeAsync(() => (cut.FindComponent<MudExRibbon>().Instance.Commands.SingleOrDefault(c => c.Id == id)
                                  ?? cut.FindComponents<MudExRibbonButton>().Select(b => b.Instance).Single(b => b.Id == id)).ExecuteAsync());

    private static string Status(IRenderedComponent<MudExNotebookEditor> cut) => cut.Find(".mud-ex-notebook-status-position").TextContent.Trim();

    private void Menu(string entry) => _popovers.WaitForAssertion(() => _popovers.FindAll(".mud-menu-item").Single(i => i.TextContent.Trim() == entry).Click());

    [Fact]
    public async Task Editor_New_HasOneSectionOnePage()
    {
        var cut = Render(_ => { });

        var notebook = cut.Instance.CurrentNotebook;
        var section = Assert.Single(notebook.Sections);
        var page = Assert.Single(section.Pages);
        Assert.Single(cut.FindAll(".mud-ex-notebook-section-tab"));
        Assert.Single(cut.FindAll(".mud-ex-notebook-page-item"));
        Assert.Same(section, cut.Instance.CurrentSection);
        Assert.Same(page, cut.Instance.CurrentPage);
        Assert.Equal("Page 1 of 1", Status(cut));

        Assert.True(await cut.InvokeAsync(() => cut.Instance.NewAsync()));

        cut.WaitForAssertion(() => Assert.NotSame(notebook, cut.Instance.CurrentNotebook));
        Assert.Single(Assert.Single(cut.Instance.CurrentNotebook.Sections).Pages);
        Assert.Equal("Notebook1.mxnb", cut.Instance.FileName);
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task Editor_AddPage_SelectsIt_UndoRemovesIt()
    {
        var notebook = Notebook.Create();
        var first = notebook.Sections[0].Pages[0];
        var cut = Render(notebook);

        cut.Find(".mud-ex-notebook-page-list-add").Click();

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".mud-ex-notebook-page-item").Count));
        var added = notebook.Sections[0].Pages[1];
        Assert.Same(added, cut.Instance.CurrentPage);
        Assert.Equal("Page 2 of 2", Status(cut));
        Assert.True(cut.Instance.IsDirty);

        await Run(cut, "nb-undo");

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-notebook-page-item")));
        Assert.DoesNotContain(added, notebook.Sections[0].Pages);
        Assert.Same(first, cut.Instance.CurrentPage);

        cut.Find(".mud-ex-notebook-page-list-items").KeyDown(new KeyboardEventArgs { Key = "n", CtrlKey = true });

        cut.WaitForAssertion(() => Assert.Equal(2, notebook.Sections[0].Pages.Count));
        Assert.Same(notebook.Sections[0].Pages[1], cut.Instance.CurrentPage);

        cut.Find(".mud-ex-notebook-editor-surface").KeyDown(new KeyboardEventArgs { Key = "PageUp", CtrlKey = true });
        cut.WaitForAssertion(() => Assert.Same(first, cut.Instance.CurrentPage));
    }

    [Fact]
    public async Task Editor_RenameSection_ByDoubleClick()
    {
        var notebook = Notebook.Create();
        var section = notebook.Sections[0];
        var cut = Render(notebook);

        cut.Find(".mud-ex-notebook-section-tab").DoubleClick();
        cut.Find(".mud-ex-notebook-section-rename").Change("Ideas");

        cut.WaitForAssertion(() => Assert.Equal("Ideas", section.Name));
        Assert.Empty(cut.FindAll(".mud-ex-notebook-section-rename"));
        Assert.Equal("Ideas", cut.Find(".mud-ex-notebook-section-tab").TextContent.Trim());

        cut.Find(".mud-ex-notebook-section-add").Click();

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".mud-ex-notebook-section-tab").Count));
        Assert.Same(notebook.Sections[1], cut.Instance.CurrentSection);
        Assert.Same(notebook.Sections[1].Pages[0], cut.Instance.CurrentPage);
        Assert.NotEmpty(cut.FindAll(".mud-ex-notebook-section-rename"));

        cut.FindAll(".mud-ex-notebook-section-tab")[0].Click();
        cut.WaitForAssertion(() => Assert.Same(section, cut.Instance.CurrentSection));

        await cut.InvokeAsync(() => cut.Instance.OnSectionDropped(0, 2));
        Assert.Equal(section, notebook.Sections[1]);
        Assert.Same(section, cut.Instance.CurrentSection);

        cut.FindAll(".mud-ex-notebook-section-tab")[1].ContextMenu();
        Menu("Move Left");
        cut.WaitForAssertion(() => Assert.Same(section, notebook.Sections[0]));
    }

    [Fact]
    public void Editor_DeleteLastPage_LeavesEmptyPage()
    {
        var notebook = Notebook.Create();
        var page = notebook.Sections[0].Pages[0];
        notebook.Execute(new RenamePageCommand(page, "Only"));
        var cut = Render(notebook);

        cut.Find(".mud-ex-notebook-page-item").ContextMenu();
        Menu("Delete");

        cut.WaitForAssertion(() => Assert.DoesNotContain(page, notebook.Sections[0].Pages));
        var fresh = Assert.Single(notebook.Sections[0].Pages);
        Assert.Equal("", fresh.Title);
        Assert.Empty(fresh.Elements);
        Assert.Same(fresh, cut.Instance.CurrentPage);
        Assert.Single(cut.FindAll(".mud-ex-notebook-page-item"));
    }

    [Fact]
    public async Task Editor_EditTitle_MarksDirty_SaveExportsMxnb()
    {
        byte[] saved = null;
        var notebook = TwoPages();
        var cut = Render(p => p
            .Add(x => x.Notebook, notebook)
            .Add(x => x.SaveRequested, (MudExFileSaveRequest r) =>
            {
                saved = ((MemoryStream)r.Content.Stream).ToArray();
                Assert.EndsWith(".mxnb", r.Content.FileName);
                Assert.Equal(MxnbType, r.Content.ContentType);
                r.Handled = true;
            }));
        cut.FindAll(".mud-ex-notebook-page-item")[1].Click();
        cut.WaitForAssertion(() => Assert.Same(notebook.Sections[0].Pages[1], cut.Instance.CurrentPage));

        cut.Find(".mud-ex-notebook-title").Input("Meeting");

        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsDirty));
        Assert.Equal("Second", notebook.Sections[0].Pages[1].Title);
        Assert.True(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));
        Assert.False(cut.Instance.IsDirty);
        Assert.Equal("Meeting", notebook.Sections[0].Pages[1].Title);
        var reloaded = Notebook.Load(new MemoryStream(saved));
        Assert.Equal(["First", "Meeting"], reloaded.Sections[0].Pages.Select(p => p.Title));

        await Run(cut, "nb-undo");
        Assert.Equal("Second", notebook.Sections[0].Pages[1].Title);
    }

    [Fact]
    public void Editor_ReadOnly_HidesEditingTabs()
    {
        var notebook = TwoPages();
        notebook.Execute(new AddSectionCommand(1, "Other", "#00AA00"));
        var cut = Render(notebook, readOnly: true);

        var ribbon = cut.FindComponent<MudExRibbon>().Instance;
        cut.WaitForAssertion(() => Assert.Equal(["View"], cut.FindComponent<MudExRibbon>().FindAll("[role=tab]").Select(t => t.TextContent.Trim())));
        var visible = ribbon.Commands.Where(c => c.Tab is { Visible: true }).Select(c => c.Id).ToList();
        Assert.DoesNotContain("nb-paste", visible);
        Assert.DoesNotContain("nb-undo", ribbon.Commands.Select(c => c.Id));
        Assert.Contains("nb-show-page-list", visible);
        Assert.Empty(cut.FindAll("input.mud-ex-notebook-title"));
        Assert.Equal("First", cut.Find(".mud-ex-notebook-title").TextContent.Trim());
        Assert.Empty(cut.FindAll(".mud-ex-notebook-page-list-add"));
        Assert.Empty(cut.FindAll(".mud-ex-notebook-section-add"));
        Assert.Empty(cut.FindAll("[draggable=true]"));

        cut.Find(".mud-ex-notebook-page-list-items").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.WaitForAssertion(() => Assert.Equal("Page 2 of 2", Status(cut)));
        cut.Find(".mud-ex-notebook-editor-surface").KeyDown(new KeyboardEventArgs { Key = "PageUp", CtrlKey = true });
        cut.WaitForAssertion(() => Assert.Equal("Page 1 of 2", Status(cut)));
        cut.FindAll(".mud-ex-notebook-section-tab")[1].Click();
        cut.WaitForAssertion(() => Assert.Same(notebook.Sections[1], cut.Instance.CurrentSection));
        cut.Find(".mud-ex-notebook-section-tab").DoubleClick();
        Assert.Empty(cut.FindAll(".mud-ex-notebook-section-rename"));
        cut.Find(".mud-ex-notebook-page-list-items").KeyDown(new KeyboardEventArgs { Key = "n", CtrlKey = true });
        Assert.Single(notebook.Sections[1].Pages);
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public void Editor_RibbonColorDefault_Is7719AA()
    {
        Assert.Equal("#7719AA", new MudExNotebookEditor().RibbonColor.ToString());

        var cut = Render(Notebook.Create());

        Assert.Equal("#7719AA", cut.FindComponent<MudExRibbon>().Instance.Color.ToString());
        Assert.StartsWith("--mud-ex-notebook-accent:#7719aa;", cut.Find(".mud-ex-notebook-editor").GetAttribute("style"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Editor_HostSwapsNotebook_ResetsFileNameAndDirty()
    {
        var cut = Render(p => p.Add(x => x.FileDisplayInfos, new Infos("ideas.mxnb", MxnbType, Bytes(TwoPages()))));
        cut.WaitForAssertion(() => Assert.Equal("ideas.mxnb", cut.Instance.FileName));
        Assert.Equal(2, cut.Instance.CurrentNotebook.Sections[0].Pages.Count);
        await cut.InvokeAsync(() => cut.Instance.CurrentNotebook.Execute(new RenamePageCommand(cut.Instance.CurrentPage, "Changed")));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsDirty));
        var other = Notebook.Create();

        cut.Render(p => p.Add(x => x.Notebook, other));

        cut.WaitForAssertion(() => Assert.Same(other, cut.Instance.CurrentNotebook));
        Assert.Same(other.Sections[0].Pages[0], cut.Instance.CurrentPage);
        Assert.Null(cut.Instance.FileName);
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task Editor_OpensOneNoteFile_SavesItAsMxnbUnderANewName()
    {
        MudExFileSaveRequest request = null;
        byte[] saved = null;
        var bytes = OneNoteFileBuilder.Build(new OnePage("From OneNote", DateTime.UtcNow, new OneOutline(1, 2, 10, new OneParagraph("Hello"))));
        var cut = Render(p => p
            .Add(x => x.FileDisplayInfos, new Infos("Quick Notes.one", "application/msonenote", bytes))
            .Add(x => x.SaveRequested, (MudExFileSaveRequest r) =>
            {
                request = r;
                saved = ((MemoryStream)r.Content.Stream).ToArray();
                r.Handled = true;
            }));
        cut.WaitForAssertion(() => Assert.Equal("From OneNote", cut.Instance.CurrentPage?.Title));
        Assert.Equal("Quick Notes", cut.Instance.CurrentSection.Name);
        Assert.Equal("Hello", Assert.IsType<TextElement>(Assert.Single(cut.Instance.CurrentPage.Elements)).Paragraphs[0].Runs[0].Text);

        Assert.True(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));

        Assert.Equal("Quick Notes.mxnb", request.Content.FileName);
        Assert.Equal(MxnbType, request.Content.ContentType);
        Assert.True(request.SaveAs);
        Assert.Equal("Quick Notes.mxnb", cut.Instance.FileName);
        Assert.Equal(["From OneNote"], Notebook.Load(new MemoryStream(saved)).Sections[0].Pages.Select(p => p.Title));
    }

    [Theory]
    [InlineData("notes.one", "application/octet-stream", true)]
    [InlineData("notes.bin", "application/msonenote", true)]
    [InlineData("Open Notebook.onetoc2", "application/msonenote", false)]
    [InlineData("Open Notebook.onetoc2", "application/octet-stream", false)]
    public async Task Editor_HandlesOneNoteSectionsButNotTheTableOfContents(string fileName, string contentType, bool handled)
    {
        Assert.Equal(handled, await new MudExNotebookEditor().CanHandleFileAsync(new Infos(fileName, contentType, []), null!));
    }

    [Fact]
    public async Task Editor_KeyDown_DoesNotRerender()
    {
        var host = NewContext().Render<CountingEditor>(p => p.Add(x => x.Notebook, Notebook.Create()));
        host.WaitForAssertion(() => Assert.NotEmpty(host.FindAll(".mud-ex-notebook-page-item")));
        await host.InvokeAsync(() => Task.Delay(50));
        var renders = host.Instance.Renders;

        foreach (var key in "notes")
        {
            foreach (var target in new[] { ".mud-ex-notebook-editor", ".mud-ex-notebook-editor-surface", ".mud-ex-notebook-page-list-items", ".mud-ex-notebook-section-tabs" })
                await host.Find(target).TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = key.ToString() });
        }

        await host.Find(".mud-ex-notebook-editor-surface").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "PageDown", CtrlKey = true });
        await host.Find(".mud-ex-notebook-title").TriggerEventAsync("oninput", new ChangeEventArgs { Value = "T" });
        await host.Find(".mud-ex-notebook-title").TriggerEventAsync("oninput", new ChangeEventArgs { Value = "Ti" });
        await host.Find(".mud-ex-notebook-title").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "z", CtrlKey = true });
        await host.InvokeAsync(() => Task.Delay(50));

        Assert.Equal(renders, host.Instance.Renders);
        Assert.True(host.Instance.IsDirty);
        Assert.False(host.Instance.CurrentNotebook.Undo.CanUndo);
    }

    private sealed class CountingEditor : MudExNotebookEditor
    {
        public int Renders { get; private set; }

        protected override void OnAfterRender(bool firstRender)
        {
            Renders++;
            base.OnAfterRender(firstRender);
        }
    }
}
