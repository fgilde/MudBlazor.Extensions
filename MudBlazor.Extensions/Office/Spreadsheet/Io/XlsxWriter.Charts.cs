using System.Globalization;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Spreadsheet.XlsxPackage;

namespace MudBlazor.Extensions.Office.Spreadsheet;

public static partial class XlsxWriter
{
    private sealed partial class PackageWriter
    {
        private XElement Drawing(SheetEntry sheet, string typeBase, SheetRels rels)
        {
            var ws = sheet.Worksheet;
            var part = sheet.Original;
            var original = Original(sheet, "drawing");
            var originals = part?.OriginalCharts ?? [];
            if (part is { DrawingPart: not null, OriginalDrawing: null }
                || ws.Charts.SequenceEqual(originals, ReferenceEqualityComparer.Instance) && part?.DrawingRoot == part?.OriginalDrawing)
                return original;

            var drawingPart = part?.DrawingPart;
            var drawingRels = new SheetRels(drawingPart != null ? ReadRelationships(source, drawingPart) : []);
            var root = part?.DrawingRoot != null ? new XElement(part.DrawingRoot) : ChartXmlWriter.NewDrawing();
            foreach (var element in root.Elements().ToList())
            {
                var anchor = element.Name.LocalName == "AlternateContent" ? Child(element, "Choice")?.Elements().FirstOrDefault() : element;
                var id = ChartXmlReader.RelationshipId(Child(Child(Child(Child(anchor, "graphicFrame"), "graphic"), "graphicData"), "chart"));
                if (id == null || originals.All(c => c.RelationshipId != id)) continue;
                var chart = ws.Charts.FirstOrDefault(c => c.RelationshipId == id);
                if (chart == null)
                {
                    element.Remove();
                    drawingRels.Remove(id);
                    Remove(originals.First(c => c.RelationshipId == id).PartName);
                }
                else if (!chart.ReadOnly) ChartXmlWriter.SetAnchor(anchor, chart);
            }

            var shapeId = root.Descendants().Where(e => e.Name.LocalName == "cNvPr")
                .Select(e => int.TryParse((string)e.Attribute("id"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0).DefaultIfEmpty(1).Max();
            foreach (var chart in ws.Charts)
            {
                if (chart.RelationshipId != null && originals.Any(c => c.RelationshipId == chart.RelationshipId))
                {
                    if (originals.Contains(chart) || chart.Source == null) continue;
                    var patched = ChartXmlWriter.Patch(chart, wb, ws);
                    if (!XNode.DeepEquals(patched, chart.Source)) _parts[chart.PartName] = s => Save(patched, s);
                    continue;
                }

                drawingPart ??= NewPart(Folder + "drawings/drawing{0}.xml", ChartXmlWriter.DrawingContentType);
                var chartPart = NewPart(Folder + "charts/chart{0}.xml", ChartXmlWriter.ChartContentType);
                var id = drawingRels.Add(typeBase + "/chart", Relative(drawingPart, chartPart));
                root.Add(ChartXmlWriter.NewAnchor(root, chart, typeBase, id, ++shapeId));
                var xml = ChartXmlWriter.Create(chart, wb, ws);
                _parts[chartPart] = s => Save(xml, s);
            }

            if (!root.HasElements)
            {
                if (drawingPart == null) return null;
                Remove(drawingPart);
                rels.Remove(part.DrawingRelationshipId);
                return null;
            }

            if (drawingRels.Changed) _parts[RelsPartOf(drawingPart)] = s => WriteRelationships(s, drawingRels.List);
            if (part?.OriginalDrawing == null || !XNode.DeepEquals(root, part.OriginalDrawing)) _parts[drawingPart] = s => Save(root, s);
            if (original != null && part?.DrawingPart != null) return original;
            var ns = sheet.Original?.Root?.Name.Namespace ?? XNamespace.Get(SheetXmlWriter.MainNs);
            return new XElement(ns + "drawing", new XAttribute(XName.Get("id", RelationshipsNs), rels.Add(typeBase + "/drawing", Relative(sheet.PartName, drawingPart))));
        }

        private static void Save(XElement root, Stream s) => SheetXmlWriter.Save(new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), root), s);
    }
}
