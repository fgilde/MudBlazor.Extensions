using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Document.FormatXml;

namespace MudBlazor.Extensions.Office.Document;

internal static class DocxReader
{
    private static readonly HashSet<string> TrackedChanges =
    [
        "ins", "del", "moveFrom", "moveTo", "rPrChange", "pPrChange", "sectPrChange", "tblPrChange", "tblPrExChange", "trPrChange", "tcPrChange",
        "tblGridChange", "numberingChange", "cellIns", "cellDel", "cellMerge", "customXmlInsRangeStart", "customXmlDelRangeStart",
        "customXmlMoveFromRangeStart", "customXmlMoveToRangeStart"
    ];

    private static readonly HashSet<string> BodyMarkers =
    [
        "bookmarkStart", "bookmarkEnd", "proofErr", "commentRangeStart", "commentRangeEnd", "permStart", "permEnd",
        "moveFromRangeStart", "moveFromRangeEnd", "moveToRangeStart", "moveToRangeEnd"
    ];

    internal static TextDocument Read(byte[] package)
    {
        var opc = OpcPackage.Open(package);
        var main = opc.Relationships("/").FirstOrDefault(r => r.Type.EndsWith("/officeDocument", StringComparison.Ordinal) && r.TargetMode == null) is { } officeDocument
            ? opc.ResolveTarget("/", officeDocument.Target)
            : null;
        if (main == null || opc.Entry(main) == null) throw new InvalidDataException("The file contains no document part.");
        var xml = opc.ReadXml(main);
        if (xml.Root?.Name != W + "document" || xml.Root.Element(W + "body") is not { } body)
            throw new InvalidDataException("The file contains no WordprocessingML document; Strict Open XML documents are not supported.");

        var rels = opc.Relationships(main);
        var (stylesPart, styles) = Part(opc, main, rels, "styles");
        var (numberingPart, numbering) = Part(opc, main, rels, "numbering");
        var (settingsPart, settings) = Part(opc, main, rels, "settings");
        var (themePart, theme) = Part(opc, main, rels, "theme");
        var contentType = opc.ContentType(main);
        var document = new TextDocument
        {
            IsTemplate = contentType?.Contains(".template.main+xml", StringComparison.Ordinal) == true,
            EvenAndOddHeaders = settings?.Root is { } root && OnOff(root, "evenAndOddHeaders") == true,
            HasTrackedChanges = body.Descendants().Any(e => e.Name.Namespace == W && TrackedChanges.Contains(e.Name.LocalName)),
            Styles = StylesXml.Read(styles),
            Numbering = NumberingXml.Read(numbering),
            Theme = DocumentTheme.Read(theme)
        };

        var inlines = new InlineXml(opc, main, rels, document);
        foreach (var element in body.Elements())
            if (element.Name == W + "sectPr")
                document.SectionList.Add(new Section { Id = "s1", Page = Page(element), Source = element });
            else
                document.BlockList.Add(document.Identify(Block(element, inlines)));
        if (document.SectionList.Count == 0) document.SectionList.Add(new Section { Id = "s1" });

        document.Package = new DocxPackage
        {
            Opc = opc, MainPart = main, MainContentType = contentType, MainXml = xml, MainRelationships = rels,
            StylesPart = stylesPart, StylesXml = styles, NumberingPart = numberingPart, NumberingXml = numbering,
            SettingsPart = settingsPart, SettingsXml = settings, ThemePart = themePart, Blocks = document.BlockList.ToList()
        };
        return document;
    }

    private static Block Block(XElement element, InlineXml inlines)
    {
        if (element.Name == W + "p") return ParagraphXml.Read(element, inlines);
        var name = element.Name.Namespace == W ? element.Name.LocalName : null;
        return name != null && BodyMarkers.Contains(name)
            ? new OpaqueBlock { Kind = "marker", Visible = false, Source = element }
            : new OpaqueBlock
            {
                Kind = name switch { "sdt" => "contentControl", "customXml" => "customXml", "altChunk" => "altChunk", _ => "other" },
                Source = element
            };
    }

    private static (string Part, XDocument Xml) Part(OpcPackage opc, string main, IEnumerable<OpcRelationship> rels, string type)
    {
        if (rels.FirstOrDefault(r => r.Type.EndsWith("/" + type, StringComparison.Ordinal) && r.TargetMode == null) is not { } rel) return (null, null);
        var part = opc.ResolveTarget(main, rel.Target);
        return opc.Entry(part) == null ? (null, null) : (part, opc.ReadXml(part));
    }

    private static PageSetup Page(XElement sectPr)
    {
        var size = sectPr.Element(W + "pgSz");
        var margin = sectPr.Element(W + "pgMar");
        var a4 = PageSetup.A4;
        return new PageSetup(
            Int(size, "w") ?? a4.Width, Int(size, "h") ?? a4.Height,
            (string)size?.Attribute(W + "orient") == "landscape" ? PageOrientation.Landscape : PageOrientation.Portrait,
            Int(margin, "top") ?? a4.MarginTop, Int(margin, "right") ?? a4.MarginRight, Int(margin, "bottom") ?? a4.MarginBottom, Int(margin, "left") ?? a4.MarginLeft,
            Int(margin, "header") ?? a4.HeaderDistance, Int(margin, "footer") ?? a4.FooterDistance, Int(margin, "gutter") ?? a4.Gutter);
    }
}
