using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>
/// Formats cell values with Excel number format codes: up to four sections (positive;negative;zero;text), conditions such as
/// <c>[&gt;100]</c>, colors such as <c>[Red]</c>, digit placeholders, grouping, scaling, percent, scientific notation, fractions,
/// text, literals and date and time codes. Format codes use <c>.</c> and <c>,</c>; the output uses the separators and names of
/// the culture.
/// </summary>
public static class NumberFormat
{
    /// <summary>The General format code.</summary>
    public const string General = "General";

    /// <summary>The text shown when a value cannot be displayed with a format, such as a negative date.</summary>
    public const string Overflow = "########";

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private const int MaxCachedFormats = 1024;

    private static readonly ConcurrentDictionary<string, Section[]> Cache = new();

    private static readonly string[] ColorNames = ["Black", "Blue", "Cyan", "Green", "Magenta", "Red", "White", "Yellow"];

    /// <summary>Formats a value for display.</summary>
    /// <param name="value">The value.</param>
    /// <param name="formatCode">The format code; null or empty means <see cref="General"/>.</param>
    /// <param name="culture">The culture for separators, month and day names and booleans.</param>
    /// <param name="color">The color of the chosen section (for example <c>Red</c> or <c>Color10</c>), or null.</param>
    /// <returns>The text; <see cref="Overflow"/> if the value cannot be shown with the format.</returns>
    public static string Format(CellValue value, string formatCode, CultureInfo culture, out string color)
    {
        TryFormat(value, formatCode, culture, out var text, out color);
        return text;
    }

    internal static bool TryFormat(CellValue value, string formatCode, CultureInfo culture, out string text, out string color)
    {
        culture ??= CultureInfo.CurrentCulture;
        color = null;
        text = "";
        switch (value.Kind)
        {
            case CellValueKind.Empty:
                return true;
            case CellValueKind.Error:
                text = value.ToString();
                return true;
            case CellValueKind.Boolean:
                text = BooleanText(value.BooleanValue, culture);
                return true;
        }

        var sections = Parse(formatCode);
        if (value.Kind == CellValueKind.Text)
        {
            var textSection = sections.Length > 3 ? sections[3] : sections.FirstOrDefault(s => s.Tokens.Any(t => t.Kind == '@'));
            text = value.TextValue;
            if (textSection == null) return true;
            color = textSection.Color;
            var sb = new StringBuilder();
            foreach (var t in textSection.Tokens)
                sb.Append(t.Kind == '@' ? value.TextValue : t.Kind == 'L' ? t.Text : "");
            text = sb.ToString();
            return true;
        }

        var n = value.NumberValue;
        var (section, keepSign) = Choose(sections, n);
        if (section == null)
        {
            text = Overflow;
            return false;
        }

        color = section.Color;
        var ok = section.IsDate ? TryFormatDate(n, section, culture, out text) : TryFormatNumber(keepSign ? n : Math.Abs(n), section, culture, out text);
        if (!ok) text = Overflow;
        return ok;
    }

    internal static string BooleanText(bool value, CultureInfo culture) => culture.TwoLetterISOLanguageName == "de"
        ? value ? "WAHR" : "FALSCH"
        : value ? "TRUE" : "FALSE";

    private static (Section Section, bool KeepSign) Choose(Section[] sections, double n)
    {
        var numeric = sections.Length > 3 ? sections[..3] : sections;
        if (numeric.Length > 1 && numeric.Length == sections.Length && numeric[^1].Tokens.Any(t => t.Kind == '@')) numeric = numeric[..^1];
        if (numeric[0].Op != null || (numeric.Length > 1 && numeric[1].Op != null))
        {
            if (Matches(numeric[0], n)) return (numeric[0], true);
            if (numeric.Length > 1 && numeric[1].Op != null && Matches(numeric[1], n)) return (numeric[1], true);
            if (numeric.Length > 2) return (numeric[2], true);
            return numeric.Length == 2 && numeric[1].Op == null ? (numeric[1], true) : (null, true);
        }

        if (n < 0 && numeric.Length > 1) return (numeric[1], false);
        if (n == 0 && numeric.Length > 2) return (numeric[2], true);
        return (numeric[0], true);
    }

