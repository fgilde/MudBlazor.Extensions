using System.Globalization;
using System.Text;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>The built-in number formats of SpreadsheetML (ids 0 to 49) that files reference by id instead of storing the code.</summary>
public static class BuiltInNumberFormats
{
    private const int ShortDateId = 14;
    private const int ShortDateTimeId = 22;

    private static readonly Dictionary<int, string> Formats = new()
    {
        [0] = "General",
        [1] = "0",
        [2] = "0.00",
        [3] = "#,##0",
        [4] = "#,##0.00",
        [5] = "$#,##0_);($#,##0)",
        [6] = "$#,##0_);[Red]($#,##0)",
        [7] = "$#,##0.00_);($#,##0.00)",
        [8] = "$#,##0.00_);[Red]($#,##0.00)",
        [9] = "0%",
        [10] = "0.00%",
        [11] = "0.00E+00",
        [12] = "# ?/?",
        [13] = "# ??/??",
        [14] = "mm-dd-yy",
        [15] = "d-mmm-yy",
        [16] = "d-mmm",
        [17] = "mmm-yy",
        [18] = "h:mm AM/PM",
        [19] = "h:mm:ss AM/PM",
        [20] = "h:mm",
        [21] = "h:mm:ss",
        [22] = "m/d/yy h:mm",
        [37] = "#,##0 ;(#,##0)",
        [38] = "#,##0 ;[Red](#,##0)",
        [39] = "#,##0.00;(#,##0.00)",
        [40] = "#,##0.00;[Red](#,##0.00)",
        [41] = "_(* #,##0_);_(* \\(#,##0\\);_(* \"-\"_);_(@_)",
        [42] = "_(\"$\"* #,##0_);_(\"$\"* \\(#,##0\\);_(\"$\"* \"-\"_);_(@_)",
        [43] = "_(* #,##0.00_);_(* \\(#,##0.00\\);_(* \"-\"??_);_(@_)",
        [44] = "_(\"$\"* #,##0.00_);_(\"$\"* \\(#,##0.00\\);_(\"$\"* \"-\"??_);_(@_)",
        [45] = "mm:ss",
        [46] = "[h]:mm:ss",
        [47] = "mmss.0",
        [48] = "##0.0E+0",
        [49] = "@"
    };

    /// <summary>
    /// Gets the code of a built-in format. With a culture, the short date (14) and short date and time (22) formats follow
    /// that culture's short date pattern, as Excel displays them.
    /// </summary>
    /// <param name="id">The format id.</param>
    /// <param name="culture">The display culture, or null for the codes defined by the standard.</param>
    /// <returns>The format code, or null if the id is not built in.</returns>
    public static string Get(int id, CultureInfo culture = null) => (id, culture) switch
    {
        (ShortDateId, not null) => ShortDate(culture),
        (ShortDateTimeId, not null) => ShortDate(culture) + " " + ShortTime(culture, false, false),
        _ => Formats.GetValueOrDefault(id)
    };

    /// <summary>Finds the id of a built-in format code.</summary>
    /// <param name="formatCode">The format code.</param>
    /// <param name="id">The id when found.</param>
    /// <param name="culture">A culture whose short date codes also map to ids 14 and 22, or null.</param>
    /// <returns>True if the code is built in.</returns>
    public static bool TryGetId(string formatCode, out int id, CultureInfo culture = null)
    {
        foreach (var candidate in Formats.Keys)
        {
            if (!string.Equals(Get(candidate, culture), formatCode, StringComparison.OrdinalIgnoreCase)) continue;
            id = candidate;
            return true;
        }

        id = -1;
        return false;
    }

    internal static string ShortDate(CultureInfo culture)
    {
        var dtf = culture.DateTimeFormat;
        var sb = new StringBuilder();
        var pattern = dtf.ShortDatePattern;
        for (var i = 0; i < pattern.Length; i++)
        {
            switch (pattern[i])
            {
                case 'M':
                    sb.Append('m');
                    break;
                case '/':
                    sb.Append(dtf.DateSeparator);
                    break;
                case '\'':
                    var end = pattern.IndexOf('\'', i + 1);
                    if (end < 0) end = pattern.Length;
                    sb.Append('"').Append(pattern, i + 1, end - i - 1).Append('"');
                    i = end;
                    break;
                default:
                    sb.Append(pattern[i]);
                    break;
            }
        }

        return sb.ToString();
    }

    internal static string ShortTime(CultureInfo culture, bool seconds, bool amPm) =>
        (amPm || !culture.DateTimeFormat.ShortTimePattern.Contains("HH") ? "h:mm" : "hh:mm")
        + (seconds ? ":ss" : "") + (amPm ? " AM/PM" : "");
}
