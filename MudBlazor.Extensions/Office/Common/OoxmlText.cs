using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Office;

internal static class OoxmlText
{
    private static readonly Regex EscapeLike = new("_x[0-9A-Fa-f]{4}_", RegexOptions.CultureInvariant);
    private static readonly Regex Escaped = new("_x([0-9A-Fa-f]{4})_", RegexOptions.CultureInvariant);

    internal static string Decode(string s) =>
        s != null && s.Contains("_x", StringComparison.Ordinal) ? Escaped.Replace(s, m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString()) : s;

    private static bool Valid(char c, bool breaks = false) =>
        c is >= ' ' and < (char)0xD800 or > (char)0xDFFF and < (char)0xFFFE || !breaks && c is '\t' or '\n';

    internal static string Escape(string s, bool literals = true, bool breaks = false)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var plain = true;
        foreach (var c in s)
        {
            if (Valid(c, breaks)) continue;
            plain = false;
            break;
        }

        if (plain && (!literals || !s.Contains("_x", StringComparison.Ordinal))) return s;
        var escaped = literals ? EscapeLike.Replace(s, m => "_x005F" + m.Value) : s;
        var sb = new StringBuilder(escaped.Length + 8);
        for (var i = 0; i < escaped.Length; i++)
        {
            var c = escaped[i];
            if (char.IsHighSurrogate(c) && i + 1 < escaped.Length && char.IsLowSurrogate(escaped[i + 1]))
            {
                sb.Append(c).Append(escaped[++i]);
                continue;
            }

            if (Valid(c, breaks)) sb.Append(c);
            else sb.Append("_x").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture)).Append('_');
        }

        return sb.ToString();
    }
}
