using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.AnimationTests;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Presentation;

public class SlideAnimationPaneTests : IAsyncLifetime
{
    private readonly BunitContext _context = new();

    public Task InitializeAsync()
    {
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        _context.Render<MudPopoverProvider>();
        _context.Render<MudDialogProvider>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    private IRenderedComponent<MudExSlideEditor> RenderEditor(Presentation presentation)
    {
        var cut = _context.Render<MudExSlideEditor>(p => p.Add(x => x.Presentation, presentation));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-slide-canvas .mud-ex-slide")));
        return cut;
    }

    private static Task Run(IRenderedComponent<MudExSlideEditor> cut, string id)
        => cut.InvokeAsync(() => cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == id).ExecuteAsync());

    private static void Tab(IRenderedComponent<MudExSlideEditor> cut, string tab) => cut.FindAll("[role='tab']").First(t => t.TextContent.Trim() == tab).Click();

    private static void Choose(IRenderedComponent<MudExSlideEditor> cut, string title) =>
        cut.WaitForAssertion(() => cut.FindAll(".mud-ex-ribbon-gallery-item").First(i => i.GetAttribute("title") == title).Click());

    [Fact]
    public async Task Transitions_ApplyToAll_IsOneUndoStep()
    {
        var presentation = SlideShowTests.Deck(false, false, false);
        var cut = RenderEditor(presentation);

        Tab(cut, "Transitions");
        Choose(cut, "Push");
        Assert.Equal(new SlideTransition(TransitionKind.Push, TransitionDirection.Left, 0.5, true, null), presentation.Slides[0].Transition);
        Assert.Equal(SlideTransition.None, presentation.Slides[1].Transition);

        await Run(cut, "pp-transition-direction-up");
        await Run(cut, "pp-transition-on-click");
        Assert.Equal(new SlideTransition(TransitionKind.Push, TransitionDirection.Up, 0.5, false, null), presentation.Slides[0].Transition);
        var pushUp = presentation.Slides[0].Transition;

        await Run(cut, "pp-transition-apply-all");

        Assert.All(presentation.Slides, s => Assert.Equal(presentation.Slides[0].Transition, s.Transition));
        Assert.Equal("Transition", presentation.Undo.UndoDescription);
        await cut.InvokeAsync(() => presentation.Undo.Undo());
        Assert.Equal([pushUp, SlideTransition.None, SlideTransition.None], presentation.Slides.Select(s => s.Transition));

        await Run(cut, "pp-transition-preview");
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-slide-show-preview .mud-ex-show-in-push-up")));
    }

    [Fact]
    public async Task AnimationPane_Reorder_UsesMoveCommand()
    {
        var presentation = new PptxBuilder().Slide(Shapes(2, 3, 4), tail: Timing(
            Group(Slice(0, Effect("entr", 10, 0, "clickEffect", 2))) + Group(Slice(0, Effect("entr", 10, 0, "clickEffect", 3))) +
            Group(Slice(0, Effect("exit", 10, 0, "clickEffect", 4))))).Load();
        var slide = presentation.Slides[0];
        var cut = RenderEditor(presentation);

        Tab(cut, "Animations");
        await Run(cut, "pp-animation-pane");
        var items = cut.FindAll(".mud-ex-slide-animation-item");
        Assert.Equal(["Shape 2", "Shape 3", "Shape 4"], items.Select(i => i.QuerySelector(".mud-ex-slide-animation-name")!.TextContent.Trim()));

        items[0].Click();
        Assert.Equal([2u], cut.Instance.SelectedShapes);
        cut.Find(".mud-ex-slide-animation-pane button[aria-label='Move Down']").Click();
        Assert.Equal([3u, 2u, 4u], slide.Animations.Select(a => a.ShapeId));
        Assert.Equal("Move animation", presentation.Undo.UndoDescription);
        Assert.Contains("mud-ex-slide-animation-item-selected", cut.FindAll(".mud-ex-slide-animation-item")[1].GetAttribute("class"));

        await cut.FindAll(".mud-ex-slide-animation-item")[2].TriggerEventAsync("ondragstart", new Microsoft.AspNetCore.Components.Web.DragEventArgs());
        await cut.FindAll(".mud-ex-slide-animation-item")[0].TriggerEventAsync("ondrop", new Microsoft.AspNetCore.Components.Web.DragEventArgs());
        Assert.Equal([4u, 3u, 2u], slide.Animations.Select(a => a.ShapeId));
        Assert.Equal("Move animation", presentation.Undo.UndoDescription);

        cut.FindAll(".mud-ex-slide-animation-item")[0].Click();
        cut.Find(".mud-ex-slide-animation-pane button[aria-label='Remove']").Click();
        Assert.Equal([3u, 2u], slide.Animations.Select(a => a.ShapeId));
        Assert.Equal("Remove animation", presentation.Undo.UndoDescription);
    }

    [Fact]
    public async Task Animations_AddFade_ShowsOrderBadge()
    {
        var presentation = new PptxBuilder().Slide(Shapes(2, 3)).Load();
        var slide = presentation.Slides[0];
        var cut = RenderEditor(presentation);
        Assert.Empty(cut.FindAll(".mud-ex-slide-animation-badge"));

        Tab(cut, "Animations");
        await cut.InvokeAsync(() => cut.FindComponent<MudExSlideCanvas>().Instance.OnSelectionAsync([3]));
        Choose(cut, "Fade");

        var effect = Assert.Single(slide.Animations);
        Assert.Equal((3u, AnimationClass.Entrance, AnimationPreset.Fade, AnimationTrigger.OnClick), (effect.ShapeId, effect.Class, effect.Preset, effect.Trigger));
        Assert.Equal("Add animation", presentation.Undo.UndoDescription);
        cut.WaitForAssertion(() => Assert.Equal("1", cut.Find(".mud-ex-slide-animation-badge[data-shape-id='3']").TextContent.Trim()));

        await cut.InvokeAsync(() => cut.FindComponents<MudExRibbonNumberBox>().Single(b => b.Instance.Id == "pp-animation-duration").Instance.ValueChanged.InvokeAsync(2));
        Assert.Equal(2, Assert.Single(slide.Animations).DurationSeconds);
        Assert.Equal("Change animation", presentation.Undo.UndoDescription);

        Choose(cut, "Fly In");
        effect = Assert.Single(slide.Animations);
        Assert.Equal((AnimationPreset.FlyIn, AnimationDirection.FromBottom, 2.0), (effect.Preset, effect.Direction, effect.DurationSeconds));
        await Run(cut, "pp-animation-direction-from-left");
        Assert.Equal(AnimationDirection.FromLeft, Assert.Single(slide.Animations).Direction);

        Tab(cut, "Home");
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-slide-animation-badge")));
    }
}
