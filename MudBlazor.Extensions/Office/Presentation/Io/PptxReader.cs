using System.Globalization;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Presentation;

internal static class PptxReader
{
    internal static readonly XNamespace P = "http://schemas.openxmlformats.org/presentationml/2006/main";
    internal static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    internal static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    internal static Presentation Read(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var opc = OpcPackage.Open(buffer.ToArray());
        var main = opc.Relationships("/").FirstOrDefault(r => r.Type.EndsWith("/officeDocument", StringComparison.Ordinal)) is { } officeDocument
            ? opc.ResolveTarget("/", officeDocument.Target)
            : "/ppt/presentation.xml";
        if (opc.Entry(main) == null) throw new InvalidDataException("The file contains no presentation part.");

        var xml = opc.ReadXml(main);
        var root = xml.Root!;
        if (root.Name.Namespace != P) throw new InvalidDataException("Strict Open XML presentations are not supported.");
        var rels = opc.Relationships(main);
        var presentation = new Presentation();
        var size = root.Element(P + "sldSz");
        presentation.SlideWidth = Long((string)size?.Attribute("cx")) ?? presentation.SlideWidth;
        presentation.SlideHeight = Long((string)size?.Attribute("cy")) ?? presentation.SlideHeight;

        foreach (var id in Ids(root, "sldMasterIdLst"))
            if (Target(opc, main, rels, id) is { } part)
            {
                var master = ReadMaster(opc, part, root.Element(P + "defaultTextStyle"));
                presentation.MasterList.Add(master);
                presentation.LayoutList.AddRange(master.LayoutList);
            }

        var package = new PptxPackage
        {
            Opc = opc, PresentationPart = main, PresentationXml = xml,
            NotesMasterPart = Ids(root, "notesMasterIdLst").Select(id => Target(opc, main, rels, id)).FirstOrDefault(p => p != null)
        };
        foreach (var sldId in root.Element(P + "sldIdLst")?.Elements(P + "sldId") ?? [])
        {
            var relationshipId = (string)sldId.Attribute(R + "id");
            if (Target(opc, main, rels, relationshipId) is not { } part) continue;
            var slidePart = ReadSlide(opc, part, relationshipId, (uint?)Long((string)sldId.Attribute("id")) ?? 0, presentation.LayoutList);
            package.Slides.Add(slidePart);
            presentation.SlideList.Add(slidePart.Slide);
        }

        presentation.Package = package;
        return presentation;
    }

    internal static RunFormat ReadRunFormat(XElement properties)
    {
        if (properties == null) return new RunFormat();
        return new RunFormat(
            Flag(properties, "b"),
            Flag(properties, "i"),
            (string)properties.Attribute("u") is { } underline ? underline != "none" : null,
            (string)properties.Attribute("strike") is { } strike ? strike != "noStrike" : null,
            Long((string)properties.Attribute("sz")) is { } size ? size / 100.0 : null,
            ColorResolver.FromXml(properties.Element(A + "solidFill")?.Elements().FirstOrDefault()),
            (string)properties.Element(A + "latin")?.Attribute("typeface"));
    }

    private static SlideMaster ReadMaster(OpcPackage opc, string part, XElement defaultTextStyle)
    {
        var root = opc.ReadXml(part).Root!;
        var rels = opc.Relationships(part);
        var master = new SlideMaster
        {
            PartName = part,
            Name = (string)root.Element(P + "cSld")?.Attribute("name"),
            ColorMap = root.Element(P + "clrMap")?.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value) ?? new Dictionary<string, string>(),
            TextStyles = root.Element(P + "txStyles"),
            DefaultTextStyle = defaultTextStyle
        };
        if (rels.FirstOrDefault(r => r.Type.EndsWith("/theme", StringComparison.Ordinal) && r.TargetMode == null) is { } themeRel
            && opc.ResolveTarget(part, themeRel.Target) is var themePart && opc.Entry(themePart) != null)
        {
            var theme = opc.ReadXml(themePart);
            master.ThemePart = themePart;
            master.FormatScheme = theme.Descendants(A + "fmtScheme").FirstOrDefault();
            master.Theme = ThemeColors.Read(theme);
            master.MajorFont = (string)theme.Descendants(A + "majorFont").FirstOrDefault()?.Element(A + "latin")?.Attribute("typeface");
            master.MinorFont = (string)theme.Descendants(A + "minorFont").FirstOrDefault()?.Element(A + "latin")?.Attribute("typeface");
        }

        master.Background = ReadBackground(opc, part, rels, root, master);
        master.ShapeList.AddRange(new ShapeReader(opc, part, rels, master, [], false).ReadTree(root));
        foreach (var id in Ids(root, "sldLayoutIdLst"))
            if (Target(opc, part, rels, id) is { } layoutPart)
            {
                var layoutRoot = opc.ReadXml(layoutPart).Root!;
                var layoutRels = opc.Relationships(layoutPart);
                var layout = new SlideLayout
                {
                    PartName = layoutPart, Master = master, Name = (string)layoutRoot.Element(P + "cSld")?.Attribute("name"), Type = (string)layoutRoot.Attribute("type"),
                    Background = ReadBackground(opc, layoutPart, layoutRels, layoutRoot, master), ShowMasterShapes = ShowMasterShapes(layoutRoot)
                };
                layout.ShapeList.AddRange(new ShapeReader(opc, layoutPart, layoutRels, master, master.ShapeList, false).ReadTree(layoutRoot));
                master.LayoutList.Add(layout);
            }

