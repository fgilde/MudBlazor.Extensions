using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

internal sealed class DocxBuilder
{
    public const string Ns =
        "xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" " +
        "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" " +
        "xmlns:m=\"http://schemas.openxmlformats.org/officeDocument/2006/math\" " +
        "xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" " +
        "xmlns:o=\"urn:schemas-microsoft-com:office:office\" " +
        "xmlns:v=\"urn:schemas-microsoft-com:vml\" " +
        "xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" " +
        "xmlns:wp14=\"http://schemas.microsoft.com/office/word/2010/wordprocessingDrawing\" " +
        "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
        "xmlns:pic=\"http://schemas.openxmlformats.org/drawingml/2006/picture\" " +
        "xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" " +
        "xmlns:dgm=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\" " +
        "xmlns:wps=\"http://schemas.microsoft.com/office/word/2010/wordprocessingShape\" " +
        "xmlns:w14=\"http://schemas.microsoft.com/office/word/2010/wordml\" " +
        "xmlns:w15=\"http://schemas.microsoft.com/office/word/2012/wordml\"";

    public const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private const string Wml = "application/vnd.openxmlformats-officedocument.wordprocessingml.";
    private const string Root = Ns + " mc:Ignorable=\"w14 w15 wp14\"";

    private readonly List<(string Id, string Type, string Target, bool External)> _rels = [];
    private readonly Dictionary<string, (byte[] Content, string ContentType)> _parts = new();
    private readonly List<string> _references = [];
    private readonly List<string> _rawRels = [];
    private string _body = "<w:p/>";
    private string _rootRels = "";
    private int _paraId = 0x10000000;

    public string MainPart { get; set; } = "/word/document.xml";

    public string SectionProperties { get; set; } = "";

    public bool Template { get; set; }

    public string MainContentType { get; set; }

    public string RawDocument { get; set; }

    public DocxBuilder RawRelationship(string xml)
    {
        _rawRels.Add(xml);
        return this;
    }

    public DocxBuilder Body(string xml)
    {
        _body = xml;
        return this;
    }

    public DocxBuilder Styles(string xml) => Part("styles.xml", "styles", Wml + "styles+xml", $"<w:styles {Root}>{xml}</w:styles>");

    public DocxBuilder Numbering(string xml) => Part("numbering.xml", "numbering", Wml + "numbering+xml", $"<w:numbering {Root}>{xml}</w:numbering>");

    public DocxBuilder Settings(string xml) => Part("settings.xml", "settings", Wml + "settings+xml", $"<w:settings {Root}>{xml}</w:settings>");

    public DocxBuilder Comments(string xml) => Part("comments.xml", "comments", Wml + "comments+xml", $"<w:comments {Root}>{xml}</w:comments>");

