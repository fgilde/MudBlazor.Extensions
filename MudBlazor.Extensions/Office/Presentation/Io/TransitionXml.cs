using System.Globalization;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Presentation.PptxReader;

namespace MudBlazor.Extensions.Office.Presentation;

internal static class TransitionXml
{
    internal static readonly XNamespace Mc = "http://schemas.openxmlformats.org/markup-compatibility/2006";

    private static readonly (TransitionKind Kind, string Name, TransitionDirection[] Directions)[] Kinds =
    [
        (TransitionKind.Cut, "cut", []),
        (TransitionKind.Fade, "fade", []),
        (TransitionKind.Push, "push", Sides),
        (TransitionKind.Wipe, "wipe", Sides),
        (TransitionKind.Split, "split", Axes),
        (TransitionKind.Cover, "cover", Sides),
        (TransitionKind.Uncover, "pull", Sides),
        (TransitionKind.Zoom, "zoom", [TransitionDirection.Out, TransitionDirection.In]),
        (TransitionKind.Dissolve, "dissolve", []),
        (TransitionKind.RandomBars, "randomBar", Axes)
    ];

    private static TransitionDirection[] Sides => [TransitionDirection.Left, TransitionDirection.Up, TransitionDirection.Right, TransitionDirection.Down];

    private static TransitionDirection[] Axes => [TransitionDirection.Horizontal, TransitionDirection.Vertical];

    internal static TransitionDirection[] Directions(TransitionKind kind) => Kinds.FirstOrDefault(k => k.Kind == kind).Directions ?? [];

    internal static SlideTransition Read(XElement slide)
    {
        var (element, choice) = Find(slide);
        if (element == null) return SlideTransition.None;
        var effect = EffectOf(element);
        var kind = effect == null ? TransitionKind.None
            : effect.Name.Namespace == P ? Kinds.FirstOrDefault(k => k.Name == effect.Name.LocalName, (TransitionKind.Other, null, null)).Kind
            : TransitionKind.Other;
        var milliseconds = Long((string)(choice ?? element).Attribute(P14 + "dur"));
        return new SlideTransition(kind, effect == null ? TransitionDirection.None : Direction(kind, effect),
            milliseconds is { } ms ? ms / 1000.0 : (string)element.Attribute("spd") switch { "slow" => 1, "med" => 0.75, _ => 0.5 },
            (string)element.Attribute("advClick") is not ("0" or "false"),
            Long((string)element.Attribute("advTm")) / 1000.0);
    }

    internal static void Write(XElement slide, SlideTransition transition)
    {
        var loaded = Read(slide);
        if (loaded == transition) return;
        var element = Find(slide).Transition;
        var old = loaded.Kind == transition.Kind && loaded.Direction == transition.Direction ? EffectOf(element) : null;
        var sound = element?.Element(P + "sndAc");
        slide.Elements(P + "transition").Remove();
        Alternates(slide).Remove();
        if (transition is { Kind: TransitionKind.None, AdvanceOnClick: true, AdvanceAfterSeconds: null }) return;
        var speed = transition.DurationSeconds <= 0.625 ? null : transition.DurationSeconds <= 0.875 ? "med" : "slow";
        SlideXml.Place(slide, new XElement(P + "transition",
            speed == null ? null : new XAttribute("spd", speed),
            transition.AdvanceOnClick ? null : new XAttribute("advClick", "0"),
            transition.AdvanceAfterSeconds is { } after ? new XAttribute("advTm", ((long)Math.Round(after * 1000)).ToString(CultureInfo.InvariantCulture)) : null,
            old != null ? new XElement(old) : Effect(transition), sound == null ? null : new XElement(sound)), "timing", "extLst");
    }

    private static XElement EffectOf(XElement transition) => transition?.Elements().FirstOrDefault(e => e.Name != P + "sndAc" && e.Name != P + "extLst");

    private static XElement Effect(SlideTransition transition)
    {
        if (Kinds.FirstOrDefault(k => k.Kind == transition.Kind) is not { Name: { } name }) return null;
        var effect = new XElement(P + name);
        var direction = transition.Direction;
        switch (transition.Kind)
        {
            case TransitionKind.Push or TransitionKind.Wipe or TransitionKind.Cover or TransitionKind.Uncover:
                effect.SetAttributeValue("dir", direction switch { TransitionDirection.Up => "u", TransitionDirection.Right => "r", TransitionDirection.Down => "d", _ => "l" });
                break;
            case TransitionKind.Split:
                effect.SetAttributeValue("orient", direction == TransitionDirection.Vertical ? "vert" : "horz");
                break;
            case TransitionKind.RandomBars:
                effect.SetAttributeValue("dir", direction == TransitionDirection.Vertical ? "vert" : "horz");
                break;
            case TransitionKind.Zoom:
                effect.SetAttributeValue("dir", direction == TransitionDirection.In ? "in" : "out");
                break;
        }

        return effect;
    }

    private static TransitionDirection Direction(TransitionKind kind, XElement effect) => kind switch
    {
        TransitionKind.Push or TransitionKind.Wipe or TransitionKind.Cover or TransitionKind.Uncover => ((string)effect.Attribute("dir") ?? "l")[0] switch
        {
            'u' => TransitionDirection.Up,
            'r' => TransitionDirection.Right,
            'd' => TransitionDirection.Down,
            _ => TransitionDirection.Left
        },
        TransitionKind.Split => (string)effect.Attribute("orient") == "vert" ? TransitionDirection.Vertical : TransitionDirection.Horizontal,
        TransitionKind.RandomBars => (string)effect.Attribute("dir") == "vert" ? TransitionDirection.Vertical : TransitionDirection.Horizontal,
        TransitionKind.Zoom => (string)effect.Attribute("dir") == "in" ? TransitionDirection.In : TransitionDirection.Out,
        _ => TransitionDirection.None
    };

    private static (XElement Transition, XElement Choice) Find(XElement slide)
    {
        if (slide.Element(P + "transition") is { } plain) return (plain, null);
        if (Alternates(slide).FirstOrDefault() is not { } alternate) return (null, null);
        return (alternate.Element(Mc + "Fallback")?.Element(P + "transition") ?? alternate.Descendants(P + "transition").First(), alternate.Element(Mc + "Choice")?.Element(P + "transition"));
    }

    private static IEnumerable<XElement> Alternates(XElement slide) => slide.Elements(Mc + "AlternateContent").Where(a => a.Descendants(P + "transition").Any()).ToList();
}
