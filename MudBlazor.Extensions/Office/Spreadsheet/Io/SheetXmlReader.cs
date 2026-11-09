using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Spreadsheet.XlsxPackage;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class SheetXmlReader
{
    internal static readonly XmlReaderSettings Settings = new()
    {
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
        DtdProcessing = DtdProcessing.Prohibit,
        CloseInput = false
    };

    private static readonly Regex Escape = new("_x([0-9A-Fa-f]{4})_", RegexOptions.CultureInvariant);

    internal static double ColumnWidthToPixels(double width) => Math.Truncate((256 * width + Math.Truncate(128.0 / 7)) / 256 * 7);

    internal static double PointsToPixels(double points) => points * 96 / 72;

    internal static string Decode(string s) =>
        s.Contains("_x", StringComparison.Ordinal) ? Escape.Replace(s, m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString()) : s;

    internal static string Text(XElement si, out bool rich)
    {
        rich = false;
        string text = null;
        foreach (var e in si.Elements())
        {
            var part = e.Name.LocalName switch
            {
                "t" => e.Value,
                "r" => e.Elements().FirstOrDefault(t => t.Name.LocalName == "t")?.Value,
                _ => null
            };
            if (e.Name.LocalName != "t") rich = true;
            if (part != null) text = text == null ? part : text + part;
        }

        return Decode(text ?? "");
    }

    internal static XElement ReadRoot(XmlReader r)
    {
        var root = new XElement(XName.Get(r.LocalName, r.NamespaceURI));
        if (r.MoveToFirstAttribute())
        {
            do
            {
                root.Add(r.NamespaceURI == XNamespace.Xmlns.NamespaceName
                    ? new XAttribute(r.Prefix == "xmlns" ? XNamespace.Xmlns + r.LocalName : "xmlns", r.Value)
                    : new XAttribute(XName.Get(r.LocalName, r.NamespaceURI), r.Value));
            } while (r.MoveToNextAttribute());

            r.MoveToElement();
        }

        return root;
    }

    internal static void Read(Stream stream, long partLength, Worksheet ws, XlsxSheetPart part, string[] strings, XlsxPackage package, string[] theme)
    {
        using var reader = XmlReader.Create(stream, Settings);
        reader.MoveToContent();
        part.Root = ReadRoot(reader);
        if (reader.IsEmptyElement) return;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType != XmlNodeType.Element)
            {
                reader.Read();
                continue;
            }

            if (reader.LocalName == "sheetData")
            {
                part.Children.Add(new XElement(XName.Get(reader.LocalName, reader.NamespaceURI)));
                ReadSheetData(reader, ws, part, strings, package);
                continue;
            }

            var element = (XElement)XNode.ReadFrom(reader);
            part.Children.Add(element);
            Apply(element, ws, part, package, theme, partLength);
        }
    }

    private static double? Number(XElement e, string name) => Finite((string)e?.Attribute(name));

    internal static double? Finite(string s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && double.IsFinite(v) ? v : null;

    private static int? Int(XElement e, string name) =>
        int.TryParse((string)e.Attribute(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static int Count(XElement e, string name, int limit) => Number(e, name) is { } v ? (int)Math.Clamp(v, 0, limit - 1) : 0;

    private static bool True(string s) => s is "1" or "true";

    private static int Xf(XlsxPackage package, string s) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var xf) && xf >= 0 && xf < package.XfStyleIds.Length ? xf : 0;

    private static void Apply(XElement e, Worksheet ws, XlsxSheetPart part, XlsxPackage package, string[] theme, long partLength)
    {
        switch (e.Name.LocalName)
        {
            case "dimension":
                if (CellRange.TryParse((string)e.Attribute("ref"), out var used)) ws.Reserve((int)Math.Min(used.Area, Math.Min(4_000_000, partLength / 10)));
                break;
            case "sheetPr":
                ws.TabColor = ThemeColors.Resolve(Child(e, "tabColor"), theme, ThemeColors.Indexed);
                break;
            case "sheetViews":
                var pane = Child(Child(e, "sheetView"), "pane");
                if ((string)pane?.Attribute("state") is "frozen" or "frozenSplit") ws.FreezeAt = Frozen(pane);
                break;
            case "sheetFormatPr":
                ws.DefaultRowHeight = DefaultRowHeight(e);
                ws.DefaultColumnWidth = DefaultColumnWidth(e);
                break;
            case "cols":
                foreach (var col in e.Elements().Where(c => c.Name.LocalName == "col"))
                {
                    var (min, max) = ColumnSpan(col);
                    var hidden = True((string)col.Attribute("hidden"));
                    var px = Number(col, "width") is { } w && (w > 0 || !hidden) ? ColumnWidthToPixels(w) : (double?)null;
                    var xf = Xf(package, (string)col.Attribute("style"));
                    var style = package.XfStyleIds[xf];
                    for (var c = min; c <= max; c++)
                    {
                        if (px is { } size && size != ws.DefaultColumnWidth) ws.SetSize(Axis.Column, c, size);
                        if (hidden) ws.SetHidden(Axis.Column, c, true);
                        if (style != 0) ws.SetAxisStyle(Axis.Column, c, style);
                        if (xf != package.CanonicalXf[style]) part.OriginalColXf[c] = xf;
                    }
                }

                break;
            case "mergeCells":
                ws.ReplaceMerges(e.Elements()
                    .Select(m => CellRange.TryParse((string)m.Attribute("ref"), out var range) ? range.Normalized : (CellRange?)null)
                    .Where(r => r is { IsSingleCell: false })
                    .Select(r => r.Value));
                break;
        }
    }

    internal static CellRef? Frozen(XElement pane)
    {
        var rows = Count(pane, "ySplit", CellRef.MaxRows);
        var cols = Count(pane, "xSplit", CellRef.MaxColumns);
        return rows > 0 || cols > 0 ? new CellRef(rows, cols) : null;
    }

    internal static double DefaultRowHeight(XElement sheetFormatPr) =>
        Number(sheetFormatPr, "defaultRowHeight") is > 0 and var height ? PointsToPixels(height) : Worksheet.StandardRowHeight;

    internal static double DefaultColumnWidth(XElement sheetFormatPr) =>
        Number(sheetFormatPr, "defaultColWidth") is > 0 and var width ? ColumnWidthToPixels(width)
        : Number(sheetFormatPr, "baseColWidth") is > 0 and var chars ? Math.Ceiling((chars * 7 + 5) / 8) * 8
        : Worksheet.StandardColumnWidth;

    internal static (int Min, int Max) ColumnSpan(XElement col)
    {
        var min = Math.Clamp(Int(col, "min") ?? 1, 1, CellRef.MaxColumns);
        var max = Math.Clamp(Int(col, "max") ?? min, min, CellRef.MaxColumns);
        return (min - 1, max - 1);
    }

    private static void ReadSheetData(XmlReader reader, Worksheet ws, XlsxSheetPart part, string[] strings, XlsxPackage package)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        var depth = reader.Depth;
        var row = -1;
        var col = -1;
        var shared = new Dictionary<string, (CellRef At, string Formula)>();
        var attributeSets = new Dictionary<string, XAttribute[]>();
        List<(CellRange Range, CellRef Anchor)> arrays = null;
        while (reader.Read() && reader.Depth > depth)
        {
            if (reader.NodeType != XmlNodeType.Element) continue;
            if (reader.LocalName == "row")
            {
                row = ReadRow(reader, ws, part, package, row, attributeSets);
                col = -1;
                arrays?.RemoveAll(a => a.Range.Bottom < row);
            }
            else if (reader.LocalName == "c")
            {
                col = ReadCell(reader, ws, part, strings, package, row, col, shared, ref arrays);
            }
        }

        reader.Read();
    }

    private static List<XAttribute> Extra(XmlReader reader, Func<string, bool> modelled)
    {
        List<XAttribute> extra = null;
        while (reader.MoveToNextAttribute())
        {
            if (reader.NamespaceURI.Length == 0 ? modelled(reader.LocalName) : reader.NamespaceURI == XNamespace.Xmlns.NamespaceName) continue;
            (extra ??= []).Add(new XAttribute(XName.Get(reader.LocalName, reader.NamespaceURI), reader.Value));
        }

        reader.MoveToElement();
        return extra;
    }

    private static int ReadRow(XmlReader reader, Worksheet ws, XlsxSheetPart part, XlsxPackage package, int previous, Dictionary<string, XAttribute[]> attributeSets)
    {
        var row = int.TryParse(reader.GetAttribute("r"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) ? r - 1 : previous + 1;
        if (row < 0 || row >= CellRef.MaxRows) return row;
        if (Extra(reader, n => n is "r" or "ht" or "hidden" or "s" or "customFormat" or "customHeight" or "spans") is { } extra)
        {
            var key = string.Join('\u0001', extra.Select(a => a.Name + "=" + a.Value));
            if (!attributeSets.TryGetValue(key, out var set)) attributeSets[key] = set = [.. extra];
            part.RowAttributes[row] = set;
        }

        if (Finite(reader.GetAttribute("ht")) is { } ht && PointsToPixels(ht) is var px && px != ws.DefaultRowHeight)
            ws.SetSize(Axis.Row, row, px);
        if (True(reader.GetAttribute("hidden"))) ws.SetHidden(Axis.Row, row, true);
        if (!True(reader.GetAttribute("customFormat"))) return row;
        var xf = Xf(package, reader.GetAttribute("s"));
        var style = package.XfStyleIds[xf];
        if (style != 0) ws.SetAxisStyle(Axis.Row, row, style);
        if (xf != package.CanonicalXf[style]) part.OriginalRowXf[row] = xf;
        return row;
    }

    private static int ReadCell(XmlReader reader, Worksheet ws, XlsxSheetPart part, string[] strings, XlsxPackage package, int row, int col,
        Dictionary<string, (CellRef At, string Formula)> shared, ref List<(CellRange Range, CellRef Anchor)> arrays)
    {
        var address = reader.GetAttribute("r");
        var style = reader.GetAttribute("s");
        var type = reader.GetAttribute("t");
        var extra = reader.AttributeCount > (address == null ? 0 : 1) + (style == null ? 0 : 1) + (type == null ? 0 : 1)
            ? Extra(reader, n => n is "r" or "s" or "t")
            : null;
        var at = CellRef.TryParse(address, out var parsed) ? parsed : new CellRef(Math.Max(row, 0), col + 1);
        var xf = Xf(package, style);
        var styleId = package.XfStyleIds[xf];
        string v = null, f = null, fType = null, fRef = null, fShared = null, inline = null;
        var hasFormula = false;
        if (!reader.IsEmptyElement)
        {
            var depth = reader.Depth;
            reader.Read();
            while (reader.Depth > depth && !reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.LocalName)
                    {
                        case "v":
                            v = reader.ReadElementContentAsString();
                            continue;
                        case "f":
                            hasFormula = true;
                            fType = reader.GetAttribute("t");
                            fRef = reader.GetAttribute("ref");
                            fShared = reader.GetAttribute("si");
                            f = reader.ReadElementContentAsString();
                            continue;
                        case "is":
                            inline = Text((XElement)XNode.ReadFrom(reader), out _);
                            continue;
                    }
                }

                reader.Read();
            }
        }

        if (!Worksheet.InBounds(at)) return at.Column;
        if (xf != package.CanonicalXf[styleId]) part.OriginalXf[at.Key] = xf;
        if (extra != null) part.CellAttributes[at.Key] = [.. extra];
        if (hasFormula && fType == "array")
        {
            var size = CellRange.TryParse(fRef, out var arrayRange) ? (arrayRange.RowCount, arrayRange.ColumnCount) : (1, 1);
            part.ArrayFormulas[at.Key] = size;
            if (extra?.FirstOrDefault(a => a.Name == "cm") is { } cm) package.DynamicArrayCm ??= cm.Value;
        }
        var sharedIndex = -1;
        var value = type switch
        {
            "s" => int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out sharedIndex) && sharedIndex >= 0 && sharedIndex < strings.Length
                ? CellValue.Text(strings[sharedIndex])
                : CellValue.Empty,
            "b" => v == null ? CellValue.Empty : CellValue.Boolean(True(v.Trim())),
            "e" => v == null ? CellValue.Empty : ErrorValue(v.Trim()),
            "str" => v == null ? CellValue.Empty : CellValue.Text(Decode(v)),
            "inlineStr" => CellValue.Text(inline ?? (v == null ? null : Decode(v))),
            "d" => DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
                ? CellValue.Number(ExcelDate.ToSerial(date))
                : CellValue.Empty,
            _ => !double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? CellValue.Empty
                : double.IsFinite(n) ? CellValue.Number(n) : CellValue.Error(CellError.Num)
        };

        string formula = null;
        if (hasFormula && fType == "shared" && fShared != null)
        {
            if (!string.IsNullOrWhiteSpace(f))
            {
                shared[fShared] = (at, f);
                formula = f;
            }
            else if (shared.TryGetValue(fShared, out var master))
                formula = ReferenceShifter.Translate(master.Formula, at.Row - master.At.Row, at.Column - master.At.Column);
        }
        else if (hasFormula && fType != "dataTable" && !string.IsNullOrWhiteSpace(f))
        {
            formula = f;
            if (fType == "array" && CellRange.TryParse(fRef, out var range) && !range.IsSingleCell) (arrays ??= []).Add((range.Normalized, at));
        }

        CellRef? anchor = null;
        if (formula == null && arrays != null)
            foreach (var (range, a) in arrays)
                if (range.Contains(at) && a != at)
                    anchor = a;

        if (formula == null && value.Kind == CellValueKind.Empty && styleId == 0 && anchor == null) return at.Column;
        var cell = ws.Ensure(at);
        cell.Value = value;
        cell.Formula = SheetXmlWriter.FromFile(formula);
        cell.StyleId = styleId;
        cell.SpillAnchor = anchor;
        if (type == "s" && sharedIndex >= 0 && package.RichStrings.ContainsKey(sharedIndex)) part.RichStringCells[at.Key] = sharedIndex;
        return at.Column;
    }

    private static CellValue ErrorValue(string code)
    {
        foreach (var (text, error) in FormulaLexer.ErrorCodes)
            if (text == code)
                return CellValue.Error(error);
        return CellValue.Error(CellError.Value);
    }
}
