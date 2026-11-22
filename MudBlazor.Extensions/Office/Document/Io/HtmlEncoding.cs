using System.Text;

namespace MudBlazor.Extensions.Office.Document;

internal static class HtmlEncoding
{
    private const int Prescan = 4096;

    private static readonly HashSet<string> WideNames = new(StringComparer.OrdinalIgnoreCase) { "unicode", "unicodefffe", "ucs-2", "ucs2", "iso-10646-ucs-2", "csunicode" };

    private static readonly HashSet<string> Latin1Names = new(StringComparer.OrdinalIgnoreCase) { "iso-8859-1", "iso8859-1", "iso_8859-1", "latin1", "l1", "us-ascii", "ascii" };

    internal static byte[] ReadLimited(Stream stream, int maxChars, Func<byte[], Encoding> encodingOf)
    {
        if (stream.CanSeek && stream.Length - stream.Position > 4L * (maxChars + 1) + 4) throw new InvalidDataException("The file is too large to read.");
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        long limit = -1;
        for (int read; (read = stream.Read(chunk, 0, chunk.Length)) > 0;)
        {
            buffer.Write(chunk, 0, read);
            if (limit < 0 && buffer.Length >= Prescan) limit = encodingOf(buffer.GetBuffer()[..Prescan]).GetMaxByteCount(maxChars) + 4L;
            if (limit >= 0 && buffer.Length > limit) throw new InvalidDataException("The file is too large to read.");
        }

        return buffer.ToArray();
    }

    internal static (Encoding Encoding, int Preamble) Detect(ReadOnlySpan<byte> bytes) =>
        Bom(bytes) ?? (Declared(Encoding.Latin1.GetString(bytes[..Math.Min(bytes.Length, Prescan)])) ?? new UTF8Encoding(false), 0);

    internal static (Encoding Encoding, int Preamble)? Bom(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF])) return (new UTF8Encoding(false), 3);
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xFE, 0, 0])) return (new UTF32Encoding(false, false), 4);
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xFE])) return (Encoding.Unicode, 2);
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xFE, 0xFF])) return (Encoding.BigEndianUnicode, 2);
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0, 0, 0xFE, 0xFF])) return (new UTF32Encoding(true, false), 4);
        return null;
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
        if (name.StartsWith("utf-16", StringComparison.OrdinalIgnoreCase) || name.StartsWith("utf-32", StringComparison.OrdinalIgnoreCase) || WideNames.Contains(name) ||
            name.Contains("utf-7", StringComparison.OrdinalIgnoreCase) || name.Contains("utf7", StringComparison.OrdinalIgnoreCase))
            return new UTF8Encoding(false);
        if (Latin1Names.Contains(name)) return CodePagesEncodingProvider.Instance.GetEncoding(1252) ?? Encoding.Latin1;
        try
        {
            var encoding = Encoding.GetEncoding(name);
            return encoding.CodePage is 65000 or 1200 or 1201 or 12000 or 12001 ? new UTF8Encoding(false) : encoding;
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException)
        {
            return CodePagesEncodingProvider.Instance.GetEncoding(name);
        }
    }
}
