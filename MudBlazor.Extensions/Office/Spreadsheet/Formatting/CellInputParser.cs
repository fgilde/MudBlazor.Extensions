using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>
/// Turns text typed into a cell into a value the way Excel does: numbers with the culture's separators, percentages,
/// currency amounts, dates and times in the culture's order or ISO form, booleans (also localized) and error codes.
/// A leading apostrophe forces text. Formulas (input starting with <c>=</c>) are not handled here.
/// </summary>
public static class CellInputParser
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static readonly Regex Time = new(@"^(?<date>.*?)\s*(?:(?<=\d)T)?(?<h>\d{1,4}):(?<m>\d{1,2})(?::(?<s>\d{1,2}(?:[.,]\d+)?))?\s*(?<ampm>[AaPp][Mm]?)?$",
        RegexOptions.CultureInvariant);

    private static readonly ConcurrentDictionary<(string Group, string Decimal), Regex> NumberPatterns = new();

    private static readonly Regex Iso = new(@"^(?<y>\d{4})-(?<m>\d{1,2})-(?<d>\d{1,2})$", RegexOptions.CultureInvariant);

    private static readonly Regex DateToken = new(@"\d+|[^\d\s/.,\-]+", RegexOptions.CultureInvariant);

    /// <summary>Parses cell input.</summary>
    /// <param name="input">The typed text.</param>
    /// <param name="culture">The culture for separators, currency, date order, month names and booleans.</param>
    /// <returns>
    /// The value and a number format code that suits it (for example <c>0%</c> or the culture's short date), or null when the
    /// cell's format should stay as it is.
    /// </returns>
    public static (CellValue Value, string SuggestedFormat) Parse(string input, CultureInfo culture)
    {
        culture ??= CultureInfo.CurrentCulture;
        if (string.IsNullOrEmpty(input)) return (CellValue.Empty, null);
        if (input[0] == '\'') return (CellValue.Text(input[1..]), null);
        var s = input.Trim();
        if (s.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || s.Equals(NumberFormat.BooleanText(true, culture), StringComparison.OrdinalIgnoreCase))
            return (CellValue.Boolean(true), null);
        if (s.Equals("FALSE", StringComparison.OrdinalIgnoreCase) || s.Equals(NumberFormat.BooleanText(false, culture), StringComparison.OrdinalIgnoreCase))
            return (CellValue.Boolean(false), null);
        var error = FormulaLexer.ErrorCodes.FirstOrDefault(e => e.Code.Equals(s, StringComparison.OrdinalIgnoreCase));
        if (error.Code != null) return (CellValue.Error(error.Error), null);
        if (TryParseNumber(s, culture, out var n, out var format) || TryParseDateTime(s, culture, out n, out format))
            return (CellValue.Number(n), format);
        return (CellValue.Text(input), null);
    }

    internal static bool TryParseValue(string text, CultureInfo culture, out double value)
    {
        value = 0;
        if (!text.Any(char.IsAsciiDigit)) return false;
        var s = text.Trim();
        return TryParseNumber(s, culture, out value, out _) || TryParseDateTime(s, culture, out value, out _);
    }

    internal static bool TryParseNumber(string s, CultureInfo culture, out double value, out string format)
    {
        value = 0;
        format = null;
        var nf = culture.NumberFormat;
        var negative = false;
        if (s.Length > 2 && s[0] == '(' && s[^1] == ')')
        {
            negative = true;
            s = s[1..^1].Trim();
        }

        negative ^= TakeSign(ref s);
        var symbol = nf.CurrencySymbol;
        var currency = 0;
        if (symbol.Length > 0 && s.StartsWith(symbol, StringComparison.Ordinal))
        {
            currency = 1;
            s = s[symbol.Length..].TrimStart();
            negative ^= TakeSign(ref s);
        }
        else if (symbol.Length > 0 && s.EndsWith(symbol, StringComparison.Ordinal))
        {
            currency = 2;
            s = s[..^symbol.Length].TrimEnd();
        }

        var percent = s.EndsWith('%');
        if (percent) s = s[..^1].TrimEnd();
        var m = NumberPatterns.GetOrAdd((nf.NumberGroupSeparator, nf.NumberDecimalSeparator), key => new Regex(
            @"^(?<int>\d+|\d{1,3}(?:" + Regex.Escape(key.Group) + @"\d{3})+)?(?:" + Regex.Escape(key.Decimal)
            + @"(?<frac>\d*))?(?:[eE](?<exp>[+-]?\d+))?$", RegexOptions.CultureInvariant)).Match(s);
        if (!m.Success || m.Groups["int"].Length + m.Groups["frac"].Length == 0) return false;
        var invariant = m.Groups["int"].Value.Replace(nf.NumberGroupSeparator, "") + "." + m.Groups["frac"].Value
                        + (m.Groups["exp"].Success ? "E" + m.Groups["exp"].Value : "");
        if (!double.TryParse(invariant, NumberStyles.Float, Inv, out value) || !double.IsFinite(value)) return false;
        if (percent) value /= 100;
        if (negative) value = -value;
        var decimals = m.Groups["frac"].Length > 0 ? ".00" : "";
        if (m.Groups["exp"].Success) format = "0.00E+00";
        else if (percent) format = "0" + decimals + "%";
        else if (currency > 0)
        {
            var literal = symbol == "$" ? "$" : "\"" + symbol + "\"";
            format = currency == 1 ? literal + "#,##0" + decimals : "#,##0" + decimals + " " + literal;
        }
        else if (m.Groups["int"].Value.Contains(nf.NumberGroupSeparator)) format = "#,##0" + decimals;

        return true;
    }

    private static bool TakeSign(ref string s)
    {
        if (s.Length < 2 || s[0] is not ('-' or '+')) return false;
        var minus = s[0] == '-';
        s = s[1..].TrimStart();
        return minus;
    }

    internal static bool TryParseDateTime(string s, CultureInfo culture, out double serial, out string format)
    {
        serial = 0;
        format = null;
        if (s.Length > 64) return false;
        var datePart = s;
        string timeFormat = null;
        var time = Time.Match(s);
        if (time.Success)
        {
            var h = int.Parse(time.Groups["h"].Value, Inv);
            var min = int.Parse(time.Groups["m"].Value, Inv);
            var sec = time.Groups["s"].Success ? double.Parse(time.Groups["s"].Value.Replace(',', '.'), Inv) : 0;
            var ampm = time.Groups["ampm"].Success;
            datePart = time.Groups["date"].Value.Trim();
            if (min > 59 || sec >= 60) return false;
            if (ampm)
            {
                if (h is < 1 or > 12) return false;
                h = h % 12 + (char.ToUpperInvariant(time.Groups["ampm"].Value[0]) == 'P' ? 12 : 0);
            }

            if (h > 23 && datePart.Length > 0) return false;
            serial = (h * 3600 + min * 60 + sec) / 86400;
            timeFormat = h > 23 ? "[h]:mm:ss" : BuiltInNumberFormats.ShortTime(culture, time.Groups["s"].Success, ampm);
            if (datePart.Length == 0)
            {
                format = timeFormat;
                return true;
            }
        }

        if (!TryParseDate(datePart, culture, out var day, out var dateFormat)) return false;
        serial += day;
        format = timeFormat == null ? dateFormat : BuiltInNumberFormats.ShortDate(culture) + " " + timeFormat;
        return true;
    }

    private static bool TryParseDate(string s, CultureInfo culture, out double serial, out string format)
    {
        serial = 0;
        format = null;
        int year, month, day = 1;
        var iso = Iso.Match(s);
        if (iso.Success)
        {
            year = int.Parse(iso.Groups["y"].Value, Inv);
            month = int.Parse(iso.Groups["m"].Value, Inv);
            day = int.Parse(iso.Groups["d"].Value, Inv);
            format = BuiltInNumberFormats.ShortDate(culture);
        }
        else
        {
            var numbers = new List<string>();
            var monthName = 0;
            foreach (Match token in DateToken.Matches(s))
            {
                if (char.IsDigit(token.Value[0])) numbers.Add(token.Value);
                else if (monthName == 0 && MonthNumber(token.Value, culture) is var named and > 0) monthName = named;
                else return false;
            }

            if (!Separated(s) || (monthName == 0 && !NumericSeparators(s, culture))) return false;
            var isYear = numbers.Select(n => n.Length >= 3 || int.Parse(n, Inv) > 31).ToList();
            year = DateTime.Today.Year;
            if (monthName > 0)
            {
                month = monthName;
                switch (numbers.Count)
                {
                    case 1 when isYear[0]:
                        year = int.Parse(numbers[0], Inv);
                        format = "mmm-yy";
                        break;
                    case 1:
                        day = int.Parse(numbers[0], Inv);
                        format = "d-mmm";
                        break;
                    case 2:
                        var yearIndex = isYear[0] ? 0 : 1;
                        year = Year(numbers[yearIndex]);
                        day = int.Parse(numbers[1 - yearIndex], Inv);
                        format = BuiltInNumberFormats.ShortDate(culture);
                        break;
                    default:
                        return false;
                }
            }
            else
            {
                var order = Order(culture);
                switch (numbers.Count)
                {
                    case 3:
                        var y = order.IndexOf('y');
                        if (isYear[0] && y != 0) order = "ymd";
                        year = Year(numbers[order.IndexOf('y')]);
                        month = int.Parse(numbers[order.IndexOf('m')], Inv);
                        day = int.Parse(numbers[order.IndexOf('d')], Inv);
                        format = BuiltInNumberFormats.ShortDate(culture);
                        break;
                    case 2 when isYear[1]:
                        month = int.Parse(numbers[0], Inv);
                        year = int.Parse(numbers[1], Inv);
                        format = "mmm-yy";
                        break;
                    case 2:
                        var dayFirst = order.IndexOf('d') < order.IndexOf('m');
                        day = int.Parse(numbers[dayFirst ? 0 : 1], Inv);
                        month = int.Parse(numbers[dayFirst ? 1 : 0], Inv);
                        format = "d-mmm";
                        break;
                    default:
                        return false;
                }
            }
        }

        if (year is < 1900 or > 9999 || month is < 1 or > 12 || day < 1 || day > ExcelDate.DaysInMonth(year, month)) return false;
        serial = ExcelDate.FromDate(year, month, day);
        return true;
    }

    private static bool Separated(string s)
    {
        for (var i = 1; i < s.Length; i++)
            if (char.IsDigit(s[i]) != char.IsDigit(s[i - 1]) && char.IsLetterOrDigit(s[i]) && char.IsLetterOrDigit(s[i - 1]))
                return false;
        return s.Length > 0 && !s.EndsWith('/') && !s.EndsWith('-') && !s.EndsWith(',');
    }

    private static bool NumericSeparators(string s, CultureInfo culture)
    {
        var parts = Regex.Split(s, @"\d+", RegexOptions.CultureInvariant).Select(p => p.Trim()).ToArray();
        var separator = parts.Length > 2 ? parts[1] : "";
        if (separator is not ("/" or "-") && separator != culture.DateTimeFormat.DateSeparator) return false;
        return parts[0].Length == 0 && parts[1..^1].All(p => p == separator) && (parts[^1].Length == 0 || (parts[^1] == separator && separator == "."));
    }

    private static int Year(string text)
    {
        var y = int.Parse(text, Inv);
        return text.Length > 2 ? y : y < 30 ? 2000 + y : 1900 + y;
    }

    private static string Order(CultureInfo culture)
    {
        var pattern = culture.DateTimeFormat.ShortDatePattern.ToLowerInvariant();
        return new string(pattern.Where(c => c is 'd' or 'm' or 'y').Distinct().ToArray());
    }

    private static int MonthNumber(string name, CultureInfo culture)
    {
        var dtf = culture.DateTimeFormat;
        var trimmed = name.TrimEnd('.');
        for (var i = 1; i <= 12; i++)
            if (trimmed.Equals(dtf.GetMonthName(i), StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals(dtf.GetAbbreviatedMonthName(i).TrimEnd('.'), StringComparison.OrdinalIgnoreCase)
                || (trimmed.Length >= 3 && dtf.GetMonthName(i).StartsWith(trimmed, StringComparison.OrdinalIgnoreCase)))
                return i;
        return 0;
    }
}
