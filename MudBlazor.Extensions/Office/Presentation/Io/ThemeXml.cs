using System.Globalization;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Presentation.PptxReader;

namespace MudBlazor.Extensions.Office.Presentation;

internal static class ThemeXml
{
    internal static PresentationTheme Read(XDocument theme) =>
        PresentationTheme.FromPalette((string)theme.Root?.Attribute("name"), ThemeColors.Read(theme), Typeface(theme, "majorFont"), Typeface(theme, "minorFont"));

    internal static XDocument Patch(XDocument source, PresentationTheme theme)
    {
        var xml = new XDocument(source);
        var elements = xml.Root?.Element(A + "themeElements");
        var current = Read(source);
        var scheme = elements?.Element(A + "clrScheme");
        for (var i = 0; i < PresentationTheme.Slots.Length; i++)
            if (current.Colors[i] != theme.Colors[i] && scheme?.Element(A + PresentationTheme.Slots[i]) is { } slot)
                slot.ReplaceNodes(new XElement(A + "srgbClr", new XAttribute("val", theme.Colors[i][1..])));
        SetTypeface(elements, "majorFont", theme.MajorFont);
        SetTypeface(elements, "minorFont", theme.MinorFont);
        return xml;
    }

    internal static XDocument WriteBackground(XDocument source, SlideBackground background)
    {
        var xml = new XDocument(source);
        var slide = xml.Root!.Element(P + "cSld");
        if (slide == null) return xml;
        var element = slide.Element(P + "bg");
        if (element == null) slide.AddFirst(element = new XElement(P + "bg"));
        element.ReplaceNodes(Background(background)?.Nodes());
        if (!element.HasElements) element.Remove();
        return xml;
    }

    internal static XElement Background(SlideBackground background)
    {
        var fill = background?.Kind switch
        {
            ShapeFillKind.None => new XElement(A + "noFill"),
            ShapeFillKind.Solid when ColorResolver.ToXml(background.Color) is { } color => new XElement(A + "solidFill", color),
            ShapeFillKind.Gradient => Gradient(background),
            _ => null
        };
        return fill == null ? null : new XElement(P + "bg", new XElement(P + "bgPr", fill, new XElement(A + "effectLst")));
    }

    private static XElement Gradient(SlideBackground background)
    {
        var stops = background.Stops.Select(s => (s.Position, Color: ColorResolver.ToXml(s.Color))).ToList();
        if (stops.Count < 2 || stops.Any(s => s.Color == null || s.Position is < 0 or > 1)) return null;
        var gradient = new XElement(A + "gradFill", new XAttribute("rotWithShape", "1"),
            new XElement(A + "gsLst", stops.Select(s => new XElement(A + "gs", new XAttribute("pos", Fixed(s.Position * 100000)), s.Color))));
        if (background.Angle is { } angle && double.IsFinite(angle))
            gradient.Add(new XElement(A + "lin", new XAttribute("ang", Fixed((angle % 360 + 360) % 360 * 60000)), new XAttribute("scaled", "0")));
        return gradient;
    }

    private static string Fixed(double value) => ((long)Math.Round(value)).ToString(CultureInfo.InvariantCulture);

    private static string Typeface(XDocument theme, string font) =>
        (string)theme.Root?.Element(A + "themeElements")?.Element(A + "fontScheme")?.Element(A + font)?.Element(A + "latin")?.Attribute("typeface");

    private static void SetTypeface(XElement elements, string font, string typeface)
    {
        if (typeface == null || elements?.Element(A + "fontScheme")?.Element(A + font) is not { } element) return;
        var latin = element.Element(A + "latin");
        if (latin == null) element.AddFirst(latin = new XElement(A + "latin"));
        if ((string)latin.Attribute("typeface") != typeface) latin.SetAttributeValue("typeface", typeface);
    }
}
