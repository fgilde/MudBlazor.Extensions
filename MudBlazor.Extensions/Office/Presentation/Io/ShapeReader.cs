using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Presentation.PptxReader;

namespace MudBlazor.Extensions.Office.Presentation;

internal sealed class ShapeReader(OpcPackage opc, string part, IReadOnlyList<OpcRelationship> rels, SlideMaster master, IReadOnlyList<SlideShape> inherited, bool matchIndex)
{
    private static readonly string[] MediaElements = ["videoFile", "audioFile", "quickTimeFile", "wavAudioFile", "audioCd", "media"];

    internal IEnumerable<SlideShape> ReadTree(XElement root) => ReadChildren(root?.Element(P + "cSld")?.Element(P + "spTree"), null);

    private IEnumerable<SlideShape> ReadChildren(XElement container, GroupShape parent) =>
        container?.Elements().Select(e => ReadShape(e, parent)).Where(s => s != null).ToList() ?? [];

    private SlideShape ReadShape(XElement e, GroupShape parent)
    {
        SlideShape shape;
        if (e.Name == P + "sp" || e.Name == P + "cxnSp") shape = new AutoShape();
        else if (e.Name == P + "pic") shape = IsMedia(e) ? new OpaqueShape { Kind = "media" } : new PictureShape();
        else if (e.Name == P + "grpSp") shape = new GroupShape();
        else if (e.Name == P + "graphicFrame") shape = new OpaqueShape { Kind = FrameKind(e) };
        else if (e.Name == P + "contentPart" || e.Name.LocalName == "AlternateContent") shape = new OpaqueShape();
        else return null;

        var properties = e.Descendants(P + "cNvPr").FirstOrDefault();
        shape.Id = (uint?)Long((string)properties?.Attribute("id")) ?? 0;
        shape.Name = (string)properties?.Attribute("name") ?? "";
        shape.Source = e;
        shape.Parent = parent;
        shape.Master = master;
        var xfrm = e.Element(P + "spPr")?.Element(A + "xfrm") ?? e.Element(P + "grpSpPr")?.Element(A + "xfrm") ?? e.Element(P + "xfrm")
                   ?? (shape is OpaqueShape ? e.Descendants().FirstOrDefault(d => d.Name.LocalName == "xfrm") : null);
        shape.Transform = ReadTransform(xfrm);
        if (e.Elements().FirstOrDefault()?.Element(P + "nvPr")?.Element(P + "ph") is { } ph)
        {
            shape.Placeholder = new PlaceholderRef((string)ph.Attribute("type") ?? "obj", (uint?)Long((string)ph.Attribute("idx")));
            shape.InheritsFrom = FindPlaceholder(shape.Placeholder);
        }

        switch (shape)
        {
            case AutoShape auto:
                ReadAutoShape(auto, e);
                break;
            case PictureShape picture:
                var embed = (string)e.Element(P + "blipFill")?.Element(A + "blip")?.Attribute(R + "embed");
                if (rels.FirstOrDefault(r => r.Id == embed && r.TargetMode == null) is { } rel)
                {
                    picture.ImagePart = opc.ResolveTarget(part, rel.Target);
                    picture.ContentType = opc.ContentType(picture.ImagePart);
                }

                break;
            case GroupShape group:
                var offset = xfrm?.Element(A + "chOff");
                var extent = xfrm?.Element(A + "chExt");
                group.ChildTransform = offset != null && extent != null
                    ? new ShapeTransform(Long((string)offset.Attribute("x")) ?? 0, Long((string)offset.Attribute("y")) ?? 0,
                        Long((string)extent.Attribute("cx")) ?? 0, Long((string)extent.Attribute("cy")) ?? 0)
                    : (group.Transform ?? default) with { Rotation = 0, FlipH = false, FlipV = false };
                group.ChildList.AddRange(ReadChildren(e, group));
                break;
        }

        return shape;
    }

    private void ReadAutoShape(AutoShape shape, XElement e)
    {
        var spPr = e.Element(P + "spPr");
        var style = e.Element(P + "style");
        var preset = spPr?.Element(A + "prstGeom");
        shape.Geometry = (string)preset?.Attribute("prst") ?? (spPr?.Element(A + "custGeom") != null ? "custom" : "rect");
        shape.Adjustments = preset?.Element(A + "avLst")?.Elements(A + "gd")
            .Select(gd => (Name: (string)gd.Attribute("name"), Value: Long(((string)gd.Attribute("fmla"))?.Replace("val ", "", StringComparison.Ordinal))))
            .Where(gd => gd.Name != null && gd.Value != null)
            .GroupBy(gd => gd.Name).ToDictionary(g => g.Key, g => g.First().Value.Value) ?? new Dictionary<string, long>();
        var fill = FillOf(spPr);
        shape.Fill = fill == null
            ? new ShapeFill(ShapeFillKind.Inherited, StyleColor(style, "fillRef"))
            : new ShapeFill(fill.Name.LocalName switch
            {
                "noFill" => ShapeFillKind.None,
                "solidFill" => ShapeFillKind.Solid,
                _ => ShapeFillKind.Other
            }, FillColor(fill));
        if (spPr?.Element(A + "ln") is { } ln)
        {
            var lineFill = FillOf(ln);
            shape.Line = new ShapeLine(lineFill == null ? StyleColor(style, "lnRef") : FillColor(lineFill), Long((string)ln.Attribute("w")) ?? 12700,
                (string)ln.Element(A + "prstDash")?.Attribute("val") ?? "solid");
        }

        shape.IsTextBox = (string)e.Element(P + "nvSpPr")?.Element(P + "cNvSpPr")?.Attribute("txBox") is "1" or "true";
        if (e.Element(P + "txBody") is { } body) shape.Text = ReadText(body);
    }

