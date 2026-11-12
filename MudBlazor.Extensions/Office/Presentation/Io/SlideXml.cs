using System.Globalization;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Presentation.PptxReader;

namespace MudBlazor.Extensions.Office.Presentation;

internal sealed class SlideXml(string part, string basis, List<OpcRelationship> relationships, OpcPackage opc, Func<string, Action<XDocument>, string> cloneTarget)
{
    internal static readonly string[] Fills = ["noFill", "solidFill", "gradFill", "blipFill", "pattFill", "grpFill"];
    private static readonly string[] AfterLine = ["effectLst", "effectDag", "scene3d", "sp3d", "extLst"];
    private static readonly string[] AfterFill = ["ln", .. AfterLine];
    private readonly HashSet<string> _used = [];
    private readonly HashSet<string> _creationIds = [];

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

    internal static bool IsShape(XElement e) =>
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

        RenewCreationId(e, shape);
        switch (shape)
        {
            case AutoShape auto:
                WriteAutoShape(e, auto);
                break;
            case GroupShape group:
                WriteTree(e, group.Children);
                break;
            case TableShape table:
                TableXml.Write(e, table);
                break;
        }

        if (shape.Source != null && shape.SourcePart != null && !string.Equals(shape.SourcePart, basis, StringComparison.OrdinalIgnoreCase)) Import(e, shape.SourcePart, shape is GroupShape);
        else if (shape is not GroupShape) Separate(e);
        return e;
    }

    private void RenewCreationId(XElement e, SlideShape shape)
    {
        if (e.Descendants(P + "cNvPr").FirstOrDefault()?.Descendants().FirstOrDefault(d => d.Name.LocalName == "creationId") is not { } creation
            || (string)creation.Attribute("id") is not { Length: > 9 } id || _creationIds.Add(id)) return;
        var renewed = id[..^9] + shape.Id.ToString("X8", CultureInfo.InvariantCulture) + "}";
        _creationIds.Add(renewed);
        foreach (var element in e.Descendants().Where(d => d.Name.LocalName == "creationId" && (string)d.Attribute("id") == id)) element.SetAttributeValue("id", renewed);
    }

    private void Separate(XElement e)
    {
        var references = e.DescendantsAndSelf().Attributes().Where(a => a.Name.Namespace == R).ToList();
        foreach (var id in references.Select(a => a.Value).Distinct().ToList())
        {
            if (relationships.Find(r => r.Id == id) is not { TargetMode: null } rel || !PptxWriter.Owned(rel.Type) || _used.Add(id)) continue;
            var copy = Clone(rel);
            foreach (var reference in references.Where(a => a.Value == id)) reference.Value = copy;
        }
    }

    private void Import(XElement e, string origin, bool group)
    {
        var rels = opc.Relationships(origin);
        var ids = new Dictionary<string, string>();
        var own = group ? e.Elements().Where(c => !IsShape(c)).SelectMany(c => c.DescendantsAndSelf()).Prepend(e) : e.DescendantsAndSelf();
        foreach (var reference in own.Attributes().Where(a => a.Name.Namespace == R).ToList())
        {
            if (!ids.TryGetValue(reference.Value, out var id))
                ids[reference.Value] = id = rels.FirstOrDefault(r => r.Id == reference.Value) is { } rel ? Import(rel, origin) : null;
            if (id == null) reference.Remove();
            else reference.Value = id;
        }
    }

    private string Import(OpcRelationship rel, string origin)
    {
        if (rel.TargetMode == null && PptxWriter.Owned(rel.Type)) return Clone(rel, origin);
        var target = rel.TargetMode == null ? PptxWriter.Relative(part, opc.ResolveTarget(origin, rel.Target)) : rel.Target;
        var existing = relationships.Find(r => r.Type == rel.Type && r.TargetMode == rel.TargetMode && r.Target == target);
        if (existing == null) relationships.Add(existing = new OpcRelationship(PptxWriter.NextId(relationships), rel.Type, target, rel.TargetMode));
        return existing.Id;
    }

    private string Clone(OpcRelationship rel, string from = null)
    {
        from ??= part;
        var target = cloneTarget(opc.ResolveTarget(from, rel.Target), rel.Type.EndsWith("/diagramData", StringComparison.Ordinal) ? data => CloneDrawing(data, from) : null);
        var copy = new OpcRelationship(PptxWriter.NextId(relationships), rel.Type, target, null);
        relationships.Add(copy);
        _used.Add(copy.Id);
        return copy.Id;
    }

    private void CloneDrawing(XDocument data, string from)
    {
        var rels = from == part ? relationships : opc.Relationships(from);
        foreach (var extension in data.Descendants().Where(d => d.Name.LocalName == "dataModelExt").ToList())
            if (rels.FirstOrDefault(r => r.Id == (string)extension.Attribute("relId")) is { TargetMode: null } drawing)
                extension.SetAttributeValue("relId", Clone(drawing, from));
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
        TableShape => new XElement(P + "graphicFrame",
            new XElement(P + "nvGraphicFramePr", new XElement(P + "cNvPr"),
                new XElement(P + "cNvGraphicFramePr", new XElement(A + "graphicFrameLocks", new XAttribute("noGrp", "1"))), NvPr(shape)),
            new XElement(P + "xfrm"), TableXml.Create()),
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

        var fill = shape.Fill.Kind switch
        {
            ShapeFillKind.None => new XElement(A + "noFill"),
            ShapeFillKind.Solid => SolidFill(shape.Fill.Color),
            _ => null
        };
        if (before.Fill != shape.Fill && (fill != null || shape.Fill.Kind == ShapeFillKind.Inherited))
        {
            spPr.Elements().Where(f => Fills.Contains(f.Name.LocalName)).Remove();
            Place(spPr, fill, AfterFill);
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
        if ((before == null || before.Color != line.Color) && (line.Color == null ? new XElement(A + "noFill") : SolidFill(line.Color)) is { } fill)
        {
            ln.Elements().Where(f => Fills.Contains(f.Name.LocalName)).Remove();
            ln.AddFirst(fill);
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
