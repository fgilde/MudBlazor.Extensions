using System.Globalization;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Presentation.PptxReader;

namespace MudBlazor.Extensions.Office.Presentation;

internal sealed class SlideXml(string part, List<OpcRelationship> relationships)
{
    internal static readonly string[] Fills = ["noFill", "solidFill", "gradFill", "blipFill", "pattFill", "grpFill"];
    private static readonly string[] AfterLine = ["effectLst", "effectDag", "scene3d", "sp3d", "extLst"];
    private static readonly string[] AfterFill = ["ln", .. AfterLine];

    internal void WriteTree(XElement container, IEnumerable<SlideShape> shapes)
    {
        container.Elements().Where(IsShape).Remove();
        var rendered = shapes.Select(Render).ToList();
        if (container.Elements().LastOrDefault(e => e.Name == P + "nvGrpSpPr" || e.Name == P + "grpSpPr") is { } header) header.AddAfterSelf(rendered);
        else container.AddFirst(rendered);
    }

    internal static XElement Place(XElement parent, XElement child, params string[] followers)
    {
        if (child == null) return null;
        if (parent.Elements().FirstOrDefault(e => followers.Contains(e.Name.LocalName)) is { } next) next.AddBeforeSelf(child);
        else parent.Add(child);
        return child;
    }

    internal static XElement SolidFill(string color) => ColorResolver.ToXml(color) is { } c ? new XElement(A + "solidFill", c) : null;

    private static bool IsShape(XElement e) =>
        e.Name.LocalName == "AlternateContent" || e.Name.Namespace == P && e.Name.LocalName is "sp" or "cxnSp" or "pic" or "grpSp" or "graphicFrame" or "contentPart";

    private XElement Render(SlideShape shape)
    {
        var e = shape.Source != null ? new XElement(shape.Source) : Create(shape);
        foreach (var branch in e.Name.LocalName == "AlternateContent" ? e.Elements().SelectMany(b => b.Elements().Take(1)) : [e])
        {
            if (branch.Descendants(P + "cNvPr").FirstOrDefault() is { } properties)
            {
                properties.SetAttributeValue("id", shape.Id);
                properties.SetAttributeValue("name", shape.Name);
            }

            WriteTransform(branch, shape);
        }

        switch (shape)
        {
            case AutoShape auto:
                WriteAutoShape(e, auto);
                break;
            case GroupShape group:
                WriteTree(e, group.Children);
                break;
        }

        return e;
    }

    private XElement Create(SlideShape shape) => shape switch
    {
        AutoShape auto => new XElement(P + "sp",
            new XElement(P + "nvSpPr", new XElement(P + "cNvPr"), new XElement(P + "cNvSpPr", auto.IsTextBox ? new XAttribute("txBox", "1") : null), NvPr(shape)),
            new XElement(P + "spPr")),
        PictureShape picture => new XElement(P + "pic",
            new XElement(P + "nvPicPr", new XElement(P + "cNvPr"), new XElement(P + "cNvPicPr", new XElement(A + "picLocks", new XAttribute("noChangeAspect", "1"))), NvPr(shape)),
            new XElement(P + "blipFill", new XElement(A + "blip", picture.ImagePart == null ? null : new XAttribute(R + "embed", ImageRelationship(picture.ImagePart))),
                new XElement(A + "stretch", new XElement(A + "fillRect"))),
            new XElement(P + "spPr", new XElement(A + "prstGeom", new XAttribute("prst", "rect"), new XElement(A + "avLst")))),
        GroupShape => new XElement(P + "grpSp",
            new XElement(P + "nvGrpSpPr", new XElement(P + "cNvPr"), new XElement(P + "cNvGrpSpPr"), new XElement(P + "nvPr")), new XElement(P + "grpSpPr")),
        _ => throw new NotSupportedException($"A {shape.GetType().Name} without source XML cannot be written.")
    };

    private static XElement NvPr(SlideShape shape) => new(P + "nvPr", shape.Placeholder is { } ph
        ? new XElement(P + "ph", ph.Type == "obj" ? null : new XAttribute("type", ph.Type), ph.Index is { } index ? new XAttribute("idx", index) : null)
        : null);

    private string ImageRelationship(string image)
    {
        var target = PptxWriter.Relative(part, image);
        var rel = relationships.Find(r => r.Type == PptxWriter.ImageRel && r.TargetMode == null && r.Target == target);
        if (rel == null) relationships.Add(rel = new OpcRelationship(PptxWriter.NextId(relationships), PptxWriter.ImageRel, target, null));
        return rel.Id;
    }

