using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>One cell of an HTML table read by <see cref="HtmlTableFormat"/>.</summary>
/// <param name="Row">Zero-based row in the table.</param>
/// <param name="Column">Zero-based column in the table, after the columns taken by spans of earlier cells.</param>
/// <param name="Value">The value.</param>
/// <param name="Formula">The formula without the leading equals sign, or null.</param>
/// <param name="Style">
/// The style read from the allowed CSS: bold, italic, underline, strikethrough, font color, fill, alignment, wrapping and number format.
/// Everything else keeps the default.
/// </param>
/// <param name="RowSpan">Number of rows the cell spans.</param>
/// <param name="ColumnSpan">Number of columns the cell spans.</param>
/// <param name="Source">
/// The cell the formula was copied from, when the HTML names it (tables written by <see cref="HtmlTableFormat.Write"/> do); a paste moves the
/// formula's relative references from there to the target. Null for other sources, whose formulas are pasted as written.
/// </param>
public sealed record HtmlTableCell(int Row, int Column, CellValue Value, string Formula, CellStyle Style, int RowSpan, int ColumnSpan, CellRef? Source = null);

/// <summary>A table read by <see cref="HtmlTableFormat.Read"/>.</summary>
/// <param name="Cells">The cells in document order.</param>
/// <param name="RowCount">Number of rows including spans.</param>
/// <param name="ColumnCount">Number of columns including spans.</param>
public sealed record HtmlTable(IReadOnlyList<HtmlTableCell> Cells, int RowCount, int ColumnCount);

/// <summary>
/// Reads and writes the HTML tables that Excel, Google Sheets and browsers put on the clipboard. The reader is a tolerant tokenizer for
/// the not well-formed HTML of Excel: it never runs or renders anything, drops scripts, styles, frames and every attribute or CSS
/// property outside a short list of formatting properties.
/// </summary>
public static class HtmlTableFormat
{
    /// <summary>Largest HTML text that is read, in characters.</summary>
    public const int MaxLength = 256 * 1024 * 1024;

    private const int MaxColumnSpan = 1000;
    private const int MaxRowSpan = 65534;

    private static readonly HashSet<string> RawText = new(StringComparer.Ordinal)
        { "script", "style", "textarea", "title", "noscript", "iframe", "noembed", "noframes", "template", "xmp", "svg", "math", "object" };

    private static readonly Regex SheetsType = new("\"1\"\\s*:\\s*(\\d+)", RegexOptions.CultureInvariant);
    private static readonly Regex SheetsNumber = new("\"3\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?(?:[eE][+-]?\\d+)?)", RegexOptions.CultureInvariant);
    private static readonly Regex CssComment = new(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.CultureInvariant);

