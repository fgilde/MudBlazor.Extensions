using System.IO.Compression;
using System.Text;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class DocumentFormatsTests
{
    [Fact]
    public void Detect_ByExtensionAndContentType()
    {
        Assert.Equal(DocumentFileFormat.Docx, DocumentFileFormats.Detect("X.DOCX"));
        Assert.Equal(DocumentFileFormat.Markdown, DocumentFileFormats.Detect(null, "text/markdown"));
        Assert.Equal(DocumentFileFormat.Doc, DocumentFileFormats.Detect(".doc"));
        Assert.Null(DocumentFileFormats.Detect("x.unknown"));
        Assert.Null(DocumentFileFormats.Detect("x.unknown", "application/octet-stream"));
        Assert.Equal(DocumentFileFormat.Html, DocumentFileFormats.Detect("a.htm"));
        Assert.Equal(DocumentFileFormat.Markdown, DocumentFileFormats.Detect("readme.markdown"));
        Assert.Equal(DocumentFileFormat.Dotx, DocumentFileFormats.Detect("t.dotx"));
        Assert.Equal(DocumentFileFormat.Docx, DocumentFileFormats.Detect("m.docm"));
        Assert.Equal(DocumentFileFormat.Html, DocumentFileFormats.Detect("noext", "Text/HTML; charset=utf-8"));
        Assert.Equal(DocumentFileFormat.Text, DocumentFileFormats.Detect("a.md.txt", "text/markdown"));
        foreach (var format in Enum.GetValues<DocumentFileFormat>())
        {
            Assert.Equal(format, DocumentFileFormats.Detect("file" + DocumentFileFormats.Extension(format)));
            Assert.Equal(format, DocumentFileFormats.Detect(null, DocumentFileFormats.ContentType(format)));
            Assert.Equal(format != DocumentFileFormat.Doc, DocumentFileFormats.CanRead(format));
            Assert.Equal(format is not (DocumentFileFormat.Odt or DocumentFileFormat.Doc), DocumentFileFormats.CanWrite(format));
        }
    }

    [Fact]
    public void DetectContent_SignaturesBeatTheName()
    {
        var docx = Bytes(TextDocument.Create(), DocumentFileFormat.Docx);
        var dotx = Bytes(TextDocument.Create(), DocumentFileFormat.Dotx);
        var docm = new DocxBuilder { MainContentType = "application/vnd.ms-word.document.macroEnabled.main+xml" }.Build();
        byte[] ole = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, .. new byte[504]];

        Assert.Equal(DocumentFileFormat.Docx, Detect(docx, "x.txt"));
        Assert.Equal(DocumentFileFormat.Dotx, Detect(dotx));
        Assert.Equal(DocumentFileFormat.Docx, Detect(docm));
        Assert.Equal(DocumentFileFormat.Odt, Detect(Zip(("mimetype", "application/vnd.oasis.opendocument.text"), ("content.xml", "<x/>"))));
        Assert.Null(Detect(Zip(("a.txt", "hello"))));
        Assert.Equal(DocumentFileFormat.Docx, Detect(Zip(("a.txt", "hello")), "broken.docx"));
        Assert.Equal(DocumentFileFormat.Rtf, Detect(Encoding.ASCII.GetBytes("{\\rtf1\\ansi x}"), "x.doc"));
        Assert.Equal(DocumentFileFormat.Doc, Detect(ole, "x.doc"));
        Assert.Null(Detect(ole));
        Assert.Equal(DocumentFileFormat.Html, Detect(Encoding.UTF8.GetBytes("\uFEFF  <!DOCTYPE html><p>x")));
        Assert.Equal(DocumentFileFormat.Html, Detect(Encoding.UTF8.GetBytes("<html lang=\"de\"><body>x")));
        Assert.Equal(DocumentFileFormat.Markdown, Detect(Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("# Title")).ToArray(), "r.md"));
        Assert.Equal(DocumentFileFormat.Markdown, Detect(Encoding.UTF8.GetBytes("<p>x</p>"), "r.md"));
        Assert.Equal(DocumentFileFormat.Markdown, Detect(Encoding.UTF8.GetBytes("# Title"), null, "text/markdown"));
        Assert.Equal(DocumentFileFormat.Text, Detect(Encoding.UTF8.GetBytes("# Title")));
        Assert.Equal(DocumentFileFormat.Text, Detect(Encoding.UTF8.GetBytes("hello"), "x.docx"));
        Assert.Equal(DocumentFileFormat.Text, Detect([]));
        Assert.Null(Detect(Enumerable.Range(0, 256).Select(i => (byte)i).ToArray(), "x.bin"));
    }

    [Fact]
    public void DetectContent_OtherPackagesAreNotWordWhateverTheName()
    {
        var xlsx = File.ReadAllBytes(TestData("Spreadsheet", "ExcelSheet.xlsx"));
        var pptx = File.ReadAllBytes(TestData("Presentation", "sample.pptx"));
        var ods = Zip(("mimetype", "application/vnd.oasis.opendocument.spreadsheet"), ("content.xml", "<x/>"));
        var types = Zip(("[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                                                "<Override PartName=\"/x.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/></Types>"));

        Assert.Null(Detect(xlsx, "report.docx"));
        Assert.Null(Detect(xlsx, "x.txt"));
        Assert.Null(Detect(pptx, "deck.docx"));
        Assert.Null(Detect(ods, "x.odt"));
        Assert.Null(Detect(types, "x.docx"));
    }

    [Fact]
    public void DetectContent_CompoundFilesByDirectory()
    {
        var xls = File.ReadAllBytes(TestData("Spreadsheet", "sample.xls"));

        Assert.Equal(DocumentFileFormat.Doc, Detect(Ole("Root Entry", "WordDocument", "1Table"), "x.bin"));
        Assert.Equal(DocumentFileFormat.Doc, Detect(Ole("Root Entry", "Data", "1Table", "x", "y", "WordDocument")));
        Assert.Null(Detect(xls, "x.doc"));
        Assert.Null(Detect(Ole("Root Entry", "Workbook"), "x.doc"));
        Assert.Null(Detect(Ole("Root Entry", "__substg1.0_0037001F", "__properties_version1.0"), "mail.doc"));
        Assert.Null(Detect(Ole("Root Entry", "PowerPoint Document"), "x.doc"));
        Assert.Equal(DocumentFileFormat.Doc, Detect(Ole("Root Entry", "Unknown"), "x.DOT"));
        Assert.Null(Detect(Ole("Root Entry", "Unknown"), "x.bin"));
    }

    [Fact]
    public void DetectContent_CompoundFiles_OnlyRootEntriesCount()
    {
        var cycle = Ole("Root Entry", "Data", "WordDocument");
        BitConverter.GetBytes(1u).CopyTo(cycle, 1024 + 128 * 2 + 0x44);

        Assert.Null(Detect(Ole("Root Entry", "Workbook", "MBD0001", "/WordDocument", "/1Table"), "x.xls"));
        Assert.Null(Detect(Ole("Root Entry", "ObjectPool", "/WordDocument"), "x.bin"));
        Assert.Equal(DocumentFileFormat.Doc, Detect(Ole("Root Entry", "ObjectPool", "/_1", "WordDocument"), "x.bin"));
        Assert.Equal(DocumentFileFormat.Doc, Detect(cycle));
        Assert.Null(DocumentFileFormats.DetectContent(new FailingStream(Ole("Root Entry", "WordDocument"), 512), "x.doc"));
    }

    [Fact]
    public void DetectContent_ZipAtOffset_ReadsThroughAWindowWithoutCopy()
    {
        using var zip = new MemoryStream();
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, true))
        {
            using (var mimetype = archive.CreateEntry("mimetype", CompressionLevel.NoCompression).Open()) mimetype.Write("application/vnd.oasis.opendocument.text"u8);
            using var padding = archive.CreateEntry("padding.bin", CompressionLevel.NoCompression).Open();
            padding.Write(new byte[8 * 1024 * 1024]);
        }

        var stream = new CountingStream([1, 2, 3, .. zip.ToArray()]) { Position = 3 };

        Assert.Equal(DocumentFileFormat.Odt, DocumentFileFormats.DetectContent(stream));
        Assert.Equal(3, stream.Position);
        Assert.True(stream.BytesRead < 256 * 1024, $"{stream.BytesRead} bytes read");
    }

    [Fact]
    public void Losses_CorruptImageSize_IsAnImageLoss()
    {
        var bmp = new byte[64];
        "BM"u8.CopyTo(bmp);
        BitConverter.GetBytes(10).CopyTo(bmp, 18);
        BitConverter.GetBytes(int.MinValue).CopyTo(bmp, 22);
        var document = HtmlReader.Read("<p>x</p>");
        var paragraph = (Paragraph)document.Blocks[0];
        document.BlockList[0] = paragraph with { Inlines = [new InlineImage(InsertImageCommand.AddMedia(document, bmp, "image/bmp"), 914400, 914400)] };

        Assert.Equal([DocumentFeature.Images], DocumentExport.Losses(document, DocumentFileFormat.Markdown));
    }

    [Fact]
    public void Losses_StyleOverNormalWithTabStops_ComparesTabsByValue()
    {
        var document = new DocxBuilder()
            .Styles("<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/><w:pPr><w:tabs><w:tab w:val=\"left\" w:pos=\"720\"/></w:tabs></w:pPr></w:style>" +
                    "<w:style w:type=\"paragraph\" w:styleId=\"Centred\"><w:name w:val=\"Centred\"/><w:basedOn w:val=\"Normal\"/><w:pPr><w:jc w:val=\"center\"/></w:pPr></w:style>")
            .Body("<w:p><w:pPr><w:pStyle w:val=\"Centred\"/></w:pPr><w:r><w:t>c</w:t></w:r></w:p>")
            .Load();

        Assert.Empty(DocumentExport.Losses(document, DocumentFileFormat.Html));
    }

    [Fact]
    public void DetectContent_RtfAfterBomAndWhitespace_HtmlNeedsATag_ZipAtOffset()
    {
        var rtf = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.ASCII.GetBytes(" \r\n\t{\\rtf1\\ansi x}")).ToArray();
        var docx = Bytes(TextDocument.Create(), DocumentFileFormat.Docx);
        var shifted = new MemoryStream([1, 2, 3, .. docx]) { Position = 3 };

        Assert.Equal(DocumentFileFormat.Rtf, Detect(rtf));
        Assert.Equal("x", ((Paragraph)TextDocument.Load(new MemoryStream(rtf), DocumentFileFormat.Rtf).Blocks[0]).Text);
        Assert.Equal(DocumentFileFormat.Docx, DocumentFileFormats.DetectContent(shifted));
        Assert.Equal(3, shifted.Position);
        Assert.Equal(DocumentFileFormat.Html, Detect(Encoding.UTF8.GetBytes("<!-- saved --><html>")));
        Assert.Equal(DocumentFileFormat.Html, Detect(Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?>\n<!DOCTYPE html><html>")));
        Assert.Equal(DocumentFileFormat.Html, Detect(Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><html xmlns=\"http://www.w3.org/1999/xhtml\">")));
        Assert.Equal(DocumentFileFormat.Text, Detect(Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><w:document/>")));
        Assert.Equal(DocumentFileFormat.Text, Detect(Encoding.UTF8.GetBytes("<p is the variable name")));
        Assert.Equal(DocumentFileFormat.Text, Detect(Encoding.UTF8.GetBytes("<ab> is not a tag")));
        Assert.Equal(DocumentFileFormat.Html, Detect(Encoding.UTF8.GetBytes("<p>x")));
    }

    [Fact]
    public void DetectContent_RestoresPositionAndNeedsSeekableStream()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("xx{\\rtf1 x}"));
        stream.Position = 2;

        Assert.Equal(DocumentFileFormat.Rtf, DocumentFileFormats.DetectContent(stream));
        Assert.Equal(2, stream.Position);
        Assert.Throws<ArgumentException>(() => DocumentFileFormats.DetectContent(new HtmlSecurityTests.EndlessStream()));
    }

    [Fact]
    public void Losses_TxtOfFormattedDocWithTable_FormattingAndTables()
    {
        var document = Formatted();

        Assert.Equal([DocumentFeature.CharacterFormatting, DocumentFeature.ParagraphFormatting, DocumentFeature.Tables],
            DocumentExport.Losses(document, DocumentFileFormat.Text));
        Assert.Empty(DocumentExport.Losses(document, DocumentFileFormat.Html));
        Assert.Equal([DocumentFeature.CharacterFormatting, DocumentFeature.ParagraphFormatting], DocumentExport.Losses(document, DocumentFileFormat.Markdown));
    }

    [Fact]
    public void Losses_Docx_Empty()
    {
        var document = Formatted();

        Assert.Empty(DocumentExport.Losses(document, DocumentFileFormat.Docx));
        Assert.Empty(DocumentExport.Losses(document, DocumentFileFormat.Dotx));
        Assert.Empty(DocumentExport.Losses(TextDocument.Create(), DocumentFileFormat.Text));
    }

    [Fact]
    public void Losses_Md_MergedCellsAndColors()
    {
        var document = HtmlReader.Read(
            "<h1>T</h1><ul><li><b>a</b></li></ul><p><span style=\"color:#FF0000\">red</span> <a href=\"https://example.com/\">l</a></p>" +
            "<table><tr><td colspan=\"2\">a</td></tr><tr><td>b</td><td>c</td></tr></table>");

        Assert.Equal([DocumentFeature.CharacterFormatting, DocumentFeature.MergedCells], DocumentExport.Losses(document, DocumentFileFormat.Markdown));
        Assert.Empty(DocumentExport.Losses(document, DocumentFileFormat.Html));
        Assert.Equal(
            [DocumentFeature.CharacterFormatting, DocumentFeature.Styles, DocumentFeature.Lists, DocumentFeature.Tables, DocumentFeature.MergedCells, DocumentFeature.Links],
            DocumentExport.Losses(document, DocumentFileFormat.Text));
        var nested = HtmlReader.Read("<table><tr><td><table><tr><td>x</td></tr></table></td></tr></table>");
        Assert.Equal([DocumentFeature.NestedTables], DocumentExport.Losses(nested, DocumentFileFormat.Markdown));
    }

    [Fact]
    public void Losses_Html_HeadersBookmarksFieldsSectionsCommentsOpaque()
    {
        var document = new DocxBuilder()
            .Header("<w:p><w:r><w:t>Head</w:t></w:r></w:p>")
            .Comments("<w:comment w:id=\"0\" w:author=\"A\" w:initials=\"A\"><w:p><w:r><w:t>c</w:t></w:r></w:p></w:comment>")
            .Body("<w:p><w:pPr><w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/></w:sectPr></w:pPr></w:p>" +
                  "<w:p><w:commentRangeStart w:id=\"0\"/><w:bookmarkStart w:id=\"0\" w:name=\"Top\"/><w:r><w:t>a</w:t></w:r><w:bookmarkEnd w:id=\"0\"/>" +
                  "<w:commentRangeEnd w:id=\"0\"/><w:r><w:commentReference w:id=\"0\"/></w:r>" +
                  "<w:fldSimple w:instr=\"PAGE\"><w:r><w:t>1</w:t></w:r></w:fldSimple><w:r><w:footnoteReference w:id=\"1\"/></w:r></w:p>")
            .Load();

        Assert.Equal(
            [DocumentFeature.Links, DocumentFeature.HeadersFooters, DocumentFeature.Sections, DocumentFeature.Comments, DocumentFeature.Fields, DocumentFeature.OpaqueContent],
            DocumentExport.Losses(document, DocumentFileFormat.Html));
    }

    [Fact]
    public void Losses_StylesOnlyWhenTheirEffectIsLost()
    {
        const string styles =
            "<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style>" +
            "<w:style w:type=\"paragraph\" w:styleId=\"ListParagraph\"><w:name w:val=\"List Paragraph\"/><w:basedOn w:val=\"Normal\"/><w:pPr><w:ind w:left=\"720\"/></w:pPr></w:style>" +
            "<w:style w:type=\"character\" w:styleId=\"Strong\"><w:name w:val=\"Strong\"/><w:rPr><w:b/></w:rPr></w:style>" +
            "<w:style w:type=\"paragraph\" w:styleId=\"Quote\"><w:name w:val=\"Quote\"/><w:basedOn w:val=\"Normal\"/><w:pPr><w:ind w:left=\"720\"/></w:pPr><w:rPr><w:i/></w:rPr></w:style>" +
            "<w:style w:type=\"paragraph\" w:styleId=\"Spaced\"><w:name w:val=\"Spaced\"/><w:basedOn w:val=\"Normal\"/><w:pPr><w:spacing w:after=\"400\"/></w:pPr></w:style>";
        TextDocument Doc(string body) => new DocxBuilder().Styles(styles).Body(body).Load();
        var listAndStrong = Doc("<w:p><w:pPr><w:pStyle w:val=\"ListParagraph\"/></w:pPr><w:r><w:rPr><w:rStyle w:val=\"Strong\"/></w:rPr><w:t>a</w:t></w:r></w:p>");
        var quote = Doc("<w:p><w:pPr><w:pStyle w:val=\"Quote\"/></w:pPr><w:r><w:t>q</w:t></w:r></w:p>");
        var spaced = Doc("<w:p><w:pPr><w:pStyle w:val=\"Spaced\"/></w:pPr><w:r><w:t>s</w:t></w:r></w:p>");

        Assert.Empty(DocumentExport.Losses(listAndStrong, DocumentFileFormat.Markdown));
        Assert.Empty(DocumentExport.Losses(listAndStrong, DocumentFileFormat.Html));
        Assert.Equal([DocumentFeature.Styles], DocumentExport.Losses(listAndStrong, DocumentFileFormat.Text));
        Assert.Equal([DocumentFeature.Styles], DocumentExport.Losses(quote, DocumentFileFormat.Markdown));
        Assert.Empty(DocumentExport.Losses(quote, DocumentFileFormat.Html));
        Assert.Equal([DocumentFeature.Styles], DocumentExport.Losses(spaced, DocumentFileFormat.Html));
        Assert.Empty(DocumentExport.Losses(spaced, DocumentFileFormat.Rtf));
    }

    [Theory]
    [InlineData("<w:start w:val=\"3\"/><w:numFmt w:val=\"decimal\"/><w:lvlText w:val=\"%1.\"/>", "", true)]
    [InlineData("<w:start w:val=\"1\"/><w:numFmt w:val=\"lowerRoman\"/><w:lvlText w:val=\"%1.\"/>", "", true)]
    [InlineData("<w:start w:val=\"1\"/><w:numFmt w:val=\"decimal\"/><w:lvlText w:val=\"%1)\"/>", "", true)]
    [InlineData("<w:start w:val=\"1\"/><w:numFmt w:val=\"decimal\"/><w:lvlText w:val=\"%1.\"/>", "<w:pStyle w:val=\"Heading1\"/>", true)]
    [InlineData("<w:start w:val=\"1\"/><w:numFmt w:val=\"decimal\"/><w:lvlText w:val=\"%1.\"/>", "", false)]
    public void Losses_Md_ListNumbering(string level, string paragraphProperties, bool lost)
    {
        var document = new DocxBuilder()
            .Styles("<w:style w:type=\"paragraph\" w:styleId=\"Heading1\"><w:name w:val=\"heading 1\"/><w:pPr><w:outlineLvl w:val=\"0\"/></w:pPr></w:style>")
            .Numbering($"<w:abstractNum w:abstractNumId=\"0\"><w:lvl w:ilvl=\"0\">{level}</w:lvl></w:abstractNum><w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\"/></w:num>")
            .Body($"<w:p><w:pPr>{paragraphProperties}<w:numPr><w:ilvl w:val=\"0\"/><w:numId w:val=\"1\"/></w:numPr></w:pPr><w:r><w:t>a</w:t></w:r></w:p>")
            .Load();

        Assert.Equal(lost, DocumentExport.Losses(document, DocumentFileFormat.Markdown).Contains(DocumentFeature.Lists));
        Assert.DoesNotContain(DocumentFeature.Lists, DocumentExport.Losses(document, DocumentFileFormat.Html));
    }

    [Fact]
    public void Losses_Md_CellBlocksImageSizeNestedOrderedLists()
    {
        var png = Convert.ToBase64String(ObjectCommandTests.Png(20, 10));

        Assert.Empty(DocumentExport.Losses(HtmlReader.Read("<ol><li>a<ol><li>b</li></ol></li></ol><table><tr><td>x</td></tr></table>"), DocumentFileFormat.Markdown));
        Assert.Equal([DocumentFeature.Tables], DocumentExport.Losses(HtmlReader.Read("<table><tr><td><p>x</p><p>y</p></td></tr></table>"), DocumentFileFormat.Markdown));
        Assert.Equal([DocumentFeature.Tables], DocumentExport.Losses(HtmlReader.Read("<table><tr><td><ul><li>x</li></ul></td></tr></table>"), DocumentFileFormat.Markdown));
        Assert.Empty(DocumentExport.Losses(HtmlReader.Read($"<p><img src=\"data:image/png;base64,{png}\"></p>"), DocumentFileFormat.Markdown));
        Assert.Equal([DocumentFeature.Images], DocumentExport.Losses(HtmlReader.Read($"<p><img src=\"data:image/png;base64,{png}\" width=\"40\"></p>"), DocumentFileFormat.Markdown));
        Assert.Empty(DocumentExport.Losses(HtmlReader.Read($"<p><img src=\"data:image/png;base64,{png}\" width=\"40\"></p>"), DocumentFileFormat.Html));
    }

    [Fact]
    public void Save_Odt_ThrowsNotSupported()
    {
        var document = TextDocument.Create();

        Assert.Throws<NotSupportedException>(() => document.Save(new MemoryStream(), DocumentFileFormat.Odt));
        Assert.Throws<NotSupportedException>(() => document.Save(new MemoryStream(), DocumentFileFormat.Doc));
        Assert.Throws<InvalidDataException>(() => TextDocument.Load(new MemoryStream([1]), DocumentFileFormat.Odt));
        Assert.Throws<NotSupportedException>(() => TextDocument.Load(new MemoryStream([1]), DocumentFileFormat.Doc));
        Assert.Throws<NotSupportedException>(() => DocumentExport.Losses(document, DocumentFileFormat.Odt));
    }

    [Fact]
    public void DetectContent_DeeplyNestedContentTypes_FallsBackToTheName()
    {
        var deep = Zip(("[Content_Types].xml", "<Types>" + string.Concat(Enumerable.Repeat("<x>", 1000)) + string.Concat(Enumerable.Repeat("</x>", 1000)) + "</Types>"));

        Assert.Equal(DocumentFileFormat.Docx, Detect(deep, "deep.docx"));
    }

    private static TextDocument Formatted() => new DocxBuilder().Body(
            "<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:rPr><w:u w:val=\"single\"/></w:rPr><w:t>under</w:t></w:r></w:p>" +
            "<w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/></w:tblPr><w:tblGrid><w:gridCol w:w=\"2000\"/></w:tblGrid>" +
            "<w:tr><w:tc><w:p><w:r><w:t>cell</w:t></w:r></w:p></w:tc></w:tr></w:tbl><w:p/>")
        .Load();

    private static string TestData(string folder, string name) => Path.Combine(AppContext.BaseDirectory, "TestData", folder, name);

    private static byte[] Ole(params string[] names)
    {
        var directorySectors = (names.Length + 3) / 4;
        var file = new byte[512 * (2 + directorySectors)];
        byte[] signature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
        signature.CopyTo(file, 0);
        BitConverter.GetBytes((ushort)0x3E).CopyTo(file, 0x18);
        BitConverter.GetBytes((ushort)3).CopyTo(file, 0x1A);
        BitConverter.GetBytes((ushort)0xFFFE).CopyTo(file, 0x1C);
        BitConverter.GetBytes((ushort)9).CopyTo(file, 0x1E);
        BitConverter.GetBytes((ushort)6).CopyTo(file, 0x20);
        BitConverter.GetBytes(1u).CopyTo(file, 0x2C);
        BitConverter.GetBytes(1u).CopyTo(file, 0x30);
        BitConverter.GetBytes(4096u).CopyTo(file, 0x38);
        BitConverter.GetBytes(0xFFFFFFFEu).CopyTo(file, 0x3C);
        BitConverter.GetBytes(0xFFFFFFFEu).CopyTo(file, 0x44);
        for (var i = 0; i < 109; i++) BitConverter.GetBytes(i == 0 ? 0u : 0xFFFFFFFFu).CopyTo(file, 0x4C + 4 * i);
        for (var i = 0; i < 128; i++) BitConverter.GetBytes(i == 0 ? 0xFFFFFFFDu : i <= directorySectors ? (i == directorySectors ? 0xFFFFFFFEu : (uint)i + 1) : 0xFFFFFFFFu).CopyTo(file, 512 + 4 * i);
        var top = -1;
        int? previousTop = null, previousChild = null;
        for (var i = 0; i < names.Length; i++)
        {
            var at = 1024 + 128 * i;
            var child = names[i].StartsWith('/');
            var name = Encoding.Unicode.GetBytes(names[i].TrimStart('/'));
            name.CopyTo(file, at);
            BitConverter.GetBytes((ushort)(name.Length + 2)).CopyTo(file, at + 0x40);
            file[at + 0x42] = (byte)(i == 0 ? 5 : 2);
            for (var link = 0x44; link <= 0x4C; link += 4) BitConverter.GetBytes(0xFFFFFFFFu).CopyTo(file, at + link);
            if (i == 0) continue;
            if (child)
            {
                file[1024 + 128 * top + 0x42] = 1;
                Link(previousChild ?? top, previousChild == null ? 0x4C : 0x48, i);
                previousChild = i;
            }
            else
            {
                Link(previousTop ?? 0, previousTop == null ? 0x4C : 0x48, i);
                (previousTop, previousChild, top) = (i, null, i);
            }
        }

        return file;

        void Link(int entry, int field, int target) => BitConverter.GetBytes((uint)target).CopyTo(file, 1024 + 128 * entry + field);
    }

    private sealed class CountingStream(byte[] data) : MemoryStream(data)
    {
        public long BytesRead { get; private set; }

        public override int Read(byte[] buffer, int offset, int count) => Count(base.Read(buffer, offset, count));

        public override int ReadByte()
        {
            var value = base.ReadByte();
            if (value >= 0) BytesRead++;
            return value;
        }

        private int Count(int read)
        {
            BytesRead += read;
            return read;
        }
    }

    private sealed class FailingStream(byte[] data, long failFrom) : MemoryStream(data)
    {
        public override int Read(byte[] buffer, int offset, int count) =>
            Position >= failFrom && Position < Length ? throw new IOException("disk") : base.Read(buffer, offset, count);
    }

    private static DocumentFileFormat? Detect(byte[] bytes, string fileName = null, string contentType = null) =>
        DocumentFileFormats.DetectContent(new MemoryStream(bytes), fileName, contentType);

    private static byte[] Bytes(TextDocument document, DocumentFileFormat format)
    {
        using var stream = new MemoryStream();
        document.Save(stream, format);
        return stream.ToArray();
    }

    private static byte[] Zip(params (string Name, string Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            foreach (var (name, content) in entries)
                using (var entry = zip.CreateEntry(name).Open())
                    entry.Write(Encoding.ASCII.GetBytes(content));
        return stream.ToArray();
    }
}
