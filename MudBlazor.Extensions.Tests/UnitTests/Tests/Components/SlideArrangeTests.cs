using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Presentation;

public class SlideArrangeTests : IAsyncLifetime
{
    private const long Inch = 914400;

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

    private static Presentation Deck() => new PptxBuilder()
        .Slide(Shape(2, "A", Xfrm(Inch, Inch, Inch, Inch)) + Shape(3, "B", Xfrm(4 * Inch, 2 * Inch, Inch, Inch)) + Shape(4, "C", Xfrm(7 * Inch, 3 * Inch, Inch, Inch)) +
               TableTests.Table(5, "", [1000000, 1000000], TableTests.Tc("a") + TableTests.Tc("b")))
        .Load();

    private IRenderedComponent<MudExSlideEditor> RenderEditor(Presentation presentation)
    {
        var cut = _context.Render<MudExSlideEditor>(p => p.Add(x => x.Presentation, presentation));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-slide-canvas .mud-ex-slide")));
        return cut;
    }

    private static Task Select(IRenderedComponent<MudExSlideEditor> cut, params uint[] ids) =>
        cut.InvokeAsync(() => cut.FindComponent<MudExSlideCanvas>().Instance.OnSelectionAsync(ids));

    private static Task Run(IRenderedComponent<MudExSlideEditor> cut, string id)
        => cut.InvokeAsync(() => cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == id).ExecuteAsync());

    private static bool Disabled(IRenderedComponent<MudExSlideEditor> cut, string id) =>
        cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == id).Disabled;

    private static Task Key(IRenderedComponent<MudExSlideEditor> cut, string key, bool shift = false) =>
        cut.Find(".mud-ex-slide-canvas").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = key, CtrlKey = true, ShiftKey = shift });

    [Fact]
    public async Task Arrange_GroupFromRibbon_SelectsGroup_UndoRestores()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var cut = RenderEditor(presentation);

        await Select(cut, 2, 3);
        await Run(cut, "pp-group");

        var group = Assert.IsType<GroupShape>(slide.Shapes.Single(s => s is GroupShape));
        Assert.Equal([2u, 3u], group.Children.Select(c => c.Id));
        Assert.Equal([group.Id], cut.Instance.SelectedShapes);
        Assert.Equal("Group", presentation.Undo.UndoDescription);
        cut.WaitForAssertion(() => Assert.False(Disabled(cut, "pp-ungroup")));

        await Key(cut, "G", shift: true);
        Assert.DoesNotContain(slide.Shapes, s => s is GroupShape);
        Assert.Equal([2u, 3u], cut.Instance.SelectedShapes);
        Assert.Equal("Ungroup", presentation.Undo.UndoDescription);

        await cut.InvokeAsync(() => presentation.Undo.Undo());
        await cut.InvokeAsync(() => presentation.Undo.Undo());
        Assert.Equal([2u, 3u, 4u, 5u], slide.Shapes.Select(s => s.Id));
        Assert.Equal(new ShapeTransform(Inch, Inch, Inch, Inch), slide.Shapes[0].Transform);
        Assert.False(presentation.Undo.CanUndo);

        await Select(cut, 3, 4);
        await Key(cut, "g");
        Assert.Equal([3u, 4u], ((GroupShape)slide.Shapes.Single(s => s is GroupShape)).Children.Select(c => c.Id));
        Assert.Single(cut.Instance.SelectedShapes);

        await cut.Find(".mud-ex-slide-canvas").TriggerEventAsync("oncontextmenu", new MouseEventArgs { ClientX = 10, ClientY = 10 });
        _popovers.WaitForAssertion(() => _popovers.FindAll(".mud-menu-item").Single(i => i.TextContent.Trim() == "Ungroup").Click());
        Assert.DoesNotContain(slide.Shapes, s => s is GroupShape);
        Assert.Equal([3u, 4u], cut.Instance.SelectedShapes);
    }

    [Fact]
    public async Task Arrange_AlignToSlideToggle_ChangesTarget()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var cut = RenderEditor(presentation);

        await Select(cut, 3, 4);
        cut.WaitForAssertion(() => Assert.True(Disabled(cut, "pp-distribute-horizontal")));
        await Run(cut, "pp-align-shapes-left");
        Assert.Equal([4 * Inch, 4 * Inch], slide.Shapes.Skip(1).Take(2).Select(s => s.Transform!.Value.X));
        Assert.Equal("Align", presentation.Undo.UndoDescription);
        await cut.InvokeAsync(() => presentation.Undo.Undo());

        await Run(cut, "pp-align-to-slide");
        cut.WaitForAssertion(() => Assert.False(Disabled(cut, "pp-distribute-horizontal")));
        Assert.Equal(true, ((MudExRibbonMenuItem)cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == "pp-align-to-slide")).Checked);
        await Run(cut, "pp-align-shapes-left");
        Assert.Equal([0L, 0L], slide.Shapes.Skip(1).Take(2).Select(s => s.Transform!.Value.X));
        await Run(cut, "pp-distribute-horizontal");
        Assert.Equal([0L, presentation.SlideWidth - Inch], slide.Shapes.Skip(1).Take(2).Select(s => s.Transform!.Value.X));

        await Run(cut, "pp-align-selected");
        Assert.Equal(false, ((MudExRibbonMenuItem)cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == "pp-align-to-slide")).Checked);
        await Select(cut, 2);
        await Run(cut, "pp-align-shapes-bottom");
        Assert.Equal(presentation.SlideHeight - Inch, slide.Shapes[0].Transform!.Value.Y);
    }

    [Fact]
    public async Task Arrange_DisabledWithoutSelection()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var cut = RenderEditor(presentation);
        string[] all = ["pp-group", "pp-ungroup", "pp-align-shapes-left", "pp-distribute-horizontal", "pp-rotate-right", "pp-flip-horizontal", "pp-bring-to-front"];

        Assert.All(all, id => Assert.True(Disabled(cut, id), id));
        await Run(cut, "pp-rotate-right");
        Assert.False(presentation.Undo.CanUndo);

        await Select(cut, 2);
        cut.WaitForAssertion(() => Assert.False(Disabled(cut, "pp-align-shapes-left")));
        Assert.Equal([true, true, true, false, false, false],
            new[] { "pp-group", "pp-ungroup", "pp-distribute-horizontal", "pp-rotate-right", "pp-flip-horizontal", "pp-bring-to-front" }.Select(id => Disabled(cut, id)));
        await Run(cut, "pp-rotate-right");
        Assert.Equal(90, slide.Shapes[0].Transform!.Value.Rotation);
        await Run(cut, "pp-flip-vertical");
        Assert.True(slide.Shapes[0].Transform!.Value.FlipV);

        await Select(cut, 2, 3, 4);
        cut.WaitForAssertion(() => Assert.False(Disabled(cut, "pp-group")));
        Assert.False(Disabled(cut, "pp-distribute-vertical"));

        await Select(cut, 2, 5);
        cut.WaitForAssertion(() => Assert.True(Disabled(cut, "pp-group")));
        Assert.True(Disabled(cut, "pp-rotate-right"));
        Assert.True(Disabled(cut, "pp-flip-horizontal"));
        Assert.False(Disabled(cut, "pp-align-shapes-left"));
        await Key(cut, "g");
        Assert.DoesNotContain(slide.Shapes, s => s is GroupShape);
    }
}
