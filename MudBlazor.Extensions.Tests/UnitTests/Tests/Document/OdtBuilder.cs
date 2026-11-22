using System.IO.Compression;
using System.Text;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

internal sealed class OdtBuilder
{
    public const string Ns =
        "xmlns:office=\"urn:oasis:names:tc:opendocument:xmlns:office:1.0\" " +
        "xmlns:style=\"urn:oasis:names:tc:opendocument:xmlns:style:1.0\" " +
        "xmlns:text=\"urn:oasis:names:tc:opendocument:xmlns:text:1.0\" " +
        "xmlns:table=\"urn:oasis:names:tc:opendocument:xmlns:table:1.0\" " +
        "xmlns:draw=\"urn:oasis:names:tc:opendocument:xmlns:drawing:1.0\" " +
        "xmlns:fo=\"urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0\" " +
        "xmlns:svg=\"urn:oasis:names:tc:opendocument:xmlns:svg-compatible:1.0\" " +
        "xmlns:xlink=\"http://www.w3.org/1999/xlink\" " +
        "xmlns:dc=\"http://purl.org/dc/elements/1.1/\" " +
        "xmlns:meta=\"urn:oasis:names:tc:opendocument:xmlns:meta:1.0\"";

    private readonly Dictionary<string, byte[]> _files = new();
    private string _body = "<text:p/>";
    private string _automatic = "";
    private string _fonts = "";
    private string _styles;
    private string _stylesAutomatic = "";
    private string _master = "";

    public string MimeType { get; set; } = "application/vnd.oasis.opendocument.text";

    public string Content { get; set; }

    public OdtBuilder Body(string xml)
    {
        _body = xml;
        return this;
    }

    public OdtBuilder AutomaticStyles(string xml)
    {
        _automatic = xml;
        return this;
    }

    public OdtBuilder Fonts(string xml)
    {
        _fonts = xml;
        return this;
    }

    public OdtBuilder Styles(string xml)
    {
        _styles = xml;
        return this;
    }

    public OdtBuilder MasterPage(string automaticStyles, string masterStyles)
    {
        _styles ??= "";
        (_stylesAutomatic, _master) = (automaticStyles, masterStyles);
        return this;
    }

    public OdtBuilder Meta(string xml) => File("meta.xml", $"<office:document-meta {Ns} office:version=\"1.3\"><office:meta>{xml}</office:meta></office:document-meta>");

    public OdtBuilder File(string name, string content) => File(name, Encoding.UTF8.GetBytes(content));

    public OdtBuilder File(string name, byte[] content)
    {
        _files[name] = content;
        return this;
    }

    public byte[] Build()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            if (MimeType != null) Add(zip, "mimetype", Encoding.ASCII.GetBytes(MimeType), CompressionLevel.NoCompression);
            Add(zip, "content.xml", Encoding.UTF8.GetBytes(Content ??
                $"<office:document-content {Ns} office:version=\"1.3\"><office:font-face-decls>{_fonts}</office:font-face-decls>" +
                $"<office:automatic-styles>{_automatic}</office:automatic-styles><office:body><office:text>{_body}</office:text></office:body></office:document-content>"));
            if (_styles != null)
                Add(zip, "styles.xml", Encoding.UTF8.GetBytes(
                    $"<office:document-styles {Ns} office:version=\"1.3\"><office:font-face-decls>{_fonts}</office:font-face-decls><office:styles>{_styles}</office:styles>" +
                    $"<office:automatic-styles>{_stylesAutomatic}</office:automatic-styles><office:master-styles>{_master}</office:master-styles></office:document-styles>"));
            foreach (var (name, content) in _files) Add(zip, name, content);
        }

        return stream.ToArray();
    }

    public TextDocument Load() => TextDocument.Load(new MemoryStream(Build()), DocumentFileFormat.Odt);

    private static void Add(ZipArchive zip, string name, byte[] content, CompressionLevel level = CompressionLevel.Optimal)
    {
        using var entry = zip.CreateEntry(name, level).Open();
        entry.Write(content);
    }
}
