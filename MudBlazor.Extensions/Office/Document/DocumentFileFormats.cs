using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Document;

/// <summary>File names, content types and capabilities of the <see cref="DocumentFileFormat"/> values.</summary>
public static class DocumentFileFormats
{
    private const int Head = 4096;
    private const string Wml = "application/vnd.openxmlformats-officedocument.wordprocessingml.";

    private static readonly Dictionary<string, DocumentFileFormat> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".docx"] = DocumentFileFormat.Docx, [".docm"] = DocumentFileFormat.Docx, [".dotx"] = DocumentFileFormat.Dotx, [".dotm"] = DocumentFileFormat.Dotx,
        [".rtf"] = DocumentFileFormat.Rtf, [".html"] = DocumentFileFormat.Html, [".htm"] = DocumentFileFormat.Html, [".txt"] = DocumentFileFormat.Text,
        [".md"] = DocumentFileFormat.Markdown, [".markdown"] = DocumentFileFormat.Markdown, [".odt"] = DocumentFileFormat.Odt, [".doc"] = DocumentFileFormat.Doc
    };

    private static readonly Dictionary<string, DocumentFileFormat> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [Wml + "document"] = DocumentFileFormat.Docx, ["application/vnd.ms-word.document.macroenabled.12"] = DocumentFileFormat.Docx,
        [Wml + "template"] = DocumentFileFormat.Dotx, ["application/vnd.ms-word.template.macroenabled.12"] = DocumentFileFormat.Dotx,
        ["application/rtf"] = DocumentFileFormat.Rtf, ["text/rtf"] = DocumentFileFormat.Rtf, ["text/html"] = DocumentFileFormat.Html,
        ["application/xhtml+xml"] = DocumentFileFormat.Html, ["text/plain"] = DocumentFileFormat.Text, ["text/markdown"] = DocumentFileFormat.Markdown,
        ["text/x-markdown"] = DocumentFileFormat.Markdown, ["application/vnd.oasis.opendocument.text"] = DocumentFileFormat.Odt,
        ["application/msword"] = DocumentFileFormat.Doc
    };

    private static readonly Dictionary<string, DocumentFileFormat> MainParts = new(StringComparer.OrdinalIgnoreCase)
    {
        [Wml + "document.main+xml"] = DocumentFileFormat.Docx, ["application/vnd.ms-word.document.macroEnabled.main+xml"] = DocumentFileFormat.Docx,
        [Wml + "template.main+xml"] = DocumentFileFormat.Dotx, ["application/vnd.ms-word.template.macroEnabledTemplate.main+xml"] = DocumentFileFormat.Dotx
    };

    private static readonly string[] HtmlTags = ["html", "head", "body", "meta", "title", "style", "script", "table", "div", "p", "h1", "h2", "h3", "h4", "h5", "h6", "b", "br", "font", "a"];

    /// <summary>Detects a format from a file name and, if the extension is unknown, from a content type such as <c>text/markdown</c>.</summary>
    /// <param name="fileName">The file name or path, or null; <c>.htm</c> counts as HTML, <c>.markdown</c> as Markdown and <c>.docm</c>/<c>.dotm</c> as
    /// <see cref="DocumentFileFormat.Docx"/>/<see cref="DocumentFileFormat.Dotx"/>.</param>
    /// <param name="contentType">The MIME type, or null; parameters such as <c>charset</c> are ignored.</param>
    /// <returns>The format, or null if neither is known.</returns>
    public static DocumentFileFormat? Detect(string fileName, string contentType = null)
    {
        if (Extensions.TryGetValue(Path.GetExtension(fileName ?? ""), out var format)) return format;
        var type = contentType?.Split(';')[0].Trim();
        return type != null && ContentTypes.TryGetValue(type, out format) ? format : null;
    }

    /// <summary>
    /// Detects a format from the content: a ZIP package by the main part type in <c>[Content_Types].xml</c> (.docx, .docm, .dotx, .dotm) or its
    /// <c>mimetype</c> entry (.odt), <c>{\rtf</c>, the OLE compound file header (.doc) and HTML markup at the start. Text without such a signature is
    /// Markdown, HTML or text by <paramref name="fileName"/> or <paramref name="contentType"/>, else plain text; binary content without a known
    /// signature falls back to the name.
    /// </summary>
    /// <param name="content">The file content; it must be seekable and its position is restored.</param>
    /// <param name="fileName">The file name, or null.</param>
    /// <param name="contentType">The MIME type, or null.</param>
    /// <returns>The format, or null for binary content of an unknown format.</returns>
    /// <exception cref="ArgumentNullException">The stream is null.</exception>
    /// <exception cref="ArgumentException">The stream is not seekable.</exception>
    public static DocumentFileFormat? DetectContent(Stream content, string fileName = null, string contentType = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanSeek) throw new ArgumentException("The stream must be seekable.", nameof(content));
        var start = content.Position;
        try
        {
            var head = new byte[Head];
            var length = content.ReadAtLeast(head, Head, false);
            content.Position = start;
            return Sniff(content, head.AsSpan(0, length), Detect(fileName, contentType));
        }
        finally
        {
            content.Position = start;
        }
    }

    /// <summary>Gets whether <see cref="TextDocument.Load"/> can read a format.</summary>
    /// <param name="format">The format.</param>
    /// <returns>True if the format can be read.</returns>
    public static bool CanRead(DocumentFileFormat format) =>
        format is DocumentFileFormat.Docx or DocumentFileFormat.Dotx or DocumentFileFormat.Rtf or DocumentFileFormat.Html or DocumentFileFormat.Text or
            DocumentFileFormat.Markdown;

    /// <summary>Gets whether <see cref="TextDocument.Save"/> can write a format.</summary>
    /// <param name="format">The format.</param>
    /// <returns>True if the format can be written.</returns>
    public static bool CanWrite(DocumentFileFormat format) =>
        format is DocumentFileFormat.Docx or DocumentFileFormat.Dotx or DocumentFileFormat.Html or DocumentFileFormat.Text or DocumentFileFormat.Markdown;

    /// <summary>Gets the MIME type of a format, for example <c>text/markdown</c>.</summary>
    /// <param name="format">The format.</param>
    /// <returns>The MIME type.</returns>
    public static string ContentType(DocumentFileFormat format) => format switch
    {
        DocumentFileFormat.Docx => Wml + "document",
        DocumentFileFormat.Dotx => Wml + "template",
        DocumentFileFormat.Rtf => "application/rtf",
        DocumentFileFormat.Html => "text/html",
        DocumentFileFormat.Text => "text/plain",
        DocumentFileFormat.Markdown => "text/markdown",
        DocumentFileFormat.Odt => "application/vnd.oasis.opendocument.text",
        DocumentFileFormat.Doc => "application/msword",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    /// <summary>Gets the usual file extension of a format with the leading dot, for example <c>.md</c>.</summary>
    /// <param name="format">The format.</param>
    /// <returns>The extension.</returns>
    public static string Extension(DocumentFileFormat format) => format switch
    {
        DocumentFileFormat.Docx => ".docx",
        DocumentFileFormat.Dotx => ".dotx",
        DocumentFileFormat.Rtf => ".rtf",
        DocumentFileFormat.Html => ".html",
        DocumentFileFormat.Text => ".txt",
        DocumentFileFormat.Markdown => ".md",
        DocumentFileFormat.Odt => ".odt",
        DocumentFileFormat.Doc => ".doc",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    private static DocumentFileFormat? Sniff(Stream content, ReadOnlySpan<byte> head, DocumentFileFormat? named)
    {
        if (head.StartsWith("PK\u0003\u0004"u8)) return Package(content) ?? named;
        if (head.StartsWith(@"{\rtf"u8)) return DocumentFileFormat.Rtf;
        if (head.StartsWith((ReadOnlySpan<byte>)[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1])) return DocumentFileFormat.Doc;
        var bom = HtmlEncoding.Bom(head);
        if (bom == null && head.Contains((byte)0)) return named;
        if (named is DocumentFileFormat.Html or DocumentFileFormat.Markdown or DocumentFileFormat.Text) return named;
        var text = (bom?.Encoding ?? Encoding.UTF8).GetString(head[(bom?.Preamble ?? 0)..]).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        return IsHtml(text) ? DocumentFileFormat.Html : DocumentFileFormat.Text;
    }

    private static bool IsHtml(string text)
    {
        if (text.StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase)) return true;
        if (!text.StartsWith('<')) return false;
        var end = 1;
        while (end < text.Length && char.IsAsciiLetterOrDigit(text[end])) end++;
        return end < text.Length && text[end] is ' ' or '>' or '/' or '\t' or '\r' or '\n' && HtmlTags.Contains(text[1..end].ToLowerInvariant());
    }

    private static DocumentFileFormat? Package(Stream content)
    {
        try
        {
            using var zip = new ZipArchive(content, ZipArchiveMode.Read, true);
            if (zip.GetEntry("mimetype") is { Length: < 256 } mimetype)
            {
                using var reader = new StreamReader(mimetype.Open(), Encoding.ASCII);
                if (reader.ReadToEnd().Trim().StartsWith("application/vnd.oasis.opendocument.text", StringComparison.Ordinal)) return DocumentFileFormat.Odt;
            }

            if (zip.GetEntry("[Content_Types].xml") is not { Length: < 1024 * 1024 } types) return null;
            using var stream = types.Open();
            using var xml = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            foreach (var type in XDocument.Load(xml).Root?.Elements().Select(e => (string)e.Attribute("ContentType")) ?? [])
                if (type != null && MainParts.TryGetValue(type, out var format))
                    return format;
            return null;
        }
        catch (Exception e) when (e is InvalidDataException or XmlException or IOException)
        {
            return null;
        }
    }
}
