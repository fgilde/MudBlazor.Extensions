using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Spreadsheet.XlsxPackage;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class SheetXmlWriter
{
    internal const string MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    internal static readonly XmlWriterSettings Settings = new()
    {
        Encoding = new UTF8Encoding(false), CheckCharacters = false, CloseOutput = false, NewLineHandling = NewLineHandling.Entitize
    };

    internal static void Save(XDocument doc, Stream stream)
    {
        using var w = XmlWriter.Create(stream, Settings);
        doc.Save(w);
    }

    private static readonly HashSet<string> Future = new(StringComparer.OrdinalIgnoreCase)
    {
        "ACOT", "ACOTH", "AGGREGATE", "ARABIC", "ARRAYTOTEXT", "BASE", "BINOM.DIST", "CEILING.MATH", "CEILING.PRECISE", "CHOOSECOLS", "CHOOSEROWS",
        "COMBINA", "CONCAT", "COT", "COTH", "COVARIANCE.P", "COVARIANCE.S", "CSC", "CSCH", "DAYS", "DECIMAL", "DROP", "EXPAND", "EXPON.DIST",
        "FILTER", "FLOOR.MATH", "FLOOR.PRECISE", "FORECAST.LINEAR", "FORMULATEXT", "HSTACK", "IFNA", "IFS", "ISFORMULA", "ISO.CEILING", "ISOWEEKNUM", "LET",
        "MAXIFS", "MINIFS", "MODE.MULT", "MODE.SNGL", "MUNIT", "NETWORKDAYS.INTL", "NORM.DIST", "NORM.INV", "NORM.S.DIST", "NORM.S.INV",
        "NUMBERVALUE", "PDURATION", "PERCENTILE.EXC", "PERCENTILE.INC", "PERCENTRANK.INC", "PERMUTATIONA", "POISSON.DIST", "QUARTILE.EXC",
        "QUARTILE.INC", "RANDARRAY", "RANK.AVG", "RANK.EQ", "RRI", "SEC", "SECH", "SEQUENCE", "SHEET", "SHEETS", "SORT", "SORTBY", "STDEV.P",
        "STDEV.S", "SWITCH", "TAKE", "TEXTAFTER", "TEXTBEFORE", "TEXTJOIN", "TEXTSPLIT", "TOCOL", "TOROW", "UNICHAR", "UNICODE", "UNIQUE",
        "VALUETOTEXT", "VAR.P", "VAR.S", "VSTACK", "WORKDAY.INTL", "WRAPCOLS", "WRAPROWS", "XLOOKUP", "XMATCH", "XOR"
    };

    private const StringComparison Ignore = StringComparison.OrdinalIgnoreCase;

    internal static string FromFile(string formula)
    {
        if (formula == null || !formula.Contains("_xl", Ignore) || Tokens(formula) is not { } tokens) return formula;
        var sb = new StringBuilder(formula.Length);
        var pos = 0;
        for (var i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            sb.Append(formula, pos, t.Start - pos);
            pos = t.Start + t.Text.Length;
            var text = t.Text;
            if (t.Kind == FormulaTokenKind.Function && Bare(text) is var bare && bare.Length < text.Length)
            {
                if (bare.Equals("ANCHORARRAY", Ignore) && i + 3 < tokens.Count && tokens[i + 1].Kind == FormulaTokenKind.OpenParen
                    && tokens[i + 2] is { Kind: FormulaTokenKind.Reference } inner && !inner.Text.Contains(':') && !inner.Text.EndsWith('#')
                    && tokens[i + 3].Kind == FormulaTokenKind.CloseParen)
                {
                    text = inner.Text + "#";
                    pos = tokens[i + 3].Start + 1;
                    i += 3;
                }
                else if (Future.Contains(bare)) text = bare;
            }
            else if (t.Kind == FormulaTokenKind.Name && text.StartsWith("_xlpm.", Ignore)) text = text[6..];

            sb.Append(text);
        }

        return sb.Append(formula, pos, formula.Length - pos).ToString();
    }

    internal static string FormulaText(string formula)
    {
        if (formula == null || formula.IndexOfAny(['(', '#', ';']) < 0 || Tokens(formula) is not { } tokens) return formula;
        var sb = new StringBuilder(formula.Length + 16);
        var pos = 0;
        var frames = new List<Frame>();
        var pendingLet = false;
        for (var i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            sb.Append(formula, pos, t.Start - pos);
            pos = t.Start + t.Text.Length;
            var text = t.Text;
            var top = frames.Count > 0 ? frames[^1] : null;
            switch (t.Kind)
            {
                case FormulaTokenKind.Function:
                    pendingLet = Bare(text).Equals("LET", Ignore);
                    if (Future.Contains(text)) text = (text.ToUpperInvariant() is "FILTER" or "SORT" ? "_xlfn._xlws." : "_xlfn.") + text;
                    break;
                case FormulaTokenKind.OpenParen or FormulaTokenKind.OpenBrace:
                    frames.Add(new Frame(pendingLet && t.Kind == FormulaTokenKind.OpenParen, t.Kind == FormulaTokenKind.OpenParen));
                    pendingLet = false;
                    break;
                case FormulaTokenKind.CloseParen or FormulaTokenKind.CloseBrace:
                    if (frames.Count > 0) frames.RemoveAt(frames.Count - 1);
                    break;
                case FormulaTokenKind.Comma or FormulaTokenKind.Semicolon when top is { Paren: true }:
                    text = ",";
                    top.Argument++;
                    break;
                case FormulaTokenKind.Reference when text.EndsWith('#'):
                    text = "_xlfn.ANCHORARRAY(" + text[..^1] + ")";
                    break;
                case FormulaTokenKind.Name when !text.StartsWith("_xlpm.", Ignore):
                    if (top is { Let: true } && top.Argument % 2 == 0 && tokens[i - 1].Kind is FormulaTokenKind.OpenParen or FormulaTokenKind.Comma or FormulaTokenKind.Semicolon
                        && i + 1 < tokens.Count && tokens[i + 1].Kind is FormulaTokenKind.Comma or FormulaTokenKind.Semicolon)
                    {
                        top.Names.Add(text);
                        text = "_xlpm." + text;
                    }
                    else if (frames.Any(f => f.Names.Contains(text))) text = "_xlpm." + text;

                    break;
            }

            sb.Append(text);
        }

        return sb.Append(formula, pos, formula.Length - pos).ToString();
    }

    private static List<FormulaToken> Tokens(string formula)
    {
        try
        {
            return FormulaLexer.Tokenize(formula, true);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string Bare(string name)
    {
        foreach (var prefix in (ReadOnlySpan<string>)["_xlfn._xlws.", "_xlfn.", "_xlws."])
            if (name.StartsWith(prefix, Ignore))
                return name[prefix.Length..];
        return name;
    }

    private sealed class Frame(bool let, bool paren)
    {
        public bool Let { get; } = let;

        public bool Paren { get; } = paren;

        public int Argument { get; set; }

        public HashSet<string> Names { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    internal static double PixelsToWidth(double px)
    {
        if (px <= 0) return 0;
        var chars = Math.Max(0, Math.Truncate((px - 5) / 7 * 100 + 0.5) / 100);
        return Math.Truncate((chars * 7 + 5) / 7 * 256) / 256;
    }

    internal static string Number(double v) => v.ToString("R", CultureInfo.InvariantCulture);

    internal static void Write(Stream stream, Worksheet ws, XlsxSheetPart part, XlsxPackage package, StylesXmlWriter styles, SharedStringsWriter strings,
        string[] theme)
    {
        var root = part?.Root ?? new XElement(XName.Get("worksheet", MainNs), new XAttribute("xmlns", MainNs),
            new XAttribute(XNamespace.Xmlns + "r", RelationshipsNs));
        var ns = root.Name.Namespace;
        var (keys, cells) = Cells(ws, part, package);
        var spills = new Dictionary<long, CellRange>();
        int left = int.MaxValue, right = -1;
        for (var i = 0; i < keys.Length; i++)
        {
            var c = CellRef.FromKey(keys[i]);
            left = Math.Min(left, c.Column);
            right = Math.Max(right, c.Column);
            if (cells[i]?.SpillAnchor is not { } a) continue;
            var range = spills.TryGetValue(a.Relative.Key, out var r) ? r : new CellRange(a.Relative, a.Relative);
            spills[a.Relative.Key] = CellRange.FromBounds(Math.Min(range.Top, c.Row), Math.Min(range.Left, c.Column), Math.Max(range.Bottom, c.Row),
                Math.Max(range.Right, c.Column));
        }

        var original = part?.Children ?? [];
        XElement Original(string name) => original.FirstOrDefault(e => e.Name == ns + name);
        var dimension = new XElement(ns + "dimension", new XAttribute("ref", keys.Length == 0 ? "A1"
            : CellRange.FromBounds(CellRef.FromKey(keys[0]).Row, left, CellRef.FromKey(keys[^1]).Row, right).ToA1()));
        var children = WorksheetChildOrder.Arrange(original,
        [
            SheetPr(Original("sheetPr"), ws, ns, theme), dimension, SheetViews(Original("sheetViews"), ws, ns),
            SheetFormatPr(Original("sheetFormatPr"), ws, ns), Cols(Original("cols"), ws, part, package, styles, ns),
            new XElement(ns + "sheetData"),
            ws.Merges.Count == 0 ? null : new XElement(ns + "mergeCells", new XAttribute("count", ws.Merges.Count),
                ws.Merges.Select(m => new XElement(ns + "mergeCell", new XAttribute("ref", m.ToA1()))))
        ], ns);

        using var w = XmlWriter.Create(stream, Settings);
        w.WriteStartDocument(true);
        w.WriteStartElement(root.GetPrefixOfNamespace(ns), root.Name.LocalName, ns.NamespaceName);
        foreach (var a in root.Attributes().Where(a => a.IsNamespaceDeclaration))
            if (a.Name.Namespace == XNamespace.None) w.WriteAttributeString("xmlns", a.Value);
            else w.WriteAttributeString("xmlns", a.Name.LocalName, XNamespace.Xmlns.NamespaceName, a.Value);
        foreach (var a in root.Attributes().Where(a => !a.IsNamespaceDeclaration))
            w.WriteAttributeString(a.Name.LocalName, a.Name.NamespaceName, a.Value);
        foreach (var child in children)
        {
            if (child.Name == ns + "sheetData") SheetData(w, ns, ws, part, package, styles, strings, keys, cells, spills);
            else child.WriteTo(w);
        }

        w.WriteEndElement();
    }

    private static (long[] Keys, Cell[] Cells) Cells(Worksheet ws, XlsxSheetPart part, XlsxPackage package)
    {
        var list = ws.UsedCells().ToList();
        var extra = part?.OriginalXf.Where(p => package.XfStyleIds[p.Value] == 0 && ws[CellRef.FromKey(p.Key)] == null).Select(p => p.Key).ToList() ?? [];
        var keys = new long[list.Count + extra.Count];
        var cells = new Cell[keys.Length];
        for (var i = 0; i < list.Count; i++)
        {
            keys[i] = list[i].Ref.Key;
            cells[i] = list[i].Cell;
        }

        extra.CopyTo(keys, list.Count);
        Array.Sort(keys, cells);
        return (keys, cells);
    }

    private static int Xf<TKey>(XlsxPackage package, Dictionary<TKey, int> original, TKey index, int styleId, StylesXmlWriter styles) =>
        original != null && original.TryGetValue(index, out var xf) && package.XfStyleIds[xf] == styleId ? xf : styles.Xf(styleId);

    private static void SheetData(XmlWriter w, XNamespace ns, Worksheet ws, XlsxSheetPart part, XlsxPackage package, StylesXmlWriter styles,
        SharedStringsWriter strings, long[] keys, Cell[] cells, Dictionary<long, CellRange> spills)
    {
        var main = ns.NamespaceName;
        var special = new SortedSet<int>(ws.RowHeights.Keys);
        special.UnionWith(ws.HiddenRows);
        special.UnionWith(ws.RowStyles.Keys);
        if (part != null)
        {
            special.UnionWith(part.RowAttributes.Keys);
            special.UnionWith(part.OriginalRowXf.Where(p => package.XfStyleIds[p.Value] == ws.RowStyle(p.Key)).Select(p => p.Key));
        }

        var rich = new Dictionary<int, string>();
        var columns = new Dictionary<int, string>();
        w.WriteStartElement("sheetData", main);
        var i = 0;
        using var next = special.GetEnumerator();
        var hasSpecial = next.MoveNext();
        while (i < keys.Length || hasSpecial)
        {
            var cellRow = i < keys.Length ? CellRef.FromKey(keys[i]).Row : int.MaxValue;
            var row = hasSpecial && next.Current <= cellRow ? next.Current : cellRow;
            if (hasSpecial && next.Current == row) hasSpecial = next.MoveNext();
            WriteRowStart(w, main, ws, part, package, styles, row);
            for (; i < keys.Length && CellRef.FromKey(keys[i]).Row == row; i++)
            {
                var at = CellRef.FromKey(keys[i]);
                if (!columns.TryGetValue(at.Column, out var name)) columns[at.Column] = name = CellRef.ColumnName(at.Column);
                WriteCell(w, main, at, name, keys[i], cells[i], part, package, styles, strings, spills, rich, ws.Workbook.Styles);
            }

            w.WriteEndElement();
        }

        w.WriteEndElement();
    }

    private static void WriteRowStart(XmlWriter w, string main, Worksheet ws, XlsxSheetPart part, XlsxPackage package, StylesXmlWriter styles, int row)
    {
        w.WriteStartElement("row", main);
        w.WriteAttributeString("r", (row + 1).ToString(CultureInfo.InvariantCulture));
        var style = ws.RowStyle(row);
        if (style != 0 || part != null && part.OriginalRowXf.TryGetValue(row, out var o) && package.XfStyleIds[o] == style)
        {
            w.WriteAttributeString("s", Xf(package, part?.OriginalRowXf, row, style, styles).ToString(CultureInfo.InvariantCulture));
            w.WriteAttributeString("customFormat", "1");
        }

        if (ws.SizeOrNull(Axis.Row, row) is { } px)
        {
            w.WriteAttributeString("ht", Number(Math.Round(px * 72 / 96, 6)));
            w.WriteAttributeString("customHeight", "1");
        }

        if (ws.IsRowHidden(row)) w.WriteAttributeString("hidden", "1");
        if (part != null && part.RowAttributes.TryGetValue(row, out var extra))
            foreach (var a in extra)
                w.WriteAttributeString(a.Name.LocalName, a.Name.NamespaceName, a.Value);
    }

    private static void WriteCell(XmlWriter w, string main, CellRef at, string column, long key, Cell cell, XlsxSheetPart part, XlsxPackage package,
        StylesXmlWriter styles, SharedStringsWriter strings, Dictionary<long, CellRange> spills, Dictionary<int, string> rich, StyleTable table)
    {
        w.WriteStartElement("c", main);
        w.WriteAttributeString("r", column + (at.Row + 1).ToString(CultureInfo.InvariantCulture));
        var xf = Xf(package, part?.OriginalXf, key, cell?.StyleId ?? 0, styles);
        if (xf != 0) w.WriteAttributeString("s", xf.ToString(CultureInfo.InvariantCulture));
        if (cell == null)
        {
            w.WriteEndElement();
            return;
        }

        var value = cell.Value;
        var formula = cell.Formula;
        if (value.Kind == CellValueKind.Error && value.ErrorValue == CellError.Circ) value = CellValue.Number(0);
        if (package?.Date1904 == true && value.Kind == CellValueKind.Number && package.IsDateStyle(table, cell.StyleId))
            value = CellValue.Number(value.NumberValue - XlsxPackage.Date1904Offset);
        string text = null;
        var type = value.Kind switch
        {
            CellValueKind.Text when formula != null => "str",
            CellValueKind.Text => "s",
            CellValueKind.Boolean => "b",
            CellValueKind.Error => "e",
            _ => null
        };
        if (type == "s")
        {
            var index = part != null && part.RichStringCells.TryGetValue(key, out var source) && package.RichStrings.TryGetValue(source, out var si)
                                      && value.TextValue == (rich.TryGetValue(source, out var plain) ? plain : rich[source] = SheetXmlReader.Text(si, out _))
                ? strings.AddRich(source)
                : strings.Add(value.TextValue);
            text = index.ToString(CultureInfo.InvariantCulture);
        }

        if (type != null) w.WriteAttributeString("t", type);
        XAttribute[] extra = null;
        part?.CellAttributes.TryGetValue(key, out extra);
        string cm = null;
        CellRange? array = null;
        if (formula != null)
        {
            var originalCm = extra?.FirstOrDefault(a => a.Name == "cm")?.Value;
            (int Rows, int Columns)? mark = part != null && part.ArrayFormulas.TryGetValue(key, out var size) ? size : null;
            if (mark is { } legacy && (originalCm == null || !spills.ContainsKey(key)))
            {
                array = CellRange.FromBounds(at.Row, at.Column, Math.Min(at.Row + legacy.Rows, CellRef.MaxRows) - 1,
                    Math.Min(at.Column + legacy.Columns, CellRef.MaxColumns) - 1);
                cm = originalCm;
            }
            else if (spills.TryGetValue(key, out var spill))
            {
                array = spill;
                cm = originalCm ?? package?.DynamicArrayCm;
            }
        }

        if (cm != null) w.WriteAttributeString("cm", cm);
        foreach (var a in extra ?? [])
        {
            if (a.Name == "cm" || a.Name == "vm" && (formula != null || value.Kind != CellValueKind.Error)) continue;
            w.WriteAttributeString(a.Name.LocalName, a.Name.NamespaceName, a.Value);
        }

        if (formula != null)
        {
            w.WriteStartElement("f", main);
            if (array is { } arrayRange)
            {
                w.WriteAttributeString("t", "array");
                w.WriteAttributeString("ref", arrayRange.ToA1());
            }

            w.WriteString(SharedStringsWriter.Escape(FormulaText(formula), false));
            w.WriteEndElement();
        }

        text ??= value.Kind switch
        {
            CellValueKind.Number => Number(value.NumberValue),
            CellValueKind.Text => SharedStringsWriter.Escape(value.TextValue),
            CellValueKind.Boolean => value.BooleanValue ? "1" : "0",
            CellValueKind.Error => value.ToString(),
            _ => null
        };
        if (text != null) w.WriteElementString("v", main, text);
        w.WriteEndElement();
    }

    private static XElement SheetPr(XElement original, Worksheet ws, XNamespace ns, string[] theme)
    {
        var sheetPr = original == null ? new XElement(ns + "sheetPr") : new XElement(original);
        var tab = Child(sheetPr, "tabColor");
        if (tab != null && ThemeColors.Resolve(tab, theme, ThemeColors.Indexed) == ws.TabColor) return sheetPr;
        tab?.Remove();
        if (ws.TabColor != null) sheetPr.AddFirst(new XElement(ns + "tabColor", new XAttribute("rgb", "FF" + ws.TabColor.TrimStart('#').ToUpperInvariant())));
        return original == null && !sheetPr.HasElements ? null : sheetPr;
    }

    private static XElement SheetViews(XElement original, Worksheet ws, XNamespace ns)
    {
        var views = original == null ? new XElement(ns + "sheetViews", new XElement(ns + "sheetView", new XAttribute("workbookViewId", 0))) : new XElement(original);
        var view = Child(views, "sheetView");
        if (view == null) return views;
        var pane = Child(view, "pane");
        var frozen = (string)pane?.Attribute("state") is "frozen" or "frozenSplit";
        if (frozen && SheetXmlReader.Frozen(pane) == ws.FreezeAt || !frozen && ws.FreezeAt == null) return views;
        if (frozen || ws.FreezeAt != null) pane?.Remove();
        view.Elements(ns + "selection").Where(s => s.Attribute("pane") != null).Remove();
        if (ws.FreezeAt is not { } at) return views;
        var active = at.Row > 0 && at.Column > 0 ? "bottomRight" : at.Row > 0 ? "bottomLeft" : "topRight";
        view.AddFirst(new XElement(ns + "pane",
            at.Column > 0 ? new XAttribute("xSplit", at.Column) : null,
            at.Row > 0 ? new XAttribute("ySplit", at.Row) : null,
            new XAttribute("topLeftCell", at.Relative.ToA1()), new XAttribute("activePane", active), new XAttribute("state", "frozen")),
            new XElement(ns + "selection", new XAttribute("pane", active)));
        return views;
    }

    private static XElement SheetFormatPr(XElement original, Worksheet ws, XNamespace ns)
    {
        var format = original == null ? new XElement(ns + "sheetFormatPr", new XAttribute("defaultRowHeight", 15)) : new XElement(original);
        if (SheetXmlReader.DefaultRowHeight(format) != ws.DefaultRowHeight)
        {
            format.SetAttributeValue("defaultRowHeight", Number(Math.Round(ws.DefaultRowHeight * 72 / 96, 6)));
            format.SetAttributeValue("customHeight", 1);
        }

        if (SheetXmlReader.DefaultColumnWidth(format) != ws.DefaultColumnWidth)
            format.SetAttributeValue("defaultColWidth", Number(PixelsToWidth(ws.DefaultColumnWidth)));
        return format;
    }

    private static XElement Cols(XElement original, Worksheet ws, XlsxSheetPart part, XlsxPackage package, StylesXmlWriter styles, XNamespace ns)
    {
        var source = new Dictionary<int, XElement>();
        foreach (var col in original?.Elements(ns + "col") ?? [])
        {
            var (min, max) = SheetXmlReader.ColumnSpan(col);
            for (var c = min; c <= max; c++) source[c] = col;
        }

        var candidates = new SortedSet<int>(ws.ColumnWidths.Keys);
        candidates.UnionWith(ws.HiddenColumns);
        candidates.UnionWith(ws.ColumnStyles.Keys);
        candidates.UnionWith(source.Keys);
        if (part != null) candidates.UnionWith(part.OriginalColXf.Keys);
        var result = new List<XElement>();
        XElement current = null;
        string currentKey = null;
        var last = -2;
        foreach (var c in candidates)
        {
            var hidden = ws.IsColumnHidden(c);
            var xf = Xf(package, part?.OriginalColXf, c, ws.ColumnStyle(c), styles);
            source.TryGetValue(c, out var from);
            var originalWidth = from == null ? null : SheetXmlReader.Finite((string)from.Attribute("width"));
            XAttribute width, custom;
            if (originalWidth is { } ow && (SheetXmlReader.ColumnWidthToPixels(ow) == ws.ColumnWidth(c)
                                            || ws.SizeOrNull(Axis.Column, c) == null && hidden && ow <= 0))
            {
                width = from.Attribute("width");
                custom = from.Attribute("customWidth");
            }
            else
            {
                width = new XAttribute("width", Number(PixelsToWidth(ws.ColumnWidth(c))));
                custom = ws.SizeOrNull(Axis.Column, c) == null ? null : new XAttribute("customWidth", 1);
            }

            if (!hidden && xf == 0 && from == null && custom == null) continue;
            var extra = from?.Attributes().Where(a => a.Name.LocalName is not ("min" or "max" or "width" or "customWidth" or "style" or "hidden")).ToList() ?? [];
            var key = string.Join('\u0001', new object[] { width.Value, custom?.Value, xf, hidden }.Concat(extra.Select(a => a.ToString())));
            if (current != null && c == last + 1 && key == currentKey)
            {
                current.SetAttributeValue("max", c + 1);
                last = c;
                continue;
            }

            current = new XElement(ns + "col", new XAttribute("min", c + 1), new XAttribute("max", c + 1), new XAttribute(width),
                xf != 0 ? new XAttribute("style", xf) : null, hidden ? new XAttribute("hidden", 1) : null, custom == null ? null : new XAttribute(custom),
                extra.Select(a => new XAttribute(a)));
            result.Add(current);
            currentKey = key;
            last = c;
        }

        return result.Count == 0 ? null : new XElement(ns + "cols", result);
    }
}
