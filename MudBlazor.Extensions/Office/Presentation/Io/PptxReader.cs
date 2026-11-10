using System.Globalization;
using System.Xml.Linq;
using MudBlazor.Extensions.Office.Spreadsheet;

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
            master.Theme = ThemeColors.Read(theme);
            master.MajorFont = (string)theme.Descendants(A + "majorFont").FirstOrDefault()?.Element(A + "latin")?.Attribute("typeface");
            master.MinorFont = (string)theme.Descendants(A + "minorFont").FirstOrDefault()?.Element(A + "latin")?.Attribute("typeface");
        }

        master.ShapeList.AddRange(new ShapeReader(opc, part, rels, master, [], false).ReadTree(root));
        foreach (var id in Ids(root, "sldLayoutIdLst"))
            if (Target(opc, part, rels, id) is { } layoutPart)
            {
                var layoutRoot = opc.ReadXml(layoutPart).Root!;
                var layout = new SlideLayout
                {
                    PartName = layoutPart, Master = master, Name = (string)layoutRoot.Element(P + "cSld")?.Attribute("name"), Type = (string)layoutRoot.Attribute("type")
                };
                layout.ShapeList.AddRange(new ShapeReader(opc, layoutPart, opc.Relationships(layoutPart), master, master.ShapeList, false).ReadTree(layoutRoot));
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
            Hidden = (string)xml.Root!.Attribute("show") is "0" or "false"
        };
        var master = slide.Layout?.Master;
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

    private static string NotesText(XDocument notes)
    {
        var body = notes.Descendants(P + "sp").FirstOrDefault(sp => (string)sp.Descendants(P + "ph").FirstOrDefault()?.Attribute("type") == "body");
        return string.Join("\n", body?.Element(P + "txBody")?.Elements(A + "p").Select(p => string.Concat(p.Elements().Select(RunText))) ?? []);
    }

    internal static string RunText(XElement node) =>
        node.Name == A + "br" ? "\n" : node.Name == A + "r" || node.Name == A + "fld" ? (string)node.Element(A + "t") ?? "" : null;

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
