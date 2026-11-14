using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Presentation;

public class SlideEditorTests : IAsyncLifetime
{
    private const string PptxType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
    private readonly List<BunitContext> _contexts = [];

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

    private static byte[] SampleBytes => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "Presentation", "sample.pptx"));

    private static Presentation Sample() => Presentation.Load(new MemoryStream(SampleBytes));

    private BunitContext NewContext()
    {
        var context = new BunitContext();
        _contexts.Add(context);
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        context.Render<MudPopoverProvider>();
        context.Render<MudDialogProvider>();
        return context;
    }

    private IRenderedComponent<MudExSlideEditor> Render(Action<ComponentParameterCollectionBuilder<MudExSlideEditor>> configure)
    {
        var cut = NewContext().Render(configure);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-slide-panel-item .mud-ex-slide")));
        return cut;
    }

    private IRenderedComponent<MudExSlideEditor> Render(Presentation presentation, bool readOnly = false)
        => Render(p => p.Add(x => x.Presentation, presentation).Add(x => x.ReadOnly, readOnly));

    private static Task Run(IRenderedComponent<MudExSlideEditor> cut, string id)
        => cut.InvokeAsync(() => (cut.FindComponent<MudExRibbon>().Instance.Commands.SingleOrDefault(c => c.Id == id)
                                  ?? cut.FindComponents<MudExRibbonButton>().Select(b => b.Instance).Single(b => b.Id == id)).ExecuteAsync());

    private static string Status(IRenderedComponent<MudExSlideEditor> cut) => cut.Find(".mud-ex-slide-status-position").TextContent.Trim();

    private static void Click(IRenderedComponent<MudExSlideEditor> cut, int index)
        => cut.FindAll(".mud-ex-slide-panel-item")[index].Click();

    [Fact]
    public void Editor_LoadsSampleDeck_ShowsSlideCountInStatus()
    {
        var cut = Render(p => p.Add(x => x.FileDisplayInfos, new Infos("sample.pptx", PptxType, SampleBytes)));

        cut.WaitForAssertion(() => Assert.Equal("Slide 1 of 3", Status(cut)));
        Assert.Equal(3, cut.FindAll(".mud-ex-slide-panel-item").Count);
        Assert.Equal(3, cut.Instance.CurrentPresentation.Slides.Count);
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task Editor_NewSlideFromRibbon_AddsSlideAfterCurrent_UndoRemovesIt()
    {
        var presentation = Sample();
        var cut = Render(presentation);
        Click(cut, 1);
        cut.WaitForAssertion(() => Assert.Equal("Slide 2 of 3", Status(cut)));

        await Run(cut, "pp-new-slide");

        cut.WaitForAssertion(() => Assert.Equal("Slide 3 of 4", Status(cut)));
        var added = presentation.Slides[2];
        Assert.Same(added, cut.Instance.CurrentSlide);
        Assert.True(cut.Instance.IsDirty);

        await Run(cut, "pp-undo");

        cut.WaitForAssertion(() => Assert.Equal(3, presentation.Slides.Count));
        Assert.DoesNotContain(added, presentation.Slides);
        Assert.Equal(3, cut.FindAll(".mud-ex-slide-panel-item").Count);
    }

    [Fact]
    public async Task Editor_DeleteSlide_SelectsNeighbour()
    {
        var presentation = Sample();
        var third = presentation.Slides[2];
        var cut = Render(presentation);
        Click(cut, 1);

        await Run(cut, "pp-delete-slide");

        cut.WaitForAssertion(() => Assert.Equal("Slide 2 of 2", Status(cut)));
        Assert.Same(third, cut.Instance.CurrentSlide);

        await Run(cut, "pp-delete-slide");

        cut.WaitForAssertion(() => Assert.Equal("Slide 1 of 1", Status(cut)));
        Assert.Same(presentation.Slides[0], cut.Instance.CurrentSlide);
    }

    [Fact]
    public async Task Editor_EditNotes_MarksDirty_SaveExportsPptx()
    {
        byte[] saved = null;
        var cut = Render(p => p
            .Add(x => x.Presentation, Sample())
            .Add(x => x.SaveRequested, (MudExFileSaveRequest r) =>
            {
                saved = ((MemoryStream)r.Content.Stream).ToArray();
                Assert.EndsWith(".pptx", r.Content.FileName);
                Assert.Equal(PptxType, r.Content.ContentType);
                r.Handled = true;
            }));
        Click(cut, 1);

        var notes = cut.Find(".mud-ex-slide-notes textarea");
        notes.Input("Speaker notes for two");
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsDirty));
        cut.Find(".mud-ex-slide-notes textarea").Blur();

        Assert.Equal("Speaker notes for two", cut.Instance.CurrentPresentation.Slides[1].Notes);
        Assert.True(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));
        Assert.False(cut.Instance.IsDirty);
        Assert.Equal("Speaker notes for two", Presentation.Load(new MemoryStream(saved)).Slides[1].Notes);
    }

    [Fact]
    public void Editor_ReadOnly_HidesEditingGroups()
    {
        var cut = Render(Sample(), readOnly: true);

        var ribbon = cut.FindComponent<MudExRibbon>().Instance;
        cut.WaitForAssertion(() => Assert.Equal(["Slide Show", "View"], cut.FindComponent<MudExRibbon>().FindAll("[role=tab]").Select(t => t.TextContent.Trim())));
        var visible = ribbon.Commands.Where(c => c.Tab is { Visible: true }).Select(c => c.Id).ToList();
        Assert.DoesNotContain("pp-new-slide", visible);
        Assert.DoesNotContain("pp-bold", visible);
        Assert.DoesNotContain("pp-undo", ribbon.Commands.Select(c => c.Id));
        Assert.Contains("pp-show-notes", visible);
        Assert.True(cut.Find(".mud-ex-slide-notes textarea").HasAttribute("readonly"));
        Assert.Empty(cut.FindAll(".mud-ex-slide-panel-item[draggable=true]"));

        cut.Find(".mud-ex-slide-canvas").KeyDown(new KeyboardEventArgs { Key = "PageDown" });
        cut.WaitForAssertion(() => Assert.Equal("Slide 2 of 3", Status(cut)));
        cut.Find(".mud-ex-slide-canvas").KeyDown(new KeyboardEventArgs { Key = "End" });
        cut.WaitForAssertion(() => Assert.Equal("Slide 3 of 3", Status(cut)));
        cut.Find(".mud-ex-slide-canvas").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        cut.WaitForAssertion(() => Assert.Equal("Slide 2 of 3", Status(cut)));
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public void Editor_RibbonColorDefault_IsC43E1C()
    {
        Assert.Equal("#C43E1C", new MudExSlideEditor().RibbonColor.ToString());

        var cut = Render(Presentation.Create());

        Assert.Equal("#C43E1C", cut.FindComponent<MudExRibbon>().Instance.Color.ToString());
        Assert.StartsWith("--mud-ex-slide-accent:#c43e1c;", cut.Find(".mud-ex-slide-editor").GetAttribute("style"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Editor_ChangeOnSlide3_DoesNotRerenderOtherThumbnails()
    {
        var presentation = Sample();
        var cut = Render(presentation);
        var thumbnails = cut.FindComponent<MudExSlidePanel>().FindComponents<MudExSlideView>();
        Assert.Equal(3, thumbnails.Count);
        var before = thumbnails.Select(t => t.RenderCount).ToArray();

        await cut.InvokeAsync(() => presentation.Execute(new SetSlideHiddenCommand([presentation.Slides[2]], true)));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-slide-panel-item.mud-ex-slide-panel-hidden")));
        Assert.Equal(before[0], thumbnails[0].RenderCount);
        Assert.Equal(before[1], thumbnails[1].RenderCount);
        Assert.True(thumbnails[2].RenderCount > before[2]);
    }

    [Fact]
    public async Task Editor_KeysWithoutShortcut_DoNotRenderTheEditor()
    {
        var host = NewContext().Render<CountingEditor>(p => p.Add(x => x.Presentation, Sample()));
        host.WaitForAssertion(() => Assert.NotEmpty(host.FindAll(".mud-ex-slide-panel-item .mud-ex-slide")));
        await host.InvokeAsync(() => Task.Delay(50));
        var renders = host.Instance.Renders;

        foreach (var key in "notes")
        {
            await host.Find(".mud-ex-slide-editor").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = key.ToString() });
            await host.Find(".mud-ex-slide-canvas").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = key.ToString() });
        }

        await host.Find(".mud-ex-slide-canvas").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "Escape" });
        await host.InvokeAsync(() => Task.Delay(50));

        Assert.Equal(renders, host.Instance.Renders);
    }

    [Fact]
    public void Editor_NotesWithoutNotesMaster_AreReadOnlyWithHint()
    {
        var presentation = new PptxBuilder().Slide("").Load();
        var cut = Render(presentation);

        Assert.True(cut.Find(".mud-ex-slide-notes textarea").HasAttribute("readonly"));
        Assert.Contains("notes", cut.Find(".mud-ex-slide-notes").TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Editor_CtrlS_SavesOnce_NotInReadOnly()
    {
        var requests = 0;
        var cut = Render(p => p.Add(x => x.Presentation, Sample()).Add(x => x.SaveRequested, (MudExFileSaveRequest r) => { requests++; r.Handled = true; }));

        cut.Find(".mud-ex-slide-ribbon").KeyDown(new KeyboardEventArgs { Key = "s", CtrlKey = true });
        cut.WaitForAssertion(() => Assert.Equal(1, requests));

        cut.Render(p => p.Add(x => x.ReadOnly, true));
        cut.Find(".mud-ex-slide-canvas").KeyDown(new KeyboardEventArgs { Key = "s", CtrlKey = true });
        Assert.Equal(1, requests);
    }

    [Fact]
    public async Task Editor_DropSlides_MovesSelectionAsOneUndoStep()
    {
        var presentation = Sample();
        var (first, second, third) = (presentation.Slides[0], presentation.Slides[1], presentation.Slides[2]);
        var cut = Render(presentation);
        Click(cut, 0);
        cut.FindAll(".mud-ex-slide-panel-item")[1].Click(new MouseEventArgs { ShiftKey = true });
        cut.WaitForAssertion(() => Assert.Equal([first, second], cut.Instance.SelectedSlides));

        await cut.Instance.OnSlidesDropped(0, 3);

        Assert.Equal([third, first, second], presentation.Slides);
        Assert.Equal("Slide 2 of 3", Status(cut));
        await Run(cut, "pp-undo");
        Assert.Equal([first, second, third], presentation.Slides);
        Assert.False(presentation.Undo.CanUndo);

        await cut.Instance.OnSlidesDropped(2, 2);
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public void Editor_LazyThumbnails_RenderWhenReportedVisible()
    {
        var presentation = Presentation.Create();
        for (var i = 0; i < 11; i++)
            presentation.Execute(new AddSlideCommand(1, presentation.Slides[0].Layout));
        var cut = Render(presentation);
        var panel = cut.FindComponent<MudExSlidePanel>();
        Assert.Equal(12, cut.FindAll(".mud-ex-slide-panel-item").Count);
        Assert.True(panel.Instance.LazyThumbnails);
        Assert.Equal(8, panel.FindComponents<MudExSlideView>().Count);

        cut.Instance.OnThumbnailsVisible([presentation.Slides[10].Key]);

        cut.WaitForAssertion(() => Assert.Equal(9, panel.FindComponents<MudExSlideView>().Count));
        Assert.NotEmpty(cut.FindAll(".mud-ex-slide-panel-item")[10].QuerySelectorAll(".mud-ex-slide"));
    }

    [Fact]
    public async Task Editor_CanvasSelection_EnablesShapeControls_ArrowsNudgeInsteadOfNavigating()
    {
        var presentation = Sample();
        var cut = Render(presentation);
        var shape = presentation.Slides[0].Shapes[0];
        var before = shape.ResolvedTransform;
        bool Disabled(string id) => cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == id).Disabled;
        Assert.True(Disabled("pp-arrange"));

        var canvas = cut.FindComponent<MudExSlideCanvas>();
        await cut.InvokeAsync(() => canvas.Instance.OnSelectionAsync([shape.Id]));

        cut.WaitForAssertion(() => Assert.False(Disabled("pp-arrange")));
        Assert.False(Disabled("pp-bold"));
        Assert.False(Disabled("pp-cut"));
        Assert.Equal([shape.Id], cut.Instance.SelectedShapes);

        cut.Find(".mud-ex-slide-canvas").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.Equal("Slide 1 of 3", Status(cut));
        Assert.Equal(before with { Y = before.Y + 914400 / 8 }, shape.ResolvedTransform);
        Assert.True(presentation.Undo.CanUndo);

        Click(cut, 1);
        cut.WaitForAssertion(() => Assert.True(Disabled("pp-arrange")));
        Assert.Empty(cut.Instance.SelectedShapes);
    }

    private sealed class CountingEditor : MudExSlideEditor
    {
        public int Renders { get; private set; }

        protected override void OnAfterRender(bool firstRender)
        {
            Renders++;
            base.OnAfterRender(firstRender);
        }
    }
}
