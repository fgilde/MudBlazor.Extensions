using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Document.FormatXml;

namespace MudBlazor.Extensions.Office.Document;

internal static partial class DocxReader
{
    private static readonly XNamespace Cp = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";
    private static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";
    private static readonly XNamespace DcTerms = "http://purl.org/dc/terms/";

    private static Dictionary<string, string> ReadHeadersFooters(OpcPackage opc, string main, IEnumerable<OpcRelationship> rels, TextDocument document)
    {
        var ids = new Dictionary<string, string>();
        var byPart = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rel in rels.Where(r => !IsExternal(r)))
        {
            HeaderFooterKind kind;
            if (rel.Type.EndsWith("/header", StringComparison.Ordinal)) kind = HeaderFooterKind.Header;
            else if (rel.Type.EndsWith("/footer", StringComparison.Ordinal)) kind = HeaderFooterKind.Footer;
            else continue;
            var part = opc.ResolveTarget(main, rel.Target);
            if (byPart.TryGetValue(part, out var shared))
                ids[rel.Id] = shared;
            else if (Optional(() => HeaderFooter(opc, part, rel.Id, kind, document)) is { } headerFooter)
            {
                document.HeaderFooterList.Add(headerFooter);
                ids[rel.Id] = byPart[part] = headerFooter.Id;
            }
        }

        return ids;
    }

    private static HeaderFooter HeaderFooter(OpcPackage opc, string part, string id, HeaderFooterKind kind, TextDocument document)
    {
        if (opc.Entry(part) == null || opc.ReadXml(part).Root is not { } root || root.Name.Namespace != W) return null;
        var blocks = Blocks(root.Elements(), new InlineXml(opc, part, Relationships(opc, part), document));
        return new HeaderFooter { Id = id, Kind = kind, PartName = part, Blocks = blocks.Select(document.Identify).ToList() };
    }

    private static void ReadComments(OpcPackage opc, string main, IEnumerable<OpcRelationship> rels, XDocument mainXml, TextDocument document)
    {
        if (PartName(opc, main, rels, "comments") is not { } part || Optional(() => opc.ReadXml(part))?.Root is not { } root) return;
        var paragraphIds = new Dictionary<XElement, string>();
        foreach (var paragraph in document.AllParagraphs())
            if (paragraph.Source != null) paragraphIds.TryAdd(paragraph.Source, paragraph.Id);
        var anchors = new Dictionary<string, string>();
        foreach (var marker in mainXml.Descendants(W + "commentRangeStart").Concat(mainXml.Descendants(W + "commentReference")))
            if ((string)marker.Attribute(W + "id") is { } markerId && !anchors.ContainsKey(markerId) &&
                marker.Ancestors(W + "p").Select(p => paragraphIds.GetValueOrDefault(p)).FirstOrDefault(i => i != null) is { } paragraphId)
                anchors[markerId] = paragraphId;

        foreach (var comment in root.Elements(W + "comment"))
        {
            var id = (string)comment.Attribute(W + "id");
            var text = string.Join("\n", comment.Descendants(W + "p").Where(p => !p.Ancestors(W + "p").Any()).Select(InlineXml.Text));
            document.CommentList.Add(new Comment(id, (string)comment.Attribute(W + "author"), (string)comment.Attribute(W + "initials"),
                Date((string)comment.Attribute(W + "date")), text, id == null ? null : anchors.GetValueOrDefault(id)));
        }
    }

    private static DocumentProperties ReadProperties(OpcPackage opc, IEnumerable<OpcRelationship> packageRels)
    {
        var part = PartName(opc, "/", packageRels, "core-properties");
        var root = part == null ? null : Optional(() => opc.ReadXml(part))?.Root;
        string Text(XName name) => (string)root?.Element(name);
        return new DocumentProperties(Text(Dc + "title"), Text(Dc + "subject"), Text(Dc + "creator"), Text(Cp + "keywords"), Text(Dc + "description"),
            Text(Cp + "lastModifiedBy"), Date(Text(DcTerms + "created")), Date(Text(DcTerms + "modified")));
    }

    private static DateTimeOffset? Date(string value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date) ? date : null;

    private static T Optional<T>(Func<T> read) where T : class
    {
        try
        {
            return read();
        }
        catch (Exception e) when (e is XmlException || e is InvalidDataException && !LoadLimit.IsExceeded(e))
        {
            return null;
        }
    }
}
