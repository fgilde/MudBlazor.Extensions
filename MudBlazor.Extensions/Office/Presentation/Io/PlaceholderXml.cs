using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Presentation.PptxReader;

namespace MudBlazor.Extensions.Office.Presentation;

internal static class PlaceholderXml
{
    private static readonly string[][] Slots =
    [
        ["buNone", "buAutoNum", "buChar", "buBlip"], ["buFontTx", "buFont"], ["buSzTx", "buSzPct", "buSzPts"], ["buClrTx", "buClr"], SlideXml.Fills,
        ["noAutofit", "normAutofit", "spAutoFit"], ["effectLst", "effectDag"], ["uLnTx", "uLn"], ["uFillTx", "uFill"]
    ];

    private static readonly string[] Order =
    [
        "lnSpc", "spcBef", "spcAft", "buClrTx", "buClr", "buSzTx", "buSzPct", "buSzPts", "buFontTx", "buFont", "buNone", "buAutoNum", "buChar", "buBlip", "tabLst",
        "defRPr", "ln", .. SlideXml.Fills, "effectLst", "effectDag", "highlight", "uLnTx", "uLn", "uFillTx", "uFill", "latin", "ea", "cs", "sym", "hlinkClick",
        "hlinkMouseOver", "rtl", "prstTxWarp", "noAutofit", "normAutofit", "spAutoFit", "scene3d", "sp3d", "flatTx"
    ];

    internal static bool Collides(Slide slide, SlideShape shape) =>
        shape.Placeholder is { } placeholder
        && (slide.Shapes.Any(s => s.Placeholder is { } other && ShapeReader.Category(other.Type) == ShapeReader.Category(placeholder.Type))
            || slide.Layout is not { } layout || !layout.ShapeList.Contains(shape.InheritsFrom) && layout.Master?.ShapeList.Contains(shape.InheritsFrom) != true);

    internal static void ToTextBox(SlideShape shape)
    {
        if (shape is AutoShape auto) Flatten(auto);
        shape.Transform = shape.ResolvedTransform;
        shape.Source?.Elements().FirstOrDefault()?.Element(P + "nvPr")?.Element(P + "ph")?.Remove();
        shape.Placeholder = null;
        shape.InheritsFrom = null;
    }

    private static void Flatten(AutoShape shape)
    {
        var source = shape.Source ??= new XElement(P + "sp",
            new XElement(P + "nvSpPr", new XElement(P + "cNvPr"), new XElement(P + "cNvSpPr"), new XElement(P + "nvPr")), new XElement(P + "spPr"));
        var inherited = Chain(shape.InheritsFrom).Select(s => s.Source).Where(s => s != null).ToList();
        source.Element(P + "nvSpPr")?.Element(P + "cNvSpPr")?.SetAttributeValue("txBox", "1");
        shape.IsTextBox = true;

        var spPr = source.Element(P + "spPr") ?? SlideXml.Place(source, new XElement(P + "spPr"), "style", "txBody", "extLst");
        if (!spPr.Elements().Any(g => g.Name == A + "prstGeom" || g.Name == A + "custGeom"))
            SlideXml.Place(spPr, new XElement(A + "prstGeom", new XAttribute("prst", shape.Geometry == "custom" ? "rect" : shape.Geometry), new XElement(A + "avLst")),
                [.. SlideXml.Fills, .. SlideXml.AfterFill]);
        if (ShapeReader.FillOf(spPr) == null && inherited.Select(s => ShapeReader.FillOf(s.Element(P + "spPr"))).FirstOrDefault(f => f != null) is { } fill)
            SlideXml.Place(spPr, new XElement(fill), SlideXml.AfterFill);
        if (spPr.Element(A + "ln") == null && inherited.Select(s => s.Element(P + "spPr")?.Element(A + "ln")).FirstOrDefault(l => l != null) is { } line)
            SlideXml.Place(spPr, new XElement(line), SlideXml.AfterLine);
        var styled = new AutoShape();
        ShapeReader.ReadStyle(styled, source);
        if (shape.Fill.Kind == ShapeFillKind.Inherited) shape.Fill = styled.Fill;
        shape.Line ??= styled.Line;

        if (shape.Text == null) return;
        var txBody = source.Element(P + "txBody") ?? SlideXml.Place(source, new XElement(P + "txBody", new XElement(A + "bodyPr"), new XElement(A + "lstStyle")), "extLst");
        var bodies = inherited.Select(s => s.Element(P + "txBody")).Prepend(txBody).Where(b => b != null).ToList();
        var bodyPr = Merge(A + "bodyPr", bodies.Select(b => b.Element(A + "bodyPr")));
        txBody.Element(A + "bodyPr")?.Remove();
        txBody.AddFirst(bodyPr);
        var own = txBody.Element(A + "lstStyle");
        var levels = Enumerable.Range(0, 9).Select(level => Merge(A + $"lvl{level + 1}pPr", TextStyles.ListLevels(shape, level))).Where(l => l.HasAttributes || l.HasElements);
        var lstStyle = new XElement(A + "lstStyle", own?.Element(A + "defPPr"), levels, own?.Element(A + "extLst"));
        own?.Remove();
        bodyPr.AddAfterSelf(lstStyle);
        var read = ShapeReader.ReadText(txBody);
        shape.Text = shape.Text with
        {
            Anchor = read.Anchor, InsetLeft = read.InsetLeft, InsetTop = read.InsetTop, InsetRight = read.InsetRight, InsetBottom = read.InsetBottom, Wrap = read.Wrap,
            FontScalePercent = read.FontScalePercent, LineSpaceReductionPercent = read.LineSpaceReductionPercent
        };
    }

    private static IEnumerable<SlideShape> Chain(SlideShape shape)
    {
        for (var s = shape; s != null; s = s.InheritsFrom) yield return s;
    }

    private static XElement Merge(XName name, IEnumerable<XElement> specificFirst)
    {
        var result = new XElement(name);
        foreach (var element in specificFirst.Where(e => e != null).Reverse()) Overlay(result, element);
        return result;
    }

    private static void Overlay(XElement target, XElement source)
    {
        foreach (var attribute in source.Attributes().Where(a => !a.IsNamespaceDeclaration)) target.SetAttributeValue(attribute.Name, attribute.Value);
        foreach (var child in source.Elements())
        {
            var slot = Slots.FirstOrDefault(s => s.Contains(child.Name.LocalName)) ?? [child.Name.LocalName];
            var existing = target.Elements().Where(e => e.Name.Namespace == child.Name.Namespace && slot.Contains(e.Name.LocalName)).ToList();
            if (child.Name == A + "defRPr" && existing.Count == 1) Overlay(existing[0], child);
            else
            {
                existing.Remove();
                target.Add(new XElement(child));
            }
        }

        target.ReplaceNodes(target.Elements().OrderBy(Rank).ToList());
    }

    private static int Rank(XElement element) => element.Name.LocalName == "extLst" ? int.MaxValue
        : Array.IndexOf(Order, element.Name.LocalName) is >= 0 and var index ? index : Order.Length;
}
