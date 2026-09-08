using System.Text;

namespace MudBlazor.Extensions.Helper.Internal;

/// <summary>
/// A minimal, dependency-free RFC 822 / MIME (.eml) parser: reads headers, walks multipart/* bodies, decodes
/// quoted-printable and base64 encoded parts and exposes the text/plain body, text/html body and inline
/// (Content-ID referenced) attachments needed to render an email similarly to <c>MsgReader</c> for .msg files.
/// </summary>
internal class MimeMessage
{
    public string Subject { get; private set; }
    public string From { get; private set; }
    public string To { get; private set; }
    public string Cc { get; private set; }
    public string Date { get; private set; }
    public string TextBody { get; private set; }
    public string HtmlBody { get; private set; }
    public List<(string ContentId, string MimeType, byte[] Data)> InlineParts { get; } = new();

    public static MimeMessage Parse(string rawText)
    {
        var message = new MimeMessage();
        var (headers, body) = SplitHeadersAndBody(rawText);

        message.Subject = DecodeHeaderValue(GetHeader(headers, "Subject"));
        message.From = DecodeHeaderValue(GetHeader(headers, "From"));
        message.To = DecodeHeaderValue(GetHeader(headers, "To"));
        message.Cc = DecodeHeaderValue(GetHeader(headers, "Cc"));
        message.Date = GetHeader(headers, "Date");

        message.ParseBody(headers, body);
        return message;
    }

    private void ParseBody(Dictionary<string, string> headers, string body)
    {
        var contentType = GetHeader(headers, "Content-Type") ?? "text/plain";
        var boundary = ExtractParameter(contentType, "boundary");

        if (boundary != null)
        {
            foreach (var part in SplitByBoundary(body, boundary))
                ParsePart(part);
        }
        else
        {
            ApplyPart(headers, body);
        }
    }

    private void ParsePart(string partText)
    {
        var (partHeaders, partBody) = SplitHeadersAndBody(partText);
        var contentType = GetHeader(partHeaders, "Content-Type") ?? "text/plain";
        var nestedBoundary = ExtractParameter(contentType, "boundary");

        if (nestedBoundary != null)
        {
            foreach (var nested in SplitByBoundary(partBody, nestedBoundary))
                ParsePart(nested);
            return;
        }

        ApplyPart(partHeaders, partBody);
    }

    private void ApplyPart(Dictionary<string, string> headers, string body)
    {
        var contentTypeHeader = GetHeader(headers, "Content-Type") ?? "text/plain";
        var contentType = contentTypeHeader.Split(';')[0].Trim().ToLowerInvariant();
        var encoding = GetHeader(headers, "Content-Transfer-Encoding")?.Trim().ToLowerInvariant();
        var contentId = GetHeader(headers, "Content-ID")?.Trim('<', '>', ' ');

        var decodedBytes = DecodeBody(body, encoding);

        if (contentId != null && (contentType.StartsWith("image/") || !contentType.StartsWith("text/")))
        {
            InlineParts.Add((contentId, contentType, decodedBytes));
            return;
        }

        var text = GetEncoding(ExtractParameter(contentTypeHeader, "charset")).GetString(decodedBytes);
        if (contentType == "text/html" && HtmlBody == null)
            HtmlBody = text;
        else if (contentType == "text/plain" && TextBody == null)
            TextBody = text;
    }

    private static byte[] DecodeBody(string body, string encoding) => encoding switch
    {
        "base64" => TryDecodeBase64(body),
        "quoted-printable" => Encoding.Latin1.GetBytes(DecodeQuotedPrintable(body)),
        _ => Encoding.Latin1.GetBytes(body)
    };

