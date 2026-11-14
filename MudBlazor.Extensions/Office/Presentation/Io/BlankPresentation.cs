using System.IO.Compression;
using System.Text;

namespace MudBlazor.Extensions.Office.Presentation;

internal static class BlankPresentation
{
    internal const string Namespaces = "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
                                       "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" " +
                                       "xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\"";

    internal const string TreeStart = "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr/>";

    private const string Declaration = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n";
    private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string ContentType = "application/vnd.openxmlformats-officedocument.";
    private const string MapOverride = "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>";

    private const string ColorMap = "<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" " +
                                    "accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>";

    private const string Background = "<p:bg><p:bgRef idx=\"1001\"><a:schemeClr val=\"bg1\"/></p:bgRef></p:bg>";

    private static readonly (string Type, string Name, string Shapes)[] Layouts =
    [
        ("title", "Title Slide",
            Placeholder(2, "Title 1", "type=\"ctrTitle\"", Xfrm(1524000, 1122363, 9144000, 2387600), "anchor=\"b\"",
                "<a:lvl1pPr algn=\"ctr\"><a:defRPr sz=\"6000\"/></a:lvl1pPr>", "Click to edit Master title style") +
            Placeholder(3, "Subtitle 2", "type=\"subTitle\" idx=\"1\"", Xfrm(1524000, 3602038, 9144000, 1655762), "",
                "<a:lvl1pPr marL=\"0\" indent=\"0\" algn=\"ctr\"><a:buNone/><a:defRPr sz=\"2400\"/></a:lvl1pPr>", "Click to edit Master subtitle style")),
        ("obj", "Title and Content",
            Placeholder(2, "Title 1", "type=\"title\"", "", "", "", "Click to edit Master title style") +
            Placeholder(3, "Content Placeholder 2", "idx=\"1\"", "", "", "", "Click to edit Master text styles")),
        ("titleOnly", "Title Only", Placeholder(2, "Title 1", "type=\"title\"", "", "", "", "Click to edit Master title style")),
        ("blank", "Blank", "")
    ];

