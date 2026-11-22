using System.Diagnostics;
using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class DocxReaderTests
{
    private static string DataPath(string name) => Path.Combine(AppContext.BaseDirectory, "TestData", "Document", name);

    private static Paragraph Para(string body, int index = 0) => (Paragraph)new DocxBuilder().Body(body).Load().Blocks[index];

    [Fact]
    public void Create_BlankA4_OneEmptyNormalParagraph_OneSection()
    {
        var document = TextDocument.Create();

        var paragraph = Assert.IsType<Paragraph>(Assert.Single(document.Blocks));
        Assert.Empty(paragraph.Inlines);
        Assert.NotNull(paragraph.Id);
        Assert.Equal("Normal", document.Styles.DefaultParagraphStyle.Id);
        Assert.Equal(["Normal", "DefaultParagraphFont", "TableNormal", "NoList"], document.Styles.All.Select(s => s.Id));
        Assert.Equal(11, document.Styles.DefaultRunFormat.SizePt);
        Assert.Equal("Calibri", document.Styles.DefaultRunFormat.Font);
        Assert.Equal(160, document.Styles.DefaultParagraphFormat.SpacingAfter);
        Assert.Equal(new LineSpacing(259, LineRule.Auto), document.Styles.DefaultParagraphFormat.Line);
        Assert.Empty(document.Numbering.Instances);
        Assert.False(document.IsTemplate);
        Assert.Equal(11906, Assert.Single(document.Sections).Page.Width);
        Assert.Equal(PageSetup.A4, document.Sections[0].Page);
        Assert.Equal(PageSetup.Letter, TextDocument.Create(PageSetup.Letter).Sections[0].Page);

        var opc = OpcPackage.Open(BlankDocument.Build(PageSetup.A4));
        Assert.Equal("720", opc.ReadXml("/word/settings.xml").Descendants(FormatXml.W + "defaultTabStop").Single().Attribute(FormatXml.W + "val")?.Value);
        Assert.Contains("/docProps/core.xml", opc.PartNames);
        Assert.Contains("/docProps/app.xml", opc.PartNames);
        Assert.DoesNotContain(opc.PartNames, p => p.Contains("theme", StringComparison.OrdinalIgnoreCase) || p.Contains("numbering", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Reader_SampleDocx_ReadsParagraphsAndText()
    {
        using var file = File.OpenRead(DataPath("sample.docx"));

        var document = TextDocument.Load(file);

        var paragraph = Assert.IsType<Paragraph>(Assert.Single(document.Blocks));
        Assert.Equal("Sample Word Document", paragraph.Text);
        var format = Assert.Single(paragraph.Inlines).Format;
        Assert.Equal("Verdana", format.Font);
        Assert.Equal(8.5, format.SizePt);
        Assert.Equal("#333333", format.Color);
        Assert.Equal(11906, Assert.Single(document.Sections).Page.Width);
        Assert.Equal(1417, document.Sections[0].Page.MarginLeft);
        Assert.False(document.HasTrackedChanges);
    }

    [Fact]
    public void Reader_MixedRuns_KeepBoundariesAndFormats()
    {
        var paragraph = Para("<w:p><w:r><w:t xml:space=\"preserve\">Normal </w:t></w:r><w:r><w:rPr><w:b/></w:rPr><w:t>bold</w:t></w:r><w:r><w:t xml:space=\"preserve\"> normal</w:t></w:r></w:p>");

        Assert.Equal(3, paragraph.Inlines.Count);
        Assert.Equal("Normal bold normal", paragraph.Text);
        Assert.Null(paragraph.Inlines[0].Format.Bold);
        Assert.True(paragraph.Inlines[1].Format.Bold);
        Assert.Null(paragraph.Inlines[2].Format.Bold);
        Assert.All(paragraph.Inlines, i => Assert.Equal("r", i.Source.Name.LocalName));
    }

    [Fact]
    public void Reader_RunChildren_SplitIntoInlines()
    {
        var paragraph = Para("<w:p><w:r><w:rPr><w:i/></w:rPr><w:t>a</w:t><w:tab/><w:t>b</w:t><w:br/><w:br w:type=\"page\"/><w:lastRenderedPageBreak/></w:r></w:p>");

        Assert.Equal("a\tb\v\f", paragraph.Text);
        Assert.Equal(5, paragraph.Inlines.Count);
        Assert.Single(paragraph.Inlines.Select(i => i.Source).Distinct());
        Assert.All(paragraph.Inlines, i => Assert.True(i.Format.Italic));
        Assert.Equal(BreakKind.Page, ((Break)paragraph.Inlines[4]).Kind);
    }

    [Fact]
    public void Reader_ProofErrAndBookmarks_AreZeroLengthMarkers()
    {
        var paragraph = Para("<w:p><w:r><w:t>Hel</w:t></w:r><w:proofErr w:type=\"spellStart\"/><w:r><w:t xml:space=\"preserve\">lo </w:t></w:r>" +
                             "<w:bookmarkStart w:id=\"0\" w:name=\"Mark\"/><w:r><w:t>World</w:t></w:r><w:bookmarkEnd w:id=\"0\"/></w:p>");

        Assert.Equal("Hello World", paragraph.Text);
        Assert.Equal(11, paragraph.Length);
        var markers = paragraph.Inlines.OfType<Marker>().ToList();
        Assert.Equal(["proofErr", "bookmarkStart", "bookmarkEnd"], markers.Select(m => m.Kind));
        Assert.Equal("Mark", markers[1].Name);
    }

    [Fact]
    public void Reader_WhitespaceOnlyPreservedText_Kept()
    {
        var paragraph = Para("<w:p><w:r><w:t>a</w:t></w:r><w:r><w:t xml:space=\"preserve\"> </w:t></w:r><w:r><w:t>b</w:t></w:r></w:p>");

        Assert.Equal("a b", paragraph.Text);
        Assert.Equal(3, paragraph.Inlines.Count);
    }

    [Fact]
    public void Reader_Hyperlink_ExternalTarget_SharedByAllInlines()
    {
        var document = new DocxBuilder().Link("rId9", "https://example.com")
            .Body("<w:p><w:r><w:t xml:space=\"preserve\">See </w:t></w:r><w:hyperlink r:id=\"rId9\" w:tooltip=\"Tip\" w:history=\"1\"><w:r><w:t>exa</w:t></w:r>" +
                  "<w:r><w:rPr><w:b/></w:rPr><w:t>mple</w:t></w:r></w:hyperlink><w:hyperlink w:anchor=\"Mark\"><w:r><w:t>jump</w:t></w:r></w:hyperlink></w:p>")
            .Load();
        var paragraph = (Paragraph)document.Blocks[0];

        Assert.Null(paragraph.Inlines[0].Link);
        var link = paragraph.Inlines[1].Link;
        Assert.Equal(("https://example.com", (string)null, "Tip"), (link.Target, link.Anchor, link.Tooltip));
        Assert.Same(link, paragraph.Inlines[2].Link);
        Assert.Equal("hyperlink", link.Source.Name.LocalName);
        Assert.Equal("#Mark", paragraph.Inlines[3].Link.SafeTarget);
        Assert.Equal("See examplejump", paragraph.Text);
    }

    [Fact]
    public void Reader_ComplexFieldInParagraph_IsOneToken()
    {
        var paragraph = Para("<w:p><w:r><w:t xml:space=\"preserve\">Page </w:t></w:r><w:r><w:fldChar w:fldCharType=\"begin\"/></w:r>" +
                             "<w:r><w:instrText xml:space=\"preserve\"> PAGE </w:instrText></w:r><w:r><w:fldChar w:fldCharType=\"separate\"/></w:r>" +
                             "<w:r><w:rPr><w:noProof/></w:rPr><w:t>3</w:t></w:r><w:r><w:fldChar w:fldCharType=\"end\"/></w:r><w:r><w:t xml:space=\"preserve\"> of</w:t></w:r></w:p>");

        Assert.Equal(3, paragraph.Inlines.Count);
        var field = Assert.IsType<OpaqueInline>(paragraph.Inlines[1]);
        Assert.Equal("field", field.Kind);
        Assert.Equal("PAGE", field.Instruction);
        Assert.Equal("3", field.DisplayText);
        Assert.Equal(5, field.Sources.Count);
        Assert.Equal("Page \uFFFC of", paragraph.Text);

        var simple = Assert.IsType<OpaqueInline>(Assert.Single(Para("<w:p><w:fldSimple w:instr=\" NUMPAGES \"><w:r><w:t>7</w:t></w:r></w:fldSimple></w:p>").Inlines));
        Assert.Equal(("field", "NUMPAGES", "7"), (simple.Kind, simple.Instruction, simple.DisplayText));
        Assert.Equal("fldSimple", Assert.Single(simple.Sources).Name.LocalName);
    }

    [Fact]
    public void Reader_FieldAcrossParagraphs_StartEndTokens_ResultEditable()
    {
        var document = new DocxBuilder().Body(
            "<w:p><w:r><w:fldChar w:fldCharType=\"begin\"/></w:r><w:r><w:instrText xml:space=\"preserve\"> TOC \\o \"1-3\" \\h </w:instrText></w:r>" +
            "<w:r><w:fldChar w:fldCharType=\"separate\"/></w:r><w:r><w:t>Intro</w:t></w:r></w:p>" +
            "<w:p><w:r><w:t>Body</w:t></w:r><w:r><w:fldChar w:fldCharType=\"begin\"/></w:r><w:r><w:instrText>PAGEREF _Toc1</w:instrText></w:r>" +
            "<w:r><w:fldChar w:fldCharType=\"separate\"/></w:r><w:r><w:t>2</w:t></w:r><w:r><w:fldChar w:fldCharType=\"end\"/></w:r></w:p>" +
            "<w:p><w:r><w:t>End</w:t></w:r><w:r><w:fldChar w:fldCharType=\"end\"/></w:r></w:p><w:p><w:r><w:t>After</w:t></w:r></w:p>").Load();
        var paragraphs = document.Blocks.Cast<Paragraph>().ToList();

        var start = Assert.IsType<OpaqueInline>(paragraphs[0].Inlines[0]);
        Assert.Equal("fieldStart", start.Kind);
        Assert.Equal("TOC \\o \"1-3\" \\h", start.Instruction);
        Assert.Equal(3, start.Sources.Count);
        Assert.Equal("\uFFFCIntro", paragraphs[0].Text);
        Assert.Equal("Body\uFFFC", paragraphs[1].Text);
        Assert.Equal("field", ((OpaqueInline)paragraphs[1].Inlines[1]).Kind);
        Assert.Equal("End\uFFFC", paragraphs[2].Text);
        Assert.Equal("fieldEnd", ((OpaqueInline)paragraphs[2].Inlines[1]).Kind);
        Assert.Equal("After", paragraphs[3].Text);
    }

    [Fact]
    public void Reader_TrackedChanges_FinalView()
    {
        var document = new DocxBuilder().Body(
            "<w:p><w:ins w:id=\"1\" w:author=\"A\"><w:r><w:t>new</w:t></w:r></w:ins><w:del w:id=\"2\" w:author=\"A\"><w:r><w:delText>old</w:delText></w:r></w:del></w:p>" +
            "<w:p><w:pPr><w:rPr><w:del w:id=\"3\" w:author=\"A\"/></w:rPr></w:pPr><w:del w:id=\"4\" w:author=\"A\"><w:r><w:delText>gone</w:delText></w:r></w:del></w:p>" +
            "<w:p><w:pPr><w:rPr><w:del w:id=\"5\" w:author=\"A\"/></w:rPr></w:pPr><w:r><w:t>kept</w:t></w:r></w:p>").Load();

        Assert.True(document.HasTrackedChanges);
        Assert.Equal("new", ((Paragraph)document.Blocks[0]).Text);
        var deletion = Assert.IsType<OpaqueBlock>(document.Blocks[1]);
        Assert.Equal("trackedDeletion", deletion.Kind);
        Assert.False(deletion.Visible);
        Assert.Equal("p", deletion.Source.Name.LocalName);
        Assert.Equal("kept", ((Paragraph)document.Blocks[2]).Text);
        Assert.False(new DocxBuilder().Body("<w:p><w:r><w:t>plain</w:t></w:r></w:p>").Load().HasTrackedChanges);
        Assert.True(new DocxBuilder().Body("<w:p><w:r><w:rPr><w:b/><w:rPrChange w:id=\"1\" w:author=\"A\"><w:rPr/></w:rPrChange></w:rPr><w:t>x</w:t></w:r></w:p>").Load().HasTrackedChanges);
    }

    [Fact]
    public void Reader_InlineImage_SizeAltMedia()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 1, 2, 3];
        var document = new DocxBuilder().Media("rId7", "image1.png", png).Body(
            "<w:p><w:r><w:drawing><wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\"><wp:extent cx=\"1905000\" cy=\"952500\"/><wp:docPr id=\"1\" name=\"Picture 1\" descr=\"Logo\"/>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"><pic:pic><pic:nvPicPr><pic:cNvPr id=\"0\" name=\"image1.png\"/><pic:cNvPicPr/></pic:nvPicPr>" +
            "<pic:blipFill><a:blip r:embed=\"rId7\"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill><pic:spPr/></pic:pic></a:graphicData></a:graphic></wp:inline></w:drawing></w:r></w:p>").Load();

        var image = Assert.IsType<InlineImage>(Assert.Single(((Paragraph)document.Blocks[0]).Inlines));
        Assert.Equal(("/word/media/image1.png", 1905000L, 952500L), (image.MediaPart, image.WidthEmu, image.HeightEmu));
        Assert.Equal("Logo", image.AltText);
        Assert.Equal(ImagePlacement.Inline, image.Placement);
        Assert.Equal(png, document.GetMedia(image.MediaPart));
        Assert.Equal("image/png", document.MediaContentType(image.MediaPart));
    }

    [Fact]
    public void Reader_AnchoredImageAlignRight_PlacementRight()
    {
        static string Anchor(string positionH) =>
            "<w:p><w:r><w:drawing><wp:anchor distT=\"0\" distB=\"0\" distL=\"114300\" distR=\"114300\" simplePos=\"0\" relativeHeight=\"1\" behindDoc=\"0\" locked=\"0\" layoutInCell=\"1\" allowOverlap=\"1\">" +
            $"<wp:simplePos x=\"0\" y=\"0\"/>{positionH}<wp:positionV relativeFrom=\"paragraph\"><wp:posOffset>0</wp:posOffset></wp:positionV>" +
            "<wp:extent cx=\"100\" cy=\"200\"/><wp:wrapSquare wrapText=\"bothSides\"/><wp:docPr id=\"2\" name=\"Picture 2\"/>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"><pic:pic><pic:blipFill><a:blip r:embed=\"rId7\"/></pic:blipFill></pic:pic></a:graphicData></a:graphic>" +
            "</wp:anchor></w:drawing></w:r></w:p>";
        InlineImage Read(string positionH) =>
            (InlineImage)((Paragraph)new DocxBuilder().Media("rId7", "image1.png", [1]).Body(Anchor(positionH)).Load().Blocks[0]).Inlines[0];

        var right = Read("<wp:positionH relativeFrom=\"column\"><wp:align>right</wp:align></wp:positionH>");
        var offset = Read("<wp:positionH relativeFrom=\"column\"><wp:posOffset>12700</wp:posOffset></wp:positionH>");

        Assert.Equal(ImagePlacement.Right, right.Placement);
        Assert.Equal((100L, 200L), (right.WidthEmu, right.HeightEmu));
        Assert.Null(right.AltText);
        Assert.Equal(ImagePlacement.Left, offset.Placement);
    }

    [Fact]
    public void Reader_ChartAndTextBox_AreOpaqueTokens()
    {
        var paragraph = Para(
            "<w:p><w:r><w:drawing><wp:inline><wp:extent cx=\"5486400\" cy=\"3200400\"/><wp:docPr id=\"1\" name=\"Chart 1\"/><a:graphic>" +
            "<a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"><c:chart r:id=\"rId8\"/></a:graphicData></a:graphic></wp:inline></w:drawing></w:r>" +
            "<w:r><mc:AlternateContent><mc:Choice Requires=\"wps\"><w:drawing><wp:anchor><wp:extent cx=\"1\" cy=\"1\"/><wp:docPr id=\"2\" name=\"Text Box 2\"/><a:graphic>" +
            "<a:graphicData uri=\"http://schemas.microsoft.com/office/word/2010/wordprocessingShape\"><wps:wsp><wps:txbx><w:txbxContent><w:p><w:r><w:t>inner</w:t></w:r></w:p></w:txbxContent></wps:txbx>" +
            "</wps:wsp></a:graphicData></a:graphic></wp:anchor></w:drawing></mc:Choice><mc:Fallback><w:pict><v:shape/></w:pict></mc:Fallback></mc:AlternateContent></w:r>" +
            "<w:r><w:drawing><wp:inline><wp:extent cx=\"1\" cy=\"1\"/><wp:docPr id=\"3\" name=\"Diagram 3\"/><a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\">" +
            "<dgm:relIds r:dm=\"rId1\" r:lo=\"rId2\" r:qs=\"rId3\" r:cs=\"rId4\"/></a:graphicData></a:graphic></wp:inline></w:drawing></w:r>" +
            "<w:r><w:pict><v:shape/></w:pict><w:object><v:shape/></w:object><w:sym w:font=\"Wingdings\" w:char=\"F04A\"/><w:unknownThing/></w:r></w:p>");

        Assert.Equal(["chart", "shape", "diagram", "vml", "ole", "symbol", "other"], paragraph.Inlines.Cast<OpaqueInline>().Select(o => o.Kind));
        Assert.Equal("", ((OpaqueInline)paragraph.Inlines[5]).DisplayText);
        Assert.Equal("\uFFFC\uFFFC\uFFFC\uFFFC\uFFFC\uFFFC\uFFFC", paragraph.Text);
    }

    [Fact]
    public void Reader_FootnoteReferences_NumberedInOrder_CustomMark()
    {
        var document = new DocxBuilder().Body(
            "<w:p><w:r><w:t>a</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"FootnoteReference\"/></w:rPr><w:footnoteReference w:id=\"1\"/></w:r>" +
            "<w:r><w:endnoteReference w:id=\"1\"/></w:r><w:r><w:footnoteReference w:customMarkFollows=\"1\" w:id=\"2\"/><w:t>*</w:t></w:r></w:p>" +
            "<w:p><w:r><w:footnoteReference w:id=\"3\"/></w:r><w:r><w:endnoteReference w:id=\"2\"/></w:r></w:p>").Load();
        var first = (Paragraph)document.Blocks[0];
        var second = (Paragraph)document.Blocks[1];

        Assert.Equal("a\uFFFC\uFFFC\uFFFC", first.Text);
        Assert.Equal([("footnote", "1"), ("endnote", "i"), ("footnote", "*")], first.Inlines.Skip(1).Cast<OpaqueInline>().Select(o => (o.Kind, o.DisplayText)));
        Assert.Equal("FootnoteReference", first.Inlines[1].Format.StyleId);
        Assert.Equal([("footnote", "2"), ("endnote", "ii")], second.Inlines.Cast<OpaqueInline>().Select(o => (o.Kind, o.DisplayText)));
    }

    [Fact]
    public void Reader_InlineSdtSmartTagMath_AreTokensWithText()
    {
        var paragraph = Para(
            "<w:p><w:r><w:t>x</w:t></w:r><w:sdt><w:sdtPr><w:alias w:val=\"Name\"/></w:sdtPr><w:sdtContent><w:r><w:t>Jane</w:t></w:r></w:sdtContent></w:sdt>" +
            "<w:smartTag w:uri=\"urn:schemas-microsoft-com:office:smarttags\" w:element=\"place\"><w:r><w:t>Berlin</w:t></w:r></w:smartTag>" +
            "<w:customXml w:element=\"tag\"><w:r><w:t>Data</w:t></w:r></w:customXml>" +
            "<m:oMath><m:r><m:t>a</m:t></m:r><m:r><m:t>=b+1</m:t></m:r></m:oMath><w:permStart w:id=\"1\" w:edGrp=\"everyone\"/><w:permEnd w:id=\"1\"/></w:p>");

        Assert.Equal([("contentControl", "Jane"), ("smartTag", "Berlin"), ("customXml", "Data"), ("equation", "a=b+1")],
            paragraph.Inlines.OfType<OpaqueInline>().Select(o => (o.Kind, o.DisplayText)));
        Assert.Equal(["permStart", "permEnd"], paragraph.Inlines.OfType<Marker>().Select(m => m.Kind));
        Assert.Equal("x\uFFFC\uFFFC\uFFFC\uFFFC", paragraph.Text);
    }

    [Fact]
    public void Reader_NoBreakAndSoftHyphen_MapToUnicode()
    {
        var paragraph = Para("<w:p><w:r><w:t>e</w:t><w:noBreakHyphen/><w:t>mail</w:t><w:softHyphen/><w:t>box</w:t><w:cr/></w:r></w:p>");

        Assert.Equal("e\u2011mail\u00ADbox\v", paragraph.Text);
        Assert.Equal(6, paragraph.Inlines.Count);
    }

    [Fact]
    public void Reader_MainPartFromRelationship()
    {
        var document = new DocxBuilder { MainPart = "/word/document2.xml" }.Body("<w:p><w:r><w:t>moved</w:t></w:r></w:p>")
            .Styles("<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style>")
            .Settings("<w:evenAndOddHeaders/>").Load();

        Assert.Equal("moved", ((Paragraph)Assert.Single(document.Blocks)).Text);
        Assert.Equal("Normal", document.Styles.DefaultParagraphStyle.Id);
        Assert.True(document.EvenAndOddHeaders);
        Assert.False(document.IsTemplate);
    }

    [Fact]
    public void Reader_Dotx_IsTemplate()
    {
        var document = new DocxBuilder { Template = true }.Body("<w:p><w:r><w:t>t</w:t></w:r></w:p>").Load();

        Assert.True(document.IsTemplate);
        Assert.Equal("t", ((Paragraph)document.Blocks[0]).Text);
    }

    [Fact]
    public void Reader_MalformedDocumentXml_ThrowsInvalidData()
    {
        var builder = new DocxBuilder { RawDocument = $"<w:document {DocxBuilder.Ns}><w:body><w:p><w:r><w:t>broken" };

        var error = Assert.Throws<InvalidDataException>(() => builder.Load());

        Assert.IsType<System.Xml.XmlException>(error.InnerException);
    }

    [Fact]
    public void Reader_RelationshipsWithoutIdOrTarget_AreSkipped()
    {
        var document = new DocxBuilder()
            .RawRelationship($"<Relationship Type=\"{DocxBuilder.RelNs}/hyperlink\" Target=\"https://example.com\" TargetMode=\"External\"/>")
            .RawRelationship($"<Relationship Id=\"rIdStyles\" Type=\"{DocxBuilder.RelNs}/styles\"/>")
            .Body("<w:p><w:r><w:t>still readable</w:t></w:r></w:p>").Load();

        Assert.Equal("still readable", ((Paragraph)Assert.Single(document.Blocks)).Text);
        Assert.Empty(document.Styles.All);
    }

    [Fact]
    public void Reader_ImageRelationship_InternalTargetMode_IsInternal_BrokenEmbedIsOpaque()
    {
        static string Picture(string id) =>
            "<w:r><w:drawing><wp:inline><wp:extent cx=\"10\" cy=\"20\"/><wp:docPr id=\"1\" name=\"Picture 1\"/><a:graphic>" +
            $"<a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"><pic:pic><pic:blipFill><a:blip r:embed=\"{id}\"/></pic:blipFill></pic:pic>" +
            "</a:graphicData></a:graphic></wp:inline></w:drawing></w:r>";
        var document = new DocxBuilder()
            .RawRelationship($"<Relationship Id=\"rId7\" Type=\"{DocxBuilder.RelNs}/image\" Target=\"media/image1.png\" TargetMode=\"Internal\"/>")
            .Extra("/word/media/image1.png", [1, 2], "image/png")
            .Body($"<w:p>{Picture("rId7")}{Picture("rId99")}</w:p>").Load();
        var inlines = ((Paragraph)document.Blocks[0]).Inlines;

        Assert.Equal("/word/media/image1.png", Assert.IsType<InlineImage>(inlines[0]).MediaPart);
        Assert.Equal("drawing", Assert.IsType<OpaqueInline>(inlines[1]).Kind);
    }

    [Fact]
    public void Reader_DirAndBdo_Descended_RubyShowsBaseText()
    {
        var paragraph = Para("<w:p><w:dir w:val=\"rtl\"><w:r><w:t>abc</w:t></w:r></w:dir><w:bdo w:val=\"ltr\"><w:r><w:t>de</w:t></w:r></w:bdo>" +
                             "<w:r><w:ruby><w:rubyPr/><w:rt><w:r><w:t>top</w:t></w:r></w:rt><w:rubyBase><w:r><w:t>base</w:t></w:r></w:rubyBase></w:ruby></w:r></w:p>");

        Assert.Equal("abcde" + (char)0xFFFC, paragraph.Text);
        Assert.Equal(["dir", "bdo"], paragraph.Inlines.Take(2).Select(i => i.Source.Parent!.Name.LocalName));
        var ruby = Assert.IsType<OpaqueInline>(paragraph.Inlines[2]);
        Assert.Equal(("ruby", "base"), (ruby.Kind, ruby.DisplayText));
    }

    [Fact]
    public void Reader_InlineSdtWithDeletedRun_DisplayTextSkipsDeletion()
    {
        var paragraph = Para("<w:p><w:sdt><w:sdtContent><w:r><w:t>keep</w:t></w:r><w:del w:id=\"1\" w:author=\"A\"><w:r><w:t>old</w:t></w:r></w:del>" +
                             "<w:moveFrom w:id=\"2\" w:author=\"A\"><w:r><w:t>moved</w:t></w:r></w:moveFrom></w:sdtContent></w:sdt></w:p>");

        Assert.Equal("keep", Assert.IsType<OpaqueInline>(Assert.Single(paragraph.Inlines)).DisplayText);
    }

    [Fact]
    public void Reader_HyperlinkContainingComplexField_OneFieldTokenWithLink()
    {
        var paragraph = Para("<w:p><w:hyperlink w:anchor=\"_Toc1\" w:history=\"1\"><w:r><w:t>Intro</w:t></w:r><w:r><w:tab/></w:r>" +
                             "<w:r><w:fldChar w:fldCharType=\"begin\"/></w:r><w:r><w:instrText xml:space=\"preserve\"> PAGEREF _Toc1 \\h </w:instrText></w:r>" +
                             "<w:r><w:fldChar w:fldCharType=\"separate\"/></w:r><w:r><w:t>1</w:t></w:r><w:r><w:fldChar w:fldCharType=\"end\"/></w:r></w:hyperlink></w:p>");

        Assert.Equal("Intro\t" + (char)0xFFFC, paragraph.Text);
        var field = Assert.IsType<OpaqueInline>(paragraph.Inlines[2]);
        Assert.Equal(("field", "PAGEREF _Toc1 \\h", "1"), (field.Kind, field.Instruction, field.DisplayText));
        Assert.Equal(5, field.Sources.Count);
        Assert.Equal("_Toc1", field.Link.Anchor);
        Assert.All(paragraph.Inlines, i => Assert.Same(field.Link, i.Link));
    }

    [Fact]
    public void Reader_3000Paragraphs_UnderTwoSeconds()
    {
        var body = string.Concat(Enumerable.Range(0, 3000).Select(i =>
            $"<w:p><w:pPr><w:spacing w:after=\"120\"/></w:pPr><w:r><w:t xml:space=\"preserve\">Paragraph {i} with </w:t></w:r><w:r><w:rPr><w:b/></w:rPr><w:t>bold</w:t></w:r>" +
            "<w:proofErr w:type=\"spellStart\"/><w:r><w:t xml:space=\"preserve\"> text.</w:t></w:r></w:p>"));
        var bytes = new DocxBuilder().Body(body).Build();

        var watch = Stopwatch.StartNew();
        var document = TextDocument.Load(new MemoryStream(bytes));
        watch.Stop();

        Assert.Equal(3000, document.Blocks.Count);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2), $"Reading took {watch.Elapsed}.");
    }
}