    /// <summary>
    /// Resolves the charset named in a Content-Type header. Unknown or missing charsets fall back to UTF-8;
    /// anything but the built in code pages would need System.Text.Encoding.CodePages, which we do not pull in.
    /// </summary>
    internal static Encoding GetEncoding(string charset)
    {
        if (string.IsNullOrWhiteSpace(charset))
            return Encoding.UTF8;
        try
        {
            return Encoding.GetEncoding(charset.Trim().Trim('"'));
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    private static byte[] TryDecodeBase64(string body)
    {
        try
        {
            return Convert.FromBase64String(body.Replace("\r", "").Replace("\n", "").Trim());
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    private static string DecodeQuotedPrintable(string input)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < input.Length; i++)
        {
            if (input[i] == '=' && i + 2 < input.Length && Uri.IsHexDigit(input[i + 1]) && Uri.IsHexDigit(input[i + 2]))
            {
                sb.Append((char)Convert.ToInt32(input.Substring(i + 1, 2), 16));
                i += 2;
            }
            else if (input[i] == '=' && (i + 1 >= input.Length || input[i + 1] == '\r' || input[i + 1] == '\n'))
            {
                // soft line break, skip the trailing CRLF that follows
                while (i + 1 < input.Length && (input[i + 1] == '\r' || input[i + 1] == '\n'))
                    i++;
            }
            else
            {
                sb.Append(input[i]);
            }
        }
        return sb.ToString();
    }

    /// <summary>Replaces cid: references in the html with inline data-URIs.</summary>
    public string ResolveCidImages(string html)
    {
        foreach (var (contentId, mimeType, data) in InlineParts)
            html = html.Replace($"cid:{contentId}", $"data:{mimeType};base64,{Convert.ToBase64String(data)}", StringComparison.OrdinalIgnoreCase);
        return html;
    }

    /// <summary>Prepends a small From/To/Subject/Date header block to the rendered html body.</summary>
    public string PrependHeaders(string html)
    {
        // Every color is stated explicitly: the block is written into an iframe together with the message body,
        // and inheriting from the host page would put dark text on the app's dark background.
        const string label = "display:inline-block;min-width:64px;color:#5f6368;font-weight:600;";

        var sb = new StringBuilder();
        sb.Append("<div style=\"border-bottom: 1px solid #dadce0; padding-bottom: 12px; margin-bottom: 16px; color: #1a1a1a; font-size: 14px; line-height: 1.6; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;\">");
        if (!string.IsNullOrWhiteSpace(Subject))
            sb.Append($"<div style=\"font-size:17px;font-weight:600;margin-bottom:6px;\">{System.Net.WebUtility.HtmlEncode(Subject)}</div>");
        if (!string.IsNullOrWhiteSpace(From))
            sb.Append($"<div><span style=\"{label}\">From</span>{System.Net.WebUtility.HtmlEncode(From)}</div>");
        if (!string.IsNullOrWhiteSpace(To))
            sb.Append($"<div><span style=\"{label}\">To</span>{System.Net.WebUtility.HtmlEncode(To)}</div>");
        if (!string.IsNullOrWhiteSpace(Cc))
            sb.Append($"<div><span style=\"{label}\">Cc</span>{System.Net.WebUtility.HtmlEncode(Cc)}</div>");
        if (!string.IsNullOrWhiteSpace(Date))
            sb.Append($"<div><span style=\"{label}\">Sent</span>{System.Net.WebUtility.HtmlEncode(Date)}</div>");
        sb.Append("</div>");
        return sb + html;
    }

    private static (Dictionary<string, string> Headers, string Body) SplitHeadersAndBody(string text)
    {
        var unfolded = text.NormalizeLineEndings();
        var separatorIndex = unfolded.IndexOf("\n\n", StringComparison.Ordinal);
        var headerBlock = separatorIndex >= 0 ? unfolded[..separatorIndex] : unfolded;
        var body = separatorIndex >= 0 ? unfolded[(separatorIndex + 2)..] : string.Empty;

        // Unfold header continuation lines (starting with whitespace) before splitting into individual headers.
        headerBlock = headerBlock.Replace("\n ", " ").Replace("\n\t", " ");

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in headerBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var index = line.IndexOf(':');
            if (index < 0)
                continue;
            var key = line[..index].Trim();
            var value = line[(index + 1)..].Trim();
            headers[key] = value;
        }

        return (headers, body);
    }

    private static string GetHeader(Dictionary<string, string> headers, string name) =>
        headers.TryGetValue(name, out var value) ? value : null;

    private static string ExtractParameter(string headerValue, string parameterName)
    {
        var marker = $"{parameterName}=";
        var index = headerValue.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        var start = index + marker.Length;
        if (start < headerValue.Length && headerValue[start] == '"')
        {
            var end = headerValue.IndexOf('"', start + 1);
            return end > start ? headerValue.Substring(start + 1, end - start - 1) : null;
        }

        var stop = headerValue.IndexOfAny(new[] { ';', ' ' }, start);
        return stop < 0 ? headerValue[start..] : headerValue[start..stop];
    }

    private static IEnumerable<string> SplitByBoundary(string body, string boundary)
    {
        var delimiter = $"--{boundary}";
        var parts = body.Split(delimiter);
        // Skip preamble (before first boundary) and epilogue (after final "--boundary--")
        foreach (var part in parts.Skip(1))
        {
            var trimmed = part.TrimStart('\n', '\r');
            if (trimmed.StartsWith("--"))
                continue; // final boundary marker
            yield return trimmed;
        }
    }

    /// <summary>Decodes RFC 2047 encoded-words (e.g. "=?UTF-8?B?...?=") that may appear in header values.</summary>
    private static string DecodeHeaderValue(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains("=?"))
            return value;

        return System.Text.RegularExpressions.Regex.Replace(value, @"=\?([^?]+)\?([BbQq])\?([^?]*)\?=", match =>
        {
            var charset = match.Groups[1].Value;
            var isBase64 = match.Groups[2].Value.Equals("B", StringComparison.OrdinalIgnoreCase);
            var encoded = match.Groups[3].Value;

            try
            {
                var encodingObj = Encoding.GetEncoding(charset);
                var bytes = isBase64 ? Convert.FromBase64String(encoded) : Encoding.Latin1.GetBytes(DecodeQuotedPrintable(encoded.Replace('_', ' ')));
                return encodingObj.GetString(bytes);
            }
            catch
            {
                return encoded;
            }
        });
    }
}
