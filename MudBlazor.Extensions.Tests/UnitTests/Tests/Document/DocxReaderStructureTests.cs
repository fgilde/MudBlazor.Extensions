using System.Text;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class DocxReaderStructureTests
{
    private static TextDocument Load(string body) => new DocxBuilder().Body(body).Load();

    private static string P(string text) => $"<w:p><w:r><w:t>{text}</w:t></w:r></w:p>";

    private static string Cell(string content, string tcPr = "") => $"<w:tc>{(tcPr == "" ? "" : $"<w:tcPr>{tcPr}</w:tcPr>")}{content}</w:tc>";

    private static IEnumerable<Block> AllBlocks(IEnumerable<Block> blocks) =>
        blocks.SelectMany(b => b is Table t ? AllBlocks(t.Rows.SelectMany(r => r.Cells).SelectMany(c => c.Blocks)).Prepend(b) : [b]);

    [Fact]
    public void Reader_Table_GridSpanVMergeWidthShadingBorders()
    {
        var document = Load(
            "<w:tbl><w:tblPr><w:tblStyle w:val=\"TableGrid\"/><w:tblW w:w=\"5000\" w:type=\"dxa\"/></w:tblPr>" +
            "<w:tblGrid><w:gridCol w:w=\"2000\"/><w:gridCol w:w=\"3000\"/></w:tblGrid>" +
            "<w:tr>" + Cell(P("wide"), "<w:tcW w:w=\"5000\" w:type=\"dxa\"/><w:gridSpan w:val=\"2\"/><w:vMerge w:val=\"restart\"/>" +
                                       "<w:tcBorders><w:top w:val=\"single\" w:sz=\"8\" w:color=\"FF0000\"/></w:tcBorders>" +
                                       "<w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"FFFF00\"/><w:vAlign w:val=\"center\"/>") + "</w:tr>" +
            "<w:tr>" + Cell("<w:p/>", "<w:gridSpan w:val=\"2\"/><w:vMerge/>") + "</w:tr>" +
            "<w:tr>" + Cell(P("a")) + Cell(P("b"), "<w:tcW w:w=\"0\" w:type=\"auto\"/>") + "</w:tr>" +
            "</w:tbl><w:p/>");

        var table = Assert.IsType<Table>(document.Blocks[0]);
        Assert.Equal([2000, 3000], table.Grid);
        Assert.Equal("TableGrid", table.Format.StyleId);
        Assert.Equal(5000, table.Format.Width);
        Assert.Equal(3, table.Rows.Count);
        var wide = Assert.Single(table.Rows[0].Cells);
        Assert.Equal(2, wide.GridSpan);
        Assert.Equal(VerticalMerge.Restart, wide.VMerge);
        Assert.Equal(5000, wide.Width);
        Assert.Equal("#FFFF00", wide.Shading);
        Assert.Equal(new Border("single", 1, "#FF0000"), wide.Borders.Top);
        Assert.Null(wide.Borders.Bottom);
        Assert.Equal(CellVerticalAlignment.Center, wide.VerticalAlign);
        Assert.Equal("wide", Assert.IsType<Paragraph>(Assert.Single(wide.Blocks)).Text);
        Assert.Equal(VerticalMerge.Continue, table.Rows[1].Cells[0].VMerge);
        var plain = table.Rows[2].Cells[1];
        Assert.Equal((1, VerticalMerge.None, (int?)null, (string)null, (TableBorders)null, (CellVerticalAlignment?)null),
            (plain.GridSpan, plain.VMerge, plain.Width, plain.Shading, plain.Borders, plain.VerticalAlign));
        Assert.Equal("tbl", table.Source.Name.LocalName);
        Assert.All(table.Rows, r => Assert.Equal("tr", r.Source.Name.LocalName));
        Assert.All(table.Rows.SelectMany(r => r.Cells), c => Assert.Equal("tc", c.Source.Name.LocalName));
        Assert.Same(table.Rows[2].Source.Elements().Last(), plain.Source);
        Assert.Same(plain.Source.Element(FormatXml.W + "p"), plain.Blocks[0].Source);
        Assert.All(AllBlocks(document.Blocks), b => Assert.Same(b, document.FindBlock(b.Id)));
    }

    [Fact]
    public void Reader_NestedTable_IsBlockInCell()
    {
        var inner = "<w:tbl><w:tblGrid><w:gridCol w:w=\"1000\"/></w:tblGrid><w:tr>" + Cell(P("inner")) + "</w:tr></w:tbl>";
        var document = Load("<w:tbl><w:tblGrid><w:gridCol w:w=\"4000\"/></w:tblGrid><w:tr>" + Cell(P("top")) + "</w:tr><w:tr>" +
                            Cell(P("before") + inner + "<w:p/>") + "</w:tr></w:tbl><w:p/>");

        var outer = Assert.IsType<Table>(document.Blocks[0]);
        var nested = Assert.IsType<Table>(outer.Rows[1].Cells[0].Blocks[1]);
        Assert.NotNull(nested.Id);
        Assert.NotEqual(outer.Id, nested.Id);
        Assert.Same(nested, document.FindBlock(nested.Id));
        var ids = AllBlocks(document.Blocks).Select(b => b.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        var (paragraph, offset) = document.Resolve(new DocumentPosition(outer.Id, [new CellStep(1, 0, 1), new CellStep(0, 0, 0)], 2));
        Assert.Equal(("inner", 2), (paragraph.Text, offset));
        Assert.Same(nested.Rows[0].Cells[0].Blocks[0], paragraph);
    }

    [Fact]
    public void Reader_HeaderRow_IsHeader()
    {
        var table = Assert.IsType<Table>(Load("<w:tbl><w:tblGrid><w:gridCol w:w=\"1000\"/></w:tblGrid>" +
                                              "<w:tr><w:trPr><w:trHeight w:val=\"400\" w:hRule=\"exact\"/><w:tblHeader/></w:trPr>" + Cell(P("head")) + "</w:tr>" +
                                              "<w:tr><w:trPr><w:tblHeader w:val=\"0\"/></w:trPr>" + Cell(P("body")) + "</w:tr></w:tbl><w:p/>").Blocks[0]);

        Assert.Equal((true, (int?)400), (table.Rows[0].IsHeader, table.Rows[0].Height));
        Assert.Equal((false, (int?)null), (table.Rows[1].IsHeader, table.Rows[1].Height));
    }

    [Fact]
    public void Reader_DeletedTrackedRow_NotModelled()
    {
        var document = Load("<w:tbl><w:tblGrid><w:gridCol w:w=\"1000\"/></w:tblGrid><w:tr>" + Cell(P("a")) + "</w:tr>" +
                            "<w:tr><w:trPr><w:del w:id=\"1\" w:author=\"A\" w:date=\"2026-01-01T00:00:00Z\"/></w:trPr>" + Cell(P("gone")) + "</w:tr>" +
                            "<w:tr><w:trPr><w:ins w:id=\"2\" w:author=\"A\" w:date=\"2026-01-01T00:00:00Z\"/></w:trPr>" + Cell(P("c")) + "</w:tr></w:tbl><w:p/>");

        var table = Assert.IsType<Table>(document.Blocks[0]);
        Assert.Equal(["a", "c"], table.Rows.Select(r => ((Paragraph)r.Cells[0].Blocks[0]).Text));
        Assert.True(document.HasTrackedChanges);
    }

    [Fact]
    public void Reader_RowsAndCellsInContentControls_AreReadThrough()
    {
        var table = Assert.IsType<Table>(Load("<w:tbl><w:tblGrid><w:gridCol w:w=\"1000\"/><w:gridCol w:w=\"1000\"/></w:tblGrid>" +
                                              "<w:sdt><w:sdtPr/><w:sdtContent><w:tr>" + Cell(P("a")) + "<w:customXml w:element=\"x\">" + Cell(P("b")) +
                                              "</w:customXml></w:tr></w:sdtContent></w:sdt></w:tbl><w:p/>").Blocks[0]);

        var row = Assert.Single(table.Rows);
        Assert.Equal(["a", "b"], row.Cells.Select(c => ((Paragraph)c.Blocks[0]).Text));
        Assert.Equal("tr", row.Source.Name.LocalName);
    }

    [Fact]
    public void Reader_TwoSections_PageSetupsAndLastBlock()
    {
        var document = Load(
            P("one") +
            "<w:p><w:pPr><w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/><w:pgMar w:top=\"1417\" w:right=\"1417\" w:bottom=\"1417\" w:left=\"1417\" " +
            "w:header=\"708\" w:footer=\"708\" w:gutter=\"0\"/></w:sectPr></w:pPr><w:r><w:t>two</w:t></w:r></w:p>" +
            P("three") +
            "<w:sectPr><w:type w:val=\"continuous\"/><w:pgSz w:w=\"16838\" w:h=\"11906\" w:orient=\"landscape\"/></w:sectPr>");

        Assert.Equal(2, document.Sections.Count);
        var (first, second) = (document.Sections[0], document.Sections[1]);
        Assert.Equal(new PageSetup(11906, 16838, PageOrientation.Portrait, 1417, 1417, 1417, 1417, 708, 708, 0), first.Page);
        Assert.Equal(document.Blocks[1].Id, first.LastBlockId);
        Assert.Equal(SectionStart.NextPage, first.Start);
        Assert.Equal((16838, 11906, PageOrientation.Landscape), (second.Page.Width, second.Page.Height, second.Page.Orientation));
        Assert.Null(second.LastBlockId);
        Assert.Equal(SectionStart.Continuous, second.Start);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal("sectPr", first.Source.Name.LocalName);
        Assert.Equal(3, document.Blocks.Count);
    }

    [Fact]
    public void Reader_NoFinalSectPr_AddsDefaultFinalSection()
    {
        var document = new DocxBuilder { RawDocument = $"<w:document {DocxBuilder.Ns}><w:body>" + P("x") + "</w:body></w:document>" }.Load();

        var section = Assert.Single(document.Sections);
        Assert.Equal((PageSetup.A4, (string)null, 1), (section.Page, section.LastBlockId, section.Columns));
    }

    [Fact]
    public void Reader_HeaderReferences_DefaultFirstEven_TitlePage()
    {
        var document = new DocxBuilder { SectionProperties = "<w:titlePg/>" }
            .Header(P("Header text")).Header(P("First header"), "first").Header(P("Even header"), "even").Footer(P("Footer text"))
            .Settings("<w:evenAndOddHeaders/>").Body(P("body")).Load();

        var section = Assert.Single(document.Sections);
        Assert.True(section.TitlePage);
        Assert.Equal([HeaderFooterType.Default, HeaderFooterType.First, HeaderFooterType.Even], section.Headers.Keys.Order());
        Assert.Equal(HeaderFooterType.Default, Assert.Single(section.Footers).Key);
        Assert.Equal(4, document.HeadersFooters.Count);
        string TextOf(HeaderFooter part) => ((Paragraph)part.Blocks[0]).Text;
        Assert.Equal("Header text", TextOf(document.HeaderFor(section, HeaderFooterKind.Header, HeaderFooterType.Default)));
        Assert.Equal("First header", TextOf(document.HeaderFor(section, HeaderFooterKind.Header, HeaderFooterType.First)));
        Assert.Equal("Even header", TextOf(document.HeaderFor(section, HeaderFooterKind.Header, HeaderFooterType.Even)));
        var footer = document.HeaderFor(section, HeaderFooterKind.Footer, HeaderFooterType.Default);
        Assert.Equal((HeaderFooterKind.Footer, "Footer text", "/word/footer4.xml"), (footer.Kind, TextOf(footer), footer.PartName));
        Assert.Equal(HeaderFooterKind.Header, document.HeaderFooter(section.Headers[HeaderFooterType.First]).Kind);
    }

    [Fact]
    public void Reader_HeaderBlocks_HaveText()
    {
        var document = new DocxBuilder()
            .Header(P("Header text") + "<w:tbl><w:tblGrid><w:gridCol w:w=\"1000\"/></w:tblGrid><w:tr>" + Cell(P("cell")) + "</w:tr></w:tbl><w:p/>")
            .Body(P("body") + "<w:tbl><w:tblGrid><w:gridCol w:w=\"1000\"/></w:tblGrid><w:tr>" + Cell(P("cell")) + "</w:tr></w:tbl><w:p/>").Load();

        var header = Assert.Single(document.HeadersFooters);
        Assert.Equal(HeaderFooterKind.Header, header.Kind);
        Assert.Equal("Header text", Assert.IsType<Paragraph>(header.Blocks[0]).Text);
        Assert.IsType<Table>(header.Blocks[1]);
        var all = AllBlocks(document.Blocks).Concat(AllBlocks(header.Blocks)).ToList();
        Assert.Equal(all.Count, all.Select(b => b.Id).Distinct().Count());
        Assert.All(all, b => Assert.Same(b, document.FindBlock(b.Id)));
        Assert.Contains(document.AllParagraphs(true), p => p.Text == "Header text");
    }

    [Fact]
    public void Reader_Columns_ReadAsInfo()
    {
        Assert.Equal(2, new DocxBuilder { SectionProperties = "<w:cols w:num=\"2\" w:space=\"708\"/>" }.Load().Sections[0].Columns);
        Assert.Equal(1, new DocxBuilder { SectionProperties = "<w:cols w:space=\"708\"/>" }.Load().Sections[0].Columns);
    }

    [Fact]
    public void Reader_Comments_AuthorDateTextParagraph()
    {
        var document = new DocxBuilder()
            .Comments("<w:comment w:id=\"0\" w:author=\"Author A\" w:date=\"2026-11-22T10:30:00Z\" w:initials=\"AA\">" +
                      "<w:p><w:r><w:annotationRef/></w:r><w:r><w:t>First line</w:t></w:r></w:p><w:p><w:r><w:t>Second line</w:t></w:r></w:p></w:comment>" +
                      "<w:comment w:id=\"1\" w:author=\"B\">" + P("Other") + "</w:comment>")
            .Body(P("intro") +
                  "<w:p><w:commentRangeStart w:id=\"0\"/><w:r><w:t>commented</w:t></w:r><w:commentRangeEnd w:id=\"0\"/>" +
                  "<w:r><w:commentReference w:id=\"0\"/></w:r></w:p>" +
                  "<w:commentRangeStart w:id=\"1\"/>" +
                  "<w:p><w:r><w:t>later</w:t></w:r><w:commentRangeEnd w:id=\"1\"/><w:r><w:commentReference w:id=\"1\"/></w:r></w:p>").Load();

        Assert.Equal(2, document.Comments.Count);
        Assert.Equal(new Comment("0", "Author A", "AA", new DateTimeOffset(2026, 11, 22, 10, 30, 0, TimeSpan.Zero), "First line\nSecond line", document.Blocks[1].Id),
            document.Comments[0]);
        Assert.Equal(new Comment("1", "B", null, null, "Other", document.Blocks[3].Id), document.Comments[1]);
    }

    [Fact]
    public void Reader_CommentInTableCell_AnchorsToCellParagraph()
    {
        var document = new DocxBuilder()
            .Comments("<w:comment w:id=\"5\" w:author=\"A\">" + P("note") + "</w:comment>")
            .Body("<w:tbl><w:tblGrid><w:gridCol w:w=\"1000\"/></w:tblGrid><w:tr>" +
                  Cell("<w:p><w:commentRangeStart w:id=\"5\"/><w:r><w:t>x</w:t></w:r></w:p>") + "</w:tr></w:tbl><w:p/>").Load();

        var cellParagraph = ((Table)document.Blocks[0]).Rows[0].Cells[0].Blocks[0];
        Assert.Equal(cellParagraph.Id, Assert.Single(document.Comments).ParagraphId);
    }

    [Fact]
    public void Reader_CoreProperties()
    {
        var document = new DocxBuilder()
            .CoreProperties("<dc:title>T</dc:title><dc:creator>Author A</dc:creator><cp:lastModifiedBy>B</cp:lastModifiedBy>" +
                            "<dcterms:created xsi:type=\"dcterms:W3CDTF\">2026-01-02T03:04:05Z</dcterms:created>").Load();

        Assert.Equal(new DocumentProperties("T", null, "Author A", null, null, "B", new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), null), document.Properties);
    }

    [Fact]
    public void Reader_BrokenOrMissingOptionalParts_AreSkipped()
    {
        var broken = Encoding.UTF8.GetBytes("<w:hdr><w:p>");
        var document = new DocxBuilder()
            .Header(P("broken")).Header(P("Header text"), "first")
            .RawRelationship($"<Relationship Id=\"rIdMissing\" Type=\"{DocxBuilder.RelNs}/footer\" Target=\"footer9.xml\"/>")
            .Comments("").CoreProperties("<dc:title>T</dc:title>")
            .Extra("/word/header1.xml", broken)
            .Extra("/word/_rels/header2.xml.rels", Encoding.UTF8.GetBytes(
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                $"<Relationship Type=\"{DocxBuilder.RelNs}/hyperlink\" Target=\"https://example.com\" TargetMode=\"External\"/>" +
                $"<Relationship Id=\"rId9\" Type=\"{DocxBuilder.RelNs}/image\"/></Relationships>"))
            .Extra("/word/comments.xml", broken).Extra("/docProps/core.xml", broken)
            .Body(P("body")).Load();

        var header = Assert.Single(document.HeadersFooters);
        Assert.Equal("Header text", ((Paragraph)header.Blocks[0]).Text);
        Assert.Equal(2, document.Sections[0].Headers.Count);
        Assert.Empty(document.Comments);
        Assert.Null(document.Properties.Title);
        Assert.Equal("body", ((Paragraph)document.Blocks[0]).Text);
    }

    [Fact]
    public void Reader_CellWithoutBlocks_GetsOneEmptyParagraph()
    {
        var document = Load("<w:tbl><w:tblGrid><w:gridCol w:w=\"1000\"/></w:tblGrid><w:tr><w:tc><w:tcPr/></w:tc></w:tr></w:tbl><w:p/>");

        var table = (Table)document.Blocks[0];
        var paragraph = Assert.IsType<Paragraph>(Assert.Single(table.Rows[0].Cells[0].Blocks));
        Assert.Empty(paragraph.Inlines);
        Assert.Null(paragraph.Source);
        Assert.NotNull(paragraph.Id);
        var ids = AllBlocks(document.Blocks).Select(b => b.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Same(paragraph, document.Resolve(new DocumentPosition(table.Id, [new CellStep(0, 0, 0)], 0)).Paragraph);
    }

    [Fact]
    public void Reader_GridBeforeAfter_ColumnOf()
    {
        var grid = "<w:tblGrid>" + string.Concat(Enumerable.Repeat("<w:gridCol w:w=\"500\"/>", 7)) + "</w:tblGrid>";
        var table = (Table)Load("<w:tbl>" + grid + "<w:tr><w:trPr><w:gridBefore w:val=\"1\"/><w:gridAfter w:val=\"2\"/></w:trPr>" +
                                Cell(P("a")) + Cell(P("b"), "<w:gridSpan w:val=\"2\"/>") + Cell(P("c")) + "</w:tr>" +
                                "<w:tr>" + Cell(P("d"), "<w:gridSpan w:val=\"7\"/>") + "</w:tr></w:tbl><w:p/>").Blocks[0];

        var (first, second) = (table.Rows[0], table.Rows[1]);
        Assert.Equal((1, 2), (first.GridBefore, first.GridAfter));
        Assert.Equal((0, 0), (second.GridBefore, second.GridAfter));
        Assert.Equal([1, 2, 4], first.Cells.Select(c => table.ColumnOf(first, c)));
        Assert.Equal(0, table.ColumnOf(second, second.Cells[0]));
        Assert.Throws<ArgumentException>(() => table.ColumnOf(first, second.Cells[0]));
        Assert.Throws<ArgumentException>(() => table.ColumnOf(first with { }, first.Cells[0]));
    }

    [Fact]
    public void Reader_LaterSection_InheritsHeader_HeaderForNullWithoutOne()
    {
        var document = new DocxBuilder().Header(P("Header text"))
            .Body("<w:p><w:pPr><w:sectPr><w:headerReference w:type=\"default\" r:id=\"rIdheader1\"/><w:pgSz w:w=\"11906\" w:h=\"16838\"/></w:sectPr></w:pPr></w:p>" +
                  P("second") + "<w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/></w:sectPr>").Load();

        var (first, second) = (document.Sections[0], document.Sections[1]);
        Assert.Empty(second.Headers);
        var header = document.HeaderFor(second, HeaderFooterKind.Header, HeaderFooterType.Default);
        Assert.Equal(first.Headers[HeaderFooterType.Default], header.Id);
        Assert.Equal("Header text", ((Paragraph)header.Blocks[0]).Text);
        Assert.Null(document.HeaderFor(second, HeaderFooterKind.Footer, HeaderFooterType.Default));
        Assert.Null(document.HeaderFor(first, HeaderFooterKind.Header, HeaderFooterType.First));
    }

    [Fact]
    public void Reader_TwoRelationshipsToOneHeaderPart_ShareOneHeaderFooter()
    {
        var document = new DocxBuilder().Header(P("Shared"))
            .RawRelationship($"<Relationship Id=\"rIdAlias\" Type=\"{DocxBuilder.RelNs}/header\" Target=\"header1.xml\"/>")
            .Body("<w:p><w:pPr><w:sectPr><w:headerReference w:type=\"default\" r:id=\"rIdheader1\"/><w:headerReference w:type=\"first\" r:id=\"rIdAlias\"/>" +
                  "<w:pgSz w:w=\"11906\" w:h=\"16838\"/></w:sectPr></w:pPr></w:p>").Load();

        var header = Assert.Single(document.HeadersFooters);
        var section = document.Sections[0];
        Assert.Equal([header.Id, header.Id], new[] { section.Headers[HeaderFooterType.Default], section.Headers[HeaderFooterType.First] });
        Assert.Same(header, document.HeaderFor(section, HeaderFooterKind.Header, HeaderFooterType.First));
    }

    [Fact]
    public void Reader_ParagraphSectPrOnLastBlock_EndsSectionBeforeDefaultFinalSection()
    {
        var document = Load(P("one") + "<w:p><w:pPr><w:sectPr><w:pgSz w:w=\"12240\" w:h=\"15840\"/></w:sectPr></w:pPr><w:r><w:t>last</w:t></w:r></w:p>");

        Assert.Equal(2, document.Sections.Count);
        Assert.Equal((document.Blocks[^1].Id, 12240), (document.Sections[0].LastBlockId, document.Sections[0].Page.Width));
        Assert.Equal(((string)null, PageSetup.A4), (document.Sections[1].LastBlockId, document.Sections[1].Page));
    }

    [Fact]
    public void Reader_CommentRangeStartAndReferenceInDifferentParagraphs_RangeStartWins()
    {
        var document = new DocxBuilder()
            .Comments("<w:comment w:id=\"7\" w:author=\"A\">" + P("note") + "</w:comment>")
            .Body("<w:p><w:commentRangeStart w:id=\"7\"/><w:r><w:t>start</w:t></w:r></w:p>" +
                  "<w:p><w:r><w:t>end</w:t></w:r><w:commentRangeEnd w:id=\"7\"/><w:r><w:commentReference w:id=\"7\"/></w:r></w:p>").Load();

        Assert.Equal(document.Blocks[0].Id, Assert.Single(document.Comments).ParagraphId);
    }

    [Fact]
    public void Reader_CommentsPartMissing_NoComments()
    {
        const string body = "<w:p><w:r><w:t>x</w:t></w:r><w:r><w:commentReference w:id=\"0\"/></w:r></w:p>";

        Assert.Empty(new DocxBuilder().Body(body).Load().Comments);
        Assert.Empty(new DocxBuilder().RawRelationship($"<Relationship Id=\"rIdC\" Type=\"{DocxBuilder.RelNs}/comments\" Target=\"comments9.xml\"/>")
            .Body(body).Load().Comments);
    }

    [Fact]
    public void Reader_MalformedCoreDate_IsNull()
    {
        var properties = new DocxBuilder().CoreProperties("<dc:title>T</dc:title><dcterms:created>not a date</dcterms:created><dcterms:modified/>").Load().Properties;

        Assert.Equal(("T", (DateTimeOffset?)null, (DateTimeOffset?)null), (properties.Title, properties.Created, properties.Modified));
    }

    [Fact]
    public void Reader_BodySdt_IsOpaqueBlockWithPreview()
    {
        var document = Load("<w:sdt><w:sdtPr><w:alias w:val=\"Box\"/></w:sdtPr><w:sdtContent>" + P("one") + P("two") + "</w:sdtContent></w:sdt><w:p/>");

        var block = Assert.IsType<OpaqueBlock>(document.Blocks[0]);
        Assert.Equal(("contentControl", true, "sdt"), (block.Kind, block.Visible, block.Source.Name.LocalName));
        Assert.Equal(["one", "two"], block.Preview.Select(b => Assert.IsType<Paragraph>(b).Text));
        Assert.All(block.Preview, b => Assert.NotNull(b.Id));
        Assert.Equal(4, document.Blocks.Concat(block.Preview).Select(b => b.Id).Distinct().Count());
    }

    [Fact]
    public void Reader_BodyLevelBookmark_IsHiddenMarkerBlock()
    {
        var document = Load("<w:bookmarkStart w:id=\"0\" w:name=\"bm\"/>" + P("x") + "<w:bookmarkEnd w:id=\"0\"/><w:permStart w:id=\"1\" w:edGrp=\"everyone\"/>" +
                            "<w:customXml w:element=\"c\">" + P("c") + "</w:customXml><w:altChunk r:id=\"rIdX\"/>");

        Assert.Equal(["marker", "other", "marker", "marker", "customXml", "altChunk"],
            document.Blocks.Select(b => b is OpaqueBlock o ? o.Kind : "other"));
        Assert.All(document.Blocks.OfType<OpaqueBlock>().Where(o => o.Kind == "marker"), o => Assert.False(o.Visible));
        Assert.Equal("bookmarkStart", document.Blocks[0].Source.Name.LocalName);
    }
}
