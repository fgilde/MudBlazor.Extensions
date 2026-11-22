using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using MudBlazor.Extensions.Office;

namespace MudBlazor.Extensions.Helper.Internal;

internal sealed class OpenDocumentFile : IDisposable
{
    internal const int ElementCost = 8;
    private const int MaxMediaTypeLength = 256;

    private static readonly XmlReaderSettings PackageXml = new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };

    private readonly MemoryStream _stream;
    private readonly ZipArchive _archive;
    private readonly PackageLimits _limits;

    public string MediaType { get; private set; }
    public string Title { get; private set; }
    public string Creator { get; private set; }
    public List<OpenDocumentPage> Pages { get; } = new();

    private OpenDocumentFile(MemoryStream stream, PackageLimits limits = null)
    {
        _stream = stream;
        _limits = limits;
        Budget = limits?.Budget ?? long.MaxValue;
        _archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
    }

    public static OpenDocumentFile Open(byte[] bytes)
    {
        var document = new OpenDocumentFile(new MemoryStream(bytes, writable: false));
        document.Read();
        return document;
    }

    internal long Budget { get; private set; }

    internal static OpenDocumentFile OpenPackage(byte[] bytes, PackageLimits limits = null)
    {
        limits ??= PackageLimits.Default;
        var document = new OpenDocumentFile(new MemoryStream(bytes, writable: false), limits);
        try
        {
            var (count, total) = (0, 0L);
            foreach (var entry in document._archive.Entries)
            {
                if (++count > limits.MaxEntries) throw LoadLimit.Exceeded($"The package has more than {limits.MaxEntries} entries.");
                if (entry.Length < 0 || entry.Length > limits.MaxTotalLength - total)
                    throw LoadLimit.Exceeded("The package entries are larger than allowed in total.");
                total += entry.Length;
            }

            if (document._archive.GetEntry("mimetype") is { Length: <= MaxMediaTypeLength }) document.MediaType = document.ReadText("mimetype")?.Trim();
            return document;
        }
        catch
        {
            document.Dispose();
            throw;
        }
    }

    private void Read()
    {
        MediaType = ReadText("mimetype")?.Trim();
        var meta = ReadXml("meta.xml");
        Title = meta?.Descendants().FirstOrDefault(e => e.Name.LocalName == "title")?.Value;
        Creator = meta?.Descendants().FirstOrDefault(e => e.Name.LocalName is "creator" or "initial-creator")?.Value;

        var content = ReadXml("content.xml") ?? throw new InvalidDataException("OpenDocument content.xml is missing.");
        var body = content.Descendants().FirstOrDefault(e => e.Name.LocalName == "body")
                   ?? throw new InvalidDataException("OpenDocument body is missing.");
        var root = body.Elements().FirstOrDefault();
        if (root == null) throw new InvalidDataException("OpenDocument body is empty.");

        switch (root.Name.LocalName)
        {
            case "spreadsheet": ReadSpreadsheet(root); break;
            case "presentation": ReadDrawPages(root, "Slide"); break;
            case "drawing": ReadDrawPages(root, "Page"); break;
            default: Pages.Add(new OpenDocumentPage(Title ?? "Document", RenderChildren(root))); break;
        }
    }

    private void ReadSpreadsheet(XElement spreadsheet)
    {
        foreach (var table in spreadsheet.Elements().Where(e => e.Name.LocalName == "table"))
        {
            var name = Attribute(table, "name") ?? $"Sheet {Pages.Count + 1}";
            var html = new StringBuilder("<table class=\"odf-table\"><tbody>");
            foreach (var row in table.Elements().Where(e => e.Name.LocalName == "table-row"))
            {
                var repeatRows = Math.Min(ParseInt(Attribute(row, "number-rows-repeated"), 1), 1000);
                var rendered = new StringBuilder("<tr>");
                foreach (var cell in row.Elements().Where(e => e.Name.LocalName is "table-cell" or "covered-table-cell"))
                {
                    var repeat = Math.Min(ParseInt(Attribute(cell, "number-columns-repeated"), 1), 1000);
                    var value = string.Join(" ", cell.Descendants().Where(e => e.Name.LocalName is "p" or "h").Select(e => e.Value));
                    for (var i = 0; i < repeat; i++) rendered.Append("<td>").Append(WebUtility.HtmlEncode(value)).Append("</td>");
                }
                rendered.Append("</tr>");
                for (var i = 0; i < repeatRows; i++) html.Append(rendered);
            }
            html.Append("</tbody></table>");
            Pages.Add(new OpenDocumentPage(name, html.ToString()));
        }
    }

    private void ReadDrawPages(XElement root, string fallback)
    {
        foreach (var page in root.Descendants().Where(e => e.Name.LocalName == "page"))
        {
            var name = Attribute(page, "name") ?? $"{fallback} {Pages.Count + 1}";
            Pages.Add(new OpenDocumentPage(name, RenderChildren(page)));
        }
        if (Pages.Count == 0) Pages.Add(new OpenDocumentPage(fallback, RenderChildren(root)));
    }

    private string RenderChildren(XElement root)
    {
        var html = new StringBuilder();
        foreach (var element in root.Descendants().Where(e => e.Name.LocalName is "h" or "p" or "table" or "image"))
        {
            if (element.Ancestors().Any(a => a != root && a.Name.LocalName is "table" or "p" or "h" or "image")) continue;
            switch (element.Name.LocalName)
            {
                case "h": html.Append("<h2>").Append(WebUtility.HtmlEncode(element.Value)).Append("</h2>"); break;
                case "p": html.Append("<p>").Append(WebUtility.HtmlEncode(element.Value)).Append("</p>"); break;
                case "table": html.Append(RenderTable(element)); break;
                case "image":
                    var href = Attribute(element, "href")?.TrimStart('.', '/');
                    var data = ReadEntry(href);
                    if (data != null)
                        html.Append("<img alt=\"Embedded image\" src=\"data:").Append(ContentType(href)).Append(";base64,").Append(Convert.ToBase64String(data)).Append("\" />");
                    break;
            }
        }
        return html.ToString();
    }

    private static string RenderTable(XElement table)
    {
        var html = new StringBuilder("<table class=\"odf-table\"><tbody>");
        foreach (var row in table.Descendants().Where(e => e.Name.LocalName == "table-row"))
        {
            html.Append("<tr>");
            foreach (var cell in row.Elements().Where(e => e.Name.LocalName is "table-cell" or "covered-table-cell"))
                html.Append("<td>").Append(WebUtility.HtmlEncode(cell.Value)).Append("</td>");
            html.Append("</tr>");
        }
        return html.Append("</tbody></table>").ToString();
    }

    internal void Spend(long units)
    {
        Reserve(units);
        Budget -= units;
    }

    internal void Reserve(long units)
    {
        if (units > Budget) throw LoadLimit.Exceeded("The document expands to too much content.");
    }

    internal XDocument ReadXml(string path)
    {
        if (_limits != null)
        {
            var bytes = ReadEntry(path, _limits.MaxXmlLength);
            if (bytes == null) return null;
            var document = BoundedXmlReader.Load(XmlReader.Create(new MemoryStream(bytes, false), PackageXml), BoundedXmlReader.MaxDepth, Budget / ElementCost, out var elements);
            Spend(elements * ElementCost);
            return document;
        }

        var entry = _archive.GetEntry(path);
        if (entry == null) return null;
        using var stream = entry.Open();
        return BoundedXmlReader.Load(stream);
    }

    private string ReadText(string path)
    {
        if (_limits != null) return ReadEntry(path) is { } bytes ? Encoding.UTF8.GetString(bytes) : null;
        var entry = _archive.GetEntry(path);
        if (entry == null) return null;
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    internal byte[] ReadEntry(string path) => ReadEntry(path, long.MaxValue);

    private byte[] ReadEntry(string path, long maxLength)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var entry = _archive.GetEntry(path.Replace('\\', '/'));
        if (entry == null) return null;
        if (_limits != null && entry.Length > Math.Min(maxLength, _limits.MaxEntryLength))
            throw LoadLimit.Exceeded($"The package entry {path} is larger than {Math.Min(maxLength, _limits.MaxEntryLength)} bytes.");
        if (_limits != null) return OpcPackage.ReadDeclared(entry, "package entry " + path);
        using var source = entry.Open();
        using var destination = new MemoryStream();
        source.CopyTo(destination);
        return destination.ToArray();
    }

    private static string Attribute(XElement element, string localName)
        => element.Attributes().FirstOrDefault(a => a.Name.LocalName == localName)?.Value;

    private static int ParseInt(string value, int fallback) => int.TryParse(value, out var result) ? result : fallback;

    private static string ContentType(string path) => Path.GetExtension(path)?.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg", ".gif" => "image/gif", ".svg" => "image/svg+xml", ".webp" => "image/webp", _ => "image/png"
    };

    public void Dispose()
    {
        _archive.Dispose();
        _stream.Dispose();
    }
}

internal sealed record OpenDocumentPage(string Name, string Html);

internal sealed record PackageLimits(int MaxEntries, long MaxTotalLength, long MaxEntryLength, long MaxXmlLength, long Budget)
{
    internal static PackageLimits Default { get; } = new(10000, 1024L * 1024 * 1024, 256L * 1024 * 1024, 64L * 1024 * 1024, 32L * 1024 * 1024);
}
