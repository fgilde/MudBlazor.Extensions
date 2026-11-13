using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Presentation;

public class SlideSectionsTests : IAsyncLifetime
{
    private readonly BunitContext _context = new();
    private IRenderedComponent<MudPopoverProvider> _popovers = null!;

    public Task InitializeAsync()
    {
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        _popovers = _context.Render<MudPopoverProvider>();
        _context.Render<MudDialogProvider>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    private static Presentation Deck() =>
        SectionTests.Builder(5, SectionTests.SectionList(("A", [256, 257]), ("B", [258, 259]), ("C", [260]))).Load();

    private IRenderedComponent<MudExSlideEditor> RenderEditor(Presentation presentation)
    {
        var cut = _context.Render<MudExSlideEditor>(p => p.Add(x => x.Presentation, presentation));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-slide-panel-item .mud-ex-slide")));
        return cut;
    }

    private static Task Run(IRenderedComponent<MudExSlideEditor> cut, string id)
        => cut.InvokeAsync(() => cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == id).ExecuteAsync());

    private static List<string> Headers(IRenderedComponent<MudExSlideEditor> cut) =>
        cut.FindAll(".mud-ex-slide-panel-section").Select(h =>
            (h.QuerySelector(".mud-ex-slide-panel-section-name")?.TextContent.Trim() ?? h.QuerySelector("input")!.GetAttribute("value")) + ":" +
            h.QuerySelector(".mud-ex-slide-panel-section-count")!.TextContent.Trim()).ToList();

    private static List<string> Items(IRenderedComponent<MudExSlideEditor> cut) =>
        cut.FindAll(".mud-ex-slide-panel-item").Select(i => i.GetAttribute("data-slide-index")!).ToList();

    private void ChooseFromSectionMenu(IRenderedComponent<MudExSlideEditor> cut, int section, string entry)
    {
        cut.FindAll(".mud-ex-slide-panel-section")[section].ContextMenu();
        _popovers.WaitForAssertion(() => _popovers.FindAll(".mud-menu-item").Single(i => i.TextContent.Trim() == entry).Click());
    }

    [Fact]
    public async Task Panel_ShowsSectionHeadersWithCounts()
    {
        var presentation = Deck();
        var cut = RenderEditor(presentation);

        Assert.Equal(["A:2", "B:2", "C:1"], Headers(cut));
        Assert.Equal(["0", "1", "2", "3", "4"], Items(cut));
        Assert.Equal("5", cut.FindAll(".mud-ex-slide-panel-number")[4].TextContent.Trim());
        Assert.All(cut.FindAll(".mud-ex-slide-panel-section"), h => Assert.Equal("true", h.GetAttribute("draggable")));

        await cut.Instance.OnSectionDropped(0, 3);
        cut.WaitForAssertion(() => Assert.Equal(["B:2", "C:1", "A:2"], Headers(cut)));
        Assert.Equal("Move section", presentation.Undo.UndoDescription);
        await cut.Instance.OnSectionDropped(0, 1);
        Assert.Equal("Move section", presentation.Undo.UndoDescription);
        await cut.InvokeAsync(() => presentation.Undo.Undo());

        cut.FindAll(".mud-ex-slide-panel-item")[4].ContextMenu();
        _popovers.WaitForAssertion(() => _popovers.FindAll(".mud-menu-item").Single(i => i.TextContent.Trim() == "Add Section").Click());
        cut.WaitForAssertion(() => Assert.Equal(["A:2", "B:2", "C:0", "Untitled Section:1"], Headers(cut)));
        Assert.Equal("Add section", presentation.Undo.UndoDescription);
        Assert.Equal("Untitled Section", cut.Find(".mud-ex-slide-panel-section-rename").GetAttribute("value"));

        var plain = RenderEditor(SectionTests.Builder(2).Load());
        Assert.Empty(plain.FindAll(".mud-ex-slide-panel-section"));
        await Run(plain, "pp-section-add");
        plain.WaitForAssertion(() => Assert.Equal(["Untitled Section:2"], Headers(plain)));
    }