    private static bool Matches(Section s, double n) => s.Op switch
    {
        null => true,
        "<" => n < s.Bound,
        "<=" => n <= s.Bound,
        ">" => n > s.Bound,
        ">=" => n >= s.Bound,
        "<>" => n != s.Bound,
        _ => n == s.Bound
    };

    internal static string GeneralText(double n, CultureInfo culture)
    {
        var a = Math.Abs(n);
        string code;
        if (a == 0) code = "0";
        else if (a >= 1e11 || a < 1e-5) code = "0.#####E+00";
        else
        {
            var decimals = Math.Max(0, 10 - (a < 1 ? 1 : (int)Math.Floor(Math.Log10(a)) + 1));
            code = decimals == 0 ? "0" : "0." + new string('#', decimals);
        }

        TryFormatNumber(n, Parse(code)[0], culture, out var text);
        var separator = culture.NumberFormat.NumberDecimalSeparator;
        text = text.Replace(separator + "E", "E", StringComparison.Ordinal);
        return text.EndsWith(separator, StringComparison.Ordinal) ? text[..^separator.Length] : text;
    }

    private static bool TryFormatNumber(double n, Section s, CultureInfo culture, out string text)
    {
        var nf = culture.NumberFormat;
        var sb = new StringBuilder();
        if (n < 0) sb.Append('-');
        var v = Math.Abs(n) * Math.Pow(100, s.Percent) / Math.Pow(1000, s.Scale);
        var tokens = s.Tokens;
        if (s.General)
        {
            foreach (var t in tokens) sb.Append(t.Kind is 'G' or '@' ? GeneralText(v, culture) : t.Kind == 'L' ? t.Text : "");
        }
        else if (s.Slash >= 0) AppendFraction(sb, v, s, culture);
        else
        {
            var end = s.Exponent >= 0 ? s.Exponent : tokens.Count;
            var point = tokens.FindIndex(0, end, t => t.Kind == '.');
            var intEnd = point >= 0 ? point : end;
            var intCount = tokens.Take(intEnd).Count(IsDigit);
            var fracCount = point >= 0 ? tokens.Skip(point).Take(end - point).Count(IsDigit) : 0;
            var exponent = 0;
            if (s.Exponent >= 0 && v != 0)
            {
                var step = intCount > 1 && tokens.Take(intEnd).Any(t => t.Kind == '#') ? intCount : 1;
                exponent = (int)Math.Floor(Math.Log10(v));
                exponent = step > 1 ? (int)Math.Floor(exponent / (double)step) * step : exponent - intCount + 1;
                if (Digits(v / Math.Pow(10, exponent), fracCount).Int.TrimStart('0').Length > intCount) exponent += step;
            }

            var (intDigits, fracDigits) = Digits(s.Exponent >= 0 ? v / Math.Pow(10, exponent) : v, fracCount);
            AppendInteger(sb, tokens, 0, intEnd, intDigits == "0" || (intCount == 0 && point < 0) ? "" : intDigits, s.Grouping, nf.NumberGroupSeparator);
            if (point >= 0) AppendDecimals(sb, tokens, point, end, fracDigits, nf.NumberDecimalSeparator);
            if (s.Exponent >= 0)
            {
                var e = tokens[s.Exponent];
                sb.Append(e.Text[0]).Append(exponent < 0 ? "-" : e.Text[1] == '+' ? "+" : "");
                var expText = Math.Abs(exponent).ToString(Inv);
                AppendInteger(sb, tokens, s.Exponent + 1, tokens.Count, expText == "0" ? "" : expText, false, "");
            }
        }

        text = sb.ToString();
        return true;
    }

    private static bool IsDigit(Token t) => t.Kind is '0' or '#' or '?';

    private static (string Int, string Frac) Digits(double v, int decimals)
    {
        string s;
        if (v < 1e27)
        {
            var d = decimal.Parse(v.ToString("G15", Inv), NumberStyles.Float, Inv);
            s = decimal.Round(d, Math.Min(decimals, 28), MidpointRounding.AwayFromZero).ToString("F" + decimals, Inv);
        }
        else s = v.ToString("F" + decimals, Inv);

        var dot = s.IndexOf('.');
        return dot < 0 ? (s, "") : (s[..dot], s[(dot + 1)..]);
    }

