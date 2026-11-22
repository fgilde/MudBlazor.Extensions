using System.Xml.Linq;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class StyleListCommandTests
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private const string StylesPart = "/word/styles.xml";
    private const string NumberingPart = "/word/numbering.xml";

    private const string Decimal =
        "<w:abstractNum w:abstractNumId=\"0\"><w:lvl w:ilvl=\"0\"><w:start w:val=\"1\"/><w:numFmt w:val=\"decimal\"/><w:lvlText w:val=\"%1.\"/>" +
        "<w:pPr><w:ind w:left=\"720\" w:hanging=\"360\"/></w:pPr></w:lvl></w:abstractNum>";

    private const string Bullet =
        "<w:abstractNum w:abstractNumId=\"3\"><w:lvl w:ilvl=\"0\"><w:start w:val=\"1\"/><w:numFmt w:val=\"bullet\"/><w:lvlText w:val=\"o\"/></w:lvl></w:abstractNum>";

    [Fact]
    public void ApplyStyle_BuiltInHeading2_AddsDefinitionChainAndLinkedStyle_RevertRemoves()
    {
        var source = new DocxBuilder()
            .Styles("<w:style w:type=\"character\" w:default=\"1\" w:styleId=\"DefaultParagraphFont\"><w:name w:val=\"Default Paragraph Font\"/><w:uiPriority w:val=\"1\"/></w:style>")
            .Body(Para("Title") + Para("Body"))
            .Build();
        var document = Load(source);
        var styles = document.Styles;
        var first = document.Blocks[0];
        var second = document.Blocks[1];

        document.Execute(new ApplyParagraphStyleCommand([first.Id], "Heading2"));

        Assert.Equal("Heading2", ((Paragraph)document.Blocks[0]).Format.StyleId);
        Assert.Same(second, document.Blocks[1]);
        Assert.Equal(["DefaultParagraphFont", "Heading2", "Heading2Char", "Normal"], document.Styles.All.Select(s => s.Id).Order(StringComparer.Ordinal));
        Assert.Equal("Normal", document.Styles.Find("Heading2").BasedOn);
        Assert.Equal("Heading2", document.Styles.Find("Heading2Char").Link);
        Assert.Same(document.Styles.Find("Normal"), document.Styles.DefaultParagraphStyle);
        Assert.All(document.Styles.All, s => Assert.False(s.BuiltIn));
        Assert.Equal(13, document.EffectiveRunFormat((Paragraph)document.Blocks[0], ((Paragraph)document.Blocks[0]).Inlines[0]).SizePt);

        var output = DocxWriterTests.Save(document);
        var written = OpcAssert.Xml(output, StylesPart).Root!.Elements(W + "style").ToList();
        Assert.Equal(["DefaultParagraphFont", "Heading2", "Heading2Char", "Normal"], written.Select(s => (string)s.Attribute(W + "styleId")).Order(StringComparer.Ordinal));
        Assert.True(XNode.DeepEquals(OpcAssert.Xml(source, StylesPart).Root!.Element(W + "style"), written[0]));
        Assert.Equal("26", (string)written.Single(s => (string)s.Attribute(W + "styleId") == "Heading2").Element(W + "rPr")!.Element(W + "sz")!.Attribute(W + "val"));
        DocxWriterTests.AssertConsistent(output);

        document.Undo.Undo();

        Assert.Same(styles, document.Styles);
        Assert.Same(first, document.Blocks[0]);
        Assert.Null(document.Styles.Find("Heading2"));
        Assert.True(OpcAssert.Parts(source)[StylesPart].AsSpan().SequenceEqual(OpcAssert.Parts(DocxWriterTests.Save(document))[StylesPart]));

        document.Undo.Redo();

        Assert.NotNull(document.Styles.Find("Heading2Char"));
    }

    [Fact]
    public void ApplyStyle_ExistingStyle_OnlySetsStyleId_DefaultStyleClearsIt()
    {
        var document = Load(Doc(Para("a", "<w:pStyle w:val=\"Heading1\"/><w:jc w:val=\"center\"/>")));
        var styles = document.Styles;
        var id = document.Blocks[0].Id;

        document.Execute(new ApplyParagraphStyleCommand([id], "Normal"));

        Assert.Same(styles, document.Styles);
        Assert.Equal(new ParagraphFormat { Alignment = ParagraphAlignment.Center }, ((Paragraph)document.Blocks[0]).Format);
        Assert.Throws<ArgumentException>(() => document.Execute(new ApplyParagraphStyleCommand([id], "NoSuchStyle")));
        Assert.Throws<ArgumentException>(() => document.Execute(new ApplyParagraphStyleCommand([id], "Strong")));
    }

    [Fact]
    public void ApplyCharacterStyle_AddsBuiltInAndNullRemoves()
    {
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;
        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 0), "abcdef"));

        document.Execute(new ApplyCharacterStyleCommand(Range(document, id, 1, id, 3), "Strong"));

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal([null, "Strong", null], paragraph.Inlines.Select(i => i.Format.StyleId));
        Assert.True(document.EffectiveRunFormat(paragraph, paragraph.Inlines[1]).Bold);
        Assert.NotNull(document.Styles.Find("Strong"));

        document.Execute(new ApplyCharacterStyleCommand(Range(document, id, 0, id, 6), null));

        Assert.All(((Paragraph)document.Blocks[0]).Inlines, i => Assert.Null(i.Format.StyleId));
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(document));
    }

    [Fact]
    public void SetHyperlink_AppliesHyperlinkStyleOnce_RemoveClearsIt()
    {
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;
        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 0), "one two three"));

        document.Execute(new SetHyperlinkCommand(Range(document, id, 0, id, 3), "https://example.com"));
        document.Execute(new SetHyperlinkCommand(Range(document, id, 8, id, 13), "#Mark"));

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.All(paragraph.Inlines, i => Assert.Equal(i.Link != null ? "Hyperlink" : null, i.Format.StyleId));
        Assert.Equal("single", document.EffectiveRunFormat(paragraph, paragraph.Inlines[0]).Underline);
        var output = DocxWriterTests.Save(document);
        Assert.Single(OpcAssert.Xml(output, StylesPart).Root!.Elements(W + "style"), s => (string)s.Attribute(W + "styleId") == "Hyperlink");
        DocxWriterTests.AssertConsistent(output);

        document.Execute(new SetHyperlinkCommand(Range(document, id, 0, id, 13), null));

        Assert.All(((Paragraph)document.Blocks[0]).Inlines, i => Assert.Null(i.Format.StyleId));
    }

    [Fact]
    public void UpdateStyleFromSelection_MovesDirectFormatIntoStyle()
    {
        var source = new DocxBuilder()
            .Styles("<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style>" +
                    "<w:style w:type=\"paragraph\" w:styleId=\"Heading1\"><w:name w:val=\"heading 1\"/><w:basedOn w:val=\"Normal\"/><w:rsid w:val=\"00112233\"/>" +
                    "<w:pPr><w:spacing w:before=\"240\"/></w:pPr><w:rPr><w:sz w:val=\"32\"/></w:rPr></w:style>")
            .Numbering(Decimal + Num(1, 0))
            .Body(Para("", "<w:pStyle w:val=\"Heading1\"/><w:numPr><w:ilvl w:val=\"0\"/><w:numId w:val=\"1\"/></w:numPr><w:spacing w:after=\"100\"/><w:jc w:val=\"center\"/>",
                      "<w:r><w:rPr><w:b/><w:color w:val=\"FF0000\"/></w:rPr><w:t>Ab</w:t></w:r><w:r><w:rPr><w:b/><w:i/></w:rPr><w:t>cd</w:t></w:r>") +
                  Para("Other", "<w:pStyle w:val=\"Heading1\"/><w:jc w:val=\"right\"/>"))
            .Build();
        var document = Load(source);
        var styles = document.Styles;
        var other = document.Blocks[1];

        document.Execute(new UpdateStyleFromSelectionCommand(document.Blocks[0].Id, "Heading1"));

        var style = document.Styles.Find("Heading1");
        Assert.Equal(new ParagraphFormat { Alignment = ParagraphAlignment.Center, SpacingBefore = 240, SpacingAfter = 100 }, style.ParagraphFormat);
        Assert.Equal(new RunFormat { Bold = true, Color = "#FF0000", SizePt = 16 }, style.RunFormat);
        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal(new ParagraphFormat { StyleId = "Heading1", Numbering = new NumberingRef(1, 0) }, paragraph.Format);
        Assert.Equal([new RunFormat(), new RunFormat { Italic = true }], paragraph.Inlines.Select(i => i.Format));
        Assert.Same(other, document.Blocks[1]);
        Assert.Equal(ParagraphAlignment.Right, document.EffectiveParagraphFormat((Paragraph)other).Alignment);
        var effective = document.EffectiveRunFormat(paragraph, paragraph.Inlines[1]);
        Assert.True(effective.Bold);
        Assert.Equal("#FF0000", effective.Color);

        var output = DocxWriterTests.Save(document);
        var before = OpcAssert.Xml(source, StylesPart).Root!.Elements(W + "style").ToList();
        var after = OpcAssert.Xml(output, StylesPart).Root!.Elements(W + "style").ToList();
        Assert.True(XNode.DeepEquals(before[0], after[0]));
        Assert.Equal("00112233", (string)after[1].Element(W + "rsid")!.Attribute(W + "val"));
        Assert.Equal("center", (string)after[1].Element(W + "pPr")!.Element(W + "jc")!.Attribute(W + "val"));
        Assert.NotNull(after[1].Element(W + "rPr")!.Element(W + "b"));
        Assert.Equal("FF0000", (string)after[1].Element(W + "rPr")!.Element(W + "color")!.Attribute(W + "val"));
        DocxWriterTests.AssertConsistent(output);
        var reloaded = Load(output).Styles.Find("Heading1");
        Assert.Equal(style.ParagraphFormat, reloaded.ParagraphFormat);
        Assert.Equal(style.RunFormat, reloaded.RunFormat);

        document.Undo.Undo();

        Assert.Same(styles, document.Styles);
    }

    [Fact]
    public void SetDefaultFont_PatchesDocDefaults_DropsThemeAttributes()
    {
        var source = new DocxBuilder()
            .Styles("<w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:asciiTheme=\"minorHAnsi\" w:eastAsia=\"SimSun\" w:hAnsiTheme=\"minorHAnsi\" w:cs=\"Arial\" w:cstheme=\"minorBidi\"/>" +
                    "<w:sz w:val=\"22\"/><w:lang w:val=\"de-DE\"/></w:rPr></w:rPrDefault></w:docDefaults>" +
                    "<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style>")
            .Body(Para("text"))
            .Build();
        var document = Load(source);
        var styles = document.Styles;

        document.Execute(new SetDefaultFontCommand("Arial", 12));

        Assert.Equal("Arial", document.Styles.DefaultRunFormat.Font);
        Assert.Equal(12, document.Styles.DefaultRunFormat.SizePt);
        Assert.Same(styles.Find("Normal"), document.Styles.Find("Normal"));
        var output = DocxWriterTests.Save(document);
        var root = OpcAssert.Xml(output, StylesPart).Root!;
        var rPr = root.Element(W + "docDefaults")!.Element(W + "rPrDefault")!.Element(W + "rPr")!;
        var fonts = rPr.Element(W + "rFonts")!;
        Assert.Equal("Arial", (string)fonts.Attribute(W + "ascii"));
        Assert.Equal("Arial", (string)fonts.Attribute(W + "hAnsi"));
        Assert.Null(fonts.Attribute(W + "asciiTheme"));
        Assert.Null(fonts.Attribute(W + "hAnsiTheme"));
        Assert.Equal("SimSun", (string)fonts.Attribute(W + "eastAsia"));
        Assert.Equal("Arial", (string)fonts.Attribute(W + "cs"));
        Assert.Equal("minorBidi", (string)fonts.Attribute(W + "cstheme"));
        Assert.Equal("24", (string)rPr.Element(W + "sz")!.Attribute(W + "val"));
        Assert.NotNull(rPr.Element(W + "lang"));
        Assert.True(XNode.DeepEquals(OpcAssert.Xml(source, StylesPart).Root!.Element(W + "style"), root.Element(W + "style")));

        document.Undo.Undo();

        Assert.Same(styles, document.Styles);
        document.Undo.Redo();
        document.Execute(new SetDefaultFontCommand("Arial", 12));
        document.Undo.Undo();
        Assert.Same(styles, document.Styles);
    }

    [Fact]
    public void ApplyList_Decimal_ContinuesPreviousList()
    {
        var document = Load(Doc(Item(1) + Para("b") + Para("c"), Decimal + Num(1, 0)));
        var numbering = document.Numbering;

        document.Execute(new ApplyListCommand([document.Blocks[1].Id, document.Blocks[2].Id], ListTemplate.Decimal));

        Assert.Same(numbering, document.Numbering);
        Assert.Equal([new NumberingRef(1, 0), new NumberingRef(1, 0)], document.Blocks.Skip(1).Select(b => ((Paragraph)b).Format.Numbering));
        Assert.Equal(["1.", "2.", "3."], Labels(document));
    }

    [Fact]
    public void ApplyList_Bullet_AfterDecimal_CreatesNewList_KeepsLevels_UndoRestoresInstances()
    {
        var document = Load(Doc(Item(1) + Item(1, 2) + Para("c"), Decimal + Num(1, 0)));
        var numbering = document.Numbering;
        var blocks = document.Blocks.ToList();

        document.Execute(new ApplyListCommand([document.Blocks[1].Id, document.Blocks[2].Id], ListTemplate.Bullet));

        var added = Assert.Single(document.Numbering.Instances, i => i.Id == 2);
        Assert.Equal(1, added.AbstractId);
        Assert.Equal(9, document.Numbering.Abstracts.Single(a => a.Id == 1).Levels.Count);
        Assert.Equal([new NumberingRef(2, 2), new NumberingRef(2, 0)], document.Blocks.Skip(1).Select(b => ((Paragraph)b).Format.Numbering));
        Assert.Equal(["1.", "\u25AA", "\u2022"], Labels(document));

        document.Undo.Undo();

        Assert.Same(numbering, document.Numbering);
        Assert.True(blocks.SequenceEqual(document.Blocks, ReferenceEqualityComparer.Instance));
    }

    [Fact]
    public void ApplyList_AllAlreadyBullet_Removes()
    {
        var document = Load(Doc(Item(1) + Para("b", "<w:pStyle w:val=\"ListBullet\"/>"), Bullet + Num(1, 3),
            "<w:style w:type=\"paragraph\" w:styleId=\"ListBullet\"><w:name w:val=\"List Bullet\"/><w:pPr><w:numPr><w:numId w:val=\"1\"/></w:numPr></w:pPr></w:style>"));

        document.Execute(new ApplyListCommand([document.Blocks[0].Id, document.Blocks[1].Id], ListTemplate.Bullet));

        Assert.Null(((Paragraph)document.Blocks[0]).Format.Numbering);
        Assert.Equal(new NumberingRef(0, 0), ((Paragraph)document.Blocks[1]).Format.Numbering);
        Assert.Equal([null, null], Labels(document));
    }

    [Fact]
    public void ApplyList_DirectNumPrIndentBeatsStyleIndent()
    {
        var document = Load(Doc(Para("a", "<w:pStyle w:val=\"Indented\"/>"), null,
            "<w:style w:type=\"paragraph\" w:styleId=\"Indented\"><w:name w:val=\"Indented\"/><w:pPr><w:ind w:left=\"2000\"/></w:pPr></w:style>"));

        document.Execute(new ApplyListCommand([document.Blocks[0].Id], ListTemplate.Decimal));

        Assert.Equal(720, document.EffectiveParagraphFormat((Paragraph)document.Blocks[0]).IndentLeft);
    }

    [Fact]
    public void RemoveList_StyleNumberingCancelledElseCleared()
    {
        var document = Load(Doc(Item(1) + Para("b", "<w:pStyle w:val=\"ListBullet\"/>") + Para("c"), Bullet + Num(1, 3),
            "<w:style w:type=\"paragraph\" w:styleId=\"ListBullet\"><w:name w:val=\"List Bullet\"/><w:pPr><w:numPr><w:numId w:val=\"1\"/></w:numPr></w:pPr></w:style>"));
        var plain = document.Blocks[2];

        document.Execute(new RemoveListCommand(document.Blocks.Select(b => b.Id).ToList()));

        Assert.Null(((Paragraph)document.Blocks[0]).Format.Numbering);
        Assert.Equal(new NumberingRef(0, 0), ((Paragraph)document.Blocks[1]).Format.Numbering);
        Assert.Same(plain, document.Blocks[2]);
    }

    [Fact]
    public void ChangeListLevel_ClampsAt8()
    {
        var document = Load(Doc(Item(1, 7) + Item(1), Decimal + Num(1, 0)));
        var ids = document.Blocks.Select(b => b.Id).ToList();

        document.Execute(new ChangeListLevelCommand(ids, 3));

        Assert.Equal([8, 3], document.Blocks.Select(b => ((Paragraph)b).Format.Numbering!.Value.Level));

        document.Execute(new ChangeListLevelCommand(ids, -20));

        Assert.Equal([0, 0], document.Blocks.Select(b => ((Paragraph)b).Format.Numbering!.Value.Level));
    }

    [Fact]
    public void ChangeListLevel_NonList_ChangesIndentBy720()
    {
        var source = Doc(Para("a", "<w:pStyle w:val=\"Indented\"/><w:numPr><w:ilvl w:val=\"2\"/></w:numPr>"), null,
            "<w:style w:type=\"paragraph\" w:styleId=\"Indented\"><w:name w:val=\"Indented\"/><w:pPr><w:ind w:left=\"360\"/></w:pPr></w:style>");
        var document = Load(source);
        var ids = new[] { document.Blocks[0].Id };

        document.Execute(new ChangeListLevelCommand(ids, 1));

        Assert.Equal(1080, ((Paragraph)document.Blocks[0]).Format.IndentLeft);
        Assert.Null(((Paragraph)document.Blocks[0]).Format.Numbering);
        var numPr = Body(DocxWriterTests.Save(document)).Element(W + "p")!.Element(W + "pPr")!.Element(W + "numPr")!;
        Assert.Equal("2", (string)numPr.Element(W + "ilvl")!.Attribute(W + "val"));
        Assert.Null(numPr.Element(W + "numId"));

        document.Execute(new ChangeListLevelCommand(ids, -5));

        Assert.Equal(0, ((Paragraph)document.Blocks[0]).Format.IndentLeft);
    }

    [Fact]
    public void RestartNumbering_ThirdOfFive_LabelsRestartAndNumWritten()
    {
        var source = Doc(Item(1) + Item(1) + Item(1) + Item(1) + Item(1), Decimal + Num(1, 0));
        var document = Load(source);
        var numbering = document.Numbering;

        document.Execute(new RestartNumberingCommand(document.Blocks[2].Id));

        Assert.Equal(["1.", "2.", "1.", "2.", "3."], Labels(document));
        Assert.Equal([1, 1, 2, 2, 2], document.Blocks.Select(b => ((Paragraph)b).Format.Numbering!.Value.NumId));
        var output = DocxWriterTests.Save(document);
        var root = OpcAssert.Xml(output, NumberingPart).Root!;
        var nums = root.Elements(W + "num").ToList();
        Assert.Equal(2, nums.Count);
        Assert.True(XNode.DeepEquals(OpcAssert.Xml(source, NumberingPart).Root!.Element(W + "num"), nums[0]));
        Assert.True(XNode.DeepEquals(OpcAssert.Xml(source, NumberingPart).Root!.Element(W + "abstractNum"), root.Element(W + "abstractNum")));
        Assert.Equal("2", (string)nums[1].Attribute(W + "numId"));
        Assert.Equal("0", (string)nums[1].Element(W + "abstractNumId")!.Attribute(W + "val"));
        var levelOverride = nums[1].Element(W + "lvlOverride")!;
        Assert.Equal("0", (string)levelOverride.Attribute(W + "ilvl"));
        Assert.Equal("1", (string)levelOverride.Element(W + "startOverride")!.Attribute(W + "val"));
        DocxWriterTests.AssertConsistent(output);
        Assert.Equal(Labels(document), Labels(Load(output)));

        document.Undo.Undo();

        Assert.Same(numbering, document.Numbering);
        Assert.True(OpcAssert.Parts(source)[NumberingPart].AsSpan().SequenceEqual(OpcAssert.Parts(DocxWriterTests.Save(document))[NumberingPart]));
    }

    [Fact]
    public void Writer_NumberingPartCreatedWhenMissing()
    {
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;
        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 0), "item"));

        document.Execute(new ApplyListCommand([id], ListTemplate.Bullet));
        var output = DocxWriterTests.Save(document);

        var opc = OpcPackage.Open(output);
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.numbering+xml", opc.ContentType(NumberingPart));
        var rel = Assert.Single(opc.Relationships("/word/document.xml"), r => r.Type == DocxBuilder.RelNs + "/numbering");
        Assert.Equal(NumberingPart, opc.ResolveTarget("/word/document.xml", rel.Target));
        var root = OpcAssert.Xml(output, NumberingPart).Root!;
        Assert.Equal(["abstractNum", "num"], root.Elements().Select(e => e.Name.LocalName));
        Assert.Empty(root.Descendants(W + "nsid"));
        DocxWriterTests.AssertConsistent(output);
        var reloaded = Load(output);
        Assert.Equal("\u2022", reloaded.ListLabel((Paragraph)reloaded.Blocks[0]));
    }

    [Fact]
    public void Writer_NewAbstractNumBeforeFirstNum()
    {
        var source = Doc(Item(1) + Para("b"), Decimal + Num(1, 0) + "<w:numIdMacAtCleanup w:val=\"1\"/>");
        var document = Load(source);

        document.Execute(new ApplyListCommand([document.Blocks[1].Id], ListTemplate.Bullet));
        var output = DocxWriterTests.Save(document);

        var elements = OpcAssert.Xml(output, NumberingPart).Root!.Elements().ToList();
        Assert.Equal(["abstractNum:0", "abstractNum:1", "num:1", "num:2", "numIdMacAtCleanup:"],
            elements.Select(e => e.Name.LocalName + ":" + ((string)e.Attribute(W + "abstractNumId") ?? (string)e.Attribute(W + "numId"))));
        var original = OpcAssert.Xml(source, NumberingPart).Root!.Elements().ToList();
        Assert.True(XNode.DeepEquals(original[0], elements[0]));
        Assert.True(XNode.DeepEquals(original[1], elements[2]));
        DocxWriterTests.AssertConsistent(output);
    }

    [Fact]
    public void Writer_StylesPartCreatedWhenMissing()
    {
        var document = new DocxBuilder().Body(Para("a")).Load();

        document.Execute(new ApplyParagraphStyleCommand([document.Blocks[0].Id], "Title"));
        var output = DocxWriterTests.Save(document);

        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml", OpcPackage.Open(output).ContentType(StylesPart));
        Assert.Equal(["Normal", "Title", "TitleChar"], OpcAssert.Xml(output, StylesPart).Root!.Elements(W + "style").Select(s => (string)s.Attribute(W + "styleId")).Order());
        DocxWriterTests.AssertConsistent(output);
        var reloaded = Load(output);
        Assert.Equal(28, reloaded.EffectiveRunFormat((Paragraph)reloaded.Blocks[0], null).SizePt);
    }

    [Fact]
    public void Split_ListParagraph_EnterContinues_EnterOnEmptyEndsList()
    {
        var document = Load(Doc(Item(1), Decimal + Num(1, 0)));
        var id = document.Blocks[0].Id;

        document.Execute(new SplitParagraphCommand(document.PositionOf(id, 4)));

        Assert.Equal(new NumberingRef(1, 0), ((Paragraph)document.Blocks[1]).Format.Numbering);
        Assert.Equal(["1.", "2."], Labels(document));

        document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[1].Id, 0)));

        Assert.Equal(2, document.Blocks.Count);
        Assert.Null(((Paragraph)document.Blocks[1]).Format.Numbering);
        Assert.Equal(["1.", null], Labels(document));
    }

    [Fact]
    public void Writer_BulletTemplate_LabelFontsLikeWord_MultilevelType()
    {
        var document = Load(Doc(Para("a") + Para("b")));

        document.Execute(new ApplyListCommand([document.Blocks[0].Id], ListTemplate.Bullet));
        document.Execute(new ApplyListCommand([document.Blocks[1].Id], ListTemplate.Multilevel));
        var output = DocxWriterTests.Save(document);

        var definitions = OpcAssert.Xml(output, NumberingPart).Root!.Elements(W + "abstractNum").ToList();
        Assert.Equal(["hybridMultilevel", "multilevel"], definitions.Select(d => (string)d.Element(W + "multiLevelType")!.Attribute(W + "val")));
        var levels = definitions[0].Elements(W + "lvl").ToList();
        Assert.Equal(9, levels.Count);
        string[] fonts = ["Symbol", "Courier New", "Wingdings"];
        string[] texts = ["\uF0B7", "o", "\uF0A7"];
        for (var i = 0; i < levels.Count; i++)
        {
            var rFonts = levels[i].Element(W + "rPr")!.Element(W + "rFonts")!;
            Assert.Equal(fonts[i % 3], (string)rFonts.Attribute(W + "ascii"));
            Assert.Equal(fonts[i % 3], (string)rFonts.Attribute(W + "hAnsi"));
            Assert.Equal("default", (string)rFonts.Attribute(W + "hint"));
            Assert.Equal(texts[i % 3], (string)levels[i].Element(W + "lvlText")!.Attribute(W + "val"));
        }

        Assert.Null(definitions[1].Element(W + "lvl")!.Element(W + "rPr"));
        var reloaded = Load(output);
        Assert.Equal("\u2022", reloaded.ListLabel((Paragraph)reloaded.Blocks[0]));
        DocxWriterTests.AssertConsistent(output);
    }

    [Fact]
    public void ApplyList_DirectLevelWithoutNumId_KeepsLevel()
    {
        var document = Load(Doc(Para("a", "<w:numPr><w:ilvl w:val=\"2\"/></w:numPr>")));

        document.Execute(new ApplyListCommand([document.Blocks[0].Id], ListTemplate.Decimal));

        Assert.Equal(2, ((Paragraph)document.Blocks[0]).Format.Numbering!.Value.Level);
    }

    [Fact]
    public void ApplyList_IdsOutOfOrder_ContinuesListBeforeFirstParagraph()
    {
        var document = Load(Doc(Item(1) + Para("b") + Para("c"), Decimal + Num(1, 0)));
        var numbering = document.Numbering;

        document.Execute(new ApplyListCommand([document.Blocks[2].Id, document.Blocks[1].Id], ListTemplate.Decimal));

        Assert.Same(numbering, document.Numbering);
        Assert.Equal(["1.", "2.", "3."], Labels(document));
    }

    [Fact]
    public void ApplyList_SelectionIntoTableCells_NumbersAllParagraphs()
    {
        var table = "<w:tbl><w:tblGrid><w:gridCol w:w=\"3000\"/><w:gridCol w:w=\"3000\"/></w:tblGrid><w:tr><w:tc>" + Para("c1") + "</w:tc><w:tc>" + Para("c2") +
                    "</w:tc></w:tr></w:tbl>";
        var document = Load(Doc(Para("a") + table + Para("b")));
        var ids = document.ParagraphsIn(new DocumentRange(document.PositionOf(document.Blocks[0].Id, 0), document.PositionOf(document.Blocks[2].Id, 1)))
            .Select(p => p.Id).ToList();

        document.Execute(new ApplyListCommand(ids, ListTemplate.Decimal));

        Assert.Equal(4, ids.Count);
        Assert.All(document.AllParagraphs(), p => Assert.Equal(new NumberingRef(1, 0), p.Format.Numbering));
        Assert.Equal(["1.", "2.", "3.", "4."], document.AllParagraphs().Select(document.ListLabel));
        var output = DocxWriterTests.Save(document);
        DocxWriterTests.AssertConsistent(output);
        Assert.Equal(4, Body(output).Descendants(W + "numId").Count());
    }

    [Fact]
    public void RestartNumbering_NegativeStart_ThrowsInConstructor() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new RestartNumberingCommand("b1", -1));

    [Fact]
    public void RestartNumbering_PlainParagraphInRange_LeftAlone()
    {
        var document = Load(Doc(Item(1) + Item(1) + Para("plain") + Item(1) + Item(1), Decimal + Num(1, 0)));
        var plain = document.Blocks[2];

        document.Execute(new RestartNumberingCommand(document.Blocks[1].Id));

        Assert.Same(plain, document.Blocks[2]);
        Assert.Equal(["1.", "1.", null, "2.", "3."], Labels(document));
    }

    [Fact]
    public void Split_EmptyListItem_OneUndoStep_UndoRestoresInstances()
    {
        var document = Load(Doc(Item(1) + Para("", "<w:numPr><w:ilvl w:val=\"0\"/><w:numId w:val=\"1\"/></w:numPr>"), Decimal + Num(1, 0)));
        var blocks = document.Blocks.ToList();
        var numbering = document.Numbering;

        document.Execute(new SplitParagraphCommand(document.PositionOf(blocks[1].Id, 0)));
        Assert.Null(((Paragraph)document.Blocks[1]).Format.Numbering);

        document.Undo.Undo();

        Assert.False(document.Undo.CanUndo);
        Assert.Same(numbering, document.Numbering);
        Assert.True(blocks.SequenceEqual(document.Blocks, ReferenceEqualityComparer.Instance));
    }

    [Fact]
    public void Unlink_RunWithOtherCharacterStyle_KeepsStyle()
    {
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;
        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 0), "one two"));
        document.Execute(new SetHyperlinkCommand(Range(document, id, 0, id, 7), "https://example.com"));
        document.Execute(new ApplyCharacterStyleCommand(Range(document, id, 0, id, 3), "Strong"));

        document.Execute(new SetHyperlinkCommand(Range(document, id, 0, id, 7), null));

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.All(paragraph.Inlines, i => Assert.Null(i.Link));
        Assert.Equal(["Strong", null], paragraph.Inlines.Select(i => i.Format.StyleId));
    }

    [Fact]
    public void SetDefaultFont_SizeOnlyOnThemeFont_KeepsThemeAttributes()
    {
        var source = new DocxBuilder()
            .Styles("<w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:asciiTheme=\"minorHAnsi\" w:hAnsiTheme=\"minorHAnsi\"/><w:sz w:val=\"22\"/></w:rPr></w:rPrDefault></w:docDefaults>" +
                    "<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style>")
            .Theme(minor: "Calibri")
            .Body(Para("text"))
            .Build();
        var document = Load(source);

        document.Execute(new SetDefaultFontCommand("Calibri", 12));

        Assert.Equal("theme:minorHAnsi", document.Styles.DefaultRunFormat.Font);
        var rPr = OpcAssert.Xml(DocxWriterTests.Save(document), StylesPart).Root!.Element(W + "docDefaults")!.Element(W + "rPrDefault")!.Element(W + "rPr")!;
        Assert.Equal("minorHAnsi", (string)rPr.Element(W + "rFonts")!.Attribute(W + "asciiTheme"));
        Assert.Null(rPr.Element(W + "rFonts")!.Attribute(W + "ascii"));
        Assert.Equal("24", (string)rPr.Element(W + "sz")!.Attribute(W + "val"));
    }

    private static TextDocument Load(byte[] bytes) => TextDocument.Load(new MemoryStream(bytes));

    private static XElement Body(byte[] package) => OpcAssert.Xml(package, "/word/document.xml").Root!.Element(W + "body")!;

    private static List<string> Labels(TextDocument document) => document.Blocks.Select(b => document.ListLabel((Paragraph)b)).ToList();

    private static DocumentRange Range(TextDocument document, string startId, int start, string endId, int end) =>
        new(document.PositionOf(startId, start), document.PositionOf(endId, end));

    private static byte[] Doc(string body, string numbering = null, string styles = "")
    {
        var builder = new DocxBuilder()
            .Styles("<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style>" +
                    "<w:style w:type=\"paragraph\" w:styleId=\"Heading1\"><w:name w:val=\"heading 1\"/><w:basedOn w:val=\"Normal\"/></w:style>" + styles)
            .Body(body);
        if (numbering != null) builder.Numbering(numbering);
        return builder.Build();
    }

    private static string Num(int id, int abstractId) => $"<w:num w:numId=\"{id}\"><w:abstractNumId w:val=\"{abstractId}\"/></w:num>";

    private static string Item(int numId, int level = 0) =>
        Para("item", $"<w:numPr><w:ilvl w:val=\"{level}\"/><w:numId w:val=\"{numId}\"/></w:numPr>");

    private static string Para(string text, string pPr = "", string runs = null) =>
        $"<w:p>{(pPr.Length > 0 ? $"<w:pPr>{pPr}</w:pPr>" : "")}{runs ?? (text.Length > 0 ? $"<w:r><w:t>{text}</w:t></w:r>" : "")}</w:p>";
}
