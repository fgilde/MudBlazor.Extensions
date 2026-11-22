using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MudBlazor.Extensions.Helper.Internal;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class OdtReaderTests
{
    [Fact]
    public void Odt_Read_SampleFile()
    {
        var bytes = File.ReadAllBytes(SamplePath);

        var document = TextDocument.Load(new MemoryStream([7, 7, .. bytes]) { Position = 2 }, DocumentFileFormat.Odt);

        var heading = Assert.IsType<Paragraph>(document.Blocks[0]);
        Assert.Equal("MudEx ODT", heading.Text);
        Assert.Equal("Heading1", heading.Format.StyleId);
        Assert.Equal("OpenDocument text rendered directly in the browser.", Assert.IsType<Paragraph>(document.Blocks[1]).Text);
        var table = Assert.IsType<Table>(document.Blocks[2]);
        Assert.All(table.Rows, r => Assert.Equal(2, r.Cells.Count));
        Assert.Equal(["Feature", "Status", "ODT", "Supported"], table.Rows.SelectMany(r => r.Cells).Select(c => Assert.IsType<Paragraph>(Assert.Single(c.Blocks)).Text));
        Assert.Equal(2, table.Grid.Count);
        Assert.IsType<Paragraph>(document.Blocks[^1]);
        Assert.Equal("MudEx OpenDocument sample", document.Properties.Title);
        Assert.False(document.Modified);
    }

    [Fact]
    public void Odt_Read_SpanFormatsAndParagraphAlign()
    {
        var document = Spans().Load();

        var styled = (Paragraph)document.Blocks[0];
        Assert.Equal(ParagraphAlignment.Center, styled.Format.Alignment);
        Assert.Null(styled.Format.StyleId);
        Assert.Equal("abcd", styled.Text);
        Assert.Equal(
        [
            new RunFormat { Bold = true, SizePt = 10 },
            new RunFormat { Bold = true, Italic = true, Underline = "single", Strike = true, SizePt = 10 },
            new RunFormat { Bold = true, Italic = true, Underline = "single", Strike = true, SizePt = 15, Color = "#336699", Font = "Georgia", Shading = "#FFFF00", VerticalAlign = VerticalAlign.Superscript },
            new RunFormat { SizePt = 10, Underline = "double", VerticalAlign = VerticalAlign.Subscript }
        ], styled.Inlines.Select(i => i.Format));
        var indented = (Paragraph)document.Blocks[1];
        Assert.Equal(new ParagraphFormat
        {
            Alignment = ParagraphAlignment.Right, IndentLeft = 1134, IndentRight = 567, IndentFirstLine = -720, SpacingBefore = 120, SpacingAfter = 144, PageBreakBefore = true
        }, indented.Format);
        Assert.Equal(new ParagraphFormat { PageBreakBefore = true }, ((Paragraph)document.Blocks[2]).Format);
        Assert.Equal("Heading2", ((Paragraph)document.Blocks[3]).Format.StyleId);
        Assert.Equal("Title", ((Paragraph)document.Blocks[4]).Format.StyleId);
        Assert.Equal("Heading3", ((Paragraph)document.Blocks[5]).Format.StyleId);
        Assert.Equal("Heading6", ((Paragraph)document.Blocks[6]).Format.StyleId);
        Assert.NotNull(document.Styles.Find("Heading2"));
    }

    [Fact]
    public void Odt_Read_BreakAfterSurvivesAFrameWithoutInlines()
    {
        const string after = "<style:style style:name=\"BA\" style:family=\"paragraph\"><style:paragraph-properties fo:break-after=\"page\"/></style:style>";

        var boxed = new OdtBuilder().AutomaticStyles(after)
            .Body("<text:p text:style-name=\"BA\">a</text:p><draw:frame text:anchor-type=\"page\"><draw:text-box><text:p>box</text:p></draw:text-box></draw:frame><text:p>next</text:p>")
            .Load();
        var empty = new OdtBuilder().AutomaticStyles(after).Body("<text:p text:style-name=\"BA\">a</text:p><draw:frame text:anchor-type=\"page\"/><text:p>next</text:p>").Load();

        Assert.Equal([("a", null), ("box", true), ("next", null)], boxed.Blocks.Cast<Paragraph>().Select(p => (p.Text, p.Format.PageBreakBefore)));
        Assert.Equal([("a", null), ("next", true)], empty.Blocks.Cast<Paragraph>().Select(p => (p.Text, p.Format.PageBreakBefore)));
    }

    [Fact]
    public void Odt_Read_RightToLeftStartAndEnd_SupplementaryBullet()
    {
        var document = new OdtBuilder()
            .AutomaticStyles("<style:style style:name=\"S\" style:family=\"paragraph\"><style:paragraph-properties fo:text-align=\"start\" style:writing-mode=\"rl-tb\"/></style:style>" +
                             "<style:style style:name=\"E\" style:family=\"paragraph\" style:parent-style-name=\"Rtl\"><style:paragraph-properties fo:text-align=\"end\"/></style:style>" +
                             "<style:style style:name=\"L\" style:family=\"paragraph\"><style:paragraph-properties fo:text-align=\"end\" style:writing-mode=\"lr-tb\"/></style:style>" +
                             "<text:list-style style:name=\"B\"><text:list-level-style-bullet text:level=\"1\" text:bullet-char=\"\uD83D\uDE00\"/></text:list-style>")
            .Styles("<style:style style:name=\"Rtl\" style:family=\"paragraph\"><style:paragraph-properties style:writing-mode=\"rl-tb\"/></style:style>")
            .Body("<text:p text:style-name=\"S\">s</text:p><text:p text:style-name=\"E\">e</text:p><text:p text:style-name=\"L\">l</text:p>" +
                  "<text:list text:style-name=\"B\"><text:list-item><text:p>smile</text:p></text:list-item></text:list>")
            .Load();

        var paragraphs = document.Blocks.Cast<Paragraph>().ToList();
        Assert.Equal([ParagraphAlignment.Right, ParagraphAlignment.Left, ParagraphAlignment.Right], paragraphs.Take(3).Select(p => p.Format.Alignment));
        Assert.Equal("\uD83D\uDE00", document.ListLabel(paragraphs[3]));
    }

    [Fact]
    public void Odt_Read_MasterPageStartsAPage_ButNotBeforeTheFirstBlock()
    {
        var document = new OdtBuilder()
            .AutomaticStyles("<style:style style:name=\"P1\" style:family=\"paragraph\" style:master-page-name=\"Standard\"><style:paragraph-properties fo:break-before=\"page\"/></style:style>" +
                             "<style:style style:name=\"P2\" style:family=\"paragraph\" style:master-page-name=\"Standard\"/>")
            .Body("<text:p text:style-name=\"P1\">first</text:p><text:p>x</text:p><text:p text:style-name=\"P2\">second</text:p>")
            .Load();

        Assert.Equal([null, null, true], document.Blocks.Cast<Paragraph>().Select(p => p.Format.PageBreakBefore));
    }

    [Fact]
    public void Odt_Read_TextSpacesBreaksNotesFieldsAndFinalState()
    {
        var document = Text().Load();

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal("a b   c\td\vef ins \uFFFCg7 2026-11-22", paragraph.Text);
        Assert.Equal(new OpaqueInline("footnote", "1"), paragraph.Inlines.OfType<OpaqueInline>().Single() with { Format = new RunFormat() });
        Assert.Equal(["in section", "frame text"], document.Blocks.Skip(1).Cast<Paragraph>().Select(p => p.Text));
        Assert.False(document.HasTrackedChanges);
        Assert.Empty(document.Comments);
    }

    [Fact]
    public void Odt_Read_NestedListLevelsAndFormats()
    {
        var document = Lists().Load();

        var paragraphs = document.Blocks.OfType<Paragraph>().Where(p => p.Length > 0).ToList();
        Assert.Equal(["one", "more of one", "sub", "deep", "two", "between", "three", "restart", "dot", "bare"], paragraphs.Select(p => p.Text));
        Assert.Equal(["1.", null, "a.", "1.a.IV)", "2.", null, "3.", "1.", "\u2022", "\u2022"], paragraphs.Select(document.ListLabel));
        Assert.Equal([0, null, 1, 2, 0, null, 0, 0, 0, 0], paragraphs.Select(p => p.Format.Numbering?.Level));
        Assert.Equal(720, paragraphs[1].Format.IndentLeft);
        Assert.Null(paragraphs[0].Format.IndentLeft);
        var levels = document.Numbering.Level(paragraphs[0].Format.Numbering!.Value);
        Assert.Equal((720, -360), (levels!.Indent.IndentLeft, levels.Indent.IndentFirstLine));
        Assert.Equal(paragraphs[0].Format.Numbering!.Value.NumId, paragraphs[6].Format.Numbering!.Value.NumId);
        Assert.NotEqual(paragraphs[0].Format.Numbering!.Value.NumId, paragraphs[7].Format.Numbering!.Value.NumId);
    }

    [Fact]
    public void Odt_Read_TableSpansHeaderAndGrid()
    {
        var document = Tables().Load();

        var table = (Table)document.Blocks[0];
        Assert.Equal([1134, 1134, 1701], table.Grid);
        Assert.Equal([true, false, false], table.Rows.Select(r => r.IsHeader));
        Assert.Equal(["2 None None #EEEEEE 2268 wide", "1 Restart None  1701 tall"], Cells(table.Rows[0]));
        Assert.Equal(["1 None None  1134 a", "1 None None  1134 ", "1 Continue None  1701 "], Cells(table.Rows[1]));
        Assert.Equal(["1 None None  1134 x|inner|", "1 None None  1134 r", "1 None None  1701 r"], Cells(table.Rows[2]));
        Assert.All(table.Rows.SelectMany(r => r.Cells), c => Assert.IsType<Paragraph>(c.Blocks[^1]));
    }

    [Fact]
    public void Odt_Read_ImageAsChar_2cmIs720000Emu()
    {
        var document = Images().Load();

        var first = (Paragraph)document.Blocks[0];
        var image = Assert.IsType<InlineImage>(first.Inlines[1]);
        Assert.Equal((720000L, 360000L, ImagePlacement.Inline, "Alt"), (image.WidthEmu, image.HeightEmu, image.Placement, image.AltText));
        Assert.Equal("a\uFFFCb", first.Text);
        Assert.Equal(ObjectCommandTests.Png(4, 2), document.GetMedia(image.MediaPart));
        Assert.Equal("image/png", document.MediaContentType(image.MediaPart));
        var anchored = Assert.IsType<InlineImage>(Assert.Single(((Paragraph)document.Blocks[1]).Inlines));
        Assert.Equal((914400L, 914400L, ImagePlacement.Left, image.MediaPart), (anchored.WidthEmu, anchored.HeightEmu, anchored.Placement, anchored.MediaPart));
        Assert.Equal("c", ((Paragraph)document.Blocks[2]).Text);
        Assert.Equal(ImagePlacement.Left, Assert.IsType<InlineImage>(Assert.Single(((Paragraph)document.Blocks[3]).Inlines)).Placement);
    }

    [Fact]
    public void Odt_Read_UnsafeLinkIsPlainText()
    {
        var document = Links().Load();

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal("bad good top", paragraph.Text);
        Assert.Equal(["bad ", "good", " ", "top"], paragraph.Inlines.Cast<TextRun>().Select(r => r.Text));
        Assert.Equal([null, "https://example.com/", null, "#Top"], paragraph.Inlines.Select(i => i.Link?.SafeTarget));
        Assert.Equal([null, "Hyperlink", null, "Hyperlink"], paragraph.Inlines.Select(i => i.Format.StyleId));
        Assert.NotNull(document.Styles.Find("Hyperlink"));
    }

    [Fact]
    public void Odt_Read_PageLayoutHeadersAndFooters()
    {
        var document = Layout().Load();

        var section = Assert.Single(document.Sections);
        Assert.Equal(new PageSetup(16838, 11906, PageOrientation.Landscape, 992, 1417, 850, 1134, 567, 850, 0), section.Page);
        Assert.True(section.TitlePage);
        Assert.Equal(ParagraphAlignment.Right, ((Paragraph)document.Blocks[0]).Format.Alignment);
        var header = (Paragraph)Assert.Single(document.HeaderFor(section, HeaderFooterKind.Header, HeaderFooterType.Default)!.Blocks);
        Assert.Equal(("Head", ParagraphAlignment.Center), (header.Text, header.Format.Alignment));
        Assert.Equal("Foot 1", ((Paragraph)document.HeaderFor(section, HeaderFooterKind.Footer, HeaderFooterType.Default)!.Blocks[0]).Text);
        Assert.Equal("First head", ((Paragraph)document.HeaderFor(section, HeaderFooterKind.Header, HeaderFooterType.First)!.Blocks[0]).Text);
        Assert.Null(document.HeaderFor(section, HeaderFooterKind.Header, HeaderFooterType.Even));
    }

    [Fact]
    public void Odt_Read_MissingOptionalPartsLoad()
    {
        var document = new OdtBuilder { MimeType = null }.Body("<text:p text:style-name=\"Nowhere\">x<draw:frame><draw:image xlink:href=\"Pictures/none.png\"/></draw:frame></text:p>").Load();

        Assert.Equal("x", ((Paragraph)document.Blocks[0]).Text);
        Assert.Equal(PageSetup.A4, document.Sections[0].Page);
        Assert.Null(document.Properties.Title);
    }

    [Fact]
    public void Odt_Load_MalformedInput_ThrowsInvalidData()
    {
        const string manifest = "<manifest:manifest xmlns:manifest=\"urn:oasis:names:tc:opendocument:xmlns:manifest:1.0\"><manifest:file-entry manifest:full-path=\"content.xml\">" +
                                "<manifest:encryption-data manifest:checksum-type=\"SHA1/1K\"/></manifest:file-entry></manifest:manifest>";
        var bomb = new MemoryStream();
        using (var zip = new ZipArchive(bomb, ZipArchiveMode.Create, true))
            for (var i = 0; i <= PackageLimits.Default.MaxEntries; i++)
                zip.CreateEntry("e" + i);

        Assert.Throws<InvalidDataException>(() => Load([1, 2, 3]));
        Assert.Throws<InvalidDataException>(() => Load(new OdtBuilder { Content = "<office:document-content" }.Build()));
        Assert.Throws<InvalidDataException>(() => Load(new OdtBuilder
        {
            Content = $"<?xml version=\"1.0\"?><!DOCTYPE x [<!ENTITY e \"E\">]><office:document-content {OdtBuilder.Ns}><office:body><office:text><text:p>&e;</text:p></office:text></office:body></office:document-content>"
        }.Build()));
        Assert.Throws<InvalidDataException>(() => Load(new OdtBuilder { Content = $"<office:document-content {OdtBuilder.Ns}><office:body><office:spreadsheet/></office:body></office:document-content>" }.Build()));
        Assert.Throws<InvalidDataException>(() => Load(Zip(("mimetype", "application/vnd.oasis.opendocument.text"))));
        Assert.Contains("encrypted", Assert.Throws<InvalidDataException>(() => Load(new OdtBuilder().File("META-INF/manifest.xml", manifest).Build())).Message);
        Assert.Throws<InvalidDataException>(() => Load(bomb.ToArray()));
        Assert.Throws<InvalidDataException>(() => Load(new OdtBuilder().Body("<text:p>" + string.Concat(Enumerable.Repeat("<text:s text:c=\"1000\"/>", 40000)) + "</text:p>").Build()));
        Assert.Throws<InvalidDataException>(() => Load(new OdtBuilder().Body("<table:table>" + string.Concat(Enumerable.Repeat(
            "<table:table-row table:number-rows-repeated=\"1000\"><table:table-cell table:number-columns-repeated=\"63\"><text:p>x</text:p></table:table-cell></table:table-row>", 20)) +
                                                                              "</table:table>").Build()));
        Assert.Equal(1000, ((Table)new OdtBuilder().Body("<table:table><table:table-row table:number-rows-repeated=\"1000\"><table:table-cell table:number-columns-repeated=\"3\">" +
                                                         "<text:p>x</text:p></table:table-cell></table:table-row></table:table>").Load().Blocks[0]).Rows.Count);
        Assert.Throws<InvalidDataException>(() => Load(new OdtBuilder().Body("<text:p>" + string.Concat(Enumerable.Repeat("<text:span>", 20000)) + "x" +
                                                                              string.Concat(Enumerable.Repeat("</text:span>", 20000)) + "</text:p>").Build()));
        Assert.Throws<InvalidDataException>(() => Load(new OdtBuilder().Body(string.Concat(Enumerable.Repeat("<text:section>", 20000)) +
                                                                              string.Concat(Enumerable.Repeat("</text:section>", 20000))).Build()));
    }

    [Fact]
    public void Odt_ThenSaveDocx_IsConsistent()
    {
        foreach (var builder in new[] { Spans(), Text(), Lists(), Tables(), Images(), Links(), Layout() })
        {
            var document = builder.Load();
            using var stream = new MemoryStream();

            document.Save(stream, DocumentFileFormat.Docx);
            var reloaded = TextDocument.Load(new MemoryStream(stream.ToArray()));

            DocxWriterTests.AssertConsistent(stream.ToArray());
            Assert.Equal(Normalized(document), Normalized(reloaded));
            Assert.Throws<NotSupportedException>(() => document.Save(new MemoryStream(), DocumentFileFormat.Odt));
        }
    }

    [Fact]
    public void OpenDocumentFile_Open_RendersAsBefore()
    {
        var dtd = new OdtBuilder
        {
            Content = $"<?xml version=\"1.0\"?><!DOCTYPE x [<!ENTITY e \"E\">]><office:document-content {OdtBuilder.Ns}><office:body><office:text><text:p>&e;</text:p></office:text></office:body></office:document-content>"
        }.Build();

        using var sample = OpenDocumentFile.Open(File.ReadAllBytes(SamplePath));

        var page = Assert.Single(sample.Pages);
        Assert.Equal(("MudEx OpenDocument sample", "MudBlazor.Extensions", "application/vnd.oasis.opendocument.text"), (sample.Title, sample.Creator, sample.MediaType));
        Assert.Equal("MudEx OpenDocument sample", page.Name);
        Assert.Equal(
            "<h2>MudEx ODT</h2><p>OpenDocument text rendered directly in the browser.</p>" +
            "<table class=\"odf-table\"><tbody><tr><td>Feature</td><td>Status</td></tr><tr><td>ODT</td><td>Supported</td></tr></tbody></table>", page.Html);
        Assert.Throws<XmlException>(() => OpenDocumentFile.Open(dtd));
    }

    private static string SamplePath => Path.Combine(AppContext.BaseDirectory, "TestData", "Document", "sample.odt");

    private static TextDocument Load(byte[] bytes) => TextDocument.Load(new MemoryStream(bytes), DocumentFileFormat.Odt);

    private static string Normalized(TextDocument document) =>
        Regex.Replace(Regex.Replace(DocumentDescriber.Describe(document), @"img \S+ (\d+x\d+) \w+", "img $1"), @"opaque footnote '(\d+)' ", "r '$1'");

    private static IEnumerable<string> Cells(TableRow row) =>
        row.Cells.Select(c => $"{c.GridSpan} {c.VMerge} {c.VerticalAlign?.ToString() ?? "None"} {c.Shading} {c.Width} " +
                              string.Join("|", c.Blocks.Select(b => b is Paragraph p ? p.Text : string.Join("|", ((Table)b).Rows.SelectMany(r => r.Cells).Select(x => ((Paragraph)x.Blocks[0]).Text)))));

    private static byte[] Zip(params (string Name, string Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            foreach (var (name, content) in entries)
                using (var entry = zip.CreateEntry(name).Open())
                    entry.Write(Encoding.ASCII.GetBytes(content));
        return stream.ToArray();
    }

    private static OdtBuilder Spans() => new OdtBuilder()
        .Fonts("<style:font-face style:name=\"Geo\" svg:font-family=\"'Georgia'\"/>")
        .Styles("<style:default-style style:family=\"paragraph\"><style:text-properties fo:font-size=\"10pt\"/></style:default-style>" +
                "<style:style style:name=\"Standard\" style:family=\"paragraph\"/>" +
                "<style:style style:name=\"Centered\" style:display-name=\"Centered Text\" style:family=\"paragraph\" style:parent-style-name=\"Standard\">" +
                "<style:paragraph-properties fo:text-align=\"center\"/><style:text-properties fo:font-weight=\"bold\"/></style:style>" +
                "<style:style style:name=\"Heading_20_2\" style:display-name=\"Heading 2\" style:family=\"paragraph\" style:parent-style-name=\"Standard\"/>" +
                "<style:style style:name=\"Title\" style:family=\"paragraph\" style:parent-style-name=\"Standard\"/>" +
                "<style:style style:name=\"Emph\" style:family=\"text\"><style:text-properties fo:font-style=\"italic\"/></style:style>")
        .AutomaticStyles(
            "<style:style style:name=\"P1\" style:family=\"paragraph\" style:parent-style-name=\"Centered\"/>" +
            "<style:style style:name=\"P2\" style:family=\"paragraph\" style:parent-style-name=\"Standard\"><style:paragraph-properties fo:text-align=\"end\" fo:margin-left=\"2cm\" " +
            "fo:margin-right=\"10mm\" fo:text-indent=\"-0.5in\" fo:margin-top=\"6pt\" fo:margin-bottom=\"0.1in\" fo:break-before=\"page\" fo:break-after=\"page\"/></style:style>" +
            "<style:style style:name=\"T1\" style:family=\"text\" style:parent-style-name=\"Emph\"><style:text-properties style:text-underline-style=\"solid\" style:text-line-through-style=\"solid\"/></style:style>" +
            "<style:style style:name=\"T2\" style:family=\"text\"><style:text-properties fo:font-size=\"150%\" fo:color=\"#336699\" style:font-name=\"Geo\" fo:background-color=\"#ffff00\" style:text-position=\"super 58%\"/></style:style>" +
            "<style:style style:name=\"T3\" style:family=\"text\"><style:text-properties fo:font-weight=\"normal\" style:text-position=\"-33% 58%\" style:text-underline-style=\"wave\" style:text-underline-type=\"double\"/></style:style>")
        .Body("<text:p text:style-name=\"P1\">a<text:span text:style-name=\"T1\">b<text:span text:style-name=\"T2\">c</text:span></text:span><text:span text:style-name=\"T3\">d</text:span></text:p>" +
              "<text:p text:style-name=\"P2\">e</text:p><text:p>f</text:p><text:p text:style-name=\"Heading_20_2\">g</text:p><text:p text:style-name=\"Title\">t</text:p>" +
              "<text:h text:outline-level=\"3\">h3</text:h><text:h text:outline-level=\"9\">h9</text:h>");

    private static OdtBuilder Text() => new OdtBuilder().Body(
        "<text:tracked-changes><text:changed-region text:id=\"c1\"><text:deletion><office:change-info><dc:creator>A</dc:creator></office:change-info><text:p>deleted</text:p>" +
        "</text:deletion></text:changed-region></text:tracked-changes>" +
        "<text:p>  a  b<text:s text:c=\"3\"/>c<text:tab/>d<text:line-break/>e<text:soft-page-break/>f <text:change text:change-id=\"c1\"/>" +
        "<text:change-start text:change-id=\"c2\"/>ins<text:change-end text:change-id=\"c2\"/> <text:note text:id=\"n1\" text:note-class=\"footnote\">" +
        "<text:note-citation>1</text:note-citation><text:note-body><text:p>note text</text:p></text:note-body></text:note>" +
        "<office:annotation><dc:creator>A</dc:creator><text:p>comment</text:p></office:annotation>g<text:page-number text:select-page=\"current\">7</text:page-number> " +
        "<text:date>2026-11-22</text:date></text:p><text:soft-page-break/>" +
        "<text:section text:name=\"S\"><text:p>in section</text:p></text:section>" +
        "<draw:frame text:anchor-type=\"page\"><draw:text-box><text:p>frame text</text:p></draw:text-box></draw:frame>");

    private static OdtBuilder Lists() => new OdtBuilder()
        .AutomaticStyles(
            "<text:list-style style:name=\"L1\"><text:list-level-style-number text:level=\"1\" style:num-suffix=\".\" style:num-format=\"1\">" +
            "<style:list-level-properties text:list-level-position-and-space-mode=\"label-alignment\"><style:list-level-label-alignment text:label-followed-by=\"listtab\" " +
            "fo:text-indent=\"-0.635cm\" fo:margin-left=\"1.27cm\"/></style:list-level-properties></text:list-level-style-number>" +
            "<text:list-level-style-number text:level=\"2\" style:num-suffix=\".\" style:num-format=\"a\"/>" +
            "<text:list-level-style-number text:level=\"3\" style:num-suffix=\")\" style:num-format=\"I\" text:display-levels=\"3\" text:start-value=\"4\"/></text:list-style>" +
            "<text:list-style style:name=\"L2\"><text:list-level-style-bullet text:level=\"1\" text:bullet-char=\"\u2022\"/></text:list-style>")
        .Body("<text:list text:style-name=\"L1\"><text:list-item><text:p>one</text:p><text:p>more of one</text:p><text:list><text:list-item><text:p>sub</text:p>" +
              "<text:list><text:list-item><text:p>deep</text:p></text:list-item></text:list></text:list-item></text:list></text:list-item>" +
              "<text:list-item><text:p>two</text:p></text:list-item></text:list><text:p>between</text:p>" +
              "<text:list text:style-name=\"L1\" text:continue-numbering=\"true\"><text:list-item><text:p>three</text:p></text:list-item></text:list>" +
              "<text:list text:style-name=\"L1\"><text:list-item><text:p>restart</text:p></text:list-item></text:list>" +
              "<text:list text:style-name=\"L2\"><text:list-item><text:p>dot</text:p></text:list-item></text:list>" +
              "<text:list><text:list-item><text:p>bare</text:p></text:list-item></text:list>");

    private static OdtBuilder Tables() => new OdtBuilder()
        .AutomaticStyles(
            "<style:style style:name=\"Tbl.A\" style:family=\"table-column\"><style:table-column-properties style:column-width=\"2cm\"/></style:style>" +
            "<style:style style:name=\"Tbl.B\" style:family=\"table-column\"><style:table-column-properties style:column-width=\"3cm\"/></style:style>" +
            "<style:style style:name=\"Tbl.C1\" style:family=\"table-cell\"><style:table-cell-properties fo:background-color=\"#eeeeee\"/></style:style>")
        .Body("<table:table table:name=\"Tbl\"><table:table-column table:style-name=\"Tbl.A\" table:number-columns-repeated=\"2\"/><table:table-column table:style-name=\"Tbl.B\"/>" +
              "<table:table-header-rows><table:table-row><table:table-cell table:number-columns-spanned=\"2\" table:style-name=\"Tbl.C1\"><text:p>wide</text:p></table:table-cell>" +
              "<table:covered-table-cell/><table:table-cell table:number-rows-spanned=\"2\"><text:p>tall</text:p></table:table-cell></table:table-row></table:table-header-rows>" +
              "<table:table-row><table:table-cell><text:p>a</text:p></table:table-cell><table:table-cell/><table:covered-table-cell/></table:table-row>" +
              "<table:table-row><table:table-cell><text:p>x</text:p><table:table><table:table-row><table:table-cell><text:p>inner</text:p></table:table-cell></table:table-row>" +
              "</table:table></table:table-cell><table:table-cell table:number-columns-repeated=\"2\"><text:p>r</text:p></table:table-cell></table:table-row></table:table>");

    private static OdtBuilder Images() => new OdtBuilder()
        .File("Pictures/a.png", ObjectCommandTests.Png(4, 2))
        .File("Pictures/fake.png", "<svg xmlns=\"http://www.w3.org/2000/svg\"/>")
        .Body("<text:p>a<draw:frame draw:name=\"i1\" text:anchor-type=\"as-char\" svg:width=\"2cm\" svg:height=\"1cm\"><draw:image xlink:href=\"Pictures/a.png\"/>" +
              "<svg:title>Alt</svg:title></draw:frame>b</text:p>" +
              "<text:p><draw:frame text:anchor-type=\"paragraph\" svg:width=\"1in\" svg:height=\"72pt\"><draw:image xlink:href=\"./Pictures/a.png\"/></draw:frame></text:p>" +
              "<text:p><draw:frame text:anchor-type=\"as-char\" svg:width=\"1cm\" svg:height=\"1cm\"><draw:image xlink:href=\"Pictures/fake.png\"/></draw:frame>" +
              "<draw:frame svg:width=\"1cm\" svg:height=\"1cm\"><draw:image xlink:href=\"Pictures/missing.png\"/></draw:frame>" +
              "<draw:frame><draw:image xlink:href=\"https://example.com/x.png\"/></draw:frame>c</text:p>" +
              "<draw:frame text:anchor-type=\"page\" svg:width=\"1cm\" svg:height=\"1cm\"><draw:image xlink:href=\"Pictures/a.png\"/></draw:frame>");

    private static OdtBuilder Links() => new OdtBuilder().Body(
        "<text:p><text:a xlink:type=\"simple\" xlink:href=\"javascript:alert(1)\">bad</text:a> <text:a xlink:href=\"https://example.com/\">good</text:a> " +
        "<text:a xlink:href=\"#Top\">top</text:a></text:p>");

    private static OdtBuilder Layout() => new OdtBuilder()
        .AutomaticStyles("<style:style style:name=\"MP1\" style:family=\"paragraph\"><style:paragraph-properties fo:text-align=\"end\"/></style:style>")
        .MasterPage(
            "<style:page-layout style:name=\"pm1\"><style:page-layout-properties fo:page-width=\"29.7cm\" fo:page-height=\"21cm\" style:print-orientation=\"landscape\" " +
            "fo:margin-top=\"1cm\" fo:margin-bottom=\"1.5cm\" fo:margin-left=\"2cm\" fo:margin-right=\"2.5cm\"/><style:header-style><style:header-footer-properties " +
            "fo:min-height=\"0.5cm\" fo:margin-bottom=\"0.25cm\"/></style:header-style><style:footer-style/></style:page-layout>" +
            "<style:style style:name=\"MP1\" style:family=\"paragraph\"><style:paragraph-properties fo:text-align=\"center\"/></style:style>",
            "<style:master-page style:name=\"Standard\" style:page-layout-name=\"pm1\"><style:header><text:p text:style-name=\"MP1\">Head</text:p></style:header>" +
            "<style:footer><text:p>Foot <text:page-number>1</text:page-number></text:p></style:footer><style:header-first><text:p>First head</text:p></style:header-first>" +
            "<style:header-left style:display=\"false\"><text:p>Left</text:p></style:header-left></style:master-page>" +
            "<style:master-page style:name=\"Other\" style:page-layout-name=\"pm2\"/>")
        .Body("<text:p text:style-name=\"MP1\">body</text:p>");
}
