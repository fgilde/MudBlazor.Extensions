using System.Text;

namespace MudBlazor.Extensions.Office.Document;

internal static class HtmlEncoding
{
    private const int Prescan = 4096;

    internal static (Encoding Encoding, int Preamble) Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF])) return (new UTF8Encoding(false), 3);
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xFE, 0, 0])) return (new UTF32Encoding(false, false), 4);
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xFE])) return (Encoding.Unicode, 2);
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xFE, 0xFF])) return (Encoding.BigEndianUnicode, 2);
        return (Declared(Encoding.Latin1.GetString(bytes[..Math.Min(bytes.Length, Prescan)])) ?? new UTF8Encoding(false), 0);
    }

    private static Encoding Declared(string head)
    {
        for (var meta = head.IndexOf("<meta", StringComparison.OrdinalIgnoreCase); meta >= 0; meta = head.IndexOf("<meta", meta + 5, StringComparison.OrdinalIgnoreCase))
        {
            var end = head.IndexOf('>', meta);
            var tag = end < 0 ? head[meta..] : head[meta..end];
            var at = tag.IndexOf("charset", StringComparison.OrdinalIgnoreCase);
            if (at < 0) continue;
            var i = at + 7;
            while (i < tag.Length && (char.IsWhiteSpace(tag[i]) || tag[i] is '=' or '"' or '\'')) i++;
            var start = i;
            while (i < tag.Length && (char.IsAsciiLetterOrDigit(tag[i]) || tag[i] is '-' or '_' or '.' or ':')) i++;
            if (Named(tag[start..i]) is { } encoding) return encoding;
        }

        return null;
    }

    private static Encoding Named(string name)
    {
        if (name.Length == 0) return null;
        if (name.StartsWith("utf-16", StringComparison.OrdinalIgnoreCase) || name.StartsWith("utf-32", StringComparison.OrdinalIgnoreCase)) return new UTF8Encoding(false);
        try
        {
            return Encoding.GetEncoding(name);
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException)
        {
            return CodePagesEncodingProvider.Instance.GetEncoding(name);
        }
    }
}
