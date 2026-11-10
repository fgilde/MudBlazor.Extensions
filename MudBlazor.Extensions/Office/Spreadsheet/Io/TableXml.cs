using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Spreadsheet.XlsxPackage;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class TableXml
{
    internal const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.table+xml";

    internal static List<SheetTable> Read(ZipArchive zip, XlsxSheetPart part)
    {
        var tables = new List<SheetTable>();
        if (part.Children.FirstOrDefault(c => c.Name.LocalName == "tableParts") is not { } parts || part.PartName == null) return tables;
        var rels = ReadRelationships(zip, part.PartName);
        foreach (var tablePart in parts.Elements().Where(e => e.Name.LocalName == "tablePart"))
        {
            var id = (string)tablePart.Attributes().FirstOrDefault(a => a.Name.LocalName == "id" && a.Name.Namespace != XNamespace.None);
            if (rels.FirstOrDefault(r => r.Id == id && !r.External) is not { } rel || Find(zip, ResolveTarget(part.PartName, rel.Target)) is not { } entry) continue;
            XElement root;
            using (var s = entry.Open()) root = LoadXml(s).Root;
            if (Table(root, entry.FullName, id) is { } table)
            {
                part.TableParts.Add((id, entry.FullName));
                tables.Add(table);
            }
            else part.OpaqueTableParts.Add(id);
        }

        return tables;
    }

    private static SheetTable Table(XElement e, string partName, string relationshipId)
    {
        if (!CellRange.TryParse((string)e.Attribute("ref"), out var range)) return null;
        var columns = Child(e, "tableColumns")?.Elements().Where(c => c.Name.LocalName == "tableColumn")
            .Select(c => new TableColumn(SheetXmlReader.Decode((string)c.Attribute("name") ?? ""), (string)c.Attribute("totalsRowFunction") is { } f and not "none" ? f : null,
                FromFile(Child(c, "calculatedColumnFormula")?.Value)) { Id = Int(c, "id"), Source = c, TotalsLabel = SheetXmlReader.Decode((string)c.Attribute("totalsRowLabel")) })
            .ToList() ?? [];
        var style = Child(e, "tableStyleInfo");
        var name = (string)e.Attribute("name") ?? (string)e.Attribute("displayName") ?? "";
        return SheetTable.FromFile(e, partName, relationshipId, Int(e, "id"), name, (string)e.Attribute("displayName") ?? name, range.Normalized,
            (string)e.Attribute("headerRowCount") is not "0", Int(e, "totalsRowCount") > 0, columns, (string)style?.Attribute("name"),
            True(style, "showRowStripes"), True(style, "showColumnStripes"), True(style, "showFirstColumn"), True(style, "showLastColumn"),
            Child(e, "autoFilter") is { } filter ? SheetXmlReader.ReadAutoFilter(filter) ?? new AutoFilter(range.Normalized, source: filter) : null);
    }

    private static string FromFile(string formula) => formula == null ? null : StructuredReferences.ToDisplay(SheetXmlWriter.FromFile(formula));

    private static int Int(XElement e, string name) =>
        int.TryParse((string)e.Attribute(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static bool True(XElement e, string name) => (string)e?.Attribute(name) is "1" or "true";

    internal static XElement Write(SheetTable t, IReadOnlyList<string> names = null)
    {
        var ns = t.Source?.Name.Namespace ?? XNamespace.Get(SheetXmlWriter.MainNs);
        var e = t.Source != null ? new XElement(t.Source) : new XElement(ns + "table");
        e.SetAttributeValue("id", t.Id);
        e.SetAttributeValue("name", t.Name);
        e.SetAttributeValue("displayName", t.DisplayName);
        e.SetAttributeValue("ref", t.Range.ToA1());
        e.SetAttributeValue("headerRowCount", t.HeaderRow ? null : "0");
        e.SetAttributeValue("totalsRowCount", t.TotalsRow ? "1" : null);
        if (t.TotalsRow) e.SetAttributeValue("totalsRowShown", null);
        else if (t.Source == null) e.SetAttributeValue("totalsRowShown", "0");

        Child(e, "autoFilter")?.Remove();
        if (t.Filter is { } filter) e.AddFirst(SheetXmlWriter.AutoFilterXml(filter, ns));

        if (t.Source != null && (string)t.Source.Attribute("ref") != t.Range.ToA1()) Child(e, "sortState")?.Remove();
        var columns = Child(e, "tableColumns");
        if (columns == null)
        {
            columns = new XElement(ns + "tableColumns");
            if ((Child(e, "sortState") ?? Child(e, "autoFilter")) is { } before) before.AddAfterSelf(columns);
            else e.AddFirst(columns);
        }

        columns.RemoveNodes();
        columns.SetAttributeValue("count", t.Columns.Count);
        var next = t.Columns.Select(c => c.Id).DefaultIfEmpty(0).Max() + 1;
        for (var i = 0; i < t.Columns.Count; i++)
            columns.Add(Column(t.Columns[i], t.Columns[i].Id > 0 ? t.Columns[i].Id : next++, names?[i] ?? t.Columns[i].Name, t.Name, ns));

        var info = Child(e, "tableStyleInfo");
        if (info == null) columns.AddAfterSelf(info = new XElement(ns + "tableStyleInfo"));
        info.SetAttributeValue("name", string.IsNullOrEmpty(t.StyleName) ? null : t.StyleName);
        info.SetAttributeValue("showFirstColumn", t.FirstColumn ? "1" : "0");
        info.SetAttributeValue("showLastColumn", t.LastColumn ? "1" : "0");
        info.SetAttributeValue("showRowStripes", t.BandedRows ? "1" : "0");
        info.SetAttributeValue("showColumnStripes", t.BandedColumns ? "1" : "0");
        return e;
    }

    private static XElement Column(TableColumn column, int id, string name, string table, XNamespace ns)
    {
        var c = column.Source != null ? new XElement(column.Source) : new XElement(ns + "tableColumn");
        c.SetAttributeValue("id", id);
        if (SheetXmlReader.Decode((string)c.Attribute("name") ?? "") != name) c.SetAttributeValue("name", SharedStringsWriter.Escape(name, breaks: true));
        c.SetAttributeValue("totalsRowFunction", column.TotalsFunction);
        c.SetAttributeValue("totalsRowLabel", SharedStringsWriter.Escape(column.TotalsLabel, breaks: true));
        var formula = Child(c, "calculatedColumnFormula");
        if (column.CalculatedFormula == null) formula?.Remove();
        else if (FromFile(formula?.Value) != column.CalculatedFormula)
        {
            if (formula == null) c.AddFirst(formula = new XElement(ns + "calculatedColumnFormula"));
            formula.Value = SharedStringsWriter.Escape(SheetXmlWriter.FormulaText(StructuredReferences.ToFile(column.CalculatedFormula, table)), false);
        }

        return c;
    }
}
