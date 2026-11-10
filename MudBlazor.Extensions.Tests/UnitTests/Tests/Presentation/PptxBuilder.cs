using System.IO.Compression;
using System.Text;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

internal sealed class PptxBuilder
{
    public const string Ns = "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
                             "xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" " +
                             "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"";

    private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private readonly List<(string Shapes, string Attributes, string? Notes, string Rels)> _slides = [];

    private readonly Dictionary<string, string> _theme = new()
    {
        ["dk1"] = "000000", ["lt1"] = "FFFFFF", ["dk2"] = "44546A", ["lt2"] = "E7E6E6", ["accent1"] = "4472C4", ["accent2"] = "ED7D31",
        ["accent3"] = "A5A5A5", ["accent4"] = "FFC000", ["accent5"] = "5B9BD5", ["accent6"] = "70AD47", ["hlink"] = "0563C1", ["folHlink"] = "954F72"
    };

    public string ExtraSlideIds { get; set; } = "";

    public long Width { get; set; } = 12192000;

    public long Height { get; set; } = 6858000;

    public string ClrMap { get; set; } = "bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" " +
                                         "accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"";

    public string MasterShapes { get; set; } =
        Placeholder(2, "Title Placeholder 1", "type=\"title\"", Xfrm(838200, 365125, 10515600, 1325563)) +
        Placeholder(3, "Text Placeholder 2", "type=\"body\" idx=\"1\"", Xfrm(838200, 1825625, 10515600, 4351338));

    public string TxStyles { get; set; } =
        "<p:titleStyle><a:lvl1pPr algn=\"l\"><a:defRPr sz=\"4400\"><a:solidFill><a:schemeClr val=\"tx1\"/></a:solidFill><a:latin typeface=\"+mj-lt\"/></a:defRPr></a:lvl1pPr></p:titleStyle>" +
        "<p:bodyStyle><a:lvl1pPr><a:defRPr sz=\"2800\"><a:solidFill><a:schemeClr val=\"tx1\"/></a:solidFill><a:latin typeface=\"+mn-lt\"/></a:defRPr></a:lvl1pPr>" +
        "<a:lvl2pPr><a:defRPr sz=\"2400\"/></a:lvl2pPr></p:bodyStyle><p:otherStyle/>";

    public string LayoutShapes { get; set; } =
        Placeholder(2, "Title 1", "type=\"title\"", Xfrm(1000000, 500000, 8000000, 1000000)) +
        Placeholder(3, "Content Placeholder 2", "idx=\"1\"", "");

    public PptxBuilder ThemeColor(string name, string rgb)
    {
        _theme[name] = rgb;
        return this;
    }

    public PptxBuilder Slide(string shapes, string attributes = "", string? notes = null, string rels = "")
    {
        _slides.Add((shapes, attributes, notes, rels));
        return this;
    }

    public Presentation Load() => Presentation.Load(new MemoryStream(Build()));

    public static string Xfrm(long x, long y, long cx, long cy, string attributes = "") =>
        $"<a:xfrm{attributes}><a:off x=\"{x}\" y=\"{y}\"/><a:ext cx=\"{cx}\" cy=\"{cy}\"/></a:xfrm>";

    public static string Placeholder(uint id, string name, string ph, string xfrm, string body = "<a:p/>") =>
        $"<p:sp><p:nvSpPr><p:cNvPr id=\"{id}\" name=\"{name}\"/><p:cNvSpPr/><p:nvPr><p:ph {ph}/></p:nvPr></p:nvSpPr>" +
        $"<p:spPr>{xfrm}</p:spPr><p:txBody><a:bodyPr/><a:lstStyle/>{body}</p:txBody></p:sp>";

    public static string Shape(uint id, string name, string spPr, string? body = null) =>
        $"<p:sp><p:nvSpPr><p:cNvPr id=\"{id}\" name=\"{name}\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr>{spPr}</p:spPr>" +
        (body == null ? "" : $"<p:txBody><a:bodyPr/><a:lstStyle/>{body}</p:txBody>") + "</p:sp>";