        return master;
    }

    private static PptxSlidePart ReadSlide(OpcPackage opc, string part, string relationshipId, uint slideId, List<SlideLayout> layouts)
    {
        var xml = opc.ReadXml(part);
        var rels = opc.Relationships(part);
        var layoutPart = Related(opc, part, rels, "/slideLayout");
        var slide = new Slide
        {
            Layout = layouts.Find(l => string.Equals(l.PartName, layoutPart, StringComparison.OrdinalIgnoreCase)),
            Hidden = (string)xml.Root!.Attribute("show") is "0" or "false",
            ReservedId = SlideTiming.MaxTarget(xml.Root)
        };
        var master = slide.Layout?.Master;
        slide.Background = ReadBackground(opc, part, rels, xml.Root, master);
        slide.ShowMasterShapes = ShowMasterShapes(xml.Root);
        slide.ShapeList.AddRange(new ShapeReader(opc, part, rels, master, (IReadOnlyList<SlideShape>)slide.Layout?.ShapeList ?? [], true).ReadTree(xml.Root));

        var notesPart = Related(opc, part, rels, "/notesSlide");
        var notesXml = notesPart == null ? null : opc.ReadXml(notesPart);
        slide.HasNotesPart = notesXml != null;
        slide.Notes = notesXml == null ? "" : NotesText(notesXml);
        return new PptxSlidePart
        {
            Slide = slide, PartName = part, RelationshipId = relationshipId, SlideId = slideId, Xml = xml, Relationships = rels, NotesPart = notesPart, NotesXml = notesXml
        };
    }

    private static SlideBackground ReadBackground(OpcPackage opc, string part, IReadOnlyList<OpcRelationship> rels, XElement root, SlideMaster master)
    {
        var background = root.Element(P + "cSld")?.Element(P + "bg");
        if (background?.Element(P + "bgPr") is { } properties) return Background(opc, part, rels, ShapeReader.FillOf(properties));
        if (background?.Element(P + "bgRef") is not { } reference) return null;
        var index = Long((string)reference.Attribute("idx")) ?? 0;
        if (index is 0 or 1000) return new SlideBackground(ShapeFillKind.None, null, null);
        var color = reference.Elements().FirstOrDefault();
        var style = master?.FormatScheme?.Element(A + (index > 1000 ? "bgFillStyleLst" : "fillStyleLst"))?.Elements().ElementAtOrDefault((int)(index % 1000) - 1);
        if (style == null) return new SlideBackground(ShapeFillKind.Solid, ColorResolver.FromXml(color), null);
        style = new XElement(style);
        if (color != null)
            foreach (var placeholder in style.Descendants(A + "schemeClr").Where(c => (string)c.Attribute("val") == "phClr").ToList())
                placeholder.ReplaceWith(new XElement(color.Name, color.Attributes(), color.Elements(), placeholder.Elements()));
        return Background(opc, master.ThemePart, opc.Relationships(master.ThemePart), style);
    }

    private static SlideBackground Background(OpcPackage opc, string part, IReadOnlyList<OpcRelationship> rels, XElement fill) => fill?.Name.LocalName switch
    {
        null => null,
        "noFill" => new SlideBackground(ShapeFillKind.None, null, null),
        "solidFill" => new SlideBackground(ShapeFillKind.Solid, ShapeReader.FillColor(fill), null),
        "blipFill" => new SlideBackground(ShapeFillKind.Picture, null, EmbeddedPart(opc, part, rels, fill)),
        _ => new SlideBackground(ShapeFillKind.Other, ShapeReader.FillColor(fill), null)
    };

    internal static string EmbeddedPart(OpcPackage opc, string part, IReadOnlyList<OpcRelationship> rels, XElement blipFill) =>
        (string)blipFill?.Element(A + "blip")?.Attribute(R + "embed") is { } embed && rels.FirstOrDefault(r => r.Id == embed && r.TargetMode == null) is { } rel
            ? opc.ResolveTarget(part, rel.Target)
            : null;

    private static bool ShowMasterShapes(XElement root) => (string)root.Attribute("showMasterSp") is not ("0" or "false");

    internal static XElement NotesBody(XDocument notes) =>
        notes.Descendants(P + "sp").FirstOrDefault(sp => (string)sp.Descendants(P + "ph").FirstOrDefault()?.Attribute("type") == "body");

    internal static string NotesText(XDocument notes) =>
        string.Join("\n", NotesBody(notes)?.Element(P + "txBody")?.Elements(A + "p").Select(p => string.Concat(p.Elements().Select(RunText))) ?? []);

    internal static string RunText(XElement node) =>
        node.Name == A + "br" ? "\n" : node.Name == A + "r" || node.Name == A + "fld" ? OoxmlText.Decode((string)node.Element(A + "t") ?? "") : null;

    private static IEnumerable<string> Ids(XElement root, string list) =>
        root.Element(P + list)?.Elements().Select(e => (string)e.Attribute(R + "id")).Where(id => id != null) ?? [];

    private static string Target(OpcPackage opc, string source, IReadOnlyList<OpcRelationship> rels, string id) =>
        rels.FirstOrDefault(r => r.Id == id && r.TargetMode == null) is { } rel && opc.ResolveTarget(source, rel.Target) is var part && opc.Entry(part) != null ? part : null;

    private static string Related(OpcPackage opc, string source, IReadOnlyList<OpcRelationship> rels, string typeSuffix) =>
        rels.Where(r => r.Type.EndsWith(typeSuffix, StringComparison.Ordinal)).Select(r => Target(opc, source, rels, r.Id)).FirstOrDefault(p => p != null);

    internal static long? Long(string value) => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static bool? Flag(XElement element, string name) => (string)element.Attribute(name) switch
    {
        "1" or "true" => true,
        "0" or "false" => false,
        _ => null
    };
}
