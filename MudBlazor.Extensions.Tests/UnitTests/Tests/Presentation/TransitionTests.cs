using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public class TransitionTests
{
    internal const string ColorMapOverride = "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>";

    internal const string Alternate =
        "<mc:AlternateContent xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:p14=\"http://schemas.microsoft.com/office/powerpoint/2010/main\">" +
        "<mc:Choice Requires=\"p14\"><p:transition spd=\"slow\" p14:dur=\"2000\" advClick=\"0\" advTm=\"3000\"><p14:vortex dir=\"r\"/></p:transition></mc:Choice>" +
        "<mc:Fallback><p:transition spd=\"slow\" advClick=\"0\" advTm=\"3000\"><p:push dir=\"u\"/></p:transition></mc:Fallback></mc:AlternateContent>";

    internal static Presentation Deck(string transition) =>
        new PptxBuilder().Slide(Shape(2, "Box", Xfrm(0, 0, 10, 10)), tail: ColorMapOverride + transition).Slide(Shape(2, "Other", Xfrm(0, 0, 10, 10))).Load();

    [Fact]
    public void Reader_Transition_FadeMedium()
    {
        var presentation = Deck("<p:transition spd=\"med\"><p:fade/></p:transition>");

        Assert.Equal(new SlideTransition(TransitionKind.Fade, TransitionDirection.None, 0.75, true, null), presentation.Slides[0].Transition);
        Assert.Equal(SlideTransition.None, presentation.Slides[1].Transition);
    }

    [Fact]
    public void Reader_Transition_AlternateContent_UsesFallbackKindAndP14Duration()
    {
        Assert.Equal(new SlideTransition(TransitionKind.Push, TransitionDirection.Up, 2, false, 3), Deck(Alternate).Slides[0].Transition);
    }

    [Fact]
    public void Reader_Transition_Unknown_IsOther()
    {
        Assert.Equal(TransitionKind.Other, Deck("<p:transition><p:circle/></p:transition>").Slides[0].Transition.Kind);
        Assert.Equal(new SlideTransition(TransitionKind.None, TransitionDirection.None, 0.5, true, 2.5), Deck("<p:transition advTm=\"2500\"/>").Slides[0].Transition);
        Assert.Equal(new SlideTransition(TransitionKind.Split, TransitionDirection.Vertical, 1, true, null),
            Deck("<p:transition spd=\"slow\"><p:split orient=\"vert\" dir=\"in\"/></p:transition>").Slides[0].Transition);
    }

    [Fact]
    public void SetTransition_UndoRedo()
    {
        var presentation = Deck(Alternate);
        var (first, second) = (presentation.Slides[0], presentation.Slides[1]);
        var before = first.Transition;
        var wipe = new SlideTransition(TransitionKind.Wipe, TransitionDirection.Left, 1, true, 5);
        var changed = new List<PresentationChangedEventArgs>();
        presentation.Changed += (_, e) => changed.Add(e);

        presentation.Execute(new SetTransitionCommand([second, first], wipe));
        Assert.Equal([wipe, wipe], presentation.Slides.Select(s => s.Transition));
        presentation.Undo.Undo();
        Assert.Equal([before, SlideTransition.None], presentation.Slides.Select(s => s.Transition));
        presentation.Undo.Redo();
        Assert.Equal([wipe, wipe], presentation.Slides.Select(s => s.Transition));

        Assert.Equal(3, changed.Count);
        Assert.All(changed, e => Assert.Equal(2, e.Slides.Count));
    }

    [Fact]
    public void SetTransition_DefaultDirection_InvalidValues_Throw()
    {
        var presentation = Deck("<p:transition><p:circle/></p:transition>");
        var (first, second) = (presentation.Slides[0], presentation.Slides[1]);

        presentation.Execute(new SetTransitionCommand([second], new SlideTransition(TransitionKind.Push, TransitionDirection.None, 0.5, true, null)));
        Assert.Equal(TransitionDirection.Left, second.Transition.Direction);
        presentation.Execute(new SetTransitionCommand([first], first.Transition with { AdvanceAfterSeconds = 4 }));
        Assert.Equal(4, first.Transition.AdvanceAfterSeconds);

        foreach (var transition in new[]
                 {
                     new SlideTransition(TransitionKind.Other, TransitionDirection.None, 0.5, true, null),
                     new SlideTransition(TransitionKind.Fade, TransitionDirection.Up, 0.5, true, null),
                     new SlideTransition(TransitionKind.Zoom, TransitionDirection.Left, 0.5, true, null),
                     new SlideTransition(TransitionKind.Fade, TransitionDirection.None, -1, true, null),
                     new SlideTransition(TransitionKind.Fade, TransitionDirection.None, 0.5, true, double.PositiveInfinity),
                     new SlideTransition((TransitionKind)99, TransitionDirection.None, 0.5, true, null)
                 })
            Assert.Throws<ArgumentException>(() => presentation.Execute(new SetTransitionCommand([first, second], transition)));
        Assert.Throws<ArgumentNullException>(() => presentation.Execute(new SetTransitionCommand([first], null)));
    }
}