    private static TextBody ReadText(XElement body)
    {
        var properties = body.Element(A + "bodyPr");
        string Body(string name) => (string)properties?.Attribute(name);
        return new TextBody
        {
            Source = body,
            Anchor = TextStyles.ParseAnchor(Body("anchor")),
            InsetLeft = Long(Body("lIns")),
            InsetTop = Long(Body("tIns")),
            InsetRight = Long(Body("rIns")),
            InsetBottom = Long(Body("bIns")),
            Wrap = Body("wrap") switch { "none" => false, "square" => true, _ => null },
            Paragraphs = body.Elements(A + "p").Select(ReadParagraph).ToList()
        };
    }

    private static TextParagraph ReadParagraph(XElement p)
    {
        var pPr = p.Element(A + "pPr");
        return new TextParagraph
        {
            Source = p,
            Level = (int)Math.Clamp(Long((string)pPr?.Attribute("lvl")) ?? 0, 0, 8),
            Align = TextStyles.ParseAlign((string)pPr?.Attribute("algn")),
            Bullet = pPr?.Elements().FirstOrDefault(b => b.Name.LocalName is "buNone" or "buChar" or "buAutoNum" or "buBlip")?.Name.LocalName switch
            {
                null => null,
                "buNone" => false,
                _ => true
            },
            Runs = p.Elements().Where(n => RunText(n) != null).Select(n => new TextRun(RunText(n), ReadRunFormat(n.Element(A + "rPr"))) { Source = n }).ToList(),
            EndFormat = p.Element(A + "endParaRPr") is { } end ? ReadRunFormat(end) : null
        };
    }

    private SlideShape FindPlaceholder(PlaceholderRef placeholder)
    {
        var candidates = inherited.Where(s => s.Placeholder != null).ToList();
        return (matchIndex && placeholder.Index != null ? candidates.Find(s => s.Placeholder.Index == placeholder.Index) : null)
               ?? candidates.Find(s => s.Placeholder.Type == placeholder.Type)
               ?? candidates.Find(s => Category(s.Placeholder.Type) == Category(placeholder.Type));
    }

    private static string Category(string type) => type switch
    {
        "ctrTitle" => "title",
        "subTitle" or "obj" or "body" => "body",
        _ => type
    };

    private static ShapeTransform? ReadTransform(XElement xfrm)
    {
        var offset = xfrm?.Element(A + "off");
        var extent = xfrm?.Element(A + "ext");
        if (offset == null || extent == null) return null;
        return new ShapeTransform(Long((string)offset.Attribute("x")) ?? 0, Long((string)offset.Attribute("y")) ?? 0,
            Long((string)extent.Attribute("cx")) ?? 0, Long((string)extent.Attribute("cy")) ?? 0,
            (Long((string)xfrm.Attribute("rot")) ?? 0) / 60000.0, (string)xfrm.Attribute("flipH") is "1" or "true", (string)xfrm.Attribute("flipV") is "1" or "true");
    }

    private static bool IsMedia(XElement picture) =>
        picture.Element(P + "nvPicPr")?.Element(P + "nvPr")?.Descendants().Any(d => MediaElements.Contains(d.Name.LocalName)) == true;

    private static string FrameKind(XElement frame) =>
        ((string)frame.Element(A + "graphic")?.Element(A + "graphicData")?.Attribute("uri"))?.TrimEnd('/') switch
        {
            { } uri when uri.EndsWith("/chart", StringComparison.Ordinal) || uri.EndsWith("/chartex", StringComparison.Ordinal) => "chart",
            { } uri when uri.EndsWith("/table", StringComparison.Ordinal) => "table",
            { } uri when uri.EndsWith("/diagram", StringComparison.Ordinal) => "smartArt",
            { } uri when uri.EndsWith("/ole", StringComparison.Ordinal) => "ole",
            _ => "other"
        };

    private static XElement FillOf(XElement properties) =>
        properties?.Elements().FirstOrDefault(f => f.Name.LocalName is "noFill" or "solidFill" or "gradFill" or "pattFill" or "blipFill" or "grpFill");

    private static string FillColor(XElement fill) => ColorResolver.FromXml(fill.Name.LocalName switch
    {
        "solidFill" => fill.Elements().FirstOrDefault(),
        "gradFill" => fill.Element(A + "gsLst")?.Elements(A + "gs").FirstOrDefault()?.Elements().FirstOrDefault(),
        "pattFill" => fill.Element(A + "fgClr")?.Elements().FirstOrDefault(),
        _ => null
    });

    private static string StyleColor(XElement style, string reference) =>
        style?.Element(A + reference) is { } r && (string)r.Attribute("idx") != "0" ? ColorResolver.FromXml(r.Elements().FirstOrDefault()) : null;
}
