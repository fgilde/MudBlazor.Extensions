using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

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

    internal static void Read(Stream stream, Worksheet ws, XlsxSheetPart part, string[] strings, XlsxPackage package, string[] theme)
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
            Apply(element, ws, package, theme);
        }
    }

    private static XElement Child(XElement e, string name) => e?.Elements().FirstOrDefault(c => c.Name.LocalName == name);

    private static double? Number(XElement e, string name) =>
        double.TryParse((string)e?.Attribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static bool True(string s) => s is "1" or "true";

    private static int StyleId(XlsxPackage package, string s) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var xf) && xf >= 0 && xf < package.XfStyleIds.Length ? package.XfStyleIds[xf] : 0;

    private static void Apply(XElement e, Worksheet ws, XlsxPackage package, string[] theme)
    {
        switch (e.Name.LocalName)
        {
            case "dimension":
                if (CellRange.TryParse((string)e.Attribute("ref"), out var used)) ws.Reserve((int)Math.Min(used.Area, 4_000_000));
                break;
            case "sheetPr":
                ws.TabColor = ThemeColors.Resolve(Child(e, "tabColor"), theme, ThemeColors.Indexed);
                break;
            case "sheetViews":
                var pane = Child(Child(e, "sheetView"), "pane");
                if ((string)pane?.Attribute("state") is "frozen" or "frozenSplit")
                {
                    var rows = (int)(Number(pane, "ySplit") ?? 0);
                    var cols = (int)(Number(pane, "xSplit") ?? 0);
                    if ((rows > 0 || cols > 0) && rows < CellRef.MaxRows && cols < CellRef.MaxColumns) ws.FreezeAt = new CellRef(rows, cols);
                }

                break;
            case "sheetFormatPr":
                if (Number(e, "defaultRowHeight") is > 0 and var height) ws.DefaultRowHeight = PointsToPixels(height);
                if (Number(e, "defaultColWidth") is > 0 and var width) ws.DefaultColumnWidth = ColumnWidthToPixels(width);
                else if (Number(e, "baseColWidth") is > 0 and var chars) ws.DefaultColumnWidth = Math.Ceiling((chars * 7 + 5) / 8) * 8;
                break;
            case "cols":
                foreach (var col in e.Elements().Where(c => c.Name.LocalName == "col"))
                {
                    var min = (int)(Number(col, "min") ?? 1) - 1;
                    var max = Math.Min((int)(Number(col, "max") ?? min + 1), CellRef.MaxColumns) - 1;
                    var hidden = True((string)col.Attribute("hidden"));
                    var px = Number(col, "width") is { } w && (w > 0 || !hidden) ? ColumnWidthToPixels(w) : (double?)null;
                    var style = StyleId(package, (string)col.Attribute("style"));
                    for (var c = Math.Max(min, 0); c <= max; c++)
                    {
                        if (px is { } size && size != ws.DefaultColumnWidth) ws.SetSize(Axis.Column, c, size);
                        if (hidden) ws.SetHidden(Axis.Column, c, true);
                        if (style != 0) ws.SetAxisStyle(Axis.Column, c, style);
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
        List<(CellRange Range, CellRef Anchor)> arrays = null;
        while (reader.Read() && reader.Depth > depth)
        {
            if (reader.NodeType != XmlNodeType.Element) continue;
            if (reader.LocalName == "row")
            {
                row = ReadRow(reader, ws, package, row);
                col = -1;
            }
            else if (reader.LocalName == "c")
            {
                col = ReadCell(reader, ws, part, strings, package, row, col, shared, ref arrays);
            }
        }

        reader.Read();
    }

    private static int ReadRow(XmlReader reader, Worksheet ws, XlsxPackage package, int previous)
    {
        var row = int.TryParse(reader.GetAttribute("r"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) ? r - 1 : previous + 1;
        if (row < 0 || row >= CellRef.MaxRows) return row;
        if (double.TryParse(reader.GetAttribute("ht"), NumberStyles.Float, CultureInfo.InvariantCulture, out var ht)
            && PointsToPixels(ht) is var px && px != ws.DefaultRowHeight)
            ws.SetSize(Axis.Row, row, px);
        if (True(reader.GetAttribute("hidden"))) ws.SetHidden(Axis.Row, row, true);
        if (True(reader.GetAttribute("customFormat")) && StyleId(package, reader.GetAttribute("s")) is var style and not 0)
            ws.SetAxisStyle(Axis.Row, row, style);
        return row;
    }

    private static int ReadCell(XmlReader reader, Worksheet ws, XlsxSheetPart part, string[] strings, XlsxPackage package, int row, int col,
        Dictionary<string, (CellRef At, string Formula)> shared, ref List<(CellRange Range, CellRef Anchor)> arrays)
    {
        string address = null, style = null, type = null;
        while (reader.MoveToNextAttribute())
        {
            switch (reader.LocalName)
            {
                case "r":
                    address = reader.Value;
                    break;
                case "s":
                    style = reader.Value;
                    break;
                case "t":
                    type = reader.Value;
                    break;
            }
        }

        reader.MoveToElement();
        var at = CellRef.TryParse(address, out var parsed) ? parsed : new CellRef(Math.Max(row, 0), col + 1);
        var styleId = StyleId(package, style);
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
            _ => double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? CellValue.Number(n) : CellValue.Empty
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
        cell.Formula = formula;
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
