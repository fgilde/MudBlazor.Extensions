using System.Xml.Linq;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public partial class DocxWriterTests
{
    private const string Field =
        "<w:r><w:t xml:space=\"preserve\">Lead </w:t></w:r><w:r><w:fldChar w:fldCharType=\"begin\"/></w:r><w:r><w:instrText xml:space=\"preserve\"> PAGE </w:instrText></w:r>" +
        "<w:r><w:fldChar w:fldCharType=\"separate\"/></w:r><w:r><w:t>1</w:t></w:r><w:r><w:fldChar w:fldCharType=\"end\"/><w:t xml:space=\"preserve\"> after</w:t></w:r>";

    [Fact]
    public void Writer_RunSharedByFieldAndText_EditElsewhere_WrittenOnce()
    {
        var document = new DocxBuilder().Body($"<w:p>{Field}</w:p>").Load();
        var id = document.Blocks[0].Id;

        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 4), "Head"));
        var output = Save(document);

        var paragraph = Body(output).Element(W + "p")!;
        Assert.Single(paragraph.Descendants(W + "fldChar"), f => (string)f.Attribute(W + "fldCharType") == "end");
        Assert.Single(paragraph.Descendants(W + "t"), t => t.Value == " after");
        Assert.Equal("Head 1 after", Text(paragraph));
        Assert.Equal(DocumentDescriber.Describe(document), DocumentDescriber.Describe(Load(output)));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_EditTextSharingFieldEndRun_OldTextNotWritten()
    {
        var document = new DocxBuilder().Body($"<w:p>{Field}</w:p>").Load();
        var id = document.Blocks[0].Id;
        Assert.Equal(" after", document.TextOf(Range(document, id, 6, id, 12)));

        document.Execute(new ReplaceTextCommand(Range(document, id, 7, id, 12), "later"));
        var output = Save(document);

        var paragraph = Body(output).Element(W + "p")!;
        Assert.DoesNotContain("after", paragraph.Value);
        Assert.Single(paragraph.Descendants(W + "fldChar"), f => (string)f.Attribute(W + "fldCharType") == "end");
        Assert.Equal("Lead 1 later", Text(paragraph));
        Assert.Equal(DocumentDescriber.Describe(document), DocumentDescriber.Describe(Load(output)));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_BoldOverField_EveryFieldRunBold_ItalicResultKept()
    {
        var document = new DocxBuilder().Body(
            "<w:p><w:r><w:fldChar w:fldCharType=\"begin\"/></w:r><w:r><w:instrText xml:space=\"preserve\"> NUMPAGES </w:instrText></w:r><w:r><w:fldChar w:fldCharType=\"separate\"/></w:r>" +
            "<w:r><w:t>2</w:t></w:r><w:r><w:rPr><w:i/></w:rPr><w:t>3</w:t></w:r><w:r><w:fldChar w:fldCharType=\"end\"/></w:r><w:r><w:t xml:space=\"preserve\"> pages</w:t></w:r></w:p>").Load();
        var id = document.Blocks[0].Id;

        document.Execute(new SetRunFormatCommand(Range(document, id, 0, id, ((Paragraph)document.Blocks[0]).Length), f => f with { Bold = true }, "Bold"));
        var output = Save(document);

        var runs = Body(output).Descendants(W + "r").ToList();
        Assert.Equal(7, runs.Count);
        Assert.All(runs, r => Assert.NotNull(r.Element(W + "rPr")?.Element(W + "b")));
        Assert.NotNull(runs.Single(r => r.Value == "3").Element(W + "rPr")!.Element(W + "i"));
        Assert.Equal(DocumentDescriber.Describe(document), DocumentDescriber.Describe(Load(output)));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_FormatOverOpaqueElements_PatchesTheirRuns()
    {
        var document = new DocxBuilder().Body(
            "<w:p><w:r><w:t>a</w:t></w:r><w:fldSimple w:instr=\" DATE \"><w:r><w:rPr><w:i/></w:rPr><w:t>today</w:t></w:r></w:fldSimple>" +
            "<w:sdt><w:sdtPr><w:id w:val=\"1\"/></w:sdtPr><w:sdtContent><w:r><w:t>c</w:t></w:r></w:sdtContent></w:sdt>" +
            "<w:smartTag w:uri=\"urn:x\" w:element=\"place\"><w:r><w:t>Paris</w:t></w:r></w:smartTag><m:oMath><m:r><m:t>x</m:t></m:r></m:oMath></w:p>").Load();
        var id = document.Blocks[0].Id;

        document.Execute(new SetRunFormatCommand(Range(document, id, 0, id, 5), f => f with { Bold = true }, "Bold"));
        var paragraph = Body(Save(document)).Element(W + "p")!;

        Assert.All(paragraph.Descendants().Where(e => e.Name.LocalName == "r"), r => Assert.NotNull(r.Element(W + "rPr")?.Element(W + "b")));
        Assert.NotNull(paragraph.Element(W + "fldSimple")!.Descendants(W + "i").SingleOrDefault());
        Assert.Equal(["r", "fldSimple", "sdt", "smartTag", "oMath"], paragraph.Elements().Select(e => e.Name.LocalName));
    }

    [Fact]
    public void Writer_TypedIntoRunWithUnknownProperties_KeepsThem()
    {
        var source = new DocxBuilder().Body(
            "<w:p><w:r><w:rPr><w:rFonts w:eastAsia=\"MS Mincho\"/><w:rtl/><w:lang w:val=\"ar-SA\" w:bidi=\"ar-SA\"/></w:rPr><w:t>abc</w:t></w:r></w:p>").Build();
        var document = Load(source);
        var id = document.Blocks[0].Id;

        document.Execute(new ReplaceTextCommand(Range(document, id, 3, id, 3), "def"));
        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 0), "X"));
        var output = Save(document);

        var runs = Body(output).Descendants(W + "r").ToList();
        Assert.Equal("Xabcdef", string.Concat(runs.Select(r => r.Value)));
        Assert.All(runs, r => Assert.True(XNode.DeepEquals(Body(source).Descendants(W + "rPr").Single(), r.Element(W + "rPr"))));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_SplitParagraphWithBidi_BothKeepPPr_OnlyFirstKeepsParaId()
    {
        var source = new DocxBuilder().Body(
            "<w:p><w:pPr><w:pBdr><w:top w:val=\"single\" w:sz=\"4\" w:space=\"1\" w:color=\"auto\"/></w:pBdr><w:bidi/></w:pPr><w:r><w:t>abcd</w:t></w:r></w:p>").Build();
        var document = Load(source);

        document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[0].Id, 2)));
        var output = Save(document);

        var paragraphs = Body(output).Elements(W + "p").ToList();
        Assert.All(paragraphs, p => Assert.True(XNode.DeepEquals(Body(source).Descendants(W + "pPr").Single(), p.Element(W + "pPr"))));
        Assert.NotNull(paragraphs[0].Attribute(W14 + "paraId"));
        Assert.Null(paragraphs[1].Attribute(W14 + "paraId"));
        Assert.Null(paragraphs[1].Attribute(W + "rsidR"));
        Assert.Equal(["ab", "cd"], paragraphs.Select(Text));
        AssertConsistent(output);

        document.Undo.Undo();
        Assert.True(OpcAssert.Parts(source)[MainPart].AsSpan().SequenceEqual(OpcAssert.Parts(Save(document))[MainPart]));
    }

    [Fact]
    public void Writer_MacroEnabled_ContentTypeFamilyAndVbaPartKept()
    {
        byte[] vba = [1, 2, 3, 4];
        const string docm = "application/vnd.ms-word.document.macroEnabled.main+xml";
        var source = new DocxBuilder { MainContentType = docm }
            .Extra("/word/vbaProject.bin", vba, "application/vnd.ms-office.vbaProject")
            .RawRelationship("<Relationship Id=\"rIdVba\" Type=\"http://schemas.microsoft.com/office/2006/relationships/vbaProject\" Target=\"vbaProject.bin\"/>")
            .Body("<w:p><w:r><w:t>macro</w:t></w:r></w:p>").Build();
        var document = Load(source);
        var id = document.Blocks[0].Id;
        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 1), "M"));

        using var asDocx = new MemoryStream();
        document.Save(asDocx);
        using var asDotx = new MemoryStream();
        document.Save(asDotx, DocumentFileFormat.Dotx);

        Assert.Equal(docm, OpcPackage.Open(asDocx.ToArray()).ContentType(MainPart));
        Assert.Equal(vba, OpcAssert.Parts(asDocx.ToArray())["/word/vbaProject.bin"]);
        Assert.Equal("application/vnd.ms-word.template.macroEnabledTemplate.main+xml", OpcPackage.Open(asDotx.ToArray()).ContentType(MainPart));
        Assert.True(Load(asDotx.ToArray()).IsTemplate);
        AssertConsistent(asDocx.ToArray());
    }

    [Fact]
    public void Writer_ChangedLinkTarget_NewRelationshipKeepsAttributes()
    {
        var document = new DocxBuilder().Link("rId20", "https://old.example")
            .Body("<w:p><w:hyperlink r:id=\"rId20\" w:history=\"1\" w:tgtFrame=\"_blank\"><w:r><w:t>link</w:t></w:r></w:hyperlink></w:p>").Load();
        var paragraph = (Paragraph)document.Blocks[0];
        var link = paragraph.Inlines[0].Link! with { Target = "https://new.example" };

        document.ApplyTyping(paragraph.Id, paragraph.Inlines.Select(i => i with { Link = link }).ToList(), DateTimeOffset.UnixEpoch);
        var output = Save(document);

        var element = Body(output).Descendants(W + "hyperlink").Single();
        Assert.Equal("_blank", (string)element.Attribute(W + "tgtFrame"));
        Assert.Equal("1", (string)element.Attribute(W + "history"));
        Assert.Equal("https://new.example", OpcPackage.Open(output).Relationships(MainPart).Single(r => r.Id == (string)element.Attribute(R + "id")).Target);
        Assert.Equal("https://new.example", Load(output).Blocks.OfType<Paragraph>().Single().Inlines[0].Link!.Target);
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_RawRunTextWithControlCharacters_WrittenAsElements()
    {
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;

        document.ApplyTyping(id, [new TextRun("a\tb\nc\vd\fe" + (char)0xFFFC + "f\r\ng")], DateTimeOffset.UnixEpoch);
        var output = Save(document);

        var run = Body(output).Descendants(W + "r").Single();
        Assert.Equal(["t:a", "tab:", "t:b", "br:", "t:c", "br:", "t:d", "br:page", "t:ef", "br:", "t:g"],
            run.Elements().Where(e => e.Name != W + "rPr").Select(e => $"{e.Name.LocalName}:{(e.Name == W + "t" ? e.Value : (string)e.Attribute(W + "type"))}"));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_DeletedContentMarkers_KeptInPlace_MoveRangesDropped()
    {
        var document = new DocxBuilder().Body(
            "<w:p><w:r><w:t>keep</w:t></w:r><w:del w:id=\"2\" w:author=\"A\"><w:bookmarkStart w:id=\"1\" w:name=\"b\"/><w:r><w:delText>x</w:delText></w:r></w:del>" +
            "<w:r><w:t>more</w:t></w:r><w:bookmarkEnd w:id=\"1\"/><w:moveFromRangeStart w:id=\"5\" w:author=\"A\" w:name=\"m\"/>" +
            "<w:moveFrom w:id=\"6\" w:author=\"A\"><w:r><w:t>moved</w:t></w:r></w:moveFrom><w:moveFromRangeEnd w:id=\"5\"/></w:p>").Load();
        var id = document.Blocks[0].Id;
        Assert.Equal("keepmore", ((Paragraph)document.Blocks[0]).Text);

        document.Execute(new ReplaceTextCommand(Range(document, id, 3, id, 4), "t"));
        var output = Save(document);

        var paragraph = Body(output).Element(W + "p")!;
        Assert.Equal(["r", "bookmarkStart", "r", "bookmarkEnd"], paragraph.Elements().Select(e => e.Name.LocalName));
        Assert.Equal("keetmore", Text(paragraph));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_PageSizeChanged_RemovesPaperCode_MarginsOnlyKeepIt()
    {
        var document = new DocxBuilder().Body(
            "<w:p/><w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\" w:code=\"9\"/><w:pgMar w:top=\"1417\" w:right=\"1417\" w:bottom=\"1134\" w:left=\"1417\" w:header=\"708\" w:footer=\"708\" w:gutter=\"0\"/></w:sectPr>").Load();
        var page = document.Sections[^1].Page;

        document.SectionList[^1] = document.SectionList[^1] with { Page = page with { MarginTop = 1000 } };
        var margins = Body(Save(document)).Element(W + "sectPr")!.Element(W + "pgSz")!;
        document.SectionList[^1] = document.SectionList[^1] with { Page = PageSetup.Letter };
        var resized = Body(Save(document)).Element(W + "sectPr")!.Element(W + "pgSz")!;

        Assert.Equal("9", (string)margins.Attribute(W + "code"));
        Assert.Null(resized.Attribute(W + "code"));
        Assert.Equal("12240", (string)resized.Attribute(W + "w"));
    }
}