    public DocxBuilder Theme(string colorScheme = null, string major = "Calibri Light", string minor = "Calibri") =>
        Part("theme/theme1.xml", "theme", "application/vnd.openxmlformats-officedocument.theme+xml",
            "<a:theme xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" name=\"Office\"><a:themeElements>" +
            (colorScheme ?? "<a:clrScheme name=\"Office\"><a:dk1><a:sysClr val=\"windowText\" lastClr=\"000000\"/></a:dk1><a:lt1><a:sysClr val=\"window\" lastClr=\"FFFFFF\"/></a:lt1>" +
                "<a:dk2><a:srgbClr val=\"44546A\"/></a:dk2><a:lt2><a:srgbClr val=\"E7E6E6\"/></a:lt2><a:accent1><a:srgbClr val=\"4472C4\"/></a:accent1>" +
                "<a:accent2><a:srgbClr val=\"ED7D31\"/></a:accent2><a:accent3><a:srgbClr val=\"A5A5A5\"/></a:accent3><a:accent4><a:srgbClr val=\"FFC000\"/></a:accent4>" +
                "<a:accent5><a:srgbClr val=\"5B9BD5\"/></a:accent5><a:accent6><a:srgbClr val=\"70AD47\"/></a:accent6><a:hlink><a:srgbClr val=\"0563C1\"/></a:hlink>" +
                "<a:folHlink><a:srgbClr val=\"954F72\"/></a:folHlink></a:clrScheme>") +
            $"<a:fontScheme name=\"Office\"><a:majorFont><a:latin typeface=\"{major}\"/><a:ea typeface=\"\"/><a:cs typeface=\"\"/></a:majorFont>" +
            $"<a:minorFont><a:latin typeface=\"{minor}\"/><a:ea typeface=\"\"/><a:cs typeface=\"\"/></a:minorFont></a:fontScheme>" +
            "<a:fmtScheme name=\"Office\">" +
            $"<a:fillStyleLst>{Repeat(Fill)}</a:fillStyleLst><a:lnStyleLst>{Repeat($"<a:ln w=\"6350\">{Fill}</a:ln>")}</a:lnStyleLst>" +
            $"<a:effectStyleLst>{Repeat("<a:effectStyle><a:effectLst/></a:effectStyle>")}</a:effectStyleLst><a:bgFillStyleLst>{Repeat(Fill)}</a:bgFillStyleLst>" +
            "</a:fmtScheme></a:themeElements></a:theme>");

    private const string Fill = "<a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill>";

    private static string Repeat(string xml) => xml + xml + xml;

    public DocxBuilder CoreProperties(string xml)
    {
        _rootRels = "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/>";
        _parts["/docProps/core.xml"] = (Encoding.UTF8.GetBytes(
            "<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" " +
            $"xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">{xml}</cp:coreProperties>"),
            "application/vnd.openxmlformats-package.core-properties+xml");
        return this;
    }

    public DocxBuilder Header(string body, string type = "default") => HeaderFooter("header", body, type);

    public DocxBuilder Footer(string body, string type = "default") => HeaderFooter("footer", body, type);

    public DocxBuilder Media(string relationshipId, string fileName, byte[] content)
    {
        _rels.Add((relationshipId, "image", "media/" + fileName, false));
        _parts["/word/media/" + fileName] = (content, Path.GetExtension(fileName) == ".png" ? "image/png" : "image/jpeg");
        return this;
    }

    public DocxBuilder Link(string relationshipId, string target)
    {
        _rels.Add((relationshipId, "hyperlink", target, true));
        return this;
    }

    public DocxBuilder Extra(string part, byte[] content, string contentType = "application/octet-stream")
    {
        _parts[part] = (content, contentType);
        return this;
    }

    public byte[] Build()
    {
        var main = MainPart.TrimStart('/');
        var folder = main[..(main.LastIndexOf('/') + 1)];
        var sectPr = _body.Contains("<w:sectPr", StringComparison.Ordinal)
            ? ""
            : $"<w:sectPr w:rsidR=\"00A1B2C3\">{string.Concat(_references)}<w:pgSz w:w=\"11906\" w:h=\"16838\"/>" +
              $"<w:pgMar w:top=\"1417\" w:right=\"1417\" w:bottom=\"1134\" w:left=\"1417\" w:header=\"708\" w:footer=\"708\" w:gutter=\"0\"/>{SectionProperties}</w:sectPr>";
        var document = RawDocument ?? $"<w:document {Root}><w:body>{WordShaped(_body)}{sectPr}</w:body></w:document>";
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            var overrides = new StringBuilder($"<Override PartName=\"{MainPart}\" ContentType=\"{MainContentType ?? Wml + (Template ? "template" : "document") + ".main+xml"}\"/>");
            foreach (var (part, (_, contentType)) in _parts) overrides.Append($"<Override PartName=\"{part}\" ContentType=\"{contentType}\"/>");
            Entry(zip, "[Content_Types].xml",
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                $"<Default Extension=\"xml\" ContentType=\"application/xml\"/>{overrides}</Types>");
            Entry(zip, "_rels/.rels",
                $"<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"{RelNs}/officeDocument\" Target=\"{main}\"/>{_rootRels}</Relationships>");
            Entry(zip, main, document);
            Entry(zip, folder + "_rels/" + main[folder.Length..] + ".rels",
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                string.Concat(_rels.Select(r => $"<Relationship Id=\"{r.Id}\" Type=\"{RelNs}/{r.Type}\" Target=\"{r.Target}\"{(r.External ? " TargetMode=\"External\"" : "")}/>")) +
                string.Concat(_rawRels) + "</Relationships>");
            foreach (var (part, (content, _)) in _parts)
                using (var entry = zip.CreateEntry(part.TrimStart('/')).Open())
                    entry.Write(content);
        }

        return stream.ToArray();
    }

    public TextDocument Load() => TextDocument.Load(new MemoryStream(Build()), Template ? DocumentFileFormat.Dotx : DocumentFileFormat.Docx);

    private DocxBuilder Part(string name, string type, string contentType, string xml)
    {
        _rels.Add(("rId" + type, type, name, false));
        _parts["/word/" + name] = (Encoding.UTF8.GetBytes(xml), contentType);
        return this;
    }

    private DocxBuilder HeaderFooter(string kind, string body, string type)
    {
        var n = _references.Count + 1;
        var id = $"rId{kind}{n}";
        _rels.Add((id, kind, $"{kind}{n}.xml", false));
        _parts[$"/word/{kind}{n}.xml"] = (Encoding.UTF8.GetBytes($"<w:{(kind == "header" ? "hdr" : "ftr")} {Root}>{WordShaped(body)}</w:{(kind == "header" ? "hdr" : "ftr")}>"),
            Wml + kind + "+xml");
        _references.Add($"<w:{kind}Reference w:type=\"{type}\" r:id=\"{id}\"/>");
        return this;
    }

    private string WordShaped(string xml)
    {
        xml = Regex.Replace(xml, "<w:p(?=[ >/])", _ => $"<w:p w14:paraId=\"{_paraId++:X8}\" w14:textId=\"77777777\" w:rsidR=\"00A1B2C3\" w:rsidRDefault=\"00A1B2C3\"");
        return Regex.Replace(xml, "<w:r(?=[ >])", "<w:r w:rsidRPr=\"00D4E5F6\"");
    }

    private static void Entry(ZipArchive zip, string name, string content)
    {
        using var entry = zip.CreateEntry(name).Open();
        entry.Write(Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" + content));
    }
}
