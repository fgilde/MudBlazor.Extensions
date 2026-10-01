using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Helper.Internal;

internal sealed class OpenDocumentFile : IDisposable
{
    private readonly MemoryStream _stream;
    private readonly ZipArchive _archive;

    public string MediaType { get; private set; }
    public string Title { get; private set; }
    public string Creator { get; private set; }
    public List<OpenDocumentPage> Pages { get; } = new();

    private OpenDocumentFile(MemoryStream stream)
    {
        _stream = stream;
        _archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
    }

    public static OpenDocumentFile Open(byte[] bytes)
    {
        var document = new OpenDocumentFile(new MemoryStream(bytes, writable: false));
        document.Read();
        return document;
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

    private XDocument ReadXml(string path)
    {
        var entry = _archive.GetEntry(path);
        if (entry == null) return null;
        using var stream = entry.Open();
        return XDocument.Load(stream, LoadOptions.None);
    }

    private string ReadText(string path)
    {
        var entry = _archive.GetEntry(path);
        if (entry == null) return null;
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private byte[] ReadEntry(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var entry = _archive.GetEntry(path.Replace('\\', '/'));
        if (entry == null) return null;
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