    /// <summary>
    /// Reads the first HTML table that has cells. Values come from Excel's <c>x:num</c>, <c>x:bool</c>, <c>x:str</c> and <c>x:fmla</c>
    /// attributes or Google Sheets' <c>data-sheets-value</c> where present; other text is parsed like typed input in <paramref name="culture"/>.
    /// </summary>
    /// <param name="html">The HTML, for example the <c>text/html</c> part of the clipboard.</param>
    /// <param name="culture">The culture for parsing numbers and dates; null for the current culture.</param>
    /// <returns>The table, or null if the HTML holds no table cell.</returns>
    /// <exception cref="ArgumentException">The HTML is longer than <see cref="MaxLength"/>.</exception>
    public static HtmlTable Read(string html, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(html)) return null;
        if (html.Length > MaxLength) throw new ArgumentException("The HTML is too large to paste.", nameof(html));
        var reader = new TableReader(culture ?? CultureInfo.CurrentCulture);
        Tokenize(html, reader);
        return reader.Result();
    }

    internal static void Tokenize(string html, IHtmlTokenSink sink)
    {
        var i = 0;
        while (i < html.Length && !sink.Done)
        {
            var lt = html.IndexOf('<', i);
            if (lt < 0)
            {
                sink.Text(html[i..]);
                break;
            }

            if (lt > i) sink.Text(html[i..lt]);
            i = ReadTag(html, lt, sink);
        }
    }

    /// <summary>
    /// Writes a range as a minimal HTML table that Excel and Google Sheets paste with values, number formats, fonts, fills, alignment and
    /// merged cells. Rows hidden by the auto filter are left out, as Excel copies only the visible rows of a filtered range.
    /// </summary>
    /// <param name="sheet">The sheet.</param>
    /// <param name="range">The range to write.</param>
    /// <param name="culture">The culture for the shown texts; null for the current culture.</param>
    /// <returns>The HTML document.</returns>
    public static string Write(Worksheet sheet, CellRange range, CultureInfo culture)
    {
        culture ??= CultureInfo.CurrentCulture;
        var r = range.Normalized;
        var rows = (sheet.FilteredRows.Count > 0 ? sheet.VisibleRowsIn(r) : Enumerable.Range(r.Top, r.RowCount)).ToList();
        var merges = sheet.Merges.Select(m => m.Normalized).Where(m => m.Intersects(r)).ToList();
        var sb = new StringBuilder("<html xmlns:x=\"urn:schemas-microsoft-com:office:excel\"><head><meta charset=\"utf-8\"></head><body><table style=\"border-collapse:collapse\">");
        foreach (var row in rows)
        {
            sb.Append("<tr>");
            for (var col = r.Left; col <= r.Right; col++)
            {
                var at = new CellRef(row, col);
                int rowSpan = 1, colSpan = 1;
                if (MergeAt(merges, at) is { } m)
                {
                    if (row != rows.First(x => x >= m.Top) || col != Math.Max(m.Left, r.Left)) continue;
                    colSpan = Math.Min(m.Right, r.Right) - col + 1;
                    rowSpan = rows.Count(x => x >= row && x <= m.Bottom);
                }

                WriteCell(sb, sheet, at, rowSpan, colSpan, culture);
            }

            sb.Append("</tr>");
        }

        return sb.Append("</table></body></html>").ToString();
    }

    private static CellRange? MergeAt(List<CellRange> merges, CellRef at)
    {
        foreach (var m in merges)
            if (m.Contains(at)) return m;
        return null;
    }

    private static void WriteCell(StringBuilder sb, Worksheet sheet, CellRef at, int rowSpan, int colSpan, CultureInfo culture)
    {
        var cell = sheet[at];
        var style = sheet.Workbook.Styles[sheet.EffectiveStyleId(at)];
        var value = cell?.HasContent == true ? cell.Value : CellValue.Empty;
        sb.Append("<td");
        if (colSpan > 1) sb.Append(" colspan=\"").Append(colSpan).Append('"');
        if (rowSpan > 1) sb.Append(" rowspan=\"").Append(rowSpan).Append('"');
        var css = Css(style);
        if (css.Length > 0) sb.Append(" style=\"").Append(WebUtility.HtmlEncode(css)).Append('"');
        if (value.Kind == CellValueKind.Number) sb.Append(" x:num=\"").Append(value.NumberValue.ToString("R", CultureInfo.InvariantCulture)).Append('"');
        else if (value.Kind == CellValueKind.Boolean) sb.Append(" x:bool=\"").Append(value.BooleanValue ? "TRUE" : "FALSE").Append('"');
        else if (value.Kind == CellValueKind.Text) sb.Append(" x:str");
        if (cell?.Formula != null)
            sb.Append(" x:fmla=\"=").Append(WebUtility.HtmlEncode(cell.Formula)).Append("\" data-mudex-at=\"").Append(at.ToA1()).Append('"');
        sb.Append('>');
        if (value.Kind != CellValueKind.Empty)
            sb.Append(WebUtility.HtmlEncode(NumberFormat.Format(value, style.NumberFormat, culture, out _)).Replace("\r\n", "\n").Replace("\n", "<br>"));
        sb.Append("</td>");
    }

    private static string Css(CellStyle s)
    {
        var css = new StringBuilder();
        var font = new string((s.Font.Name ?? "").Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-').ToArray()).Trim();
        if (font.Length > 0) css.Append("font-family:").Append(font).Append(';');
        css.Append("font-size:").Append(s.Font.Size.ToString("0.##", CultureInfo.InvariantCulture)).Append("pt;");
        if (s.Font.Bold) css.Append("font-weight:700;");
        if (s.Font.Italic) css.Append("font-style:italic;");
        if (s.Font.Underline || s.Font.Strikethrough)
            css.Append("text-decoration:").Append(string.Join(' ', new[] { s.Font.Underline ? "underline" : null, s.Font.Strikethrough ? "line-through" : null }.Where(x => x != null))).Append(';');
        if (Color(s.Font.Color?.ToLowerInvariant()) is { } color) css.Append("color:").Append(color).Append(';');
        if (Color(s.Fill.Color?.ToLowerInvariant()) is { } fill) css.Append("background:").Append(fill).Append(';');
        if (s.Alignment.Horizontal != CellHorizontalAlignment.General) css.Append("text-align:").Append(s.Alignment.Horizontal.ToString().ToLowerInvariant()).Append(';');
        if (s.Alignment.Vertical != CellVerticalAlignment.Bottom) css.Append("vertical-align:").Append(s.Alignment.Vertical == CellVerticalAlignment.Top ? "top" : "middle").Append(';');
        if (s.Alignment.WrapText) css.Append("white-space:normal;");
        if (!string.IsNullOrEmpty(s.NumberFormat) && s.NumberFormat != NumberFormat.General)
            css.Append("mso-number-format:\"").Append(s.NumberFormat.Replace("\\", "\\\\").Replace("\"", "\\0022 ")).Append("\";");
        return css.ToString();
    }

    private static int ReadTag(string html, int at, IHtmlTokenSink reader)
    {
        if (string.CompareOrdinal(html, at, "<!--", 0, 4) == 0)
        {
            var end = html.IndexOf("-->", at + 4, StringComparison.Ordinal);
            return end < 0 ? html.Length : end + 3;
        }

        if (at + 1 < html.Length && html[at + 1] is '!' or '?')
        {
            var gt = html.IndexOf('>', at);
            return gt < 0 ? html.Length : gt + 1;
        }

        var closing = at + 1 < html.Length && html[at + 1] == '/';
        var start = at + (closing ? 2 : 1);
        if (start >= html.Length || !char.IsAsciiLetter(html[start]))
        {
            reader.Text("<");
            return at + 1;
        }

        var nameEnd = start;
        while (nameEnd < html.Length && (char.IsAsciiLetterOrDigit(html[nameEnd]) || html[nameEnd] is ':' or '-' or '_')) nameEnd++;
        var name = html[start..nameEnd].ToLowerInvariant();
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var next = ReadAttributes(html, nameEnd, attributes, out var selfClosing);
        if (closing)
        {
            reader.End(name);
            return next;
        }

        if (RawText.Contains(name) && !selfClosing)
        {
            var end = html.IndexOf("</" + name, next, StringComparison.OrdinalIgnoreCase);
            if (name == "style") reader.StyleSheet(end < 0 ? html[next..] : html[next..end]);
            if (end < 0) return html.Length;
            var gt = html.IndexOf('>', end);
            return gt < 0 ? html.Length : gt + 1;
        }

        reader.Start(name, attributes);
        return next;
    }

    private static int ReadAttributes(string html, int i, Dictionary<string, string> attributes, out bool selfClosing)
    {
        selfClosing = false;
        while (i < html.Length)
        {
            var ch = html[i];
            if (ch == '>') return i + 1;
            if (ch == '/' || char.IsWhiteSpace(ch))
            {
                selfClosing = ch == '/' && i + 1 < html.Length && html[i + 1] == '>';
                i++;
                continue;
            }

            var nameStart = i;
            while (i < html.Length && !char.IsWhiteSpace(html[i]) && html[i] is not ('=' or '>' or '/')) i++;
            var name = html[nameStart..i];
            while (i < html.Length && char.IsWhiteSpace(html[i])) i++;
            var value = "";
            if (i < html.Length && html[i] == '=')
            {
                i++;
                while (i < html.Length && char.IsWhiteSpace(html[i])) i++;
                if (i < html.Length && html[i] is '"' or '\'')
                {
                    var end = html.IndexOf(html[i], i + 1);
                    if (end < 0) end = html.Length;
                    value = html[(i + 1)..end];
                    i = Math.Min(html.Length, end + 1);
                }
                else
                {
                    var valueStart = i;
                    while (i < html.Length && !char.IsWhiteSpace(html[i]) && html[i] != '>') i++;
                    value = html[valueStart..i];
                }
            }

            if (name.Length > 0) attributes.TryAdd(name, WebUtility.HtmlDecode(value));
        }

        return i;
    }

    private static CellStyle ApplyCss(CellStyle s, string declarations, CultureInfo culture)
    {
        foreach (var declaration in Split(declarations, ';'))
        {
            var colon = declaration.IndexOf(':');
            if (colon <= 0) continue;
            var property = declaration[..colon].Trim().ToLowerInvariant();
            var value = declaration[(colon + 1)..].Trim();
            var important = value.IndexOf("!important", StringComparison.OrdinalIgnoreCase);
            if (important >= 0) value = value[..important].Trim();
            var v = value.ToLowerInvariant();
            s = property switch
            {
                "font-weight" => s with { Font = s.Font with { Bold = v is "bold" or "bolder" || (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var weight) && weight >= 600) } },
                "font-style" => s with { Font = s.Font with { Italic = v is "italic" or "oblique" } },
                "text-decoration" or "text-decoration-line" => s with { Font = s.Font with { Underline = v.Contains("underline"), Strikethrough = v.Contains("line-through") } },
                "color" => s with { Font = s.Font with { Color = Color(v) } },
                "background" or "background-color" => s with { Fill = new CellFill { Color = Split(v, ' ').Select(Color).FirstOrDefault(c => c != null) } },
                "text-align" => s with { Alignment = s.Alignment with { Horizontal = Horizontal(v) ?? s.Alignment.Horizontal } },
                "vertical-align" => s with { Alignment = s.Alignment with { Vertical = Vertical(v) ?? s.Alignment.Vertical } },
                "white-space" => s with { Alignment = s.Alignment with { WrapText = v is "normal" or "pre-wrap" or "pre-line" or "break-spaces" } },
                "mso-number-format" => NumberFormatCode(value, culture) is { } code ? s with { NumberFormat = code } : s,
                _ => s
            };
        }

        return s;
    }

    private static CellHorizontalAlignment? Horizontal(string v) => v switch
    {
        "general" => CellHorizontalAlignment.General,
        "left" or "start" => CellHorizontalAlignment.Left,
        "center" => CellHorizontalAlignment.Center,
        "right" or "end" => CellHorizontalAlignment.Right,
        "justify" => CellHorizontalAlignment.Justify,
        _ => null
    };

    private static CellVerticalAlignment? Vertical(string v) => v switch
    {
        "top" => CellVerticalAlignment.Top,
        "middle" => CellVerticalAlignment.Center,
        "bottom" => CellVerticalAlignment.Bottom,
        _ => null
    };

    internal static IEnumerable<string> Split(string text, char separator)
    {
        var start = 0;
        var depth = 0;
        var quote = '\0';
        for (var i = 0; i <= text.Length; i++)
        {
            if (i < text.Length)
            {
                var c = text[i];
                if (c == '\\' && i + 1 < text.Length)
                {
                    i++;
                    continue;
                }

                if (quote != '\0')
                {
                    if (c == quote) quote = '\0';
                    continue;
                }

                if (c is '"' or '\'') quote = c;
                else if (c == '(') depth++;
                else if (c == ')') depth = Math.Max(0, depth - 1);
                if (depth > 0 || (c != separator && !(separator == ' ' && char.IsWhiteSpace(c)))) continue;
            }

            if (i > start) yield return text[start..Math.Min(i, text.Length)];
            start = i + 1;
        }
    }

    internal static string Color(string v)
    {
        if (string.IsNullOrEmpty(v)) return null;
        if (v[0] == '#')
        {
            var hex = v[1..];
            if (!hex.All(char.IsAsciiHexDigit)) return null;
            if (hex.Length == 3) hex = string.Concat(hex.Select(c => new string(c, 2)));
            return hex.Length == 6 ? "#" + hex.ToUpperInvariant() : null;
        }

        if (v.StartsWith("rgb", StringComparison.Ordinal) && v.IndexOf('(') is var open and > 0 && v.EndsWith(')'))
        {
            var parts = v[(open + 1)..^1].Split([',', ' ', '/'], StringSplitOptions.RemoveEmptyEntries);
            var rgb = parts.Take(3).Select(p => byte.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out var b) ? b : (int?)null).ToList();
            if (rgb.Count != 3 || rgb.Any(b => b == null)) return null;
            if (parts.Length > 3 && double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var alpha) && alpha == 0) return null;
            return $"#{rgb[0]:X2}{rgb[1]:X2}{rgb[2]:X2}";
        }

        if (!v.All(char.IsAsciiLetter)) return null;
        var named = System.Drawing.Color.FromName(v);
        return named.IsKnownColor && !named.IsSystemColor && named.A == 255 ? $"#{named.R:X2}{named.G:X2}{named.B:X2}" : null;
    }

    private static string NumberFormatCode(string value, CultureInfo culture)
    {
        var v = value.Trim();
        if (v.Length > 1 && v[0] is '"' or '\'')
        {
            var end = 1;
            while (end < v.Length && v[end] != v[0]) end += v[end] == '\\' ? 2 : 1;
            v = v[1..Math.Min(end, v.Length)];
        }

        var code = Unescape(v).Trim() switch
        {
            var n when n.Equals("General", StringComparison.OrdinalIgnoreCase) => NumberFormat.General,
            var n when n.Equals("Fixed", StringComparison.OrdinalIgnoreCase) => "0.00",
            var n when n.Equals("Standard", StringComparison.OrdinalIgnoreCase) => "#,##0.00",
            var n when n.Equals("Percent", StringComparison.OrdinalIgnoreCase) => "0.00%",
            var n when n.Equals("Scientific", StringComparison.OrdinalIgnoreCase) => "0.00E+00",
            var n when n.Equals("Short Date", StringComparison.OrdinalIgnoreCase) => BuiltInNumberFormats.Get(14, culture),
            var n when n.Equals("Medium Date", StringComparison.OrdinalIgnoreCase) => "d-mmm-yy",
            var n when n.Equals("Long Date", StringComparison.OrdinalIgnoreCase) => "dddd, mmmm d, yyyy",
            var n when n.Equals("Short Time", StringComparison.OrdinalIgnoreCase) => "h:mm",
            var n when n.Equals("Medium Time", StringComparison.OrdinalIgnoreCase) => "h:mm AM/PM",
            var n when n.Equals("Long Time", StringComparison.OrdinalIgnoreCase) => "h:mm:ss AM/PM",
            var n => n
        };
        return code.Length is > 0 and <= 255 ? code : null;
    }

    private static string Unescape(string s)
    {
        var sb = new StringBuilder(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] != '\\' || i + 1 >= s.Length)
            {
                sb.Append(s[i]);
                continue;
            }

            var j = i + 1;
            var hexEnd = j;
            while (hexEnd < s.Length && hexEnd - j < 4 && char.IsAsciiHexDigit(s[hexEnd])) hexEnd++;
            if (hexEnd == j)
            {
                sb.Append(s[j]);
                i = j;
                continue;
            }

            var code = int.Parse(s[j..hexEnd], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            sb.Append(code is > 0 and < 0xD800 or > 0xDFFF ? (char)code : '�');
            i = hexEnd < s.Length && s[hexEnd] == ' ' ? hexEnd : hexEnd - 1;
        }

        return sb.ToString();
    }

    private sealed class TableReader(CultureInfo culture) : IHtmlTokenSink
    {
        private readonly List<HtmlTableCell> _cells = [];
        private readonly List<int> _busyUntil = [];
        private readonly Dictionary<string, string> _rules = new(StringComparer.OrdinalIgnoreCase);
        private int _depth;
        private int _row = -1;
        private int _column;
        private int _rows;
        private int _columns;
        private OpenCell _cell;

        public bool Done { get; private set; }

        public HtmlTable Result()
        {
            Close();
            return _cells.Count == 0 ? null : new HtmlTable(_cells, _rows, _columns);
        }

        public void StyleSheet(string css)
        {
            css = CssComment.Replace(css.Replace("<!--", " ").Replace("-->", " "), " ");
            var i = 0;
            while (i < css.Length)
            {
                var open = css.IndexOf('{', i);
                if (open < 0) return;
                var selector = css[i..open].Trim();
                var depth = 1;
                var j = open + 1;
                while (j < css.Length && depth > 0)
                {
                    if (css[j] == '{') depth++;
                    else if (css[j] == '}') depth--;
                    j++;
                }

                var body = css[(open + 1)..Math.Max(open + 1, depth == 0 ? j - 1 : j)];
                i = j;
                if (selector.StartsWith('@') || body.Contains('{')) continue;
                foreach (var part in selector.Split(','))
                    if (RuleKey(part.Trim()) is { } key)
                        _rules[key] = _rules.GetValueOrDefault(key) + ";" + body;
            }
        }

        private static string RuleKey(string selector)
        {
            var s = selector.ToLowerInvariant();
            if (s is "td" or "th") return s;
            var dot = s.IndexOf('.');
            if (dot < 0 || (dot > 0 && s[..dot] is not ("td" or "th"))) return null;
            var name = s[(dot + 1)..];
            return name.Length > 0 && name.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_') ? "." + name : null;
        }

        public void Start(string name, Dictionary<string, string> attributes)
        {
            if (name == "table")
            {
                if (_depth++ == 0)
                {
                    _row = -1;
                    _busyUntil.Clear();
                }

                return;
            }

            if (_depth != 1) return;
            switch (name)
            {
                case "tr":
                    Close();
                    _row++;
                    _column = 0;
                    break;
                case "td" or "th":
                    Close();
                    Open(name, attributes);
                    break;
                case "br":
                    _cell?.Text.Append('\n');
                    break;
                case "b" or "strong" when _cell != null:
                    _cell.Bold = true;
                    break;
                case "i" or "em" when _cell != null:
                    _cell.Italic = true;
                    break;
                case "u" when _cell != null:
                    _cell.Underline = true;
                    break;
                case "s" or "strike" or "del" when _cell != null:
                    _cell.Strikethrough = true;
                    break;
            }
        }

        public void End(string name)
        {
            if (name == "table")
            {
                if (_depth == 0 || --_depth != 0) return;
                Close();
                Done = _cells.Count > 0;
                return;
            }

            if (_depth == 1 && name is "td" or "th" or "tr") Close();
        }

        public void Text(string text)
        {
            if (_depth == 1 && _cell != null) _cell.Append(WebUtility.HtmlDecode(text));
        }

        private void Open(string name, Dictionary<string, string> attributes)
        {
            if (_row < 0) _row = 0;
            var col = _column;
            while (col < _busyUntil.Count && _busyUntil[col] >= _row) col++;
            var colSpan = Span(attributes, "colspan", MaxColumnSpan);
            var rowSpan = Span(attributes, "rowspan", MaxRowSpan);
            _column = col + colSpan;
            if (_row >= CellRef.MaxRows || col >= CellRef.MaxColumns) return;
            colSpan = Math.Min(colSpan, CellRef.MaxColumns - col);
            rowSpan = Math.Min(rowSpan, CellRef.MaxRows - _row);
            for (var c = col; c < col + colSpan; c++)
            {
                while (_busyUntil.Count <= c) _busyUntil.Add(-1);
                _busyUntil[c] = _row + rowSpan - 1;
            }

            var style = ApplyCss(CellStyle.Default, _rules.GetValueOrDefault(name) ?? "", culture);
            foreach (var cls in (attributes.GetValueOrDefault("class") ?? "").Split(' ', '\t', '\r', '\n'))
                if (cls.Length > 0 && _rules.TryGetValue("." + cls, out var rule))
                    style = ApplyCss(style, rule, culture);
            if (attributes.TryGetValue("style", out var inline)) style = ApplyCss(style, inline, culture);
            _cell = new OpenCell(_row, col, rowSpan, colSpan, style, attributes);
        }

        private static int Span(Dictionary<string, string> attributes, string name, int max) =>
            attributes.TryGetValue(name, out var text) && int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? Math.Clamp(n, 1, max) : 1;

        private void Close()
        {
            if (_cell is not { } c) return;
            _cell = null;
            var style = c.Style with
            {
                Font = c.Style.Font with
                {
                    Bold = c.Style.Font.Bold || c.Bold, Italic = c.Style.Font.Italic || c.Italic,
                    Underline = c.Style.Font.Underline || c.Underline, Strikethrough = c.Style.Font.Strikethrough || c.Strikethrough
                }
            };
            var (value, formula) = Resolve(c.Attributes, c.Finish(), ref style);
            var source = formula != null && c.Attributes.TryGetValue("data-mudex-at", out var at) && CellRef.TryParse(at, out var s) ? s : (CellRef?)null;
            _cells.Add(new HtmlTableCell(c.Row, c.Column, value, formula, style, c.RowSpan, c.ColumnSpan, source));
            _rows = Math.Max(_rows, c.Row + c.RowSpan);
            _columns = Math.Max(_columns, c.Column + c.ColumnSpan);
        }

        private (CellValue Value, string Formula) Resolve(Dictionary<string, string> a, string text, ref CellStyle style)
        {
            var formula = a.TryGetValue("x:fmla", out var f) && f.Length > 1 && f[0] == '=' ? f[1..] : null;
            if (a.TryGetValue("x:bool", out var b)) return (CellValue.Boolean(b.Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase)), formula);
            if (a.TryGetValue("x:num", out var n) && double.TryParse(n, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                return (CellValue.Number(number), formula);
            if (a.ContainsKey("x:str")) return (CellValue.Text(text), formula);
            if (a.TryGetValue("data-sheets-value", out var sheets) && SheetsType.Match(sheets) is { Success: true } type)
            {
                if (type.Groups[1].Value == "2") return (CellValue.Text(text), formula);
                if (type.Groups[1].Value == "3" && SheetsNumber.Match(sheets) is { Success: true } m
                                                && double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                    return (CellValue.Number(number), formula);
            }

            if (text.Length == 0) return (CellValue.Empty, formula);
            if (style.NumberFormat == "@") return (CellValue.Text(text), formula);
            var (value, suggested) = CellInputParser.Parse(text, culture);
            if (suggested != null && style.NumberFormat == NumberFormat.General) style = style with { NumberFormat = suggested };
            return (value, formula);
        }
    }

    private sealed class OpenCell(int row, int column, int rowSpan, int columnSpan, CellStyle style, Dictionary<string, string> attributes)
    {
        public int Row => row;

        public int Column => column;

        public int RowSpan => rowSpan;

        public int ColumnSpan => columnSpan;

        public CellStyle Style => style;

        public Dictionary<string, string> Attributes => attributes;

        public StringBuilder Text { get; } = new();

        public bool Bold { get; set; }

        public bool Italic { get; set; }

        public bool Underline { get; set; }

        public bool Strikethrough { get; set; }

        public void Append(string text)
        {
            foreach (var c in text)
            {
                if (c is not (' ' or '\t' or '\r' or '\n' or '\f'))
                {
                    Text.Append(c);
                    continue;
                }

                if (Text.Length > 0 && Text[^1] is not (' ' or '\n')) Text.Append(' ');
            }
        }

        public string Finish() =>
            string.Join('\n', Text.ToString().Split('\n').Select(line => line.Trim(' '))).Trim('\n').Replace(' ', ' ');
    }
}

internal interface IHtmlTokenSink
{
    bool Done { get; }

    void Text(string text);

    void Start(string name, Dictionary<string, string> attributes);

    void End(string name);

    void StyleSheet(string css);
}
