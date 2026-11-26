using System.Text;

namespace MudBlazor.Extensions.Office.Document;

internal static class BlankDocument
{
    private const string Wml = "application/vnd.openxmlformats-officedocument.wordprocessingml.";
    private const string Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/";
    private const string W = "xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"";
    private const string Declaration = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n";

    private const string Styles =
        $"<w:styles {W}><w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:ascii=\"Calibri\" w:eastAsia=\"Calibri\" w:hAnsi=\"Calibri\" w:cs=\"Times New Roman\"/>" +
        "<w:sz w:val=\"22\"/><w:szCs w:val=\"22\"/><w:lang w:val=\"en-US\" w:eastAsia=\"en-US\" w:bidi=\"ar-SA\"/></w:rPr></w:rPrDefault>" +
        "<w:pPrDefault><w:pPr><w:spacing w:after=\"160\" w:line=\"259\" w:lineRule=\"auto\"/></w:pPr></w:pPrDefault></w:docDefaults>" +
        "<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/><w:qFormat/></w:style>" +
        "<w:style w:type=\"character\" w:default=\"1\" w:styleId=\"DefaultParagraphFont\"><w:name w:val=\"Default Paragraph Font\"/><w:uiPriority w:val=\"1\"/><w:semiHidden/><w:unhideWhenUsed/></w:style>" +
        "<w:style w:type=\"table\" w:default=\"1\" w:styleId=\"TableNormal\"><w:name w:val=\"Normal Table\"/><w:uiPriority w:val=\"99\"/><w:semiHidden/><w:unhideWhenUsed/>" +
        "<w:tblPr><w:tblInd w:w=\"0\" w:type=\"dxa\"/><w:tblCellMar><w:top w:w=\"0\" w:type=\"dxa\"/><w:left w:w=\"108\" w:type=\"dxa\"/><w:bottom w:w=\"0\" w:type=\"dxa\"/>" +
        "<w:right w:w=\"108\" w:type=\"dxa\"/></w:tblCellMar></w:tblPr></w:style>" +
        "<w:style w:type=\"numbering\" w:default=\"1\" w:styleId=\"NoList\"><w:name w:val=\"No List\"/><w:uiPriority w:val=\"99\"/><w:semiHidden/><w:unhideWhenUsed/></w:style></w:styles>";

    private const string Settings =
        $"<w:settings {W}><w:defaultTabStop w:val=\"720\"/><w:characterSpacingControl w:val=\"doNotCompress\"/>" +
        "<w:compat><w:compatSetting w:name=\"compatibilityMode\" w:uri=\"http://schemas.microsoft.com/office/word\" w:val=\"15\"/></w:compat></w:settings>";

    private const string Core =
        "<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" " +
        "xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:dcmitype=\"http://purl.org/dc/dcmitype/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"/>";

    private const string App =
        "<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\" " +
        "xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\"><Application>MudBlazor.Extensions</Application></Properties>";

    internal static byte[] Build(PageSetup page)
    {
        var orientation = page.Orientation == PageOrientation.Landscape ? " w:orient=\"landscape\"" : "";
        var document =
            $"<w:document {W} xmlns:r=\"{Rel[..^1]}\"><w:body><w:p/><w:sectPr><w:pgSz w:w=\"{page.Width}\" w:h=\"{page.Height}\"{orientation}/>" +
            $"<w:pgMar w:top=\"{page.MarginTop}\" w:right=\"{page.MarginRight}\" w:bottom=\"{page.MarginBottom}\" w:left=\"{page.MarginLeft}\" " +
            $"w:header=\"{page.HeaderDistance}\" w:footer=\"{page.FooterDistance}\" w:gutter=\"{page.Gutter}\"/><w:cols w:space=\"708\"/><w:docGrid w:linePitch=\"360\"/></w:sectPr></w:body></w:document>";
        using var stream = new MemoryStream();
        using (var writer = new OpcPackageWriter(_ => null, stream))
        {
            writer.PutContentTypes(
                new Dictionary<string, string> { ["rels"] = "application/vnd.openxmlformats-package.relationships+xml", ["xml"] = "application/xml" },
                new Dictionary<string, string>
                {
                    ["/word/document.xml"] = Wml + "document.main+xml",
                    ["/word/styles.xml"] = Wml + "styles+xml",
                    ["/word/settings.xml"] = Wml + "settings+xml",
                    ["/docProps/core.xml"] = "application/vnd.openxmlformats-package.core-properties+xml",
                    ["/docProps/app.xml"] = "application/vnd.openxmlformats-officedocument.extended-properties+xml"
                });
            writer.PutRelationships("/", [
                new OpcRelationship("rId1", Rel + "officeDocument", "word/document.xml", null),
                new OpcRelationship("rId2", "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties", "docProps/core.xml", null),
                new OpcRelationship("rId3", Rel + "extended-properties", "docProps/app.xml", null)
            ]);
            writer.PutRelationships("/word/document.xml", [
                new OpcRelationship("rId1", Rel + "styles", "styles.xml", null),
                new OpcRelationship("rId2", Rel + "settings", "settings.xml", null)
            ]);
            Put(writer, "/word/document.xml", document);
            Put(writer, "/word/styles.xml", Styles);
            Put(writer, "/word/settings.xml", Settings);
            Put(writer, "/docProps/core.xml", Core);
            Put(writer, "/docProps/app.xml", App);
        }

        return stream.ToArray();
    }

    private static void Put(OpcPackageWriter writer, string part, string xml) => writer.Put(part, Encoding.UTF8.GetBytes(Declaration + xml));
}