    internal static byte[] Build()
    {
        var parts = new List<(string Name, string ContentType, string Xml)>
        {
            ("_rels/.rels", null, Rels(("officeDocument", "ppt/presentation.xml"))),
            ("ppt/presentation.xml", "presentationml.presentation.main", PresentationXml),
            ("ppt/_rels/presentation.xml.rels", null, Rels(("slideMaster", "slideMasters/slideMaster1.xml"), ("notesMaster", "notesMasters/notesMaster1.xml"),
                ("slide", "slides/slide1.xml"), ("theme", "theme/theme1.xml"), ("presProps", "presProps.xml"), ("viewProps", "viewProps.xml"), ("tableStyles", "tableStyles.xml"))),
            ("ppt/presProps.xml", "presentationml.presProps", $"<p:presentationPr {Namespaces}/>"),
            ("ppt/viewProps.xml", "presentationml.viewProps", $"<p:viewPr {Namespaces}/>"),
            ("ppt/tableStyles.xml", "presentationml.tableStyles", $"<a:tblStyleLst xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" def=\"{TableShape.DefaultStyleId}\"/>"),
            ("ppt/theme/theme1.xml", "theme", Theme),
            ("ppt/theme/theme2.xml", "theme", Theme),
            ("ppt/slideMasters/slideMaster1.xml", "presentationml.slideMaster", Master),
            ("ppt/slideMasters/_rels/slideMaster1.xml.rels", null,
                Rels([.. Layouts.Select((_, i) => ("slideLayout", $"../slideLayouts/slideLayout{i + 1}.xml")), ("theme", "../theme/theme1.xml")])),
            ("ppt/notesMasters/notesMaster1.xml", "presentationml.notesMaster", NotesMaster),
            ("ppt/notesMasters/_rels/notesMaster1.xml.rels", null, Rels(("theme", "../theme/theme2.xml"))),
            ("ppt/slides/slide1.xml", "presentationml.slide",
                $"<p:sld {Namespaces}><p:cSld><p:spTree>{TreeStart}" +
                Placeholder(2, "Title 1", "type=\"ctrTitle\"", "", "", "", null) + Placeholder(3, "Subtitle 2", "type=\"subTitle\" idx=\"1\"", "", "", "", null) +
                $"</p:spTree></p:cSld>{MapOverride}</p:sld>"),
            ("ppt/slides/_rels/slide1.xml.rels", null, Rels(("slideLayout", "../slideLayouts/slideLayout1.xml")))
        };
        for (var i = 0; i < Layouts.Length; i++)
        {
            var (type, name, shapes) = Layouts[i];
            parts.Add(($"ppt/slideLayouts/slideLayout{i + 1}.xml", "presentationml.slideLayout",
                $"<p:sldLayout {Namespaces} type=\"{type}\" preserve=\"1\"><p:cSld name=\"{name}\"><p:spTree>{TreeStart}{shapes}</p:spTree></p:cSld>{MapOverride}</p:sldLayout>"));
            parts.Add(($"ppt/slideLayouts/_rels/slideLayout{i + 1}.xml.rels", null, Rels(("slideMaster", "../slideMasters/slideMaster1.xml"))));
        }

        var types = new StringBuilder("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                                      "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                                      "<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
        foreach (var (name, contentType, _) in parts.Where(p => p.ContentType != null))
            types.Append($"<Override PartName=\"/{name}\" ContentType=\"{ContentType}{contentType}+xml\"/>");
        parts.Insert(0, ("[Content_Types].xml", null, types.Append("</Types>").ToString()));

        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, true))
            foreach (var (name, _, xml) in parts)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name, CompressionLevel.Optimal).Open(), new UTF8Encoding(false));
                writer.Write(Declaration + xml);
            }

        return buffer.ToArray();
    }

    private static string Rels(params (string Type, string Target)[] rels) =>
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        string.Concat(rels.Select((r, i) => $"<Relationship Id=\"rId{i + 1}\" Type=\"{RelNs}/{r.Type}\" Target=\"{r.Target}\"/>")) + "</Relationships>";

    private static string Xfrm(long x, long y, long cx, long cy) => $"<a:xfrm><a:off x=\"{x}\" y=\"{y}\"/><a:ext cx=\"{cx}\" cy=\"{cy}\"/></a:xfrm>";

    private static string Placeholder(int id, string name, string ph, string xfrm, string bodyPr, string lstStyle, string prompt) =>
        $"<p:sp><p:nvSpPr><p:cNvPr id=\"{id}\" name=\"{name}\"/><p:cNvSpPr><a:spLocks noGrp=\"1\"/></p:cNvSpPr><p:nvPr><p:ph {ph}/></p:nvPr></p:nvSpPr>" +
        $"<p:spPr>{xfrm}</p:spPr><p:txBody><a:bodyPr {bodyPr}/><a:lstStyle>{lstStyle}</a:lstStyle>" +
        (prompt == null ? "<a:p><a:endParaRPr lang=\"en-US\"/></a:p>" : $"<a:p><a:r><a:rPr lang=\"en-US\"/><a:t>{prompt}</a:t></a:r></a:p>") + "</p:txBody></p:sp>";

    private static string Level(int level, string attributes, string children, int size) =>
        $"<a:lvl{level}pPr {attributes} defTabSz=\"914400\" rtl=\"0\" eaLnBrk=\"1\" latinLnBrk=\"0\" hangingPunct=\"1\">{children}" +
        $"<a:defRPr sz=\"{size}\" kern=\"1200\"><a:solidFill><a:schemeClr val=\"tx1\"/></a:solidFill>" +
        $"<a:latin typeface=\"+mn-lt\"/><a:ea typeface=\"+mn-ea\"/><a:cs typeface=\"+mn-cs\"/></a:defRPr></a:lvl{level}pPr>";

    private static string Bullet(int level, long margin, int size) =>
        Level(level, $"marL=\"{margin}\" indent=\"-228600\" algn=\"l\"",
            "<a:lnSpc><a:spcPct val=\"90000\"/></a:lnSpc><a:spcBef><a:spcPts val=\"500\"/></a:spcBef><a:buFont typeface=\"Arial\"/><a:buChar char=\"&#8226;\"/>", size);

    private static string Plain(int level, int size) => Level(level, $"marL=\"{(level - 1) * 457200}\" algn=\"l\"", "", size);

    private static string PresentationXml =>
        $"<p:presentation {Namespaces} saveSubsetFonts=\"1\"><p:sldMasterIdLst><p:sldMasterId id=\"2147483648\" r:id=\"rId1\"/></p:sldMasterIdLst>" +
        "<p:notesMasterIdLst><p:notesMasterId r:id=\"rId2\"/></p:notesMasterIdLst><p:sldIdLst><p:sldId id=\"256\" r:id=\"rId3\"/></p:sldIdLst>" +
        "<p:sldSz cx=\"12192000\" cy=\"6858000\"/><p:notesSz cx=\"6858000\" cy=\"9144000\"/>" +
        $"<p:defaultTextStyle><a:defPPr><a:defRPr lang=\"en-US\"/></a:defPPr>{string.Concat(Enumerable.Range(1, 9).Select(l => Plain(l, 1800)))}</p:defaultTextStyle></p:presentation>";

    private static string Master =>
        $"<p:sldMaster {Namespaces}><p:cSld>{Background}<p:spTree>{TreeStart}" +
        Placeholder(2, "Title Placeholder 1", "type=\"title\"", Xfrm(838200, 365125, 10515600, 1325563) + "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>",
            "vert=\"horz\" lIns=\"91440\" tIns=\"45720\" rIns=\"91440\" bIns=\"45720\" rtlCol=\"0\" anchor=\"ctr\"", "", "Click to edit Master title style") +
        Placeholder(3, "Text Placeholder 2", "type=\"body\" idx=\"1\"", Xfrm(838200, 1825625, 10515600, 4351338) + "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>",
            "vert=\"horz\" lIns=\"91440\" tIns=\"45720\" rIns=\"91440\" bIns=\"45720\" rtlCol=\"0\"", "", "Click to edit Master text styles") +
        $"</p:spTree></p:cSld>{ColorMap}<p:sldLayoutIdLst>" +
        string.Concat(Layouts.Select((_, i) => $"<p:sldLayoutId id=\"{2147483649L + i}\" r:id=\"rId{i + 1}\"/>")) + "</p:sldLayoutIdLst><p:txStyles>" +
        "<p:titleStyle><a:lvl1pPr algn=\"l\" defTabSz=\"914400\" rtl=\"0\" eaLnBrk=\"1\" latinLnBrk=\"0\" hangingPunct=\"1\"><a:lnSpc><a:spcPct val=\"90000\"/></a:lnSpc>" +
        "<a:spcBef><a:spcPct val=\"0\"/></a:spcBef><a:buNone/><a:defRPr sz=\"4400\" kern=\"1200\"><a:solidFill><a:schemeClr val=\"tx1\"/></a:solidFill>" +
        "<a:latin typeface=\"+mj-lt\"/><a:ea typeface=\"+mj-ea\"/><a:cs typeface=\"+mj-cs\"/></a:defRPr></a:lvl1pPr></p:titleStyle>" +
        $"<p:bodyStyle>{Bullet(1, 228600, 2800)}{Bullet(2, 685800, 2400)}{Bullet(3, 1143000, 2000)}{Bullet(4, 1600200, 1800)}{Bullet(5, 2057400, 1800)}</p:bodyStyle>" +
        $"<p:otherStyle><a:defPPr><a:defRPr lang=\"en-US\"/></a:defPPr>{Plain(1, 1800)}</p:otherStyle></p:txStyles></p:sldMaster>";

    private static string NotesMaster =>
        $"<p:notesMaster {Namespaces}><p:cSld>{Background}<p:spTree>{TreeStart}" +
        "<p:sp><p:nvSpPr><p:cNvPr id=\"2\" name=\"Slide Image Placeholder 1\"/><p:cNvSpPr><a:spLocks noGrp=\"1\" noRot=\"1\" noChangeAspect=\"1\"/></p:cNvSpPr>" +
        $"<p:nvPr><p:ph type=\"sldImg\" idx=\"2\"/></p:nvPr></p:nvSpPr><p:spPr>{Xfrm(685800, 1143000, 5486400, 3086100)}<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>" +
        "<a:noFill/><a:ln w=\"12700\"><a:solidFill><a:prstClr val=\"black\"/></a:solidFill></a:ln></p:spPr></p:sp>" +
        Placeholder(3, "Notes Placeholder 2", "type=\"body\" sz=\"quarter\" idx=\"3\"", Xfrm(685800, 4400550, 5486400, 3600450) + "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>",
            "vert=\"horz\" lIns=\"91440\" tIns=\"45720\" rIns=\"91440\" bIns=\"45720\" rtlCol=\"0\"", "", "Click to edit Master text styles") +
        $"</p:spTree></p:cSld>{ColorMap}<p:notesStyle>{Plain(1, 1200)}</p:notesStyle></p:notesMaster>";

    private static string Theme =>
        "<a:theme xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" name=\"MudEx\"><a:themeElements><a:clrScheme name=\"MudEx\">" +
        "<a:dk1><a:sysClr val=\"windowText\" lastClr=\"000000\"/></a:dk1><a:lt1><a:sysClr val=\"window\" lastClr=\"FFFFFF\"/></a:lt1>" +
        string.Concat(new[] { ("dk2", "44546A"), ("lt2", "E7E6E6"), ("accent1", "4472C4"), ("accent2", "ED7D31"), ("accent3", "A5A5A5"), ("accent4", "FFC000"),
            ("accent5", "5B9BD5"), ("accent6", "70AD47"), ("hlink", "0563C1"), ("folHlink", "954F72") }.Select(c => $"<a:{c.Item1}><a:srgbClr val=\"{c.Item2}\"/></a:{c.Item1}>")) +
        "</a:clrScheme><a:fontScheme name=\"MudEx\"><a:majorFont><a:latin typeface=\"Calibri Light\"/><a:ea typeface=\"\"/><a:cs typeface=\"\"/></a:majorFont>" +
        "<a:minorFont><a:latin typeface=\"Calibri\"/><a:ea typeface=\"\"/><a:cs typeface=\"\"/></a:minorFont></a:fontScheme><a:fmtScheme name=\"MudEx\">" +
        "<a:fillStyleLst><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill><a:solidFill><a:schemeClr val=\"phClr\"><a:tint val=\"50000\"/></a:schemeClr></a:solidFill>" +
        "<a:solidFill><a:schemeClr val=\"phClr\"><a:shade val=\"80000\"/></a:schemeClr></a:solidFill></a:fillStyleLst><a:lnStyleLst>" +
        string.Concat(new[] { 6350, 12700, 19050 }.Select(w => $"<a:ln w=\"{w}\" cap=\"flat\" cmpd=\"sng\" algn=\"ctr\"><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill>" +
                                                                  "<a:prstDash val=\"solid\"/><a:miter lim=\"800000\"/></a:ln>")) +
        "</a:lnStyleLst><a:effectStyleLst><a:effectStyle><a:effectLst/></a:effectStyle><a:effectStyle><a:effectLst/></a:effectStyle><a:effectStyle><a:effectLst/></a:effectStyle>" +
        "</a:effectStyleLst><a:bgFillStyleLst><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill><a:solidFill><a:schemeClr val=\"phClr\"><a:tint val=\"95000\"/></a:schemeClr>" +
        "</a:solidFill><a:solidFill><a:schemeClr val=\"phClr\"><a:shade val=\"90000\"/></a:schemeClr></a:solidFill></a:bgFillStyleLst></a:fmtScheme></a:themeElements>" +
        "<a:objectDefaults/><a:extraClrSchemeLst/></a:theme>";
}
