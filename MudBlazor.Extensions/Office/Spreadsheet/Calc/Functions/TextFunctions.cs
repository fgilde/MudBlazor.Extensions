using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class TextFunctions
{
    private const string Cat = "Text";

    internal static void Register(FunctionRegistry r)
    {
        r.Add(Cat, "CONCAT(text1, [text2], ...)", "Joins the texts of all arguments and ranges.",
            (ctx, a) => Fn.Text(string.Concat(Items(ctx, a, 0))));
        r.Add(Cat, "CONCATENATE(text1, [text2], ...)", "Joins several texts into one.", Fn.Lift(a => Fn.Text(string.Concat(a.Select(Fn.Str)))));
        r.Add(Cat, "TEXTJOIN(delimiter, ignore_empty, text1, [text2], ...)", "Joins texts with a delimiter, optionally skipping empty values.", (ctx, a) =>
        {
            var delimiters = a[0].IsMissing ? [""] : Items(ctx, [a[0]], 0).ToList();
            var ignore = Fn.Bool(Fn.Scalar(ctx, a[1]));
            var sb = new StringBuilder();
            var i = 0;
            foreach (var item in Items(ctx, a, 2))
            {
                if (ignore && item.Length == 0) continue;
                if (i > 0) sb.Append(delimiters[(i - 1) % delimiters.Count]);
                sb.Append(item);
                i++;
            }

            return Fn.Text(sb.ToString());
        });
        r.Add(Cat, "LEFT(text, [num_chars])", "Returns the first characters of a text.", Fn.Lift(a =>
        {
            var (s, n) = (Fn.Str(a[0]), Fn.Int(a, 1, 1));
            return n < 0 ? throw Fn.Error(CellError.Value) : CellValue.Text(s[..Math.Min(n, s.Length)]);
        }));
        r.Add(Cat, "RIGHT(text, [num_chars])", "Returns the last characters of a text.", Fn.Lift(a =>
        {
            var (s, n) = (Fn.Str(a[0]), Fn.Int(a, 1, 1));
            return n < 0 ? throw Fn.Error(CellError.Value) : CellValue.Text(s[^Math.Min(n, s.Length)..]);
        }));
        r.Add(Cat, "MID(text, start_num, num_chars)", "Returns characters from the middle of a text.", Fn.Lift(a =>
        {
            var (s, start, n) = (Fn.Str(a[0]), Fn.Int(a[1]), Fn.Int(a[2]));
            if (start < 1 || n < 0) throw Fn.Error(CellError.Value);
            return CellValue.Text(start > s.Length ? "" : s.Substring(start - 1, Math.Min(n, s.Length - start + 1)));
        }));
        r.Add(Cat, "LEN(text)", "Returns the number of characters in a text.", Fn.Lift(a => CellValue.Number(Fn.Str(a[0]).Length)));
        r.Add(Cat, "FIND(find_text, within_text, [start_num])", "Finds one text in another (case-sensitive) and returns its position.",
            Fn.Lift(a => Position(a, (s, find, start) => s.IndexOf(find, start, StringComparison.Ordinal))));
        r.Add(Cat, "SEARCH(find_text, within_text, [start_num])", "Finds one text in another (case-insensitive, with wildcards) and returns its position.",
            Fn.Lift(a => Position(a, (s, find, start) =>
            {
                var m = Regex.Match(s[start..], Fn.WildcardPattern(find, false), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
                return m.Success ? m.Index + start : -1;
            })));
        r.Add(Cat, "SUBSTITUTE(text, old_text, new_text, [instance_num])", "Replaces existing text with new text.", Fn.Lift(a =>
        {
            var (s, old, replacement) = (Fn.Str(a[0]), Fn.Str(a[1]), Fn.Str(a[2]));
            if (old.Length == 0) return CellValue.Text(s);
            if (a.Length < 4) return Fn.Text(s.Replace(old, replacement, StringComparison.Ordinal));
            var instance = Fn.Int(a[3]);
            if (instance < 1) throw Fn.Error(CellError.Value);
            for (int i = 0, found = 0; (i = s.IndexOf(old, i, StringComparison.Ordinal)) >= 0; i += old.Length)
                if (++found == instance)
                    return Fn.Text(s[..i] + replacement + s[(i + old.Length)..]);
            return CellValue.Text(s);
        }));
        r.Add(Cat, "REPLACE(old_text, start_num, num_chars, new_text)", "Replaces part of a text by position.", Fn.Lift(a =>
        {
            var (s, start, n, replacement) = (Fn.Str(a[0]), Fn.Int(a[1]), Fn.Int(a[2]), Fn.Str(a[3]));
            if (start < 1 || n < 0) throw Fn.Error(CellError.Value);
            var from = Math.Min(start - 1, s.Length);
            return Fn.Text(s[..from] + replacement + s[Math.Min(s.Length, from + n)..]);
        }));
        r.Add(Cat, "UPPER(text)", "Converts text to upper case.", Fn.Lift(a => CellValue.Text(Fn.Str(a[0]).ToUpperInvariant())));
        r.Add(Cat, "LOWER(text)", "Converts text to lower case.", Fn.Lift(a => CellValue.Text(Fn.Str(a[0]).ToLowerInvariant())));
        r.Add(Cat, "PROPER(text)", "Capitalizes the first letter of each word.", Fn.Lift(a =>
        {
            var chars = Fn.Str(a[0]).ToCharArray();
            for (var i = 0; i < chars.Length; i++)
                chars[i] = i > 0 && char.IsLetter(chars[i - 1]) ? char.ToLowerInvariant(chars[i]) : char.ToUpperInvariant(chars[i]);
            return CellValue.Text(new string(chars));
        }));
        r.Add(Cat, "TRIM(text)", "Removes leading and trailing spaces and collapses inner runs of spaces.",
            Fn.Lift(a => CellValue.Text(string.Join(' ', Fn.Str(a[0]).Split(' ', StringSplitOptions.RemoveEmptyEntries)))));
        r.Add(Cat, "CLEAN(text)", "Removes non-printable characters.", Fn.Lift(a => CellValue.Text(new string(Fn.Str(a[0]).Where(c => c >= 32).ToArray()))));
        r.Add(Cat, "REPT(text, number_times)", "Repeats text a number of times.", Fn.Lift(a =>
        {
            var (s, n) = (Fn.Str(a[0]), Fn.Int(a[1]));
            if (n < 0 || (long)s.Length * n > 32767) throw Fn.Error(CellError.Value);
            return CellValue.Text(string.Concat(Enumerable.Repeat(s, n)));
        }));
        r.Add(Cat, "EXACT(text1, text2)", "Checks whether two texts are exactly the same (case-sensitive).",
            Fn.Lift(a => CellValue.Boolean(Fn.Str(a[0]) == Fn.Str(a[1]))));
        r.Add(Cat, "CHAR(number)", "Returns the character for a code from 1 to 255.", Fn.Lift(a =>
        {
            var n = Fn.Int(a[0]);
            return n is < 1 or > 255 ? throw Fn.Error(CellError.Value) : CellValue.Text(((char)n).ToString());
        }));
        r.Add(Cat, "CODE(text)", "Returns the code of the first character of a text.", Fn.Lift(a =>
        {
            var s = Fn.Str(a[0]);
            return s.Length == 0 ? throw Fn.Error(CellError.Value) : CellValue.Number(s[0]);
        }));
        r.Add(Cat, "UNICHAR(number)", "Returns the Unicode character for a code point.", Fn.Lift(a =>
        {
            var n = Fn.Int(a[0]);
            if (n is < 1 or > 0x10FFFF or (>= 0xD800 and <= 0xDFFF)) throw Fn.Error(CellError.Value);
            return CellValue.Text(char.ConvertFromUtf32(n));
        }));
        r.Add(Cat, "UNICODE(text)", "Returns the Unicode code point of the first character.", Fn.Lift(a =>
        {
            var s = Fn.Str(a[0]);
            return s.Length == 0 ? throw Fn.Error(CellError.Value) : CellValue.Number(char.ConvertToUtf32(s, 0));
        }));
        r.Add(Cat, "T(value)", "Returns the text of a value, or empty text for other values.",
            Fn.Lift(a => a[0].Kind switch
            {
                CellValueKind.Text => a[0],
                CellValueKind.Error => a[0],
                _ => CellValue.Text("")
            }));
        r.Add(Cat, "TEXT(value, format_text)", "Formats a number with a format code such as \"0.00\" or \"yyyy-mm-dd\".",
            Fn.Lift((ctx, a) =>
            {
                var format = Fn.Str(a[1]);
                if (a[0].Kind == CellValueKind.Text && !Coercion.TryToNumber(a[0], out _, out _)) return a[0];
                return Fn.Text(FormatNumber(Fn.Num(a[0]), format, ctx.Culture));
            }));
        r.Add(Cat, "FIXED(number, [decimals], [no_commas])", "Formats a number as text with a fixed number of decimals.", Fn.Lift(a =>
        {
            var (n, d) = (Fn.Num(a[0]), Fn.Int(a, 1, 2));
            if (d > 127) throw Fn.Error(CellError.Value);
            var rounded = MathFunctions.Round(n, d, m => Math.Round(m, MidpointRounding.AwayFromZero));
            var pattern = (Fn.Bool(a, 2, false) ? "0" : "#,##0") + (d > 0 ? "." + new string('0', d) : "");
            return CellValue.Text(rounded.ToString(pattern, Fn.Inv));
        }));
        r.Add(Cat, "DOLLAR(number, [decimals])", "Formats a number as currency text.", Fn.Lift(a =>
        {
            var (n, d) = (Fn.Num(a[0]), Fn.Int(a, 1, 2));
            if (d > 127) throw Fn.Error(CellError.Value);
            var rounded = MathFunctions.Round(n, d, m => Math.Round(m, MidpointRounding.AwayFromZero));
            var pattern = "$#,##0" + (d > 0 ? "." + new string('0', d) : "");
            return CellValue.Text(rounded.ToString(pattern + ";(" + pattern + ")", Fn.Inv));
        }));
        r.Add(Cat, "VALUE(text)", "Converts text that looks like a number, percentage, date or time into a number.", Fn.Lift((ctx, a) => a[0].Kind switch
        {
            CellValueKind.Number or CellValueKind.Empty => CellValue.Number(a[0].NumberValue),
            CellValueKind.Text => ParseValue(a[0].TextValue, ctx.Culture),
            CellValueKind.Error => a[0],
            _ => throw Fn.Error(CellError.Value)
        }));
        r.Add(Cat, "NUMBERVALUE(text, [decimal_separator], [group_separator])", "Converts text to a number with the given separators.", Fn.Lift((ctx, a) =>
        {
            if (a[0].Kind is CellValueKind.Number) return a[0];
            var s = new string(Fn.Str(a[0]).Where(c => !char.IsWhiteSpace(c)).ToArray());
            var dec = a.Length > 1 ? Fn.Str(a[1]) : ctx.Culture.NumberFormat.NumberDecimalSeparator;
            var group = a.Length > 2 ? Fn.Str(a[2]) : ctx.Culture.NumberFormat.NumberGroupSeparator;
            if (dec.Length == 0) throw Fn.Error(CellError.Value);
            var percent = 0;
            while (s.EndsWith('%'))
            {
                s = s[..^1];
                percent++;
            }

            if (s.Length == 0) return CellValue.Number(0);
            if (group.Length > 0 && group[0] != dec[0]) s = s.Replace(group[0].ToString(), "");
            s = s.Replace(dec[0], '.');
            if (!double.TryParse(s, NumberStyles.Float, Fn.Inv, out var n)) throw Fn.Error(CellError.Value);
            return Fn.Number(n / Math.Pow(100, percent));
        }));
        r.Add(Cat, "VALUETOTEXT(value, [format])", "Returns the text of any value; format 1 quotes text.", Fn.Lift(a =>
        {
            var strict = Fn.Int(a, 1, 0);
            if (strict is not (0 or 1)) throw Fn.Error(CellError.Value);
            return CellValue.Text(ArrayText(a[0], strict == 1));
        }));
        r.Add(Cat, "ARRAYTOTEXT(array, [format])", "Returns the text of an array; format 1 gives an array constant.", (ctx, a) =>
        {
            var array = Fn.Array(ctx, a[0]);
            var strict = a.Count > 1 ? Fn.Int(Fn.Scalar(ctx, a[1])) : 0;
            if (strict is not (0 or 1)) return CalcValue.Error(CellError.Value);
            var rows = Enumerable.Range(0, array.Rows)
                .Select(r => string.Join(strict == 1 ? "," : ", ", Enumerable.Range(0, array.Columns).Select(c => ArrayText(array[r, c], strict == 1))));
            return Fn.Text(strict == 1 ? "{" + string.Join(";", rows) + "}" : string.Join(", ", rows));
        });
        r.Add(Cat, "TEXTBEFORE(text, delimiter, [instance_num], [match_mode], [match_end], [if_not_found])", "Returns the text before a delimiter.",
            (ctx, a) => Around(ctx, a, true));
        r.Add(Cat, "TEXTAFTER(text, delimiter, [instance_num], [match_mode], [match_end], [if_not_found])", "Returns the text after a delimiter.",
            (ctx, a) => Around(ctx, a, false));
        r.Add(Cat, "TEXTSPLIT(text, col_delimiter, [row_delimiter], [ignore_empty], [match_mode], [pad_with])", "Splits text into rows and columns.", TextSplit);
    }

    private static IEnumerable<string> Items(FunctionContext ctx, IReadOnlyList<CalcArg> args, int from)
    {
        for (var i = from; i < args.Count; i++)
        {
            if (args[i].IsMissing)
            {
                yield return "";
                continue;
            }

            var value = args[i].Value;
            if (value.Kind == CalcValueKind.Scalar)
            {
                yield return Fn.Str(value.Scalar);
                continue;
            }

            var array = ctx.ToArray(value) ?? throw Fn.Error(CellError.Value);
            for (var r = 0; r < array.Rows; r++)
                for (var c = 0; c < array.Columns; c++)
                    yield return Fn.Str(array[r, c]);
        }
    }

    private static CellValue Position(CellValue[] a, Func<string, string, int, int> find)
    {
        var (needle, s, start) = (Fn.Str(a[0]), Fn.Str(a[1]), Fn.Int(a, 2, 1));
        if (start < 1 || start > s.Length + 1) throw Fn.Error(CellError.Value);
        if (needle.Length == 0) return CellValue.Number(start);
        var i = find(s, needle, start - 1);
        return i < 0 ? throw Fn.Error(CellError.Value) : CellValue.Number(i + 1);
    }

    private static string ArrayText(CellValue v, bool strict) => v.Kind switch
    {
        CellValueKind.Text when strict => "\"" + v.TextValue.Replace("\"", "\"\"") + "\"",
        CellValueKind.Number => v.NumberValue.ToString("G15", Fn.Inv),
        _ => v.ToString()
    };

    private static CellValue ParseValue(string text, CultureInfo culture)
    {
        var s = text.Trim();
        const NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands | NumberStyles.AllowCurrencySymbol;
        if (double.TryParse(s, styles, culture, out var n)) return Fn.Number(n);
        if (s.EndsWith('%') && double.TryParse(s[..^1], styles, culture, out n)) return Fn.Number(n / 100);
        if (s.Contains(':') && TimeSpan.TryParse(s, culture, out var time) && time.Days == 0) return CellValue.Number(time.TotalDays);
        if (DateTime.TryParse(s, culture, DateTimeStyles.AllowWhiteSpaces, out var date)) return CellValue.Number(ExcelDate.ToSerial(date));
        throw Fn.Error(CellError.Value);
    }

    private static CalcValue Around(FunctionContext ctx, IReadOnlyList<CalcArg> a, bool before)
    {
        var text = Fn.Str(Fn.Scalar(ctx, a[0]));
        var delimiter = Fn.Str(Fn.Scalar(ctx, a[1]));
        var instance = a.Count > 2 && !a[2].IsMissing ? Fn.Int(Fn.Scalar(ctx, a[2])) : 1;
        var ignoreCase = a.Count > 3 && Fn.Int(Fn.Scalar(ctx, a[3])) == 1;
        var matchEnd = a.Count > 4 && Fn.Bool(Fn.Scalar(ctx, a[4]));
        if (instance == 0 || Math.Abs(instance) > Math.Max(text.Length, 1)) return CalcValue.Error(CellError.Value);
        var positions = new List<int>();
        if (delimiter.Length > 0)
            for (var i = 0; (i = text.IndexOf(delimiter, i, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) >= 0; i += delimiter.Length)
                positions.Add(i);
        int at, length = delimiter.Length;
        if (instance > 0 && instance <= positions.Count) at = positions[instance - 1];
        else if (instance < 0 && -instance <= positions.Count) at = positions[^-instance];
        else if (delimiter.Length == 0) (at, length) = (instance > 0 ? 0 : text.Length, 0);
        else if (matchEnd && Math.Abs(instance) == positions.Count + 1) (at, length) = (instance > 0 ? text.Length : 0, 0);
        else return a.Count > 5 && !a[5].IsMissing ? a[5].Value : CalcValue.Error(CellError.NA);
        return CalcValue.Text(before ? text[..at] : text[(at + length)..]);
    }

    private static CalcValue TextSplit(FunctionContext ctx, IReadOnlyList<CalcArg> a)
    {
        var text = Fn.Str(Fn.Scalar(ctx, a[0]));
        var ignoreEmpty = a.Count > 3 && !a[3].IsMissing && Fn.Bool(Fn.Scalar(ctx, a[3]));
        var options = a.Count > 4 && !a[4].IsMissing && Fn.Int(Fn.Scalar(ctx, a[4])) == 1 ? RegexOptions.IgnoreCase : RegexOptions.None;
        var pad = a.Count > 5 && !a[5].IsMissing ? Fn.Scalar(ctx, a[5]) : CellValue.Error(CellError.NA);
        string[] Split(string s, int arg)
        {
            var delimiters = arg < a.Count && !a[arg].IsMissing ? Items(ctx, [a[arg]], 0).Where(d => d.Length > 0).ToList() : [];
            var parts = delimiters.Count == 0
                ? [s]
                : Regex.Split(s, string.Join("|", delimiters.OrderByDescending(d => d.Length).Select(Regex.Escape)), options | RegexOptions.CultureInvariant);
            return ignoreEmpty ? parts.Where(p => p.Length > 0).ToArray() : parts;
        }

        var rows = Split(text, 2).Select(row => Split(row, 1)).ToList();
        if (rows.Count == 0) return CalcValue.Error(CellError.Calc);
        var array = new CalcArray(rows.Count, Math.Max(1, rows.Max(x => x.Length)));
        for (var r = 0; r < array.Rows; r++)
            for (var c = 0; c < array.Columns; c++)
                array[r, c] = c < rows[r].Length ? CellValue.Text(rows[r][c]) : pad;
        return CalcValue.FromArray(array);
    }

    private static string FormatNumber(double n, string format, CultureInfo culture)
    {
        var sections = SplitSections(format);
        var section = sections[0];
        if (n < 0 && sections.Count > 1 && sections[1].Length > 0)
        {
            section = sections[1];
            n = -n;
        }
        else if (n == 0 && sections.Count > 2)
            section = sections[2];

        if (section.Equals("General", StringComparison.OrdinalIgnoreCase)) return n.ToString("G10", culture);
        if (IsDateFormat(section))
        {
            if (n < 0 || n >= ExcelDate.MaxSerial + 1) throw Fn.Error(CellError.Value);
            return FormatDate(n, section, culture);
        }

        var sb = new StringBuilder();
        for (var i = 0; i < section.Length; i++)
        {
            var c = section[i];
            switch (c)
            {
                case '"':
                    var end = section.IndexOf('"', i + 1);
                    if (end < 0) end = section.Length;
                    sb.Append('"').Append(section, i + 1, Math.Max(0, end - i - 1)).Append('"');
                    i = end;
                    break;
                case '\\' when i + 1 < section.Length:
                    sb.Append('\\').Append(section[++i]);
                    break;
                case '[':
                    i = Math.Max(i, section.IndexOf(']', i));
                    break;
                case '_' when i + 1 < section.Length:
                    sb.Append(' ');
                    i++;
                    break;
                case '*' when i + 1 < section.Length:
                    i++;
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        var net = sb.ToString();
        return net.Length == 0 ? "" : n.ToString(net, culture);
    }

    private static List<string> SplitSections(string format)
    {
        var sections = new List<string>();
        var start = 0;
        var quoted = false;
        for (var i = 0; i < format.Length; i++)
        {
            if (format[i] == '"') quoted = !quoted;
            else if (format[i] == '\\') i++;
            else if (format[i] == ';' && !quoted)
            {
                sections.Add(format[start..i]);
                start = i + 1;
            }
        }

        sections.Add(format[start..]);
        return sections;
    }

    private static bool IsDateFormat(string section)
    {
        for (var i = 0; i < section.Length; i++)
        {
            switch (section[i])
            {
                case '"':
                    i = section.IndexOf('"', i + 1) is var e and >= 0 ? e : section.Length;
                    break;
                case '\\':
                case '_':
                case '*':
                    i++;
                    break;
                case '[':
                    i = Math.Max(i, section.IndexOf(']', i));
                    break;
                default:
                    if ("yYmMdDhHsS".Contains(section[i])) return true;
                    break;
            }
        }

        return false;
    }

    private static string FormatDate(double serial, string format, CultureInfo culture)
    {
        var tokens = new List<(char Kind, int Count, string Text)>();
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '"')
            {
                var end = format.IndexOf('"', i + 1);
                if (end < 0) end = format.Length;
                tokens.Add(('"', 0, format[(i + 1)..end]));
                i = end;
            }
            else if (c == '\\' && i + 1 < format.Length) tokens.Add(('"', 0, format[++i].ToString()));
            else if (c == '[') i = Math.Max(i, format.IndexOf(']', i));
            else if (string.Compare(format, i, "AM/PM", 0, 5, StringComparison.OrdinalIgnoreCase) == 0)
            {
                tokens.Add(('A', 5, null));
                i += 4;
            }
            else if (string.Compare(format, i, "A/P", 0, 3, StringComparison.OrdinalIgnoreCase) == 0)
            {
                tokens.Add(('A', 3, null));
                i += 2;
            }
            else if ("ymdhs".Contains(char.ToLowerInvariant(c)))
            {
                var run = 1;
                while (i + run < format.Length && char.ToLowerInvariant(format[i + run]) == char.ToLowerInvariant(c)) run++;
                tokens.Add((char.ToLowerInvariant(c), run, null));
                i += run - 1;
            }
            else tokens.Add(('"', 0, c.ToString()));
        }

        var twelveHour = tokens.Any(t => t.Kind == 'A');
        var (year, month, day) = ExcelDate.ToDate(serial);
        var seconds = (int)Math.Round((serial - Math.Floor(serial)) * 86400);
        var (hour, minute, second) = (seconds / 3600 % 24, seconds / 60 % 60, seconds % 60);
        var dow = (DayOfWeek)(((int)Math.Floor(serial) + 6) % 7);
        var dtf = culture.DateTimeFormat;
        var sb = new StringBuilder();
        for (var i = 0; i < tokens.Count; i++)
        {
            var (kind, count, text) = tokens[i];
            switch (kind)
            {
                case '"':
                    sb.Append(text);
                    break;
                case 'y':
                    sb.Append(count <= 2 ? (year % 100).ToString("00", Fn.Inv) : year.ToString("0000", Fn.Inv));
                    break;
                case 'm' when count <= 2 && IsMinute(tokens, i):
                    sb.Append(minute.ToString(count == 1 ? "0" : "00", Fn.Inv));
                    break;
                case 'm':
                    sb.Append(count switch
                    {
                        1 => month.ToString(Fn.Inv),
                        2 => month.ToString("00", Fn.Inv),
                        3 => dtf.GetAbbreviatedMonthName(month),
                        4 => dtf.GetMonthName(month),
                        _ => dtf.GetMonthName(month)[..1]
                    });
                    break;
                case 'd':
                    sb.Append(count switch
                    {
                        1 => day.ToString(Fn.Inv),
                        2 => day.ToString("00", Fn.Inv),
                        3 => dtf.GetAbbreviatedDayName(dow),
                        _ => dtf.GetDayName(dow)
                    });
                    break;
                case 'h':
                    var h = twelveHour ? (hour % 12 == 0 ? 12 : hour % 12) : hour;
                    sb.Append(h.ToString(count == 1 ? "0" : "00", Fn.Inv));
                    break;
                case 's':
                    sb.Append(second.ToString(count == 1 ? "0" : "00", Fn.Inv));
                    break;
                case 'A':
                    sb.Append(count == 5 ? (hour < 12 ? "AM" : "PM") : (hour < 12 ? "A" : "P"));
                    break;
            }
        }

        return sb.ToString();
    }

    private static bool IsMinute(List<(char Kind, int Count, string Text)> tokens, int at)
    {
        var previous = tokens.Take(at).LastOrDefault(t => t.Kind != '"').Kind;
        var next = tokens.Skip(at + 1).FirstOrDefault(t => t.Kind != '"').Kind;
        return previous == 'h' || next == 's';
    }
}
