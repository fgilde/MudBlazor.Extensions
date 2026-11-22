using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using RtfPipe;

namespace MudBlazor.Extensions.Office.Document;

internal static class RtfFormat
{
    private static readonly Regex Markup = new(
        "(?<tab>\\uFFFC?<span style=\"display:inline-block;width:[^\"<>]*\"></span>)|(<t[dh](?:\\s[^<>]*)?>)&nbsp;(?=</t[dh]>)|<[a-zA-Z][a-zA-Z0-9]*\\s[^<>]*>",
        RegexOptions.CultureInvariant);

    private static readonly Regex Margin = new(@"(?<=[""';])margin:([^;""']*)", RegexOptions.CultureInvariant);

    internal static TextDocument Read(Stream stream) => Read(stream, RtfLimits.Default);

    internal static TextDocument Read(Stream stream, RtfLimits limits)
    {
        var maxChars = DocumentLoadLimits.Current.RtfCharacters;
        var bytes = HtmlEncoding.ReadLimited(stream, maxChars, _ => Encoding.Latin1);
        var start = Start(bytes);
        if (start < 0) throw new InvalidDataException("The file is not an RTF document.");
        if (bytes.Length > maxChars) throw new InvalidDataException("The RTF is too large to read.");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var culture = CultureInfo.CurrentCulture;
        try
        {
            var rtf = Prepare(Encoding.Latin1.GetString(bytes, start, bytes.Length - start), limits);
            bytes = null;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            string html;
            using (var writer = new LimitedWriter(limits.Html))
            {
                Rtf.ToHtml(new RtfSource(new StringReader(rtf)), writer, new RtfHtmlSettings { ImageUriGetter = ImageUri });
                rtf = null;
                html = writer.ToString();
            }

            return HtmlReader.Read(Markup.Replace(html, Rewrite), Text);
        }
        catch (Exception e) when (e is not (OutOfMemoryException or InvalidDataException))
        {
            throw new InvalidDataException("The RTF document is damaged.", e);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    internal static int Start(ReadOnlySpan<byte> bytes)
    {
        var at = bytes.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]) ? 3 : 0;
        while (at < bytes.Length && bytes[at] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n') at++;
        return bytes[at..].StartsWith(@"{\rtf"u8) ? at : -1;
    }

    internal static string Prepare(string rtf, RtfLimits limits)
    {
        var output = new StringBuilder(rtf.Length + 4096);
        RtfPolicy.Apply(RtfTokenizer.Tokenize(rtf, limits.Tokens), limits, token => token.Write(output));
        return output.ToString();
    }

    internal static List<RtfToken> Canonical(string rtf, RtfLimits limits)
    {
        var tokens = new List<RtfToken>();
        RtfPolicy.Apply(RtfTokenizer.Tokenize(rtf, limits.Tokens), limits, tokens.Add);
        return tokens;
    }

    internal static string Emit(IEnumerable<RtfToken> tokens)
    {
        var output = new StringBuilder();
        foreach (var token in tokens) token.Write(output);
        return output.ToString();
    }

    private static string ImageUri(Picture picture) =>
        picture.Bytes is { Length: > 0 } bytes && picture.MimeType() is "image/png" or "image/jpeg"
            ? "data:" + picture.MimeType() + ";base64," + Convert.ToBase64String(bytes)
            : null;

    private static string Rewrite(Match match)
    {
        if (match.Groups["tab"].Success)
            return "<span style=\"white-space:pre\">\t</span>";
        var tag = match.Groups[1].Success ? match.Groups[1].Value : match.Value;
        return Margin.Replace(tag, m => LeftMargin(m.Groups[1].Value));
    }

    private static string LeftMargin(string values)
    {
        var parts = values.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var left = parts.Length switch { 0 => "0", 1 => parts[0], 2 or 3 => parts[1], _ => parts[3] };
        return left == "0" ? "" : "margin-left:" + left;
    }

    private static string Text(string text)
    {
        if (!text.Contains('\u00A0')) return text;
        var chars = text.ToCharArray();
        for (var i = 1; i < chars.Length; i++)
            if (chars[i] == '\u00A0' && chars[i - 1] == ' ') chars[i] = ' ';
        var length = chars.Length > 1 && chars[^1] == '\u00A0' && chars[^2] == '\n' ? chars.Length - 1 : chars.Length;
        return new string(chars, 0, length);
    }

    private sealed class LimitedWriter(int limit) : StringWriter(CultureInfo.InvariantCulture)
    {
        public override void Write(char value)
        {
            Check(1);
            base.Write(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            Check(count);
            base.Write(buffer, index, count);
        }

        public override void Write(ReadOnlySpan<char> buffer)
        {
            Check(buffer.Length);
            base.Write(buffer);
        }

        public override void Write(string value)
        {
            Check(value?.Length ?? 0);
            base.Write(value);
        }

        private void Check(int count)
        {
            if ((long)GetStringBuilder().Length + count > limit) throw new InvalidDataException("The RTF converts to too much HTML.");
        }
    }
}

internal readonly record struct RtfLimits(int Depth, int Tokens, int Paragraphs, long RunWork, long TableWork, int Html)
{
    internal const long CellWork = 1500;

    internal const int DateLength = 32;

    internal const int TableNesting = 32;

    internal const int ListLevel = 8;

    internal static RtfLimits Default { get; } = new(256, 1 << 22, 1 << 17, 1L << 33, 1L << 27, HtmlReader.MaxLength);
}