    private static void WriteTransform(XElement e, SlideShape shape)
    {
        var xfrm = ShapeReader.XfrmOf(e, shape is OpaqueShape);
        if (ShapeReader.ReadTransform(xfrm) == shape.Transform) return;
        if (shape.Transform is not { } t)
        {
            xfrm?.Remove();
            return;
        }

        if (xfrm == null)
        {
            if ((e.Element(P + "spPr") ?? e.Element(P + "grpSpPr")) is not { } holder) return;
            holder.AddFirst(xfrm = new XElement(A + "xfrm"));
        }

        xfrm.SetAttributeValue("rot", t.Rotation == 0 ? null : (long)Math.Round(t.Rotation * 60000));
        xfrm.SetAttributeValue("flipH", t.FlipH ? "1" : null);
        xfrm.SetAttributeValue("flipV", t.FlipV ? "1" : null);
        var offset = xfrm.Element(A + "off") ?? AddFirst(xfrm, new XElement(A + "off"));
        offset.SetAttributeValue("x", t.X);
        offset.SetAttributeValue("y", t.Y);
        var extent = xfrm.Element(A + "ext") ?? AddAfter(offset, new XElement(A + "ext"));
        extent.SetAttributeValue("cx", t.Width);
        extent.SetAttributeValue("cy", t.Height);
        if (shape is GroupShape group && xfrm.Element(A + "chOff") == null)
        {
            var space = group.ChildTransform;
            extent.AddAfterSelf(new XElement(A + "chOff", new XAttribute("x", space.X), new XAttribute("y", space.Y)),
                new XElement(A + "chExt", new XAttribute("cx", space.Width), new XAttribute("cy", space.Height)));
        }
    }

    private static void WriteAutoShape(XElement e, AutoShape shape)
    {
        var before = new AutoShape();
        ShapeReader.ReadStyle(before, e);
        var spPr = e.Element(P + "spPr") ?? Place(e, new XElement(P + "spPr"), "style", "txBody", "extLst");
        var sameAdjustments = before.Adjustments.Count == shape.Adjustments.Count && !before.Adjustments.Except(shape.Adjustments).Any();
        if (shape.Geometry != "custom" && (shape.Source == null && shape.Placeholder == null || before.Geometry != shape.Geometry || !sameAdjustments))
        {
            spPr.Elements().Where(g => g.Name == A + "prstGeom" || g.Name == A + "custGeom").Remove();
            Place(spPr, new XElement(A + "prstGeom", new XAttribute("prst", shape.Geometry),
                new XElement(A + "avLst", shape.Adjustments.Select(a =>
                    new XElement(A + "gd", new XAttribute("name", a.Key), new XAttribute("fmla", "val " + a.Value.ToString(CultureInfo.InvariantCulture)))))), [.. Fills, .. AfterFill]);
        }

        if (before.Fill != shape.Fill && shape.Fill.Kind != ShapeFillKind.Other)
        {
            spPr.Elements().Where(f => Fills.Contains(f.Name.LocalName)).Remove();
            Place(spPr, shape.Fill.Kind switch
            {
                ShapeFillKind.None => new XElement(A + "noFill"),
                ShapeFillKind.Solid => SolidFill(shape.Fill.Color),
                _ => null
            }, AfterFill);
        }

        if (before.Line != shape.Line) WriteLine(spPr, before.Line, shape.Line);
        TextBodyXml.Write(e, shape.Text);
    }

    private static void WriteLine(XElement spPr, ShapeLine before, ShapeLine line)
    {
        var ln = spPr.Element(A + "ln");
        if (line == null)
        {
            ln?.Remove();
            return;
        }

        ln ??= Place(spPr, new XElement(A + "ln"), AfterLine);
        if (before?.Width != line.Width) ln.SetAttributeValue("w", line.Width);
        if (before == null || before.Color != line.Color)
        {
            ln.Elements().Where(f => Fills.Contains(f.Name.LocalName)).Remove();
            ln.AddFirst(line.Color == null ? new XElement(A + "noFill") : SolidFill(line.Color));
        }

        if (before == null || before.Dash != line.Dash)
        {
            ln.Elements().Where(d => d.Name == A + "prstDash" || d.Name == A + "custDash").Remove();
            Place(ln, new XElement(A + "prstDash", new XAttribute("val", line.Dash ?? "solid")), "round", "bevel", "miter", "headEnd", "tailEnd", "extLst");
        }
    }

    private static XElement AddFirst(XElement parent, XElement child)
    {
        parent.AddFirst(child);
        return child;
    }

    private static XElement AddAfter(XElement sibling, XElement child)
    {
        sibling.AddAfterSelf(child);
        return child;
    }
}
