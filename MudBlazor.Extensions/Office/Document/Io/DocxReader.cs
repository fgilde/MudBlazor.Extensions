using System.Xml;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Document.FormatXml;

namespace MudBlazor.Extensions.Office.Document;

internal static partial class DocxReader
{
    private static readonly HashSet<string> TrackedChanges =
    [
        "ins", "del", "moveFrom", "moveTo", "rPrChange", "pPrChange", "sectPrChange", "tblPrChange", "tblPrExChange", "trPrChange", "tcPrChange",
        "tblGridChange", "numberingChange", "cellIns", "cellDel", "cellMerge", "customXmlInsRangeStart", "customXmlDelRangeStart",
        "customXmlMoveFromRangeStart", "customXmlMoveToRangeStart"
    ];

    internal static TextDocument Read(byte[] package)
    {
        try
        {
            return ReadPackage(package);
        }
        catch (XmlException e)
        {
            throw new InvalidDataException("The file contains malformed XML.", e);
        }
    }

    internal static bool IsExternal(OpcRelationship relationship) => relationship.TargetMode == "External";

    private static TextDocument ReadPackage(byte[] package)
    {
        var opc = OpcPackage.Open(package);
        var packageRels = Relationships(opc, "/");
        var main = packageRels.FirstOrDefault(r => r.Type.EndsWith("/officeDocument", StringComparison.Ordinal) && !IsExternal(r)) is { } officeDocument
            ? opc.ResolveTarget("/", officeDocument.Target)
            : null;
        if (main == null || opc.Entry(main) == null) throw new InvalidDataException("The file contains no document part.");
        var xml = opc.ReadXml(main);
        if (xml.Root?.Name != W + "document" || xml.Root.Element(W + "body") is not { } body)
            throw new InvalidDataException("The file contains no WordprocessingML document; Strict Open XML documents are not supported.");

        var rels = Relationships(opc, main);
        var (stylesPart, styles) = Part(opc, main, rels, "styles");
        var (numberingPart, numbering) = Part(opc, main, rels, "numbering");
        var (settingsPart, settings) = Part(opc, main, rels, "settings");
        var (themePart, theme) = Part(opc, main, rels, "theme");
        var contentType = opc.ContentType(main);
        var document = new TextDocument
        {
            IsTemplate = contentType?.Contains(".template.main+xml", StringComparison.Ordinal) == true,
            EvenAndOddHeaders = settings?.Root is { } root && OnOff(root, "evenAndOddHeaders") == true,
            Styles = StylesXml.Read(styles),
            Numbering = NumberingXml.Read(numbering),
            Theme = DocumentTheme.Read(theme),
            Properties = ReadProperties(opc, packageRels)
        };

        var headerIds = ReadHeadersFooters(opc, main, rels, document);
        ReadBody(body, new InlineXml(opc, main, rels, document), headerIds, document);
        ReadComments(opc, main, rels, xml, document);
        document.HasTrackedChanges = HasTrackedChanges(body) || document.HeadersFooters.SelectMany(h => h.Blocks).Any(b => HasTrackedChanges(b.Source));

        document.Package = new DocxPackage
        {
            Opc = opc, MainPart = main, MainContentType = contentType, MainXml = xml, MainRelationships = rels,
            StylesPart = stylesPart, StylesXml = styles, NumberingPart = numberingPart, NumberingXml = numbering,
            SettingsPart = settingsPart, SettingsXml = settings, ThemePart = themePart, Blocks = document.BlockList.ToList(),
            Sections = document.SectionList.ToList(), HeadersFooters = document.HeaderFooterList.ToList(), HeaderIds = headerIds, Properties = document.Properties
        };
        return document;
    }

    private static void ReadBody(XElement body, InlineXml inlines, IReadOnlyDictionary<string, string> headerIds, TextDocument document)
    {
        XElement final = null;
        foreach (var element in body.Elements())
            if (element.Name == W + "sectPr")
                final = element;
            else
            {
                var block = document.Identify(Block(element, inlines));
                document.BlockList.Add(block);
                if (element.Name == W + "p" && element.Element(W + "pPr")?.Element(W + "sectPr") is { } sectPr)
                    document.SectionList.Add(SectionXml.Read(sectPr, SectionId(document), block.Id, headerIds));
            }

        document.SectionList.Add(final == null ? new Section { Id = SectionId(document) } : SectionXml.Read(final, SectionId(document), null, headerIds));
    }

    private static string SectionId(TextDocument document) => "s" + (document.SectionList.Count + 1);

    private static List<Block> Blocks(IEnumerable<XElement> elements, InlineXml inlines) =>
        elements.Where(e => e.Name != W + "sectPr").Select(e => Block(e, inlines)).ToList();

    private static Block Block(XElement element, InlineXml inlines)
    {
        if (element.Name == W + "p") return ParagraphXml.Read(element, inlines);
        if (element.Name == W + "tbl") return TableXml.Read(element, e => Blocks(e, inlines));
        var name = element.Name.Namespace == W ? element.Name.LocalName : null;
        if (name != null && InlineXml.Markers.Contains(name)) return new OpaqueBlock { Kind = "marker", Visible = false, Source = element };
        if (name == "sdt")
            return new OpaqueBlock { Kind = "contentControl", Preview = Blocks(element.Element(W + "sdtContent")?.Elements() ?? [], inlines), Source = element };
        return new OpaqueBlock { Kind = name is "customXml" or "altChunk" ? name : "other", Source = element };
    }

    private static bool HasTrackedChanges(XElement element) =>
        element != null && element.DescendantsAndSelf().Any(e => e.Name.Namespace == W && TrackedChanges.Contains(e.Name.LocalName));

    private static (string Part, XDocument Xml) Part(OpcPackage opc, string main, IEnumerable<OpcRelationship> rels, string type) =>
        PartName(opc, main, rels, type) is { } part ? (part, opc.ReadXml(part)) : (null, null);

    private static string PartName(OpcPackage opc, string source, IEnumerable<OpcRelationship> rels, string type)
    {
        if (rels.FirstOrDefault(r => r.Type.EndsWith("/" + type, StringComparison.Ordinal) && !IsExternal(r)) is not { } rel) return null;
        var part = opc.ResolveTarget(source, rel.Target);
        return opc.Entry(part) == null ? null : part;
    }

    private static List<OpcRelationship> Relationships(OpcPackage opc, string part) =>
        opc.Relationships(part).Where(r => r.Id != null && r.Type != null && r.Target != null).ToList();
}