    private static void AppendInteger(StringBuilder sb, List<Token> tokens, int from, int to, string digits, bool grouping, string separator)
    {
        var part = new StringBuilder();
        var first = -1;
        for (var i = from; i < to; i++)
            if (IsDigit(tokens[i]))
            {
                first = i;
                break;
            }

        if (first < 0)
        {
            for (var i = from; i < to; i++)
                if (tokens[i].Kind == 'L') sb.Append(tokens[i].Text);
            sb.Append(digits);
            return;
        }

        var di = digits.Length - 1;
        var position = 0;
        void Digit(char c)
        {
            if (grouping && position > 0 && position % 3 == 0) part.Insert(0, separator);
            part.Insert(0, c);
            position++;
        }

        for (var i = to - 1; i >= from; i--)
        {
            var t = tokens[i];
            if (!IsDigit(t))
            {
                if (t.Kind == 'L') part.Insert(0, t.Text);
                else if (t.Kind == '%') part.Insert(0, '%');
                continue;
            }

            if (di >= 0) Digit(digits[di--]);
            else if (t.Kind == '0') Digit('0');
            else if (t.Kind == '?')
            {
                part.Insert(0, ' ');
                position++;
            }

            if (i == first)
                while (di >= 0)
                    Digit(digits[di--]);
        }

        sb.Append(part);
    }

    private static void AppendDecimals(StringBuilder sb, List<Token> tokens, int point, int end, string digits, string separator)
    {
        var shown = digits.ToCharArray();
        var placeholders = new List<char>();
        for (var i = point; i < end; i++)
            if (IsDigit(tokens[i]))
                placeholders.Add(tokens[i].Kind);
        for (var j = shown.Length - 1; j >= 0 && shown[j] == '0' && placeholders[j] != '0'; j--)
            shown[j] = placeholders[j] == '?' ? ' ' : '\0';
        var k = 0;
        for (var i = point; i < end; i++)
        {
            var t = tokens[i];
            if (t.Kind == '.') sb.Append(i == point ? separator : "");
            else if (IsDigit(t))
            {
                if (shown[k] != '\0') sb.Append(shown[k]);
                k++;
            }
            else if (t.Kind == 'L') sb.Append(t.Text);
            else if (t.Kind == '%') sb.Append('%');
        }
    }

    private static void AppendFraction(StringBuilder sb, double v, Section s, CultureInfo culture)
    {
        var tokens = s.Tokens;
        var numStart = s.Slash;
        while (numStart > 0 && IsDigit(tokens[numStart - 1])) numStart--;
        var denEnd = s.Slash + 1;
        while (denEnd < tokens.Count && (IsDigit(tokens[denEnd]) || tokens[denEnd] is { Kind: 'L', Text: [>= '0' and <= '9'] })) denEnd++;
        var intEnd = numStart;
        while (intEnd > 0 && !IsDigit(tokens[intEnd - 1])) intEnd--;
        var hasInt = intEnd > 0;
        var denTokens = tokens.Skip(s.Slash + 1).Take(denEnd - s.Slash - 1).ToList();
        var fixedDen = denTokens.Count > 0 && denTokens[0].Kind == 'L'
            ? int.Parse(string.Concat(denTokens.Select(t => t.Kind == 'L' ? t.Text : "0")), Inv)
            : 0;

        var whole = hasInt ? Math.Floor(v) : 0;
        var f = v - whole;
        long num, den;
        if (fixedDen > 0) (num, den) = ((long)Math.Round(f * fixedDen, MidpointRounding.AwayFromZero), fixedDen);
        else
        {
            var max = (long)Math.Pow(10, Math.Min(denTokens.Count, 4)) - 1;
            (num, den) = (0, 1);
            var best = double.MaxValue;
            for (long d = 1; d <= max; d++)
            {
                var candidate = Math.Round(f * d, MidpointRounding.AwayFromZero);
                var error = Math.Abs(f - candidate / d);
                if (error >= best) continue;
                (best, num, den) = (error, (long)candidate, d);
                if (error == 0) break;
            }
        }

        if (hasInt && num == den)
        {
            whole++;
            num = 0;
        }

        var wholeDigits = whole == 0 ? "" : whole.ToString("F0", Inv);
        if (hasInt)
        {
            if (num == 0 && wholeDigits.Length == 0) wholeDigits = "0";
            var intPart = new StringBuilder();
            AppendInteger(intPart, tokens, 0, intEnd, wholeDigits, s.Grouping, culture.NumberFormat.NumberGroupSeparator);
            if (intPart.ToString().Trim().Length > 0 || num == 0)
            {
                sb.Append(intPart);
                for (var i = intEnd; i < numStart; i++)
                    if (tokens[i].Kind == 'L') sb.Append(tokens[i].Text);
            }
        }

        var fraction = new StringBuilder();
        var numTokens = tokens.Skip(numStart).Take(s.Slash - numStart).ToList();
        var numText = num.ToString(Inv);
        fraction.Append(numText.PadLeft(numTokens.Count, numTokens.FirstOrDefault().Kind == '0' ? '0' : ' '));
        fraction.Append('/');
        var denText = den.ToString(Inv);
        fraction.Append(fixedDen > 0 ? denText : denText.PadRight(denTokens.Count, ' '));
        sb.Append(hasInt && num == 0 ? new string(' ', fraction.Length) : fraction.ToString());
        for (var i = denEnd; i < tokens.Count; i++)
            if (tokens[i].Kind == 'L') sb.Append(tokens[i].Text);
    }

