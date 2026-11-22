using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Document;

/// <summary>File names, content types and capabilities of the <see cref="DocumentFileFormat"/> values.</summary>
public static class DocumentFileFormats
{
    private const int Head = 4096;
    private const int MaxDirectorySectors = 64;
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

    private static readonly byte[] CompoundSignature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

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
    /// <c>mimetype</c> entry (.odt), <c>{\rtf</c> after an optional UTF-8 byte order mark and white space, an OLE compound file with a
    /// <c>WordDocument</c> stream at its root (.doc; one in an embedded object does not count) and HTML markup at the start. Another Office or OpenDocument package (.xlsx, .pptx, .ods) and an Excel,
    /// PowerPoint or Outlook compound file give null whatever the name says; a ZIP without content types falls back to the name, and an unknown
    /// compound file counts as .doc only if the name ends with .doc or .dot. Text without a signature is Markdown, HTML or text by
    /// <paramref name="fileName"/> or <paramref name="contentType"/>, else HTML if it starts with markup, else plain text; other binary content
    /// falls back to the name.
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
            return Sniff(content, start, head.AsSpan(0, length), fileName, Detect(fileName, contentType));
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
        format is DocumentFileFormat.Docx or DocumentFileFormat.Dotx or DocumentFileFormat.Rtf or DocumentFileFormat.Html or DocumentFileFormat.Text or
            DocumentFileFormat.Markdown;

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

    private static DocumentFileFormat? Sniff(Stream content, long start, ReadOnlySpan<byte> head, string fileName, DocumentFileFormat? named)
    {
        if (head.StartsWith("PK\u0003\u0004"u8)) return Package(content, start) is var (format, known) && known ? format : named;
        if (RtfFormat.Start(head) >= 0) return DocumentFileFormat.Rtf;
        if (head.StartsWith(CompoundSignature)) return Compound(content, start, fileName);
        var bom = HtmlEncoding.Bom(head);
        if (bom == null && head.Contains((byte)0)) return named;
        if (named is DocumentFileFormat.Html or DocumentFileFormat.Markdown or DocumentFileFormat.Text) return named;
        var text = (bom?.Encoding ?? Encoding.UTF8).GetString(head[(bom?.Preamble ?? 0)..]).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        return IsHtml(text) ? DocumentFileFormat.Html : DocumentFileFormat.Text;
    }

    private static bool IsHtml(string text)
    {
        if (text.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
        {
            var end = text.IndexOf("?>", StringComparison.Ordinal);
            if (end < 0) return false;
            text = text[(end + 2)..].TrimStart(' ', '\t', '\r', '\n');
        }

        if (text.StartsWith("<!--", StringComparison.Ordinal) || text.StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase)) return true;
        if (!text.StartsWith('<')) return false;
        var name = 1;
        while (name < text.Length && char.IsAsciiLetterOrDigit(text[name])) name++;
        return name < text.Length && text[name] is ' ' or '>' or '/' or '\t' or '\r' or '\n' && HtmlTags.Contains(text[1..name].ToLowerInvariant()) &&
               text.IndexOf('>', name) >= 0;
    }

    private static (DocumentFileFormat? Format, bool Known) Package(Stream content, long start)
    {
        try
        {
            using var zip = new ZipArchive(new OffsetStream(content, start), ZipArchiveMode.Read);
            if (zip.GetEntry("mimetype") is { Length: < 256 } mimetype)
            {
                using var reader = new StreamReader(mimetype.Open(), Encoding.ASCII);
                return (reader.ReadToEnd().Trim().StartsWith("application/vnd.oasis.opendocument.text", StringComparison.Ordinal) ? DocumentFileFormat.Odt : null, true);
            }

            if (zip.GetEntry("[Content_Types].xml") is not { Length: < 1024 * 1024 } types) return (null, false);
            using var stream = types.Open();
            using var xml = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            foreach (var type in XDocument.Load(xml).Root?.Elements().Select(e => (string)e.Attribute("ContentType")) ?? [])
                if (type != null && MainParts.TryGetValue(type, out var format))
                    return (format, true);
            return (null, true);
        }
        catch (Exception e) when (e is InvalidDataException or XmlException or IOException)
        {
            return (null, false);
        }
    }

    private static DocumentFileFormat? Compound(Stream content, long start, string fileName)
    {
        HashSet<string> names;
        try
        {
            names = RootNames(content, start);
        }
        catch (IOException)
        {
            return null;
        }

        if (names.Contains("WordDocument")) return DocumentFileFormat.Doc;
        if (names.Any(n => n is "Workbook" or "Book" or "PowerPoint Document" or "__properties_version1.0" || n.StartsWith("__substg1.0_", StringComparison.Ordinal)))
            return null;
        return Path.GetExtension(fileName ?? "").ToLowerInvariant() is ".doc" or ".dot" ? DocumentFileFormat.Doc : null;
    }

    private static HashSet<string> RootNames(Stream content, long start)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var header = new byte[512];
        content.Position = start;
        if (content.ReadAtLeast(header, header.Length, false) < header.Length) return names;
        var shift = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(0x1E));
        if (shift is not (9 or 12)) return names;
        var size = 1 << shift;
        var fat = Enumerable.Range(0, (int)Math.Min(BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x2C)), 109u))
            .Select(i => BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x4C + 4 * i))).ToList();
        var buffer = new byte[size];
        var entries = new List<(string Name, int Type, uint Left, uint Right, uint Child)>();
        var sector = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x30));
        for (var count = 0; count < MaxDirectorySectors && Sector(content, start, sector, buffer); count++)
        {
            for (var entry = 0; entry + 128 <= size; entry += 128)
            {
                var length = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(entry + 0x40));
                var name = length is >= 4 and <= 64 && length % 2 == 0 ? Encoding.Unicode.GetString(buffer, entry, length - 2) : null;
                entries.Add((name, buffer[entry + 0x42], BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(entry + 0x44)),
                    BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(entry + 0x48)), BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(entry + 0x4C))));
            }

            var index = (int)(sector / (size / 4));
            if (index >= fat.Count || !Sector(content, start, fat[index], buffer)) break;
            sector = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan((int)(sector % (size / 4)) * 4));
        }

        if (entries.Count == 0 || entries[0].Type != 5) return names;
        var pending = new Stack<uint>([entries[0].Child]);
        var seen = new HashSet<uint>();
        while (pending.TryPop(out var id))
        {
            if (id >= entries.Count || !seen.Add(id)) continue;
            var (name, type, left, right, _) = entries[(int)id];
            if (name != null && type is 1 or 2) names.Add(name);
            pending.Push(left);
            pending.Push(right);
        }

        return names;
    }

    private static bool Sector(Stream content, long start, uint sector, byte[] buffer)
    {
        var offset = start + ((long)sector + 1) * buffer.Length;
        if (sector >= 0xFFFFFFFA || offset + buffer.Length > content.Length) return false;
        content.Position = offset;
        return content.ReadAtLeast(buffer, buffer.Length, false) == buffer.Length;
    }
}
