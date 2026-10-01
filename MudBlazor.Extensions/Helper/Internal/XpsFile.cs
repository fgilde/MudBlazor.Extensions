using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Helper.Internal;

internal sealed class XpsFile : IDisposable
{
    private readonly MemoryStream _stream;
    private readonly ZipArchive _archive;
    public List<XpsPage> Pages { get; } = new();

    private XpsFile(byte[] bytes)
    {
        _stream = new MemoryStream(bytes, writable: false);
        _archive = new ZipArchive(_stream, ZipArchiveMode.Read, leaveOpen: true);
    }

    public static XpsFile Open(byte[] bytes)
    {
        var result = new XpsFile(bytes);
        result.Read();
        return result;
    }

    private void Read()
    {
        var pageEntries = _archive.Entries
            .Where(e => e.FullName.EndsWith(".fpage", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var entry in pageEntries)
        {
            using var source = entry.Open();
            var xml = XDocument.Load(source, LoadOptions.None);
            var page = xml.Root;
            var width = Number(Attribute(page, "Width"), 816);
            var height = Number(Attribute(page, "Height"), 1056);
            var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {F(width)} {F(height)}\" role=\"img\">");
            foreach (var element in page.Descendants())
            {
                if (element.Name.LocalName == "Path") RenderPath(svg, element, entry.FullName);
                else if (element.Name.LocalName == "Glyphs") RenderGlyphs(svg, element);
            }
            svg.Append("</svg>");
            Pages.Add(new XpsPage(Pages.Count + 1, width, height, svg.ToString()));
        }
        if (Pages.Count == 0) throw new InvalidDataException("The XPS package contains no FixedPage parts.");
    }

    private void RenderPath(StringBuilder svg, XElement path, string pagePath)
    {
        var data = Attribute(path, "Data");
        var opacity = Number(Attribute(path, "Opacity"), 1);
        var fill = Paint(Attribute(path, "Fill"));
        var stroke = Paint(Attribute(path, "Stroke"));
        var thickness = Number(Attribute(path, "StrokeThickness"), 1);

        var imageBrush = path.Descendants().FirstOrDefault(e => e.Name.LocalName == "ImageBrush");
        if (imageBrush != null)
        {
            var source = Attribute(imageBrush, "ImageSource")?.Split('?', '#')[0];
            var bytes = ReadRelative(pagePath, source);
            if (bytes != null)
            {
                var bounds = Bounds(data);
                if (bounds != null)
                {
                    svg.Append("<image x=\"").Append(F(bounds.Value.X)).Append("\" y=\"").Append(F(bounds.Value.Y))
                        .Append("\" width=\"").Append(F(bounds.Value.Width)).Append("\" height=\"").Append(F(bounds.Value.Height))
                        .Append("\" opacity=\"").Append(F(opacity)).Append("\" href=\"data:").Append(ContentType(source))
                        .Append(";base64,").Append(Convert.ToBase64String(bytes)).Append("\" />");
                }
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(data)) return;
        svg.Append("<path d=\"").Append(WebUtility.HtmlEncode(data)).Append("\" fill=\"").Append(fill)
            .Append("\" stroke=\"").Append(stroke).Append("\" stroke-width=\"").Append(F(thickness))
            .Append("\" opacity=\"").Append(F(opacity)).Append("\" />");
    }

    private static void RenderGlyphs(StringBuilder svg, XElement glyph)
    {
        var text = Attribute(glyph, "UnicodeString");
        if (string.IsNullOrEmpty(text)) return;
        var x = Number(Attribute(glyph, "OriginX"), 0);
        var y = Number(Attribute(glyph, "OriginY"), 0);
        var size = Number(Attribute(glyph, "FontRenderingEmSize"), 12);
        var fill = Paint(Attribute(glyph, "Fill"));
        var opacity = Number(Attribute(glyph, "Opacity"), 1);
        svg.Append("<text x=\"").Append(F(x)).Append("\" y=\"").Append(F(y)).Append("\" font-size=\"")
            .Append(F(size)).Append("\" fill=\"").Append(fill).Append("\" opacity=\"").Append(F(opacity)).Append("\">")
            .Append(WebUtility.HtmlEncode(text)).Append("</text>");
    }

    private byte[] ReadRelative(string pagePath, string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return null;
        var baseUri = new Uri("http://xps/" + pagePath.Replace('\\', '/'));
        var path = Uri.UnescapeDataString(new Uri(baseUri, source).AbsolutePath.TrimStart('/'));
        var entry = _archive.GetEntry(path) ?? _archive.Entries.FirstOrDefault(e => e.FullName.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (entry == null) return null;
        using var input = entry.Open();
        using var output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }

    private static (double X, double Y, double Width, double Height)? Bounds(string data)
    {
        if (string.IsNullOrWhiteSpace(data)) return null;
        var values = System.Text.RegularExpressions.Regex.Matches(data, @"-?\d+(?:\.\d+)?")
            .Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture)).ToArray();
        if (values.Length < 4) return null;
        var xs = values.Where((_, i) => i % 2 == 0).ToArray();
        var ys = values.Where((_, i) => i % 2 == 1).ToArray();
        var minX = xs.Min(); var minY = ys.Min();
        return (minX, minY, Math.Max(1, xs.Max() - minX), Math.Max(1, ys.Max() - minY));
    }

    private static string Attribute(XElement element, string name) => element?.Attributes().FirstOrDefault(a => a.Name.LocalName == name)?.Value;
    private static double Number(string value, double fallback) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : fallback;
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string Paint(string value) => string.IsNullOrWhiteSpace(value) || value.StartsWith('{') ? "none" : WebUtility.HtmlEncode(value);
    private static string ContentType(string path) => Path.GetExtension(path)?.ToLowerInvariant() switch
    { ".jpg" or ".jpeg" => "image/jpeg", ".tif" or ".tiff" => "image/tiff", ".gif" => "image/gif", _ => "image/png" };

    public void Dispose() { _archive.Dispose(); _stream.Dispose(); }
}

internal sealed record XpsPage(int Number, double Width, double Height, string Svg);
