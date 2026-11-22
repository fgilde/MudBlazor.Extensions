using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using RtfPipe;

namespace MudBlazor.Extensions.Office.Document;

internal static class RtfFormat
{
    private const int MaxDepth = 256;
    private const int MaxTokens = 1 << 22;
    private const long MaxRunWork = 1L << 33;
    private const long MaxTableWork = 1L << 29;
    private const char TabMark = '\uFDD0';
    private static readonly Regex Tag = new(@"<[a-zA-Z][a-zA-Z0-9]*\s[^<>]*>", RegexOptions.CultureInvariant);
    private static readonly Regex Margin = new(@"(?<=[""';])margin:([^;""']*)", RegexOptions.CultureInvariant);

    internal static TextDocument Read(Stream stream)
    {
        var bytes = HtmlEncoding.ReadLimited(stream, HtmlReader.MaxLength, _ => Encoding.Latin1);
        if (!bytes.AsSpan().StartsWith(@"{\rtf"u8)) throw new InvalidDataException("The file is not an RTF document.");
        if (bytes.Length > HtmlReader.MaxLength) throw new InvalidDataException("The RTF is too large to read.");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var rtf = Prepare(Encoding.Latin1.GetString(bytes));
        var culture = CultureInfo.CurrentCulture;
        string html;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            html = Rtf.ToHtml(new RtfSource(new StringReader(rtf)), new RtfHtmlSettings { ImageUriGetter = ImageUri });
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            throw new InvalidDataException("The RTF document is damaged.", e);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }

        return HtmlReader.Read(Tag.Replace(html, tag => Margin.Replace(tag.Value, m => LeftMargin(m.Groups[1].Value))), Text);
    }

    private static string Prepare(string rtf)
    {
        var tabs = new List<(int Start, int Length)>();
        int depth = 0, tokens = 0;
        long segments = 0, runWork = 0, elements = 0, tableBreaks = 0;
        bool text = false, inTable = false;
        for (var i = 0; i < rtf.Length; i++)
        {
            var c = rtf[i];
            if (c is '{' or '}')
            {
                if (c == '{' && ++depth > MaxDepth) throw new InvalidDataException("The RTF is nested too deeply.");
                if (c == '}' && depth > 0) depth--;
                tokens++;
                text = false;
            }
            else if (c == '\t')
            {
                tabs.Add((i, 1));
                Text();
            }
            else if (c == '\\' && i + 1 < rtf.Length && char.IsAsciiLetter(rtf[i + 1]))
            {
                var start = i++;
                while (i < rtf.Length && char.IsAsciiLetter(rtf[i])) i++;
                var word = rtf.AsSpan(start + 1, i - start - 1);
                if (i < rtf.Length && rtf[i] == '-') i++;
                while (i < rtf.Length && char.IsAsciiDigit(rtf[i])) i++;
                if (i < rtf.Length && rtf[i] == ' ') i++;
                i--;
                tokens++;
                if (word is "tab")
                {
                    tabs.Add((start, i - start + 1));
                    Text();
                }
                else if (word is "par" or "sect" or "cell" or "nestcell" or "row" or "nestrow") End(word is not ("par" or "sect"), word is "sect");
                else if (word is not "u") text = false;
                if (word is "line") segments++;
                else if (word is "intbl") inTable = true;
                else if (word is "pard") inTable = false;
            }
            else if (c == '\\' && i + 1 < rtf.Length)
            {
                var symbol = rtf[++i];
                if (symbol is '\r' or '\n') End(false, false);
                else if (symbol == '\'') i += 2;
                else if (symbol == '*') (tokens, text) = (tokens + 1, false);
                if (symbol is '\'' or '\\' or '{' or '}' or '~' or '-' or '_') Text();
            }
            else if (c is not ('\r' or '\n'))
                Text();

            if (tokens > MaxTokens) throw new InvalidDataException("The RTF has too many control words.");
        }

        End(false, true);
        if (tabs.Count == 0) return rtf;
        var result = new StringBuilder(rtf.Length);
        var at = 0;
        foreach (var (start, length) in tabs)
        {
            result.Append(rtf, at, start - at).Append(TabMark);
            at = start + length;
        }

        return result.Append(rtf, at, rtf.Length - at).ToString();

        void Text()
        {
            if (!text) segments++;
            text = true;
        }

        void End(bool cell, bool section)
        {
            runWork += segments * segments;
            (segments, text) = (0, false);
            elements++;
            if (cell || inTable) tableBreaks++;
            if (runWork > MaxRunWork || tableBreaks * elements > MaxTableWork) throw new InvalidDataException("The RTF is too complex to read.");
            if (section) (elements, tableBreaks) = (0, 0);
        }
    }

    private static string ImageUri(Picture picture) =>
        picture.Bytes is { Length: > 0 } bytes && picture.MimeType() is "image/png" or "image/jpeg"
            ? "data:" + picture.MimeType() + ";base64," + Convert.ToBase64String(bytes)
            : null;

    private static string LeftMargin(string values)
    {
        var parts = values.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var left = parts.Length switch { 0 => "0", 1 => parts[0], 2 or 3 => parts[1], _ => parts[3] };
        return left == "0" ? "" : "margin-left:" + left;
    }

    private static string Text(string text)
    {
        if (text.IndexOfAny([TabMark, '\u00A0']) < 0) return text;
        if (text == "\u00A0") return "";
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            if (chars[i] == TabMark) chars[i] = '\t';
            else if (chars[i] == '\u00A0' && i > 0 && chars[i - 1] == ' ') chars[i] = ' ';
        var length = chars.Length > 1 && chars[^1] == '\u00A0' && chars[^2] == '\n' ? chars.Length - 1 : chars.Length;
        return new string(chars, 0, length);
    }
}
