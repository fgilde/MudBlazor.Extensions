using System.Xml.Linq;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public partial class DocxWriterTests
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace W14 = "http://schemas.microsoft.com/office/word/2010/wordml";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace Wp = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
    private const string MainPart = "/word/document.xml";

    private static string SamplePath => Path.Combine(AppContext.BaseDirectory, "TestData", "Document", "sample.docx");

    [Fact]
    public void Writer_Unmodified_EveryPartByteIdentical()
    {
        foreach (var source in new[] { File.ReadAllBytes(SamplePath), Rich().Build(), Save(TextDocument.Create()) })
        {
            var output = Save(Load(source));

            var before = OpcAssert.Parts(source);
            var after = OpcAssert.Parts(output);
            Assert.Equal(before.Keys.Order(), after.Keys.Order());
            foreach (var (part, bytes) in before) Assert.True(bytes.AsSpan().SequenceEqual(after[part]), part);
            AssertConsistent(output);
        }
    }

    [Fact]
    public void Writer_EditedParagraph_AllOtherBodyChildrenXmlIdentical()
    {
        var source = Rich().Build();
        var document = Load(source);
        var edited = document.Blocks[1].Id;

        document.Execute(new ReplaceTextCommand(Range(document, edited, 0, edited, 5), "Changed"));
        var output = Save(document);

        var before = Body(source).Elements().ToList();
        var after = Body(output).Elements().ToList();
        Assert.Equal(before.Count, after.Count);
        for (var i = 0; i < before.Count; i++)
            if (i != 1)
                Assert.True(XNode.DeepEquals(before[i], after[i]), $"body child {i}");
        Assert.Equal("Changed paragraph", Text(after[1]));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_EditThenUndo_DocumentPartByteIdentical()
    {
        var source = Rich().Build();
        var document = Load(source);
        var id = document.Blocks[1].Id;
        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 6), "Changed"));
        document.Execute(new SetRunFormatCommand(Range(document, id, 0, id, 3), f => f with { Bold = true }, "Bold"));
        var header = document.HeadersFooters[0].Blocks[0].Id;
        document.Execute(new ReplaceTextCommand(Range(document, header, 0, header, 1), "X"));

        document.Undo.Undo();
        document.Undo.Undo();
        document.Undo.Undo();
        var output = Save(document);

        var before = OpcAssert.Parts(source);
        var after = OpcAssert.Parts(output);
        foreach (var (part, bytes) in before) Assert.True(bytes.AsSpan().SequenceEqual(after[part]), part);
    }

    [Fact]
    public void Writer_NestedCellEditThenUndo_DocumentXmlIdentical()
    {
        var source = Rich().Build();
        var document = Load(source);
        var inner = document.AllParagraphs().Single(p => p.Text == "inner");

        document.Execute(new ReplaceTextCommand(Range(document, inner.Id, 0, inner.Id, 2), "ON"));
        var edited = Save(document);
        document.Undo.Undo();
        var output = Save(document);

        Assert.Contains("ONner", Body(edited).Value);
        Assert.True(XNode.DeepEquals(OpcAssert.Xml(source, MainPart), OpcAssert.Xml(output, MainPart)));
        AssertConsistent(output);
        AssertConsistent(edited);
    }

    [Fact]
    public void Writer_EditMiddleOfParagraphWithUnknownChildren_KeepsThemInPlace()
    {
        var source = new DocxBuilder().Body(
            "<w:p w14:noSpellErr=\"1\"><w:pPr><w:pBdr><w:top w:val=\"single\" w:sz=\"4\" w:space=\"1\" w:color=\"auto\"/></w:pBdr><w:jc w:val=\"center\"/></w:pPr>" +
            "<w:r><w:t xml:space=\"preserve\">Start </w:t></w:r>" +
            "<w:smartTag w:uri=\"urn:x\" w:element=\"place\"><w:r><w:t>Paris</w:t></w:r></w:smartTag>" +
            "<m:oMath><m:r><m:t>x=1</m:t></m:r></m:oMath>" +
            "<w:permStart w:id=\"7\" w:edGrp=\"everyone\"/>" +
            "<w:r><w:t xml:space=\"preserve\"> edit me </w:t></w:r>" +
            "<w:permEnd w:id=\"7\"/>" +
            "<w:sdt><w:sdtPr><w:id w:val=\"5\"/></w:sdtPr><w:sdtContent><w:r><w:t>control</w:t></w:r></w:sdtContent></w:sdt>" +
            "<w:r><w:t xml:space=\"preserve\"> end</w:t></w:r></w:p>").Build();
        var document = Load(source);
        var paragraph = (Paragraph)document.Blocks[0];
        var inlines = paragraph.Inlines.Select(i => i is TextRun { Text: " edit me " } run ? run with { Text = " changed me " } : i).ToList();

        document.ApplyTyping(paragraph.Id, inlines, DateTimeOffset.UnixEpoch);
        var output = Save(document);

        var before = Body(source).Element(W + "p")!;
        var after = Body(output).Element(W + "p")!;
        Assert.Equal(before.Attributes().Select(a => a.ToString()), after.Attributes().Select(a => a.ToString()));
        Assert.Equal(before.Elements().Select(e => e.Name), after.Elements().Select(e => e.Name));
        foreach (var (b, a) in before.Elements().Zip(after.Elements()))
            if (b.Name != W + "r" || !b.Value.Contains("edit"))
                Assert.True(XNode.DeepEquals(b, a), b.Name.LocalName);
        Assert.Equal(" changed me ", after.Elements(W + "r").ElementAt(1).Value);
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_RunEdit_KeepsUnknownRPrChildren()
    {
        var source = new DocxBuilder().Body(
            "<w:p><w:r><w:rPr><w:rFonts w:ascii=\"Arial\" w:hAnsi=\"Arial\" w:cs=\"Arial\"/><w:b/><w:lang w:val=\"de-DE\"/><w14:ligatures w14:val=\"standardContextual\"/></w:rPr>" +
            "<w:t>Hello</w:t></w:r></w:p>").Build();
        var document = Load(source);
        var id = document.Blocks[0].Id;

        var run = (TextRun)((Paragraph)document.Blocks[0]).Inlines[0];
        document.ApplyTyping(id, [run with { Text = "Hello there" }], DateTimeOffset.UnixEpoch);
        var output = Save(document);

        var written = Body(output).Descendants(W + "r").Single();
        Assert.True(XNode.DeepEquals(Body(source).Descendants(W + "rPr").Single(), written.Element(W + "rPr")));
        Assert.Equal("Hello there", written.Element(W + "t")!.Value);
        Assert.Equal("00D4E5F6", (string)written.Attribute(W + "rsidRPr"));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_SplitRunByFormat_ThreeRunsKeepUnknownRPr_ToggleBackRestoresOriginalRun()
    {
        var source = new DocxBuilder().Body(
            "<w:p><w:r><w:rPr><w:i/><w:lang w:val=\"de-DE\"/><w14:ligatures w14:val=\"standardContextual\"/></w:rPr><w:lastRenderedPageBreak/><w:t>one two three</w:t></w:r></w:p>").Build();
        var document = Load(source);
        var id = document.Blocks[0].Id;
        var middle = Range(document, id, 4, id, 7);

        document.Execute(new SetRunFormatCommand(middle, f => f with { Bold = true }, "Bold"));
        var split = Save(document);
        document.Execute(new SetRunFormatCommand(middle, f => f with { Bold = null }, "Bold"));
        var back = Save(document);

        var runs = Body(split).Descendants(W + "r").ToList();
        Assert.Equal(["one ", "two", " three"], runs.Select(r => r.Value));
        Assert.All(runs, r => Assert.NotNull(r.Element(W + "rPr")!.Element(W + "lang")));
        Assert.All(runs, r => Assert.NotNull(r.Element(W + "rPr")!.Element(W14 + "ligatures")));
        Assert.Equal([false, true, false], runs.Select(r => r.Element(W + "rPr")!.Element(W + "b") != null));
        Assert.Equal(["b", "i", "lang", "ligatures"], runs[1].Element(W + "rPr")!.Elements().Select(e => e.Name.LocalName));
        Assert.True(XNode.DeepEquals(Body(source).Descendants(W + "r").Single(), Body(back).Descendants(W + "r").Single()));
        AssertConsistent(split);
    }

    [Fact]
    public void Writer_DeleteInsideRun_SameSourceNewText_Written()
    {
        var document = new DocxBuilder().Body("<w:p><w:r><w:t>one two three</w:t></w:r></w:p>").Load();
        var id = document.Blocks[0].Id;

        document.Execute(new ReplaceTextCommand(Range(document, id, 4, id, 7), ""));

        var output = Save(document);

        Assert.Equal("one  three", Body(output).Descendants(W + "t").Single().Value);
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_EditedParagraphWithTrackedChanges_FinalState_UntouchedKeepMarks()
    {
        const string tracked =
            "<w:p><w:pPr><w:rPr><w:ins w:id=\"1\" w:author=\"A\" w:date=\"2026-01-01T00:00:00Z\"/></w:rPr><w:pPrChange w:id=\"2\" w:author=\"A\"><w:pPr/></w:pPrChange></w:pPr>" +
            "<w:r><w:t xml:space=\"preserve\">Keep </w:t></w:r>" +
            "<w:ins w:id=\"3\" w:author=\"A\"><w:r><w:t xml:space=\"preserve\">inserted </w:t></w:r></w:ins>" +
            "<w:del w:id=\"4\" w:author=\"A\"><w:r><w:delText xml:space=\"preserve\">gone </w:delText></w:r></w:del>" +
            "<w:moveFrom w:id=\"5\" w:author=\"A\"><w:r><w:t>away</w:t></w:r></w:moveFrom>" +
            "<w:moveTo w:id=\"6\" w:author=\"A\"><w:r><w:t xml:space=\"preserve\">here </w:t></w:r></w:moveTo>" +
            "<w:dir w:val=\"rtl\"><w:r><w:t xml:space=\"preserve\">rtl </w:t></w:r></w:dir>" +
            "<w:bdo w:val=\"rtl\"><w:r><w:t>bdo</w:t></w:r></w:bdo>" +
            "<w:r><w:rPr><w:b/><w:rPrChange w:id=\"7\" w:author=\"A\"><w:rPr/></w:rPrChange></w:rPr><w:t>!</w:t></w:r></w:p>";
        var source = new DocxBuilder().Body(tracked + tracked).Build();
        var document = Load(source);
        var id = document.Blocks[0].Id;
        Assert.Equal("Keep inserted here rtl bdo!", ((Paragraph)document.Blocks[0]).Text);

        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 4), "Kept"));
        var output = Save(document);

        var edited = Body(output).Elements(W + "p").First();
        Assert.Equal("Kept inserted here rtl bdo!", Text(edited));
        Assert.DoesNotContain(edited.Descendants(), e => e.Name.LocalName is "ins" or "del" or "moveFrom" or "moveTo" or "rPrChange" or "pPrChange" or "delText");
        Assert.Equal("rtl ", edited.Element(W + "dir")!.Value);
        Assert.Equal("bdo", edited.Element(W + "bdo")!.Value);
        Assert.NotNull(edited.Elements(W + "r").Last().Element(W + "rPr")!.Element(W + "b"));
        Assert.True(XNode.DeepEquals(Body(source).Elements(W + "p").Last(), Body(output).Elements(W + "p").Last()));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_SplitParagraph_NewHalfHasNoParaId()
    {
        var source = new DocxBuilder().Body("<w:p><w:r><w:t>Hello World</w:t></w:r></w:p>").Build();
        var document = Load(source);

        document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[0].Id, 5)));
        var output = Save(document);

        var paragraphs = Body(output).Elements(W + "p").ToList();
        Assert.Equal(["Hello", " World"], paragraphs.Select(Text));
        Assert.Equal((string)Body(source).Element(W + "p")!.Attribute(W14 + "paraId"), (string)paragraphs[0].Attribute(W14 + "paraId"));
        Assert.Null(paragraphs[1].Attribute(W14 + "paraId"));
        Assert.Null(paragraphs[1].Attribute(W14 + "textId"));
        Assert.Equal("preserve", (string)paragraphs[1].Descendants(W + "t").Single().Attribute(XNamespace.Xml + "space"));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_SplitAtSectionEnd_NewParagraphCarriesSectPr()
    {
        var source = Sections().Build();
        var document = Load(source);
        var first = (Paragraph)document.Blocks[0];

        document.Execute(new SplitParagraphCommand(document.PositionOf(first.Id, first.Length)));
        var output = Save(document);

        var paragraphs = Body(output).Elements(W + "p").ToList();
        Assert.Null(paragraphs[0].Element(W + "pPr")?.Element(W + "sectPr"));
        Assert.True(XNode.DeepEquals(Body(source).Element(W + "p")!.Descendants(W + "sectPr").Single(), paragraphs[1].Element(W + "pPr")!.Element(W + "sectPr")));
        AssertConsistent(output);
        Assert.Equal(DocumentDescriber.Describe(document), DocumentDescriber.Describe(Load(output)));
    }

    [Fact]
    public void Writer_MergeAcrossSectionEnd_SingleSectPrPerParagraph()
    {
        var source = Sections().Build();
        var document = Load(source);

        document.Execute(new MergeParagraphsCommand(document.Blocks[0].Id));
        var output = Save(document);

        var paragraphs = Body(output).Elements(W + "p").ToList();
        Assert.Equal(2, paragraphs.Count);
        Assert.Equal("FirstSecond", Text(paragraphs[0]));
        Assert.Single(paragraphs[0].Element(W + "pPr")!.Elements(W + "sectPr"));
        Assert.True(XNode.DeepEquals(Body(source).Elements(W + "p").ElementAt(1).Descendants(W + "sectPr").Single(), paragraphs[0].Descendants(W + "sectPr").Single()));
        AssertConsistent(output);
        Assert.Equal(DocumentDescriber.Describe(document), DocumentDescriber.Describe(Load(output)));
    }

    [Fact]
    public void Writer_SectionPageSetup_PatchesPgSzPgMar_KeepsDocGridAndCols()
    {
        var builder = new DocxBuilder { SectionProperties = "<w:cols w:space=\"708\"/><w:docGrid w:linePitch=\"360\"/>" }.Body("<w:p><w:r><w:t>x</w:t></w:r></w:p>");
        var source = builder.Build();
        var document = Load(source);
        var page = new PageSetup(16838, 11906, PageOrientation.Landscape, 1000, 1100, 1200, 1300, 600, 650, 10);

        document.SectionList[^1] = document.SectionList[^1] with { Page = page, TitlePage = true };
        var output = Save(document);

        var before = Body(source).Element(W + "sectPr")!;
        var after = Body(output).Element(W + "sectPr")!;
        Assert.Equal(["pgSz", "pgMar", "cols", "titlePg", "docGrid"], after.Elements().Select(e => e.Name.LocalName));
        Assert.Equal("landscape", (string)after.Element(W + "pgSz")!.Attribute(W + "orient"));
        Assert.Equal("16838", (string)after.Element(W + "pgSz")!.Attribute(W + "w"));
        Assert.Equal("10", (string)after.Element(W + "pgMar")!.Attribute(W + "gutter"));
        Assert.True(XNode.DeepEquals(before.Element(W + "docGrid"), after.Element(W + "docGrid")));
        Assert.True(XNode.DeepEquals(before.Element(W + "cols"), after.Element(W + "cols")));
        Assert.Equal(before.Attributes().Select(a => a.ToString()), after.Attributes().Select(a => a.ToString()));
        Assert.Equal(page, Load(output).Sections[^1].Page);
        Assert.True(Load(output).Sections[^1].TitlePage);
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_NewHyperlink_ExternalRelAndOneHyperlinkElement()
    {
        var document = new DocxBuilder().Body("<w:p><w:r><w:t xml:space=\"preserve\">Hello </w:t></w:r><w:r><w:rPr><w:b/></w:rPr><w:t>World</w:t></w:r></w:p>").Load();
        var id = document.Blocks[0].Id;

        document.Execute(new SetHyperlinkCommand(Range(document, id, 3, id, 9), "https://example.org/a?b=c&d"));
        var output = Save(document);

        var link = Assert.Single(Body(output).Descendants(W + "hyperlink"));
        Assert.Equal("lo Wor", link.Value);
        Assert.Equal(2, link.Elements(W + "r").Count());
        var rel = OpcPackage.Open(output).Relationships(MainPart).Single(r => r.Id == (string)link.Attribute(R + "id"));
        Assert.Equal("https://example.org/a?b=c&d", rel.Target);
        Assert.Equal("External", rel.TargetMode);
        Assert.EndsWith("/hyperlink", rel.Type);
        var reread = (Paragraph)Load(output).Blocks[0];
        Assert.Equal("https://example.org/a?b=c&d", reread.Inlines.Single(i => i.Link != null && i is TextRun { Text: "lo " }).Link!.Target);
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_LeadingSpace_PreserveAttribute()
    {
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;

        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 0), " lead\na  b\nplain\ntrail "));
        var output = Save(document);
        var texts = Body(output).Descendants(W + "t").ToList();

        Assert.Equal([" lead", "a  b", "plain", "trail "], texts.Select(t => t.Value));
        Assert.Equal(["preserve", "preserve", null, "preserve"], texts.Select(t => (string)t.Attribute(XNamespace.Xml + "space")));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_ControlCharacters_Dropped()
    {
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;

        document.ApplyTyping(id, [new TextRun("a\u0001b\uD800c\uFFFEd\uD83D\uDE00")], DateTimeOffset.UnixEpoch);
        var output = Save(document);

        Assert.Equal("abcd\uD83D\uDE00", Body(output).Descendants(W + "t").Single().Value);
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_DotxAsDocx_ChangesMainContentType()
    {
        var template = new DocxBuilder { Template = true }.Body("<w:p><w:r><w:t>x</w:t></w:r></w:p>").Build();
        var document = TextDocument.Load(new MemoryStream(template), DocumentFileFormat.Dotx);
        Assert.True(document.IsTemplate);

        var kept = Save(document);
        using var stream = new MemoryStream();
        document.Save(stream);
        var asDocx = stream.ToArray();

        Assert.True(OpcAssert.Parts(template)["/[Content_Types].xml"].AsSpan().SequenceEqual(OpcAssert.Parts(kept)["/[Content_Types].xml"]));
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml", OpcPackage.Open(asDocx).ContentType(MainPart));
        Assert.False(Load(asDocx).IsTemplate);
        Assert.True(OpcAssert.Parts(template)[MainPart].AsSpan().SequenceEqual(OpcAssert.Parts(asDocx)[MainPart]));
        using var dotx = new MemoryStream();
        Load(asDocx).Save(dotx, DocumentFileFormat.Dotx);
        Assert.True(Load(dotx.ToArray()).IsTemplate);
        AssertConsistent(asDocx);
    }

    [Fact]
    public void Writer_RoundTrip_ReadBackEqualsModel()
    {
        var document = TextDocument.Load(File.OpenRead(SamplePath));
        var first = document.Blocks[0].Id;
        document.Execute(new ReplaceTextCommand(Range(document, first, 0, first, 6), "Example"));
        document.Execute(new SetRunFormatCommand(Range(document, first, 2, first, 5), f => f with { Bold = true, Font = "theme:minorHAnsi", Color = "theme:accent1 shade=BF" }, "Format"));
        document.Execute(new SplitParagraphCommand(document.PositionOf(first, 4)));
        var second = document.Blocks[1].Id;
        document.Execute(new SetParagraphFormatCommand([second], f => f with
        {
            Alignment = ParagraphAlignment.Center, IndentFirstLine = -360, IndentLeft = 720, SpacingBefore = 120, Line = new LineSpacing(360, LineRule.Auto),
            Tabs = [new TabStop(1440, TabAlignment.Right, "dot")], Shading = "#FFEE00", KeepWithNext = false
        }, "Paragraph"));
        document.Execute(new SetHyperlinkCommand(Range(document, second, 1, second, 3), "#Top"));
        document.Execute(new InsertBreakCommand(document.PositionOf(second, 2), BreakKind.Page));
        document.ApplyTyping(second, [.. ((Paragraph)document.FindBlock(second)).Inlines, new TextRun(" typed") { Format = new RunFormat { Underline = "double", SizePt = 10.5, Highlight = "yellow" } }], DateTimeOffset.UnixEpoch);
        document.Execute(new MergeParagraphsCommand(first));
        var output = Save(document);

        Assert.Equal(DocumentDescriber.Describe(document), DocumentDescriber.Describe(Load(output)));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_ThemeFont_WrittenAsThemeAttributes()
    {
        var document = new DocxBuilder().Body("<w:p><w:r><w:rPr><w:rFonts w:ascii=\"Arial\" w:hAnsi=\"Arial\" w:cs=\"Arial\"/></w:rPr><w:t>x</w:t></w:r></w:p>").Load();
        var id = document.Blocks[0].Id;

        document.Execute(new SetRunFormatCommand(Range(document, id, 0, id, 1), f => f with { Font = "theme:minorHAnsi" }, "Font"));
        var output = Save(document);
        var fonts = Body(output).Descendants(W + "rFonts").Single();

        Assert.Equal("minorHAnsi", (string)fonts.Attribute(W + "asciiTheme"));
        Assert.Equal("minorHAnsi", (string)fonts.Attribute(W + "hAnsiTheme"));
        Assert.Equal("Arial", (string)fonts.Attribute(W + "cs"));
        Assert.DoesNotContain(fonts.Attributes(), a => a.Value.StartsWith("theme:", StringComparison.Ordinal));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_NumPrWithoutNumId_KeptWhenParagraphEdited()
    {
        var source = new DocxBuilder().Body("<w:p><w:pPr><w:numPr><w:ilvl w:val=\"1\"/></w:numPr></w:pPr><w:r><w:t>item</w:t></w:r></w:p>").Build();
        var document = Load(source);
        var id = document.Blocks[0].Id;

        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 1), "I"));
        var output = Save(document);

        Assert.True(XNode.DeepEquals(Body(source).Descendants(W + "pPr").Single(), Body(output).Descendants(W + "pPr").Single()));
        Assert.Equal("Item", Text(Body(output).Element(W + "p")!));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_EditedHeader_PartRewritten_OtherPartsCopied()
    {
        var source = Rich().Build();
        var document = Load(source);
        var header = document.HeadersFooters[0];
        var id = header.Blocks[0].Id;

        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 6), "Kopf"));
        document.Execute(new SetHyperlinkCommand(Range(document, id, 0, id, 4), "https://example.org"));
        var output = Save(document);

        var part = "/word/header1.xml";
        Assert.Equal("Kopf text", OpcAssert.Xml(output, part).Root!.Value);
        Assert.Single(OpcPackage.Open(output).Relationships(part), r => r.Target == "https://example.org");
        var before = OpcAssert.Parts(source);
        var after = OpcAssert.Parts(output);
        foreach (var name in before.Keys.Where(p => p != part && p != "/word/_rels/header1.xml.rels" && p != "/word/styles.xml"))
            Assert.True(before[name].AsSpan().SequenceEqual(after[name]), name);
        Assert.Single(OpcAssert.Xml(output, "/word/styles.xml").Root!.Elements(W + "style"), s => (string)s.Attribute(W + "styleId") == "Hyperlink");
        AssertConsistent(output);
        Assert.Equal(DocumentDescriber.Describe(document), DocumentDescriber.Describe(Load(output)));
    }

    [Fact]
    public void Writer_ChangedProperties_PatchesCoreXmlOnly()
    {
        var source = Rich().Build();
        var document = Load(source);

        document.Properties = document.Properties with { Title = "New title", Modified = new DateTimeOffset(2026, 11, 22, 9, 30, 0, TimeSpan.Zero) };
        var output = Save(document);

        var core = OpcAssert.Xml(output, "/docProps/core.xml").Root!;
        Assert.Equal("New title", core.Elements().Single(e => e.Name.LocalName == "title").Value);
        Assert.Equal("Old author", core.Elements().Single(e => e.Name.LocalName == "creator").Value);
        Assert.Equal(document.Properties, Load(output).Properties);
        Assert.True(OpcAssert.Parts(source)[MainPart].AsSpan().SequenceEqual(OpcAssert.Parts(output)[MainPart]));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_InvalidArguments_Throw()
    {
        var document = TextDocument.Create();

        Assert.Throws<ArgumentNullException>(() => DocxWriter.Write(null!, new MemoryStream()));
        Assert.Throws<ArgumentNullException>(() => DocxWriter.Write(document, null!));
        Assert.Throws<ArgumentException>(() => DocxWriter.Write(document, new MemoryStream([], false)));
        Assert.Throws<NotSupportedException>(() => document.Save(new MemoryStream(), DocumentFileFormat.Rtf));
    }

    internal static byte[] Save(TextDocument document, bool? asTemplate = null)
    {
        using var stream = new MemoryStream();
        DocxWriter.Write(document, stream, asTemplate);
        var output = stream.ToArray();
        Assert.Equal(document.AllParagraphs(true).Select(Signature), Load(output).AllParagraphs(true).Select(Signature));
        return output;
    }

    private static TextDocument Load(byte[] bytes) => TextDocument.Load(new MemoryStream(bytes));

    private static XElement Body(byte[] package) => OpcAssert.Xml(package, MainPart).Root!.Element(W + "body")!;

    private static string Text(XElement paragraph) => string.Concat(paragraph.Descendants(W + "t").Select(t => t.Value));

    private static DocumentRange Range(TextDocument document, string startId, int start, string endId, int end) =>
        new(document.PositionOf(startId, start), document.PositionOf(endId, end));

    private static DocxBuilder Sections() => new DocxBuilder().Body(
        "<w:p><w:pPr><w:sectPr><w:type w:val=\"continuous\"/><w:pgSz w:w=\"12240\" w:h=\"15840\"/><w:pgMar w:top=\"1440\" w:right=\"1440\" w:bottom=\"1440\" w:left=\"1440\" w:header=\"720\" w:footer=\"720\" w:gutter=\"0\"/></w:sectPr></w:pPr><w:r><w:t>First</w:t></w:r></w:p>" +
        "<w:p><w:pPr><w:sectPr><w:pgSz w:w=\"16838\" w:h=\"11906\" w:orient=\"landscape\"/><w:pgMar w:top=\"1000\" w:right=\"1000\" w:bottom=\"1000\" w:left=\"1000\" w:header=\"700\" w:footer=\"700\" w:gutter=\"0\"/><w:cols w:num=\"2\" w:space=\"720\"/></w:sectPr></w:pPr><w:r><w:t>Second</w:t></w:r></w:p>" +
        "<w:p><w:r><w:t>Third</w:t></w:r></w:p>" +
        "<w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/><w:pgMar w:top=\"1417\" w:right=\"1417\" w:bottom=\"1134\" w:left=\"1417\" w:header=\"708\" w:footer=\"708\" w:gutter=\"0\"/></w:sectPr>");

    private static DocxBuilder Rich() => new DocxBuilder()
        .Styles("<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style>" +
                "<w:style w:type=\"paragraph\" w:styleId=\"Heading1\"><w:name w:val=\"heading 1\"/><w:basedOn w:val=\"Normal\"/></w:style>" +
                "<w:style w:type=\"table\" w:styleId=\"TableGrid\"><w:name w:val=\"Table Grid\"/></w:style>")
        .Numbering("<w:abstractNum w:abstractNumId=\"0\"><w:lvl w:ilvl=\"0\"><w:start w:val=\"1\"/><w:numFmt w:val=\"decimal\"/><w:lvlText w:val=\"%1.\"/></w:lvl></w:abstractNum>" +
                   "<w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\"/></w:num>")
        .Settings("<w:defaultTabStop w:val=\"720\"/>")
        .Theme()
        .CoreProperties("<dc:title>Old title</dc:title><dc:creator>Old author</dc:creator><dcterms:created xsi:type=\"dcterms:W3CDTF\">2020-01-01T00:00:00Z</dcterms:created>")
        .Header("<w:p><w:r><w:t>Header text</w:t></w:r></w:p>")
        .Footer("<w:p><w:r><w:t>Footer</w:t></w:r></w:p>")
        .Link("rId20", "https://example.com")
        .Extra("/customXml/item1.xml", "<root>unknown</root>"u8.ToArray(), "application/xml")
        .Body(
            "<w:p><w:pPr><w:pStyle w:val=\"Heading1\"/></w:pPr><w:bookmarkStart w:id=\"0\" w:name=\"Top\"/><w:r><w:t>Title</w:t></w:r><w:bookmarkEnd w:id=\"0\"/></w:p>" +
            "<w:p><w:pPr><w:numPr><w:ilvl w:val=\"0\"/><w:numId w:val=\"1\"/></w:numPr></w:pPr><w:r><w:t xml:space=\"preserve\">Plain </w:t></w:r><w:proofErr w:type=\"spellStart\"/>" +
            "<w:r><w:rPr><w:i/></w:rPr><w:t>paragraph</w:t></w:r><w:proofErr w:type=\"spellEnd\"/></w:p>" +
            "<w:tbl><w:tblPr><w:tblStyle w:val=\"TableGrid\"/><w:tblW w:w=\"0\" w:type=\"auto\"/></w:tblPr><w:tblGrid><w:gridCol w:w=\"4000\"/><w:gridCol w:w=\"4000\"/></w:tblGrid>" +
            "<w:tr><w:tc><w:tcPr><w:tcW w:w=\"4000\" w:type=\"dxa\"/></w:tcPr><w:p><w:r><w:t>a</w:t></w:r></w:p></w:tc>" +
            "<w:tc><w:tcPr><w:tcW w:w=\"4000\" w:type=\"dxa\"/></w:tcPr><w:p><w:r><w:t>b</w:t></w:r></w:p>" +
            "<w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/></w:tblPr><w:tblGrid><w:gridCol w:w=\"2000\"/></w:tblGrid><w:tr><w:tc><w:p><w:r><w:t>inner</w:t></w:r></w:p></w:tc></w:tr></w:tbl>" +
            "<w:p/></w:tc></w:tr></w:tbl>" +
            "<w:p><w:hyperlink r:id=\"rId20\" w:history=\"1\"><w:r><w:t>link</w:t></w:r></w:hyperlink></w:p>" +
            "<w:sdt><w:sdtPr><w:id w:val=\"9\"/></w:sdtPr><w:sdtContent><w:p><w:r><w:t>in control</w:t></w:r></w:p></w:sdtContent></w:sdt>" +
            "<w:p><w:r><w:t>Last</w:t></w:r></w:p>");
}
