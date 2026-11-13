using System.Xml;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Presentation.PptxReader;

namespace MudBlazor.Extensions.Office.Presentation;

internal sealed class ShapeReader(
    OpcPackage opc, string part, IReadOnlyList<OpcRelationship> rels, SlideMaster master, IReadOnlyList<SlideShape> inherited, bool matchIndex,
    IReadOnlyDictionary<string, string> colorMap = null)
{
    private static readonly XNamespace ChartNs = "http://schemas.openxmlformats.org/drawingml/2006/chart";
    private static readonly string[] MediaElements = ["videoFile", "audioFile", "quickTimeFile", "wavAudioFile", "audioCd", "media"];

    internal IEnumerable<SlideShape> ReadTree(XElement root) => ReadChildren(root?.Element(P + "cSld")?.Element(P + "spTree"), null);

    private IEnumerable<SlideShape> ReadChildren(XElement container, GroupShape parent) =>
        container?.Elements().Select(e => ReadShape(e, parent)).Where(s => s != null).ToList() ?? [];

    private SlideShape ReadShape(XElement e, GroupShape parent)
    {
        SlideShape shape;
        if (e.Name == P + "sp" || e.Name == P + "cxnSp") shape = new AutoShape { IsConnector = e.Name == P + "cxnSp" };
        else if (e.Name == P + "pic") shape = IsMedia(e) ? new OpaqueShape { Kind = "media" } : new PictureShape();
        else if (e.Name == P + "grpSp") shape = new GroupShape();
        else if (e.Name == P + "graphicFrame") shape = TableXml.Of(e) is { } tbl && TableXml.Readable(tbl) ? TableXml.Read(tbl) : ReadFrame(e);
        else if (e.Name == P + "contentPart" || e.Name.LocalName == "AlternateContent") shape = new OpaqueShape();
        else return null;

        var properties = e.Descendants(P + "cNvPr").FirstOrDefault();
        shape.Id = (uint?)Long((string)properties?.Attribute("id")) ?? 0;
        shape.Name = (string)properties?.Attribute("name") ?? "";
        shape.Source = e;
        shape.Parent = parent;
        shape.Master = master;
        shape.ColorMap = colorMap;
        var xfrm = XfrmOf(e, shape is OpaqueShape);
        shape.Transform = ReadTransform(xfrm);
        if (e.Elements().FirstOrDefault()?.Element(P + "nvPr")?.Element(P + "ph") is { } ph)
        {
            shape.Placeholder = new PlaceholderRef((string)ph.Attribute("type") ?? "obj", (uint?)Long((string)ph.Attribute("idx")));
            shape.InheritsFrom = FindPlaceholder(shape.Placeholder);
            shape.HasCustomPrompt = (string)ph.Attribute("hasCustomPrompt") is "1" or "true";
        }

        switch (shape)
        {
            case AutoShape auto:
                ReadAutoShape(auto, e);
                if (auto.Fill.Kind == ShapeFillKind.Picture) auto.Fill = auto.Fill with { ImagePart = EmbeddedPart(opc, part, rels, FillOf(e.Element(P + "spPr"))) };
                break;
            case PictureShape picture:
                var blipFill = e.Element(P + "blipFill");
                picture.ImagePart = EmbeddedPart(opc, part, rels, blipFill);
                if (picture.ImagePart != null) picture.ContentType = opc.ContentType(picture.ImagePart);
                picture.Crop = ReadCrop(blipFill?.Element(A + "srcRect"));

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

    private static void ReadAutoShape(AutoShape shape, XElement e)
    {
        ReadStyle(shape, e);
        shape.IsTextBox = (string)e.Element(P + "nvSpPr")?.Element(P + "cNvSpPr")?.Attribute("txBox") is "1" or "true";
        if (e.Element(P + "txBody") is { } body) shape.Text = ReadText(body);
    }

    internal static void ReadStyle(AutoShape shape, XElement e)
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
        shape.Fill = fill == null ? new ShapeFill(ShapeFillKind.Inherited, StyleColor(style, "fillRef")) : ReadFill(fill);
        if (spPr?.Element(A + "ln") is { } ln)
        {
            var lineFill = FillOf(ln);
            shape.Line = new ShapeLine(lineFill == null ? StyleColor(style, "lnRef") : FillColor(lineFill), Long((string)ln.Attribute("w")) ?? 12700,
                (string)ln.Element(A + "prstDash")?.Attribute("val") ?? "solid") { ColorInherited = lineFill == null };
        }
    }

    internal static ShapeFill ReadFill(XElement fill) => fill.Name.LocalName switch
    {
        "noFill" => new ShapeFill(ShapeFillKind.None),
        "solidFill" => new ShapeFill(ShapeFillKind.Solid, FillColor(fill)),
        "blipFill" => new ShapeFill(ShapeFillKind.Picture),
        "gradFill" => ReadGradient(fill),
        _ => new ShapeFill(ShapeFillKind.Other, FillColor(fill))
    };

    private static ShapeFill ReadGradient(XElement fill)
    {
        var stops = fill.Element(A + "gsLst")?.Elements(A + "gs")
            .Select(gs => new GradientStop((Long((string)gs.Attribute("pos")) ?? 0) / 100000.0, ColorResolver.FromXml(gs.Elements().FirstOrDefault())))
            .Where(gs => gs.Color != null).OrderBy(gs => gs.Position).ToList() ?? [];
        return new ShapeFill(ShapeFillKind.Gradient, stops.FirstOrDefault()?.Color)
        {
            Stops = stops, Angle = fill.Element(A + "lin") is { } lin ? (Long((string)lin.Attribute("ang")) ?? 0) / 60000.0 : null
        };
    }

    private static ImageCrop ReadCrop(XElement srcRect)
    {
        if (srcRect == null) return null;
        double Edge(string name) => (Long((string)srcRect.Attribute(name)) ?? 0) / 100000.0;
        return new ImageCrop(Edge("l"), Edge("t"), Edge("r"), Edge("b"));
    }

    internal static TextBody ReadText(XElement body)
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
            Wrap = ParseWrap(Body("wrap")),
            FontScalePercent = Percent(properties?.Element(A + "normAutofit")?.Attribute("fontScale")),
            LineSpaceReductionPercent = Percent(properties?.Element(A + "normAutofit")?.Attribute("lnSpcReduction")),
            Paragraphs = body.Elements(A + "p").Select(ReadParagraph).ToList()
        };
    }

    private static TextParagraph ReadParagraph(XElement p) => ReadParagraphProperties(p.Element(A + "pPr")) with
    {
        Source = p,
        Runs = p.Elements().Where(n => RunText(n) != null)
            .Select(n => new TextRun(RunText(n), ReadRunFormat(n.Element(A + "rPr"))) { Source = n, FieldType = n.Name == A + "fld" ? (string)n.Attribute("type") : null })
            .ToList(),
        EndFormat = p.Element(A + "endParaRPr") is { } end ? ReadRunFormat(end) : null
    };

    internal static TextParagraph ReadParagraphProperties(XElement pPr)
    {
        var bullet = pPr?.Elements().FirstOrDefault(b => b.Name.LocalName is "buNone" or "buChar" or "buAutoNum" or "buBlip");
        return new TextParagraph
        {
            Level = ParseLevel(pPr),
            Align = TextStyles.ParseAlign((string)pPr?.Attribute("algn")),
            Bullet = ParseBullet(pPr),
            MarginLeft = Long((string)pPr?.Attribute("marL")),
            Indent = Long((string)pPr?.Attribute("indent")),
            LineSpacingPercent = Percent(pPr?.Element(A + "lnSpc")?.Element(A + "spcPct")?.Attribute("val")),
            SpaceBeforePt = Long((string)pPr?.Element(A + "spcBef")?.Element(A + "spcPts")?.Attribute("val")) / 100.0,
            SpaceAfterPt = Long((string)pPr?.Element(A + "spcAft")?.Element(A + "spcPts")?.Attribute("val")) / 100.0,
            BulletKind = bullet?.Name.LocalName switch
            {
                null => BulletKind.Inherited,
                "buNone" => BulletKind.None,
                "buAutoNum" => BulletKind.AutoNumber,
                _ => BulletKind.Char
            },
            BulletChar = (string)bullet?.Attribute("char"),
            AutoNumberType = (string)bullet?.Attribute("type"),
            AutoNumberStart = (int?)Long((string)bullet?.Attribute("startAt")),
            BulletFont = (string)pPr?.Element(A + "buFont")?.Attribute("typeface")
        };
    }

    private static double? Percent(XAttribute value) => Long((string)value) / 1000.0;

    internal static bool? ParseWrap(string value) => value switch
    {
        "none" => false,
        "square" => true,
        _ => null
    };

    internal static int ParseLevel(XElement pPr) => (int)Math.Clamp(Long((string)pPr?.Attribute("lvl")) ?? 0, 0, 8);

    internal static bool? ParseBullet(XElement pPr) =>
        pPr?.Elements().FirstOrDefault(b => b.Name.LocalName is "buNone" or "buChar" or "buAutoNum" or "buBlip")?.Name.LocalName switch
        {
            null => null,
            "buNone" => false,
            _ => true
        };

    internal static XElement XfrmOf(XElement e, bool opaque) =>
        e.Element(P + "spPr")?.Element(A + "xfrm") ?? e.Element(P + "grpSpPr")?.Element(A + "xfrm") ?? e.Element(P + "xfrm")
        ?? (opaque ? e.Descendants().FirstOrDefault(d => d.Name.LocalName == "xfrm") : null);

    private SlideShape FindPlaceholder(PlaceholderRef placeholder)
    {
        var candidates = inherited.Where(s => s.Placeholder != null).ToList();
        return (matchIndex && placeholder.Index != null ? candidates.Find(s => s.Placeholder.Index == placeholder.Index) : null)
               ?? candidates.Find(s => s.Placeholder.Type == placeholder.Type)
               ?? candidates.Find(s => Category(s.Placeholder.Type) == Category(placeholder.Type));
    }

    internal static string Category(string type) => type switch
    {
        "ctrTitle" => "title",
        "subTitle" or "obj" or "body" => "body",
        _ => type
    };

    internal static ShapeTransform? ReadTransform(XElement xfrm)
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

    private OpaqueShape ReadFrame(XElement e)
    {
        var shape = new OpaqueShape { Kind = FrameKind(e) };
        if (shape.Kind == "chart") shape.Chart = ReadChart(e);
        return shape;
    }

    private ChartSnapshot ReadChart(XElement e)
    {
        var id = (string)e.Element(A + "graphic")?.Element(A + "graphicData")?.Element(ChartNs + "chart")?.Attribute(R + "id");
        if (id == null || rels.FirstOrDefault(r => r.Id == id && r.TargetMode == null) is not { } rel) return null;
        try
        {
            return ChartSnapshotReader.Read(opc.ReadXml(opc.ResolveTarget(part, rel.Target)), c => ColorResolver.Resolve(c, master, colorMap));
        }
        catch (Exception ex) when (ex is XmlException or IOException or InvalidDataException or KeyNotFoundException)
        {
            return null;
        }
    }

    private static string FrameKind(XElement frame) =>
        ((string)frame.Element(A + "graphic")?.Element(A + "graphicData")?.Attribute("uri"))?.TrimEnd('/') switch
        {
            { } uri when uri.EndsWith("/chart", StringComparison.Ordinal) || uri.EndsWith("/chartex", StringComparison.Ordinal) => "chart",
            { } uri when uri.EndsWith("/table", StringComparison.Ordinal) => "table",
            { } uri when uri.EndsWith("/diagram", StringComparison.Ordinal) => "smartArt",
            { } uri when uri.EndsWith("/ole", StringComparison.Ordinal) => "ole",
            _ => "other"
        };

    internal static XElement FillOf(XElement properties) =>
        properties?.Elements().FirstOrDefault(f => f.Name.LocalName is "noFill" or "solidFill" or "gradFill" or "pattFill" or "blipFill" or "grpFill");

    internal static string FillColor(XElement fill) => ColorResolver.FromXml(fill.Name.LocalName switch
    {
        "solidFill" => fill.Elements().FirstOrDefault(),
        "gradFill" => fill.Element(A + "gsLst")?.Elements(A + "gs").FirstOrDefault()?.Elements().FirstOrDefault(),
        "pattFill" => fill.Element(A + "fgClr")?.Elements().FirstOrDefault(),
        _ => null
    });

    internal static string StyleColor(XElement style, string reference) =>
        style?.Element(A + reference) is { } r && (string)r.Attribute("idx") != "0" ? ColorResolver.FromXml(r.Elements().FirstOrDefault()) : null;
}
