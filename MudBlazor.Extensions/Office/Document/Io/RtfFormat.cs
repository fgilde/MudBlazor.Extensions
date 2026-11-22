using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using RtfPipe;

namespace MudBlazor.Extensions.Office.Document;

internal static class RtfFormat
{
    private const char TabMark = (char)0xFFFC;
    private const string CodePageFallback = "1252";

    private static readonly HashSet<string> Destinations =
        ["fonttbl", "colortbl", "stylesheet", "info", "pict", "object", "objdata", "fldinst", "listtext", "pntext", "listtable", "listoverridetable"];

    private static readonly Regex Markup = new(
        "(?<tab>\\uFFFC?<span style=\"display:inline-block;width:[^\"<>]*\"></span>)|(<t[dh](?:\\s[^<>]*)?>)&nbsp;(?=</t[dh]>)|<[a-zA-Z][a-zA-Z0-9]*\\s[^<>]*>",
        RegexOptions.CultureInvariant);

    private static readonly Regex Margin = new(@"(?<=[""';])margin:([^;""']*)", RegexOptions.CultureInvariant);

    internal static TextDocument Read(Stream stream) => Read(stream, RtfLimits.Default);

    internal static TextDocument Read(Stream stream, RtfLimits limits)
    {
        var bytes = HtmlEncoding.ReadLimited(stream, HtmlReader.MaxLength, _ => Encoding.Latin1);
        var start = Start(bytes);
        if (start < 0) throw new InvalidDataException("The file is not an RTF document.");
        if (bytes.Length > HtmlReader.MaxLength) throw new InvalidDataException("The RTF is too large to read.");
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

    private static string Prepare(string rtf, RtfLimits limits)
    {
        StringBuilder output = null;
        var copied = 0;
        var groups = new Group[limits.Depth + 2];
        groups[0] = new Group(1, false, false, 0);
        int depth = 0, tokens = 0, paragraphs = 0;
        long segments = 0, runWork = 0, tableWork = 0, elements = 0;
        bool text = false, inTable = false;
        for (var i = 0; i < rtf.Length; i++)
        {
            var c = rtf[i];
            if (c == '{')
            {
                if (++depth > limits.Depth) throw new InvalidDataException("The RTF is nested too deeply.");
                groups[depth] = groups[depth - 1] with { Fresh = true, Bin = 0 };
                Token();
            }
            else if (c == '}')
            {
                if (depth > 0) depth--;
                Token();
            }
            else if (c == '\t')
            {
                Token();
                Tab(i);
            }
            else if (c == '\\' && i + 1 < rtf.Length && char.IsAsciiLetter(rtf[i + 1]))
                i = Word(i) - 1;
            else if (c == '\\' && i + 1 < rtf.Length)
            {
                var symbol = rtf[++i];
                if (symbol == '*' && groups[depth].Fresh) groups[depth].Skip = true;
                groups[depth].Fresh = false;
                if (symbol is '\r' or '\n') End("par");
                else if (symbol is '\'' or '\\' or '{' or '}' or '~' or '-' or '_') Text();
                else Token();
                if (symbol == '\'') i += 2;
            }
            else if (c is not ('\r' or '\n'))
            {
                groups[depth].Fresh = false;
                Text();
                if (groups[depth].Bin > 0) i = (int)Math.Min(rtf.Length, (long)i + groups[depth].Bin) - 1;
            }

            if (tokens > limits.Tokens) throw new InvalidDataException("The RTF has too many control words.");
        }

        End("sect");
        return output == null ? rtf : output.Append(rtf, copied, rtf.Length - copied).ToString();

        int Word(int at)
        {
            var start = at + 1;
            var end = start;
            while (end < rtf.Length && char.IsAsciiLetter(rtf[end])) end++;
            var word = rtf.AsSpan(start, end - start);
            var digits = end;
            if (end + 1 < rtf.Length && rtf[end] == '-' && char.IsAsciiDigit(rtf[end + 1])) end++;
            while (end < rtf.Length && char.IsAsciiDigit(rtf[end])) end++;
            var number = end == digits ? (int?)null : long.TryParse(rtf.AsSpan(digits, end - digits), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n)
                ? (int)Math.Clamp(n, int.MinValue, int.MaxValue)
                : rtf[digits] == '-' ? int.MinValue : int.MaxValue;
            Token();
            if (groups[depth].Fresh && Destinations.Contains(word.ToString())) groups[depth].Skip = true;
            groups[depth].Fresh = false;
            if (word is "u")
            {
                Text();
                return Fallback(end, groups[depth].Uc);
            }

            if (end < rtf.Length && rtf[end] == ' ') end++;
            if (word is "tab") Tab(at);
            else if (word is "line") segments++;
            else if (word is "par" or "sect" or "cell" or "nestcell" or "row" or "nestrow") End(word.ToString());
            else if (word is "intbl") inTable = true;
            else if (word is "pard") inTable = false;
            else if (word is "uc") groups[depth].Uc = number ?? 0;
            else if (word is "bin") groups[depth].Bin = Math.Max(1, number ?? 1);
            else if (word is "ansicpg" or "cpg" && number is { } codePage && !Available(codePage))
                Replace(at, end, "\\" + word.ToString() + CodePageFallback + " ");
            return end;
        }

        int Fallback(int at, int count)
        {
            for (var k = 0; k < count && at < rtf.Length; k++)
            {
                var ch = rtf[at++];
                if (ch == ' ' && k == 0 && at < rtf.Length) ch = rtf[at++];
                if (ch == '\\' && at < rtf.Length && rtf[at] == '\'') at += 3;
            }

            return Math.Min(at, rtf.Length);
        }

        void Tab(int at)
        {
            segments++;
            if (!groups[depth].Skip) Replace(at, at, TabMark.ToString());
        }

        void Replace(int from, int to, string replacement)
        {
            output ??= new StringBuilder(rtf.Length + 4096);
            output.Append(rtf, copied, from - copied).Append(replacement);
            copied = to;
        }

        void Token()
        {
            tokens++;
            text = false;
        }

        void Text()
        {
            if (!text) segments++;
            text = true;
        }

        void End(string word)
        {
            runWork += segments * segments;
            (segments, text) = (0, false);
            if (word is not ("par" or "sect") || word is "par" && inTable) tableWork += elements + RtfLimits.CellWork;
            if (word is "row" or "nestrow" || word is "par" && !inTable) elements++;
            if (word is not "sect") paragraphs++;
            if (runWork > limits.RunWork || tableWork > limits.TableWork || paragraphs > limits.Paragraphs)
                throw new InvalidDataException("The RTF is too complex to read.");
            if (word is "sect") elements = 0;
        }
    }

    private static bool Available(int codePage) =>
        codePage is 0 or 42 or 1252 or 20127 or 28591 or 65001 || codePage is > 0 and <= ushort.MaxValue && CodePagesEncodingProvider.Instance.GetEncoding(codePage) != null;

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

    private record struct Group(int Uc, bool Skip, bool Fresh, int Bin);

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

    internal static RtfLimits Default { get; } = new(256, 1 << 22, 1 << 17, 1L << 33, 1L << 27, HtmlReader.MaxLength);
}