    public byte[] Build()
    {
        var parts = new Dictionary<string, string>();
        var overrides = new StringBuilder();
        void Add(string part, string? contentType, string xml)
        {
            parts[part] = xml;
            if (contentType != null) overrides.Append($"<Override PartName=\"/{part}\" ContentType=\"application/vnd.openxmlformats-{contentType}+xml\"/>");
        }

        string Rels(params (string Id, string Type, string Target)[] rels) =>
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            string.Concat(rels.Select(r => $"<Relationship Id=\"{r.Id}\" Type=\"{RelNs}/{r.Type}\" Target=\"{r.Target}\"/>")) + "</Relationships>";

        Add("_rels/.rels", null, Rels(("rId1", "officeDocument", "ppt/presentation.xml")));
        var slideIds = string.Concat(_slides.Select((_, i) => $"<p:sldId id=\"{256 + i}\" r:id=\"rId{i + 2}\"/>"));
        Add("ppt/presentation.xml", "officedocument.presentationml.presentation.main",
            $"<p:presentation {Ns}><p:sldMasterIdLst><p:sldMasterId id=\"2147483648\" r:id=\"rId1\"/></p:sldMasterIdLst>" +
            $"<p:sldIdLst>{slideIds}{ExtraSlideIds}</p:sldIdLst><p:sldSz cx=\"{Width}\" cy=\"{Height}\"/><p:notesSz cx=\"6858000\" cy=\"9144000\"/>" +
            "<p:defaultTextStyle><a:lvl1pPr><a:defRPr sz=\"1800\"><a:solidFill><a:schemeClr val=\"tx1\"/></a:solidFill></a:defRPr></a:lvl1pPr></p:defaultTextStyle></p:presentation>");
        Add("ppt/_rels/presentation.xml.rels", null,
            Rels([("rId1", "slideMaster", "slideMasters/slideMaster1.xml"), .._slides.Select((_, i) => ($"rId{i + 2}", "slide", $"slides/slide{i + 1}.xml"))]));

        var colors = string.Concat(_theme.Select(c => $"<a:{c.Key}><a:srgbClr val=\"{c.Value}\"/></a:{c.Key}>"));
        Add("ppt/theme/theme1.xml", "officedocument.theme",
            $"<a:theme {Ns} name=\"Test\"><a:themeElements><a:clrScheme name=\"Test\">{colors}</a:clrScheme>" +
            "<a:fontScheme name=\"Test\"><a:majorFont><a:latin typeface=\"Major Font\"/></a:majorFont><a:minorFont><a:latin typeface=\"Minor Font\"/></a:minorFont></a:fontScheme>" +
            "</a:themeElements></a:theme>");
        Add("ppt/slideMasters/slideMaster1.xml", "officedocument.presentationml.slideMaster",
            $"<p:sldMaster {Ns}><p:cSld><p:spTree>{TreeStart}{MasterShapes}</p:spTree></p:cSld><p:clrMap {ClrMap}/>" +
            $"<p:sldLayoutIdLst><p:sldLayoutId id=\"2147483649\" r:id=\"rId1\"/></p:sldLayoutIdLst><p:txStyles>{TxStyles}</p:txStyles></p:sldMaster>");
        Add("ppt/slideMasters/_rels/slideMaster1.xml.rels", null,
            Rels(("rId1", "slideLayout", "../slideLayouts/slideLayout1.xml"), ("rId2", "theme", "../theme/theme1.xml")));
        Add("ppt/slideLayouts/slideLayout1.xml", "officedocument.presentationml.slideLayout",
            $"<p:sldLayout {Ns} type=\"obj\"><p:cSld name=\"Title and Content\"><p:spTree>{TreeStart}{LayoutShapes}</p:spTree></p:cSld></p:sldLayout>");
        Add("ppt/slideLayouts/_rels/slideLayout1.xml.rels", null, Rels(("rId1", "slideMaster", "../slideMasters/slideMaster1.xml")));

        for (var i = 0; i < _slides.Count; i++)
        {
            var (shapes, attributes, notes, rels) = _slides[i];
            var n = i + 1;
            Add($"ppt/slides/slide{n}.xml", "officedocument.presentationml.slide",
                $"<p:sld {Ns} {attributes}><p:cSld><p:spTree>{TreeStart}{shapes}</p:spTree></p:cSld></p:sld>");
            var slideRels = new List<(string, string, string)> { ("rId1", "slideLayout", "../slideLayouts/slideLayout1.xml") };
            if (notes != null)
            {
                slideRels.Add(("rId2", "notesSlide", $"../notesSlides/notesSlide{n}.xml"));
                var paragraphs = string.Concat(notes.Split('\n').Select(line => $"<a:p><a:r><a:t>{line}</a:t></a:r></a:p>"));
                Add($"ppt/notesSlides/notesSlide{n}.xml", "officedocument.presentationml.notesSlide",
                    $"<p:notes {Ns}><p:cSld><p:spTree>{TreeStart}" +
                    Placeholder(2, "Slide Image Placeholder 1", "type=\"sldImg\"", "", "") +
                    Placeholder(3, "Notes Placeholder 2", "type=\"body\" idx=\"1\"", "", paragraphs) +
                    Placeholder(4, "Slide Number Placeholder 3", "type=\"sldNum\" idx=\"5\"", "", "<a:p><a:r><a:t>99</a:t></a:r></a:p>") +
                    "</p:spTree></p:cSld></p:notes>");
                Add($"ppt/notesSlides/_rels/notesSlide{n}.xml.rels", null, Rels(("rId1", "slide", $"../slides/slide{n}.xml")));
            }

            Add($"ppt/slides/_rels/slide{n}.xml.rels", null, Rels([.. slideRels]).Replace("</Relationships>", rels + "</Relationships>"));
        }

        parts["[Content_Types].xml"] = "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                                       "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                                       "<Default Extension=\"xml\" ContentType=\"application/xml\"/><Default Extension=\"png\" ContentType=\"image/png\"/>" +
                                       overrides + "</Types>";

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            foreach (var (name, xml) in parts)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
                writer.Write(xml);
            }

        return ms.ToArray();
    }

    private const string TreeStart = "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr/>";
}