    private static bool TryFormatDate(double serial, Section s, CultureInfo culture, out string text)
    {
        text = Overflow;
        if (!(serial >= 0 && serial < ExcelDate.MaxSerial + 1)) return false;
        var fd = s.Tokens.Where(t => t.Kind == 'f').Select(t => t.Count).DefaultIfEmpty(0).Max();
        var scale = Math.Pow(10, fd);
        var ticks = Math.Round(serial * 86400 * scale, MidpointRounding.AwayFromZero);
        var days = Math.Floor(ticks / (86400 * scale));
        var rest = ticks - days * 86400 * scale;
        var seconds = (long)Math.Floor(rest / scale);
        var fraction = ((long)(rest - seconds * scale)).ToString(new string('0', Math.Max(fd, 1)), Inv);
        var (year, month, day) = ExcelDate.ToDate(Math.Min(days, ExcelDate.MaxSerial));
        var (hour, minute, second) = (seconds / 3600, seconds / 60 % 60, seconds % 60);
        var twelveHour = s.Tokens.Any(t => t.Kind == 'A');
        var dtf = culture.DateTimeFormat;
        var dow = (DayOfWeek)(((long)days + 6) % 7);
        var sb = new StringBuilder();
        foreach (var t in s.Tokens)
        {
            sb.Append(t.Kind switch
            {
                'L' => t.Text,
                'y' => t.Count <= 2 ? (year % 100).ToString("00", Inv) : year.ToString("0000", Inv),
                'm' => t.Count switch
                {
                    1 => month.ToString(Inv),
                    2 => month.ToString("00", Inv),
                    3 => dtf.GetAbbreviatedMonthName(month).TrimEnd('.'),
                    4 => dtf.GetMonthName(month),
                    _ => dtf.GetMonthName(month)[..1]
                },
                'd' => t.Count switch
                {
                    1 => day.ToString(Inv),
                    2 => day.ToString("00", Inv),
                    3 => dtf.GetAbbreviatedDayName(dow).TrimEnd('.'),
                    _ => dtf.GetDayName(dow)
                },
                'h' => Pad(twelveHour ? (hour % 12 == 0 ? 12 : hour % 12) : hour, t.Count),
                'n' => Pad(minute, t.Count),
                's' => Pad(second, t.Count),
                'H' => Pad((long)days * 24 + hour, t.Count),
                'M' => Pad((long)days * 1440 + hour * 60 + minute, t.Count),
                'S' => Pad((long)days * 86400 + seconds, t.Count),
                'f' => culture.NumberFormat.NumberDecimalSeparator + fraction[..t.Count],
                'A' when t.Text.Length == 5 => hour < 12 ? "AM" : "PM",
                'A' => hour < 12 ? t.Text[..1] : t.Text[2..],
                _ => ""
            });
        }

        text = sb.ToString();
        return true;
    }

    private static string Pad(long value, int count) => value.ToString(new string('0', Math.Max(count, 1)), Inv);

    private static Section[] Parse(string formatCode)
    {
        if (Cache.Count > MaxCachedFormats) Cache.Clear();
        return Cache.GetOrAdd(string.IsNullOrEmpty(formatCode) ? General : formatCode, Compile);
    }

