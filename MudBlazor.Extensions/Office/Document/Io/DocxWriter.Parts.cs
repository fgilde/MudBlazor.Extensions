using System.Globalization;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Document.FormatXml;

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
        private void WriteStyles()
        {
            if (!ReferenceEquals(document.Styles, package.Styles))
                PutDefinitions(package.StylesPart, package.StylesXml, StylesXml.Write(package.StylesXml, package.Styles, document.Styles), "styles");
        }

        private void WriteNumbering()
        {
            if (!ReferenceEquals(document.Numbering, package.Numbering))
                PutDefinitions(package.NumberingPart, package.NumberingXml, NumberingXml.Write(package.NumberingXml, document.Numbering), "numbering");
        }

        private void PutDefinitions(string part, XDocument original, XDocument xml, string type)
        {
            if (original != null && XNode.DeepEquals(original, xml)) return;
            if (part == null)
            {
                if (_mainRelationships.FirstOrDefault(r => r.Type.EndsWith("/" + type, StringComparison.Ordinal) && r.TargetMode == null) is { } rel)
                    part = _opc.ResolveTarget(package.MainPart, rel.Target);
                else
                {
                    var folder = package.MainPart[..(package.MainPart.LastIndexOf('/') + 1)];
                    part = _opc.Entry(folder + type + ".xml") == null ? folder + type + ".xml" : writer.NextPartName(folder + type + "{0}.xml");
                    _mainRelationships.Add(new OpcRelationship(OpcPackageWriter.NextId(_mainRelationships), RelNs + "/" + type, OpcPackageWriter.Relative(package.MainPart, part), null));
                    _mainRelationshipsChanged = true;
                }

                if (!_overrides.ContainsKey(part)) SetOverride(part, Wml + type + "+xml");
            }

            Put(part, xml);
        }

        private void PrepareHeadersFooters()
        {
            var referenced = document.SectionList.SelectMany(s => s.Headers.Values.Concat(s.Footers.Values)).ToHashSet();
            var before = package.Sections.SelectMany(s => s.Headers.Values.Concat(s.Footers.Values)).ToHashSet();
            foreach (var dropped in package.HeadersFooters.Where(h => before.Contains(h.Id) && !referenced.Contains(h.Id)))
            {
                _done.Add(dropped.PartName);
                _done.Add(OpcPackage.RelsPartOf(dropped.PartName));
                if (_mainRelationships.RemoveAll(r => r.TargetMode == null &&
                                                      string.Equals(_opc.ResolveTarget(package.MainPart, r.Target), dropped.PartName, StringComparison.OrdinalIgnoreCase)) > 0)
                    _mainRelationshipsChanged = true;
                if (_overrides.Remove(dropped.PartName)) _contentTypesChanged = true;
            }

            var folder = package.MainPart[..(package.MainPart.LastIndexOf('/') + 1)];
            foreach (var added in document.HeaderFooterList.Where(h => h.PartName == null && referenced.Contains(h.Id)))
            {
                var kind = added.Kind == HeaderFooterKind.Header ? "header" : "footer";
                var part = writer.NextPartName(folder + kind + "{0}.xml");
                _newParts[added.Id] = part;
                _mainRelationships.Add(new OpcRelationship(added.Id, RelNs + "/" + kind, OpcPackageWriter.Relative(package.MainPart, part), null));
                _mainRelationshipsChanged = true;
                SetOverride(part, Wml + kind + "+xml");
            }
        }

        private void WriteNewHeaderFooter(HeaderFooter headerFooter, string part)
        {
            var rels = new List<OpcRelationship>();
            _context.Begin(part, rels, null);
            var root = new XElement(W + (headerFooter.Kind == HeaderFooterKind.Header ? "hdr" : "ftr"),
                new XAttribute(XNamespace.Xmlns + "w", W.NamespaceName), new XAttribute(XNamespace.Xmlns + "r", RelNs),
                headerFooter.Blocks.Select(b => _context.Block(b, false)));
            if (!root.HasElements) root.Add(new XElement(W + "p"));
            Put(part, new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), root));
            if (rels.Count > 0) PutRelationships(part, rels);
        }

        private static readonly HashSet<string> BeforeEvenAndOddHeaders =
        [
            "writeProtection", "view", "zoom", "removePersonalInformation", "removeDateAndTime", "doNotDisplayPageBoundaries", "displayBackgroundShape",
            "printPostScriptOverText", "printFractionalCharacterWidth", "printFormsData", "embedTrueTypeFonts", "embedSystemFonts", "saveSubsetFonts",
            "saveFormsData", "mirrorMargins", "alignBordersAndEdges", "bordersDoNotSurroundHeader", "bordersDoNotSurroundFooter", "gutterAtTop",
            "hideSpellingErrors", "hideGrammaticalErrors", "activeWritingStyle", "proofState", "formsDesign", "attachedTemplate", "linkStyles",
            "stylePaneFormatFilter", "stylePaneSortMethod", "documentType", "mailMerge", "revisionView", "trackRevisions", "doNotTrackMoves",
            "doNotTrackFormatting", "documentProtection", "autoFormatOverride", "styleLockTheme", "styleLockQFSet", "defaultTabStop", "autoHyphenation",
            "consecutiveHyphenLimit", "hyphenationZone", "doNotHyphenateCaps", "showEnvelope", "summaryLength", "clickAndTypeStyle", "defaultTableStyle"
        ];

        private void WriteSettings()
        {
            var original = package.SettingsXml?.Root is { } source && FormatXml.OnOff(source, "evenAndOddHeaders") == true;
            if (document.EvenAndOddHeaders == original) return;
            var xml = package.SettingsXml != null
                ? new XDocument(package.SettingsXml)
                : new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), new XElement(W + "settings", new XAttribute(XNamespace.Xmlns + "w", W.NamespaceName)));
            var root = xml.Root!;
            root.Elements(W + "evenAndOddHeaders").Remove();
            if (document.EvenAndOddHeaders)
            {
                var element = new XElement(W + "evenAndOddHeaders");
                if (root.Elements().LastOrDefault(e => e.Name.Namespace == W && BeforeEvenAndOddHeaders.Contains(e.Name.LocalName)) is { } previous) previous.AddAfterSelf(element);
                else root.AddFirst(element);
            }

            PutDefinitions(package.SettingsPart, package.SettingsXml, xml, "settings");
        }

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
