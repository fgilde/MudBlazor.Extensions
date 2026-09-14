using System.IO.Compression;
using System.Text;

namespace MudBlazor.Extensions.Core.EInvoice;

/// <summary>
/// Reads the files a pdf carries inside it, which is where ZUGFeRD and Factur-X keep the invoice xml.
/// </summary>
/// <remarks>
/// No pdf library: an embedded file is always a stream object of its own - the pdf format does not allow
/// streams inside object streams - so walking the raw bytes from one "stream" keyword to the next finds every
/// one of them. Deflate is what those streams use in practice, and ZLibStream is in the framework.
/// This reads attachments; it is not a pdf parser and does not try to be one.
/// </remarks>
public static class MudExPdfAttachments
{
    private static readonly byte[] StreamKeyword = Encoding.ASCII.GetBytes("stream");
    private static readonly byte[] EndStreamKeyword = Encoding.ASCII.GetBytes("endstream");

    /// <summary>True when the bytes start with the pdf signature.</summary>
    public static bool IsPdf(byte[] bytes)
        => bytes is { Length: > 4 } && bytes[0] == '%' && bytes[1] == 'P' && bytes[2] == 'D' && bytes[3] == 'F';

    /// <summary>
    /// Returns the decoded content of every stream of the document that turns out to be xml, together with
    /// the file name the pdf announced for it when there is one.
    /// </summary>
    public static IEnumerable<(string Name, string Content)> ReadXmlAttachments(byte[] pdf)
    {
        if (!IsPdf(pdf))
            yield break;

        var names = ReadEmbeddedFileNames(pdf);
        var index = 0;
        var found = 0;

        while (found < 8 && TryReadNextStream(pdf, ref index, out var content))
        {
            var text = Decode(content);
            if (text == null || !LooksLikeXml(text))
                continue;

            found++;
            yield return (names.ElementAtOrDefault(found - 1), text);
        }
    }

    /// <summary>
    /// The /F and /UF entries of the file specifications, in the order they appear. Only a hint for the ui -
    /// matching them to a stream exactly would need the cross reference table.
    /// </summary>
    private static List<string> ReadEmbeddedFileNames(byte[] pdf)
    {
        var result = new List<string>();
        var text = Encoding.ASCII.GetString(pdf, 0, Math.Min(pdf.Length, 4 * 1024 * 1024));

        foreach (var key in new[] { "/UF (", "/F (" })
        {
            var at = 0;
            while ((at = text.IndexOf(key, at, StringComparison.Ordinal)) >= 0)
            {
                at += key.Length;
                var end = text.IndexOf(')', at);
                if (end < 0)
                    break;

                var name = text.Substring(at, end - at);
                if (name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) && !result.Contains(name))
                    result.Add(name);
                at = end;
            }
        }

        return result;
    }

    private static bool TryReadNextStream(byte[] pdf, ref int index, out byte[] content)
    {
        content = null;
        var start = IndexOf(pdf, StreamKeyword, index);
        if (start < 0)
            return false;

        var from = start + StreamKeyword.Length;

        // The keyword is followed by CRLF or LF, and nothing else is allowed between it and the data.
        if (from < pdf.Length && pdf[from] == '\r') from++;
        if (from < pdf.Length && pdf[from] == '\n') from++;

        var end = IndexOf(pdf, EndStreamKeyword, from);
        if (end < 0)
        {
            index = pdf.Length;
            return false;
        }

        var length = end - from;
        while (length > 0 && (pdf[from + length - 1] == '\n' || pdf[from + length - 1] == '\r'))
            length--;

        content = new byte[length];
        Array.Copy(pdf, from, content, 0, length);
        index = end + EndStreamKeyword.Length;
        return true;
    }

    private static string Decode(byte[] content)
    {
        if (content.Length == 0)
            return null;

        // Deflate, with or without the zlib header, and the rare uncompressed stream.
        foreach (var candidate in new[] { TryInflate(content, zlib: true), TryInflate(content, zlib: false), content })
        {
            if (candidate is not { Length: > 0 })
                continue;

            var text = Encoding.UTF8.GetString(candidate);
            if (LooksLikeXml(text))
                return text;
        }

        return null;
    }

    private static byte[] TryInflate(byte[] content, bool zlib)
    {
        try
        {
            using var source = new MemoryStream(content);
            using Stream decompressor = zlib ? new ZLibStream(source, CompressionMode.Decompress) : new DeflateStream(source, CompressionMode.Decompress);
            using var target = new MemoryStream();
            decompressor.CopyTo(target);
            return target.ToArray();
        }
        catch (Exception)
        {
            // Not deflate, or not this variant of it - the caller tries the next one.
            return null;
        }
    }

    private static bool LooksLikeXml(string text)
    {
        var head = text.Substring(0, Math.Min(text.Length, 200)).TrimStart('\uFEFF', ' ', '\r', '\n', '\t');
        return head.StartsWith("<?xml", StringComparison.Ordinal) || head.StartsWith("<", StringComparison.Ordinal);
    }

    private static int IndexOf(byte[] haystack, byte[] needle, int start)
    {
        for (var i = Math.Max(start, 0); i <= haystack.Length - needle.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] == needle[j])
                    continue;
                match = false;
                break;
            }

            if (match)
                return i;
        }

        return -1;
    }
}