    [Fact]
    public async Task Panel_CollapseSection_HidesThumbnails()
    {
        var presentation = Deck();
        var cut = RenderEditor(presentation);

        cut.FindAll(".mud-ex-slide-panel-section")[1].Click();

        Assert.Equal(["0", "1", "4"], Items(cut));
        Assert.Equal("false", cut.FindAll(".mud-ex-slide-panel-section")[1].GetAttribute("aria-expanded"));
        Assert.Equal("5", cut.FindAll(".mud-ex-slide-panel-number")[2].TextContent.Trim());
        Assert.Equal(["A:2", "B:2", "C:1"], Headers(cut));

        await Run(cut, "pp-section-collapse-all");
        cut.WaitForAssertion(() => Assert.Empty(Items(cut)));
        await Run(cut, "pp-section-expand-all");
        cut.WaitForAssertion(() => Assert.Equal(5, Items(cut).Count));

        ChooseFromSectionMenu(cut, 2, "Collapse All");
        cut.WaitForAssertion(() => Assert.Empty(Items(cut)));
        cut.Find(".mud-ex-slide-canvas").KeyDown(new KeyboardEventArgs { Key = "End" });
        cut.WaitForAssertion(() => Assert.Equal(["4"], Items(cut)));

        Assert.False(presentation.Undo.CanUndo);
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task Panel_RenameSection_Inline()
    {
        var presentation = Deck();
        var cut = RenderEditor(presentation);

        ChooseFromSectionMenu(cut, 1, "Rename Section");
        var input = cut.WaitForElement(".mud-ex-slide-panel-section-rename");
        Assert.Equal("B", input.GetAttribute("value"));
        await input.TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "a" });
        input.Change("Middle");

        cut.WaitForAssertion(() => Assert.Equal(["A:2", "Middle:2", "C:1"], Headers(cut)));
        Assert.Empty(cut.FindAll(".mud-ex-slide-panel-section-rename"));
        Assert.Equal("Rename section", presentation.Undo.UndoDescription);

        ChooseFromSectionMenu(cut, 0, "Rename Section");
        await cut.WaitForElement(".mud-ex-slide-panel-section-rename").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "Escape" });
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-slide-panel-section-rename")));
        Assert.Equal("A", presentation.Sections[0].Name);

        await cut.InvokeAsync(() => cut.FindComponent<MudExSlidePanel>().Instance.StartRename(presentation.Sections[2]));
        cut.WaitForElement(".mud-ex-slide-panel-section-rename").Change("  ");
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-slide-panel-section-rename")));
        Assert.Equal("C", presentation.Sections[2].Name);

        await cut.InvokeAsync(() => presentation.Undo.Undo());
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public async Task Panel_RemoveSectionAndSlides_IsOneUndoStep()
    {
        var presentation = Deck();
        var slides = presentation.Slides.ToList();
        var cut = RenderEditor(presentation);
        cut.FindAll(".mud-ex-slide-panel-item")[3].Click();

        ChooseFromSectionMenu(cut, 1, "Remove Section & Slides");

        cut.WaitForAssertion(() => Assert.Equal(["A:2", "C:1"], Headers(cut)));
        Assert.Equal([slides[0], slides[1], slides[4]], presentation.Slides);
        Assert.Equal("Remove section", presentation.Undo.UndoDescription);

        await cut.InvokeAsync(() => presentation.Undo.Undo());
        cut.WaitForAssertion(() => Assert.Equal(["A:2", "B:2", "C:1"], Headers(cut)));
        Assert.Equal(slides, presentation.Slides);
        Assert.False(presentation.Undo.CanUndo);

        ChooseFromSectionMenu(cut, 0, "Remove Section");
        cut.WaitForAssertion(() => Assert.Equal(["B:4", "C:1"], Headers(cut)));
        ChooseFromSectionMenu(cut, 0, "Move Section Down");
        cut.WaitForAssertion(() => Assert.Equal(["C:1", "B:4"], Headers(cut)));
        ChooseFromSectionMenu(cut, 0, "Remove All Sections");
        cut.WaitForAssertion(() => Assert.Empty(Headers(cut)));
        Assert.Equal(5, presentation.Slides.Count);
    }
}
