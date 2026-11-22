using System.Globalization;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Document;

public static partial class DocxWriter
{
    private const string CoreType = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties";
    private static readonly XNamespace Cp = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";
    private static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";
    private static readonly XNamespace DcTerms = "http://purl.org/dc/terms/";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    private sealed partial class Job
    {
        private void WriteProperties()
        {
            var properties = document.Properties;
            if (properties == package.Properties) return;
            var rels = Relationships("/");
            var rel = rels.FirstOrDefault(r => r.Type == CoreType && r.TargetMode == null);
            var part = rel == null ? "/docProps/core.xml" : _opc.ResolveTarget("/", rel.Target);
            if (rel == null && _opc.Entry(part) != null) part = writer.NextPartName("/docProps/core{0}.xml");
            var xml = _opc.Entry(part) != null
                ? _opc.ReadXml(part)
                : new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), new XElement(Cp + "coreProperties", new XAttribute(XNamespace.Xmlns + "cp", Cp)));
            var root = xml.Root!;
            foreach (var (prefix, ns) in new[] { ("dc", Dc), ("dcterms", DcTerms), ("xsi", Xsi) })
                if (root.GetPrefixOfNamespace(ns) == null) root.SetAttributeValue(XNamespace.Xmlns + prefix, ns.NamespaceName);
            Set(root, Dc + "title", properties.Title);
            Set(root, Dc + "subject", properties.Subject);
            Set(root, Dc + "creator", properties.Author);
            Set(root, Cp + "keywords", properties.Keywords);
            Set(root, Dc + "description", properties.Description);
            Set(root, Cp + "lastModifiedBy", properties.LastModifiedBy);
            SetDate(root, DcTerms + "created", properties.Created);
            SetDate(root, DcTerms + "modified", properties.Modified);
            Put(part, xml);
            if (!_opc.OverrideContentTypes.ContainsKey(part)) SetOverride(part, "application/vnd.openxmlformats-package.core-properties+xml");
            if (rel != null) return;
            rels.Add(new OpcRelationship(OpcPackageWriter.NextId(rels), CoreType, part.TrimStart('/'), null));
            PutRelationships("/", rels);
        }

        private static void Set(XElement root, XName name, string value)
        {
            var element = root.Element(name);
            if (value == null) element?.Remove();
            else if (element == null) root.Add(new XElement(name, value));
            else element.Value = value;
        }

        private static void SetDate(XElement root, XName name, DateTimeOffset? value)
        {
            Set(root, name, value?.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
            if (value != null && root.Element(name)!.Attribute(Xsi + "type") == null) root.Element(name)!.SetAttributeValue(Xsi + "type", "dcterms:W3CDTF");
        }
    }
}