    private static Section[] Compile(string code)
    {
        var sections = new List<Section>();
        var start = 0;
        for (var i = 0; i <= code.Length; i++)
        {
            if (i < code.Length)
            {
                var c = code[i];
                if (c == '"')
                {
                    var end = code.IndexOf('"', i + 1);
                    i = end < 0 ? code.Length - 1 : end;
                    continue;
                }

                if (c is '\\' or '_' or '*')
                {
                    i++;
                    continue;
                }

                if (c == '[')
                {
                    var end = code.IndexOf(']', i);
                    i = end < 0 ? code.Length - 1 : end;
                    continue;
                }

                if (c != ';') continue;
            }

            if (sections.Count < 4) sections.Add(ParseSection(code[start..Math.Min(i, code.Length)]));
            start = i + 1;
        }

        return sections.ToArray();
    }

    private static Section ParseSection(string code)
    {
        var s = new Section();
        var tokens = s.Tokens;
        void Literal(string text) => tokens.Add(new Token('L', text));
        for (var i = 0; i < code.Length; i++)
        {
            var c = code[i];
            var lower = char.ToLowerInvariant(c);
            if (c == '"')
            {
                var end = code.IndexOf('"', i + 1);
                if (end < 0) end = code.Length;
                Literal(code[(i + 1)..end]);
                i = end;
            }
            else if (c == '\\' && i + 1 < code.Length) Literal(code[++i].ToString());
            else if (c == '_' && i + 1 < code.Length)
            {
                Literal(" ");
                i++;
            }
            else if (c == '*' && i + 1 < code.Length) i++;
            else if (c == '[')
            {
                var end = code.IndexOf(']', i);
                if (end < 0) end = code.Length;
                Bracket(s, code[(i + 1)..end]);
                i = end;
            }
            else if (string.Compare(code, i, General, 0, General.Length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                tokens.Add(new Token('G', null));
                s.General = true;
                i += General.Length - 1;
            }
            else if (string.Compare(code, i, "AM/PM", 0, 5, StringComparison.OrdinalIgnoreCase) == 0)
            {
                tokens.Add(new Token('A', code.Substring(i, 5)));
                i += 4;
            }
            else if (string.Compare(code, i, "A/P", 0, 3, StringComparison.OrdinalIgnoreCase) == 0)
            {
                tokens.Add(new Token('A', code.Substring(i, 3)));
                i += 2;
            }
            else if (lower == 'e' && i + 1 < code.Length && code[i + 1] is '+' or '-')
            {
                tokens.Add(new Token('E', code.Substring(i, 2)));
                i++;
            }
            else if (c is '0' or '#' or '?' or '.' or ',' or '%' or '/' or '@') tokens.Add(new Token(c, c.ToString()));
            else if (lower is 'y' or 'm' or 'd' or 'h' or 's')
            {
                var run = 1;
                while (i + run < code.Length && char.ToLowerInvariant(code[i + run]) == lower) run++;
                tokens.Add(new Token(lower, null, run));
                s.IsDate = true;
                i += run - 1;
            }
            else Literal(c.ToString());
        }

        if (s.IsDate) ResolveDate(s);
        else ResolveNumber(s);
        return s;
    }

    private static void Bracket(Section s, string content)
    {
        var lower = content.ToLowerInvariant();
        if (lower.Length > 0 && lower.All(ch => ch == lower[0]) && lower[0] is 'h' or 'm' or 's')
        {
            s.Tokens.Add(new Token(char.ToUpperInvariant(lower[0]), null, lower.Length));
            s.IsDate = true;
        }
        else if (ColorNames.FirstOrDefault(n => n.Equals(content, StringComparison.OrdinalIgnoreCase)) is { } name) s.Color = name;
        else if (lower.StartsWith("color") && int.TryParse(lower[5..], NumberStyles.None, Inv, out var index)) s.Color = "Color" + index;
        else if (lower.Length > 0 && lower[0] is '<' or '>' or '=')
        {
            var op = lower.StartsWith("<=") || lower.StartsWith(">=") || lower.StartsWith("<>") ? lower[..2] : lower[..1];
            if (double.TryParse(lower[op.Length..], NumberStyles.Float, Inv, out var bound)) (s.Op, s.Bound) = (op, bound);
        }
        else if (content.StartsWith('$'))
        {
            var dash = content.IndexOf('-');
            var symbol = dash < 0 ? content[1..] : content[1..dash];
            if (symbol.Length > 0) s.Tokens.Add(new Token('L', symbol));
        }
    }

    private static void ResolveDate(Section s)
    {
        var tokens = s.Tokens;
        for (var i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Kind == '.' && i + 1 < tokens.Count && tokens[i + 1].Kind == '0')
            {
                var run = 0;
                while (i + 1 + run < tokens.Count && tokens[i + 1 + run].Kind == '0') run++;
                tokens[i] = new Token('f', null, run);
                tokens.RemoveRange(i + 1, run);
            }
            else if (t.Kind is '0' or '#' or '?' or '.' or ',' or '%' or '/' or '@' or 'E' or 'G')
                tokens[i] = new Token('L', t.Kind == 'G' ? General : t.Text);
        }

        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Kind != 'm' || tokens[i].Count > 2) continue;
            var previous = tokens.Take(i).LastOrDefault(x => x.Kind is 'y' or 'm' or 'd' or 'h' or 's' or 'H' or 'M' or 'S' or 'n').Kind;
            var next = tokens.Skip(i + 1).FirstOrDefault(x => x.Kind is 'y' or 'm' or 'd' or 'h' or 's' or 'H' or 'M' or 'S' or 'n').Kind;
            if (previous is 'h' or 'H' || next is 's' or 'S') tokens[i] = new Token('n', null, tokens[i].Count);
        }
    }

    private static void ResolveNumber(Section s)
    {
        var tokens = s.Tokens;
        s.Exponent = tokens.FindIndex(t => t.Kind == 'E');
        s.Slash = s.Exponent < 0 && !s.General ? tokens.FindIndex(t => t.Kind == '/') : -1;
        if (s.Slash >= 0 && !(s.Slash > 0 && IsDigit(tokens[s.Slash - 1]))) s.Slash = -1;
        for (var i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Kind == '%') s.Percent++;
            else if (t.Kind == '/' && i != s.Slash) tokens[i] = new Token('L', "/");
            else if (t.Kind == '.' && (s.Slash >= 0 || tokens.FindIndex(x => x.Kind == '.') != i)) tokens[i] = new Token('L', ".");
        }

        s.General |= !tokens.Any(IsDigit) && tokens.Any(t => t.Kind == '@');

        var mantissaEnd = s.Exponent >= 0 ? s.Exponent : tokens.Count;
        var lastDigit = mantissaEnd > 0 ? tokens.FindLastIndex(mantissaEnd - 1, mantissaEnd, IsDigit) : -1;
        var point = tokens.FindIndex(0, mantissaEnd, t => t.Kind == '.');
        var intEnd = point >= 0 ? point : mantissaEnd;
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Kind != ',') continue;
            if (i > lastDigit && lastDigit >= 0 && tokens.Skip(lastDigit + 1).Take(i - lastDigit).All(x => x.Kind == ',')) s.Scale++;
            else if (i < intEnd && tokens.Take(i).Any(IsDigit) && tokens.Skip(i + 1).Take(intEnd - i - 1).Any(IsDigit)) s.Grouping = true;
            else continue;
            tokens[i] = new Token(',', null);
        }

        for (var i = 0; i < tokens.Count; i++)
            if (tokens[i] is { Kind: ',', Text: not null })
                tokens[i] = new Token('L', ",");
        tokens.RemoveAll(t => t is { Kind: ',', Text: null });
        s.Exponent = tokens.FindIndex(t => t.Kind == 'E');
        if (s.Slash >= 0) s.Slash = tokens.FindIndex(t => t.Kind == '/');
    }

    private readonly record struct Token(char Kind, string Text, int Count = 1);

    private sealed class Section
    {
        public List<Token> Tokens { get; } = [];
        public string Color { get; set; }
        public string Op { get; set; }
        public double Bound { get; set; }
        public bool IsDate { get; set; }
        public bool General { get; set; }
        public bool Grouping { get; set; }
        public int Percent { get; set; }
        public int Scale { get; set; }
        public int Exponent { get; set; } = -1;
        public int Slash { get; set; } = -1;
    }
}
