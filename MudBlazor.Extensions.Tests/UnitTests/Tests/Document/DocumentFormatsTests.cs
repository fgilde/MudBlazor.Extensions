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
            var supported = format is not (DocumentFileFormat.Odt or DocumentFileFormat.Doc);
            Assert.Equal(supported, DocumentFileFormats.CanRead(format));
            Assert.Equal(supported && format != DocumentFileFormat.Rtf, DocumentFileFormats.CanWrite(format));
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
        Assert.Equal(DocumentFileFormat.Doc, Detect(ole));
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
    public void Save_Odt_ThrowsNotSupported()
    {
        var document = TextDocument.Create();

        Assert.Throws<NotSupportedException>(() => document.Save(new MemoryStream(), DocumentFileFormat.Odt));
        Assert.Throws<NotSupportedException>(() => document.Save(new MemoryStream(), DocumentFileFormat.Doc));
        Assert.Throws<NotSupportedException>(() => TextDocument.Load(new MemoryStream([1]), DocumentFileFormat.Odt));
        Assert.Throws<NotSupportedException>(() => DocumentExport.Losses(document, DocumentFileFormat.Odt));
    }

    private static TextDocument Formatted() => new DocxBuilder().Body(
            "<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:rPr><w:u w:val=\"single\"/></w:rPr><w:t>under</w:t></w:r></w:p>" +
            "<w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/></w:tblPr><w:tblGrid><w:gridCol w:w=\"2000\"/></w:tblGrid>" +
            "<w:tr><w:tc><w:p><w:r><w:t>cell</w:t></w:r></w:p></w:tc></w:tr></w:tbl><w:p/>")
        .Load();

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
