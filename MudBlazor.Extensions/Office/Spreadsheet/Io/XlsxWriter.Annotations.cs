using System.Globalization;
using System.Text;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Spreadsheet.XlsxPackage;

namespace MudBlazor.Extensions.Office.Spreadsheet;

public static partial class XlsxWriter
{
    private sealed partial class PackageWriter
    {
        private int _drawing = -1;

        private string Folder => _workbookPart[..(_workbookPart.LastIndexOf('/') + 1)];

        private string NewPart(string pattern, string contentType)
        {
            var existing = new HashSet<string>((source?.Entries.Select(e => e.FullName) ?? []).Concat(_added), StringComparer.OrdinalIgnoreCase);
            var n = 1;
            string name;
            while (existing.Contains(name = string.Format(CultureInfo.InvariantCulture, pattern, n))) n++;
            _added.Add(name);
            if (contentType != null) _overrides["/" + name] = contentType;
            return name;
        }

        private (XElement Links, XElement Legacy) Annotations(SheetEntry sheet, string typeBase, SheetRels rels)
        {
            var ns = sheet.Original?.Root?.Name.Namespace ?? XNamespace.Get(SheetXmlWriter.MainNs);
            var opaque = sheet.Original?.OpaqueLinks ?? [];
            var used = opaque.Select(AnnotationXml.RelationshipId).Where(id => id != null).ToHashSet();
            var links = AnnotationXml.WriteLinks(Original(sheet, "hyperlinks"), sheet.Worksheet.Links, opaque, ns, link =>
            {
                if (string.IsNullOrEmpty(link.Target)) return null;
                var target = link.Target.Replace(" ", "%20", StringComparison.Ordinal);
                var id = rels.List.Any(r => r.Id == link.RelationshipId && IsType(r, "hyperlink") && (r.Target == link.Target || r.Target == target))
                    ? link.RelationshipId
                    : rels.Add(typeBase + "/hyperlink", target, true);
                used.Add(id);
                return id;
            });
            foreach (var rel in rels.List.Where(r => IsType(r, "hyperlink") && !used.Contains(r.Id)).ToList()) rels.Remove(rel.Id);
            return (links, Notes(sheet, typeBase, rels, ns));
        }

        private XElement Notes(SheetEntry sheet, string typeBase, SheetRels rels, XNamespace ns)
        {
            var ws = sheet.Worksheet;
            var part = sheet.Original;
            var legacy = Original(sheet, "legacyDrawing");
            if (ws.NotesReadOnly || ws.Notes.SequenceEqual(part?.OriginalNotes ?? [], ReferenceEqualityComparer.Instance)) return legacy;

            if (ws.Notes.Count > 0)
            {
                var comments = part?.CommentsPart ?? NewPart(Folder + "comments{0}.xml", AnnotationXml.CommentsContentType);
                if (part?.CommentsPart == null) rels.Add(typeBase + "/comments", Relative(sheet.PartName, comments));
                var doc = AnnotationXml.WriteComments(part?.CommentsRoot, ws.Notes);
                _parts[comments] = s => SheetXmlWriter.Save(doc, s);
            }
            else if (part?.CommentsPart != null)
            {
                Remove(part.CommentsPart);
                rels.Remove(part.CommentsRelationshipId);
            }

            var text = part?.VmlText != null ? AnnotationXml.WriteVml(part.VmlText, ws.Notes, 0)
                : ws.Notes.Count > 0 ? AnnotationXml.WriteVml(null, ws.Notes, NextDrawing())
                : null;
            if (text == null)
            {
                if (part?.VmlPart != null)
                {
                    Remove(part.VmlPart);
                    rels.Remove(part.VmlRelationshipId);
                }

                return null;
            }

            var bytes = Encoding.Latin1.GetBytes(text);
            if (part?.VmlPart != null)
            {
                _parts[part.VmlPart] = s => s.Write(bytes);
                return legacy;
            }

            var vml = NewPart(Folder + "drawings/vmlDrawing{0}.vml", null);
            if (!_defaults.ContainsKey("vml")) _defaults["vml"] = AnnotationXml.VmlContentType;
            _parts[vml] = s => s.Write(bytes);
            var id = rels.Add(typeBase + "/vmlDrawing", Relative(sheet.PartName, vml));
            legacy = legacy != null ? new XElement(legacy) : new XElement(ns + "legacyDrawing");
            legacy.Attributes().FirstOrDefault(a => a.Name.LocalName == "id" && a.Name.Namespace != XNamespace.None)?.Remove();
            legacy.SetAttributeValue(XName.Get("id", RelationshipsNs), id);
            return legacy;
        }

        private int NextDrawing()
        {
            if (_drawing < 0)
                _drawing = source?.Entries.Where(e => e.FullName.EndsWith(".vml", StringComparison.OrdinalIgnoreCase))
                    .Select(e => Read(e, s => AnnotationXml.MaxDrawing(AnnotationXml.ReadVml(s))))
                    .DefaultIfEmpty(0).Max() ?? 0;
            return ++_drawing;
        }

        private static XElement Original(SheetEntry sheet, string name) => sheet.Original?.Children.FirstOrDefault(c => c.Name.LocalName == name);

        private sealed class SheetRels(List<XlsxRelationship> list)
        {
            public List<XlsxRelationship> List { get; } = list;

            public bool Changed { get; private set; }

            public string Add(string type, string target, bool external = false)
            {
                var n = 1;
                while (List.Any(r => r.Id == "rId" + n)) n++;
                List.Add(new XlsxRelationship("rId" + n, type, target, external));
                Changed = true;
                return "rId" + n;
            }

            public void Remove(string id)
            {
                if (id != null && List.RemoveAll(r => r.Id == id) > 0) Changed = true;
            }
        }
    }
}
