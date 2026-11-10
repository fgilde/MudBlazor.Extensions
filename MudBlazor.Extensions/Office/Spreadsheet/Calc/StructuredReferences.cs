using System.Text;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class StructuredReferences
{
    private const StringComparison Ignore = StringComparison.OrdinalIgnoreCase;
    private const string ThisRowItem = "#This Row";

    private static readonly (string Text, StructuredItem Item)[] Keywords =
        [("#All", StructuredItem.All), ("#Headers", StructuredItem.Headers), ("#Data", StructuredItem.Data), ("#Totals", StructuredItem.Totals), (ThisRowItem, StructuredItem.ThisRow)];

    internal static StructuredRefNode Parse(string text)
    {
        var open = text.IndexOf('[');
        if (open < 0 || text[^1] != ']') throw new FormatException($"'{text}' is not a structured reference.");
        var table = open == 0 ? null : text[..open];
        var raw = text[(open + 1)..^1];
        var inner = raw.Trim();
        if (inner.Length == 0) return new StructuredRefNode(table, null, null, StructuredItem.Data);
        if (inner[0] == '@')
        {
            var rest = inner[1..].Trim();
            var (c1, c2) = rest.Length == 0 ? (null, null) : rest[0] == '[' ? Columns(Groups(rest)) : (Unescape(rest), null);
            return new StructuredRefNode(table, c1, c2, StructuredItem.ThisRow);
        }

        if (inner[0] == '#') return new StructuredRefNode(table, null, null, Item(inner));
        if (inner[0] != '[') return new StructuredRefNode(table, Unescape(raw), null, StructuredItem.Data);
        var groups = Groups(inner);
        StructuredItem items = 0;
        foreach (var g in groups.Where(g => g.Second == null && g.First.StartsWith('#'))) items |= Item(g.First);
        var (first, second) = Columns(groups.Where(g => g.Second != null || !g.First.StartsWith('#')).ToList());
        return new StructuredRefNode(table, first, second, items == 0 ? StructuredItem.Data : items);
    }

    internal static string Format(StructuredRefNode node, bool file)
    {
        var sb = new StringBuilder(node.Table);
        var columns = node.Column1 == null ? null
            : node.Column2 == null ? "[" + Escape(node.Column1) + "]"
            : "[" + Escape(node.Column1) + "]:[" + Escape(node.Column2) + "]";
        if (node.Items == StructuredItem.ThisRow && !file)
            return sb.Append("[@").Append(node.Column2 == null && node.Column1 != null && node.Column1.All(c => char.IsLetterOrDigit(c) || c == '_')
                ? node.Column1
                : columns).Append(']').ToString();
        if (node.Items == StructuredItem.Data && node.Column2 == null && Plain(node.Column1))
            return sb.Append('[').Append(node.Column1).Append(']').ToString();
        var parts = new List<string>();
        if (node.Items == StructuredItem.All) parts.Add("[#All]");
        else if (node.Items != StructuredItem.Data || columns == null)
            parts.AddRange(Keywords.Where(k => k.Item != StructuredItem.All && (node.Items & k.Item) != 0).Select(k => "[" + k.Text + "]"));
        if (columns != null) parts.Add(columns);
        return (parts.Count == 1 && columns == null ? sb.Append(parts[0]) : sb.Append('[').AppendJoin(',', parts).Append(']')).ToString();
    }

    private static bool Plain(string column) => column == null || column.All(c => char.IsLetterOrDigit(c) || c is ' ' or '_');

    internal static SheetTable Find(Workbook wb, Worksheet host, CellRef cell, StructuredRefNode node, out Worksheet sheet)
    {
        if (node.Table != null) return wb.FindTable(node.Table, out sheet);
        sheet = host;
        return host?.TableAt(cell);
    }

    internal static CalcValue Resolve(Workbook wb, Worksheet host, CellRef cell, StructuredRefNode node)
    {
        var table = Find(wb, host, cell, node, out var sheet);
        if (table == null) return CalcValue.Error(CellError.Ref);
        return Area(table, cell, node) is { } range ? CalcValue.FromReference([new RefArea(sheet, range)])
            : CalcValue.Error(node.Items == StructuredItem.ThisRow && Columns(table, node) != null ? CellError.Value : CellError.Ref);
    }

    private static CellRange? Area(SheetTable table, CellRef cell, StructuredRefNode node)
    {
        if (Columns(table, node) is not var (left, right)) return null;
        var data = table.DataRange;
        var items = node.Items;
        if (items == StructuredItem.ThisRow)
            return cell.Row >= data.Top && cell.Row <= data.Bottom ? CellRange.FromBounds(cell.Row, left, cell.Row, right) : null;
        var headers = (items & StructuredItem.Headers) != 0 && table.HeaderRow;
        var totals = (items & StructuredItem.Totals) != 0 && table.TotalsRow;
        var rows = (items & StructuredItem.Data) != 0;
        if (!headers && !totals && !rows || (items & StructuredItem.All) == (StructuredItem.Headers | StructuredItem.Totals)) return null;
        var top = headers ? table.Range.Top : rows ? data.Top : table.Range.Bottom;
        var bottom = totals ? table.Range.Bottom : rows ? data.Bottom : table.Range.Top;
        return CellRange.FromBounds(top, left, bottom, right);
    }

    private static (int Left, int Right)? Columns(SheetTable table, StructuredRefNode node)
    {
        if (node.Column1 == null) return (table.Range.Left, table.Range.Right);
        var a = table.ColumnIndex(node.Column1);
        var b = node.Column2 == null ? a : table.ColumnIndex(node.Column2);
        if (a < 0 || b < 0) return null;
        return (table.Range.Left + Math.Min(a, b), table.Range.Left + Math.Max(a, b));
    }

    internal static string RequireTable(string formula, bool insideTable) =>
        insideTable || formula == null || !formula.Contains('[')
        || !FormulaLexer.Tokenize(formula, true).Any(t => t.Kind == FormulaTokenKind.StructuredReference && TryParse(t.Text) is { Table: null })
            ? formula
            : throw new ArgumentException($"'{formula}' uses a structured reference without a table name such as [Col] or [@Col]; that only works in a cell of a table. Write Table1[Col] instead.");

    internal static string ToDisplay(string formula) =>
        formula == null || !formula.Contains(ThisRowItem, Ignore) ? formula
        : Rewrite(formula, t => t.Text.Contains(ThisRowItem, Ignore) && TryParse(t.Text) is { } node ? Format(node, false) : null);

    internal static string ToFile(string formula, string table) =>
        formula == null || !formula.Contains('[') ? formula
        : Rewrite(formula, t => TryParse(t.Text) is not { } node ? null
            : node.Table == null && table == null ? "#REF!"
            : node.Table == null || node.Items == StructuredItem.ThisRow || !Plain(node.Column1) || !Plain(node.Column2) ? Format(node with { Table = node.Table ?? table }, true)
            : null);

    internal static string RenameTable(string formula, string oldName, string newName) =>
        formula == null || !formula.Contains(oldName, Ignore) ? formula
        : Rewrite(formula, t => t.Kind == FormulaTokenKind.StructuredReference && t.Text.IndexOf('[') == oldName.Length && t.Text.StartsWith(oldName, Ignore)
            ? newName + t.Text[oldName.Length..]
            : t.Kind == FormulaTokenKind.Name && t.Text.Equals(oldName, Ignore) ? newName : null, true);

    internal static string RenameColumns(string formula, Func<StructuredRefNode, bool> target, IReadOnlyDictionary<string, string> names) =>
        formula == null || !formula.Contains('[') ? formula
        : Rewrite(formula, t =>
        {
            if (TryParse(t.Text) is not { } node || !target(node)) return null;
            var renamed = node with
            {
                Column1 = node.Column1 != null && names.TryGetValue(node.Column1, out var a) ? a : node.Column1,
                Column2 = node.Column2 != null && names.TryGetValue(node.Column2, out var b) ? b : node.Column2
            };
            return renamed == node ? null : Format(renamed, t.Text.Contains(ThisRowItem, Ignore));
        });

    internal static string Replace(string formula, string table, Func<StructuredRefNode, string> replace) =>
        formula == null || !formula.Contains('[') && !formula.Contains(table, Ignore) ? formula
        : Rewrite(formula, t => t.Kind == FormulaTokenKind.Name
            ? t.Text.Equals(table, Ignore) ? replace(new StructuredRefNode(table, null, null, StructuredItem.Data)) : null
            : TryParse(t.Text) is { } node ? replace(node) : null, true);

    internal static string Signature(Workbook wb) =>
        string.Join('|', wb.Sheets.SelectMany(s => s.Tables.Select(t =>
            $"{t.Name}!{t.Range.ToA1()}!{t.HeaderRow}{t.TotalsRow}!{string.Join('\u0001', t.Columns.Select(c => c.Name))}")));

    private static StructuredRefNode TryParse(string text)
    {
        try
        {
            return Parse(text);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string Rewrite(string formula, Func<FormulaToken, string> rewrite, bool names = false)
    {
        StringBuilder sb = null;
        var last = 0;
        foreach (var t in FormulaLexer.Tokenize(formula, true))
        {
            if (t.Kind != FormulaTokenKind.StructuredReference && !(names && t.Kind == FormulaTokenKind.Name)) continue;
            var text = rewrite(t);
            if (text == null) continue;
            sb ??= new StringBuilder(formula.Length + 16);
            sb.Append(formula, last, t.Start - last).Append(text);
            last = t.Start + t.Text.Length;
        }

        return sb == null ? formula : sb.Append(formula, last, formula.Length - last).ToString();
    }

    private static StructuredItem Item(string keyword)
    {
        foreach (var (text, item) in Keywords)
            if (string.Equals(text, keyword.Trim(), Ignore) || text == ThisRowItem && keyword.Replace(" ", "").Equals("#ThisRow", Ignore))
                return item;
        throw new FormatException($"Unknown table item '{keyword}'.");
    }

    private static (string, string) Columns(List<(string First, string Second)> groups) => groups.Count switch
    {
        0 => (null, null),
        1 => groups[0],
        _ => throw new FormatException("A structured reference can name only one column or column range.")
    };

    private static List<(string First, string Second)> Groups(string s)
    {
        var list = new List<(string, string)>();
        var i = 0;
        while (i < s.Length)
        {
            var first = Group(s, ref i);
            string second = null;
            Skip(s, ref i);
            if (i < s.Length && s[i] == ':')
            {
                i++;
                Skip(s, ref i);
                second = Group(s, ref i);
                Skip(s, ref i);
            }

            list.Add((first, second));
            if (i == s.Length) break;
            if (s[i] != ',') throw new FormatException($"Unexpected '{s[i]}' in structured reference.");
            i++;
            Skip(s, ref i);
        }

        return list;
    }

    private static string Group(string s, ref int i)
    {
        if (i >= s.Length || s[i] != '[') throw new FormatException("Expected '[' in structured reference.");
        var sb = new StringBuilder();
        for (i++; i < s.Length; i++)
        {
            if (s[i] == '\'' && i + 1 < s.Length) sb.Append(s[++i]);
            else if (s[i] == ']')
            {
                i++;
                return sb.ToString();
            }
            else if (s[i] == '[') throw new FormatException("Unexpected '[' in structured reference.");
            else sb.Append(s[i]);
        }

        throw new FormatException("Unterminated structured reference.");
    }

    private static void Skip(string s, ref int i)
    {
        while (i < s.Length && s[i] == ' ') i++;
    }

    private static string Unescape(string s)
    {
        if (s.IndexOfAny(['\'', '[', ']']) < 0) return s;
        var sb = new StringBuilder(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == '\'' && i + 1 < s.Length) sb.Append(s[++i]);
            else if (s[i] is '[' or ']') throw new FormatException("Unexpected bracket in structured reference.");
            else sb.Append(s[i]);
        }

        return sb.ToString();
    }

    private static string Escape(string s)
    {
        if (s.IndexOfAny(['\'', '[', ']', '#']) < 0) return s;
        var sb = new StringBuilder(s.Length + 4);
        foreach (var c in s)
        {
            if (c is '\'' or '[' or ']' or '#') sb.Append('\'');
            sb.Append(c);
        }

        return sb.ToString();
    }
}
