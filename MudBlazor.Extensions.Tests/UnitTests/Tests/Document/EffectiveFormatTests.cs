using System.Xml.Linq;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class EffectiveFormatTests
{
    private const string Ns = "xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"";

    [Fact]
    public void Effective_DocDefaultsThenStyleChainThenDirect()
    {
        var document = Doc(Styles("""
            <w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:ascii="Calibri" w:hAnsi="Calibri"/><w:sz w:val="22"/></w:rPr></w:rPrDefault></w:docDefaults>
            <w:style w:type="paragraph" w:styleId="Base"><w:name w:val="Base"/><w:rPr><w:color w:val="0000FF"/><w:sz w:val="24"/></w:rPr></w:style>
            <w:style w:type="paragraph" w:styleId="Child"><w:name w:val="Child"/><w:basedOn w:val="Base"/><w:rPr><w:b/></w:rPr></w:style>
            """), P("Child", Rpr("<w:sz w:val=\"28\"/>")));

        var format = RunOf(document);

        Assert.Equal(14, format.SizePt);
        Assert.True(format.Bold);
        Assert.Equal("#0000FF", format.Color);
        Assert.Equal("Calibri", format.Font);
    }

    [Fact]
    public void Effective_ToggleBold_ParagraphAndCharacterStyle_Cancel()
    {
        var styles = Styles("""
            <w:style w:type="paragraph" w:styleId="Strong1"><w:rPr><w:b/></w:rPr></w:style>
            <w:style w:type="character" w:styleId="StrongChar"><w:rPr><w:b/></w:rPr></w:style>
            """);

        var cancelled = RunOf(Doc(styles, P("Strong1", Rpr("<w:rStyle w:val=\"StrongChar\"/>"))));
        var direct = RunOf(Doc(styles, P("Strong1", Rpr("<w:rStyle w:val=\"StrongChar\"/><w:b/>"))));

        Assert.False(cancelled.Bold);
        Assert.True(direct.Bold);
        Assert.Equal("StrongChar", direct.StyleId);
    }

    [Fact]
    public void Effective_TableStyleBelowParagraphStyle()
    {
        var document = Doc(Styles("""
            <w:style w:type="table" w:styleId="Grid"><w:rPr><w:color w:val="FF0000"/><w:sz w:val="20"/></w:rPr></w:style>
            <w:style w:type="paragraph" w:styleId="Body"><w:rPr><w:sz w:val="24"/></w:rPr></w:style>
            """), TableOf("Grid", TableLook.None, P("Body", new RunFormat())));
        var paragraph = document.AllParagraphs().Single();

        var format = document.EffectiveRunFormat(paragraph, paragraph.Inlines[0]);

        Assert.Equal("#FF0000", format.Color);
        Assert.Equal(12, format.SizePt);
    }

    [Fact]
    public void Effective_TableStyleFirstRowConditional_OnlyWithLookFirstRow()
    {
        var styles = Styles("""
            <w:style w:type="table" w:styleId="Grid"><w:tblStylePr w:type="firstRow"><w:rPr><w:b/></w:rPr></w:tblStylePr></w:style>
            """);
        var withLook = Doc(styles, TableOf("Grid", TableLook.FirstRow, P(null, new RunFormat()), P(null, new RunFormat())));
        var withoutLook = Doc(styles, TableOf("Grid", TableLook.None, P(null, new RunFormat()), P(null, new RunFormat())));

        bool? Bold(TextDocument document, int row)
        {
            var paragraph = document.AllParagraphs().ElementAt(row);
            return document.EffectiveRunFormat(paragraph, paragraph.Inlines[0]).Bold;
        }

        Assert.True(Bold(withLook, 0));
        Assert.False(Bold(withLook, 1));
        Assert.False(Bold(withoutLook, 0));
    }

    [Fact]
    public void Effective_NumberingIndentBelowDirectIndent()
    {
        var document = Doc(Styles(""), new Paragraph
        {
            Format = new ParagraphFormat { Numbering = new NumberingRef(1, 0), IndentLeft = 1440 },
            Inlines = [new TextRun("x")]
        });
        document.Numbering = SingleLevelNumbering("<w:ind w:left=\"720\" w:hanging=\"360\"/>");

        var format = document.EffectiveParagraphFormat((Paragraph)document.Blocks[0]);

        Assert.Equal(1440, format.IndentLeft);
        Assert.Equal(-360, format.IndentFirstLine);
        Assert.Equal(new NumberingRef(1, 0), format.Numbering);
    }

    [Fact]
    public void Effective_DirectNumbering_IndentAboveParagraphStyle()
    {
        var document = Doc(Styles("""
            <w:style w:type="paragraph" w:styleId="ListParagraph"><w:pPr><w:ind w:left="360"/></w:pPr></w:style>
            """), new Paragraph { Format = new ParagraphFormat { StyleId = "ListParagraph", Numbering = new NumberingRef(1, 0) }, Inlines = [new TextRun("x")] });
        document.Numbering = SingleLevelNumbering("<w:ind w:left=\"1440\" w:hanging=\"360\"/>");

        var format = document.EffectiveParagraphFormat((Paragraph)document.Blocks[0]);

        Assert.Equal(1440, format.IndentLeft);
        Assert.Equal("ListParagraph", format.StyleId);
    }

    [Fact]
    public void Effective_ThemeFontAndShade()
    {
        var document = Doc(Styles(""), P(null, Rpr("<w:rFonts w:asciiTheme=\"minorHAnsi\" w:hAnsiTheme=\"minorHAnsi\"/><w:color w:val=\"4472C4\" w:themeColor=\"accent1\" w:themeShade=\"BF\"/>")));
        document.Theme = DocumentTheme.Read(XDocument.Parse("""
            <a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="Test"><a:themeElements>
              <a:clrScheme name="Test"><a:accent1><a:srgbClr val="808080"/></a:accent1></a:clrScheme>
              <a:fontScheme name="Test"><a:majorFont><a:latin typeface="Aptos Display"/></a:majorFont><a:minorFont><a:latin typeface="Aptos"/></a:minorFont></a:fontScheme>
            </a:themeElements></a:theme>
            """));

        var format = RunOf(document);

        Assert.Equal("Aptos", format.Font);
        Assert.Equal("#606060", format.Color);
    }

    [Fact]
    public void Effective_BasedOnCycle_Terminates()
    {
        var document = Doc(Styles("""
            <w:style w:type="paragraph" w:styleId="A"><w:basedOn w:val="B"/><w:rPr><w:sz w:val="30"/></w:rPr></w:style>
            <w:style w:type="paragraph" w:styleId="B"><w:basedOn w:val="A"/><w:rPr><w:i/></w:rPr></w:style>
            """), P("A", new RunFormat()));

        var format = RunOf(document);

        Assert.Equal(15, format.SizePt);
        Assert.True(format.Italic);
    }

    [Fact]
    public void Effective_TabStops_ChildOverridesAndClearsInherited()
    {
        var document = Doc(Styles("""
            <w:style w:type="paragraph" w:styleId="Base"><w:pPr><w:tabs><w:tab w:val="left" w:pos="720"/><w:tab w:val="left" w:pos="1440"/></w:tabs></w:pPr></w:style>
            <w:style w:type="paragraph" w:styleId="Child"><w:basedOn w:val="Base"/><w:pPr><w:tabs><w:tab w:val="clear" w:pos="720"/><w:tab w:val="right" w:pos="1440" w:leader="dot"/></w:tabs></w:pPr></w:style>
            """), new Paragraph { Format = new ParagraphFormat { StyleId = "Child", Tabs = [new TabStop(2880, TabAlignment.Center, null)] } });

        var format = document.EffectiveParagraphFormat((Paragraph)document.Blocks[0]);

        Assert.Equal([new TabStop(1440, TabAlignment.Right, "dot"), new TabStop(2880, TabAlignment.Center, null)], format.Tabs);
    }

    [Fact]
    public void Gallery_QuickFormatByPriority_ThenMissingBuiltIns()
    {
        var styles = Styles("""
            <w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/><w:uiPriority w:val="0"/><w:qFormat/></w:style>
            <w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="heading 1"/><w:uiPriority w:val="9"/><w:qFormat/></w:style>
            <w:style w:type="character" w:styleId="Mine"><w:name w:val="Mine"/><w:uiPriority w:val="5"/><w:qFormat/></w:style>
            <w:style w:type="paragraph" w:styleId="Plain"><w:name w:val="Plain"/><w:uiPriority w:val="1"/></w:style>
            <w:style w:type="table" w:styleId="Grid"><w:name w:val="Grid"/><w:uiPriority w:val="1"/><w:qFormat/></w:style>
            """);

        var gallery = styles.Gallery;

        Assert.Equal(
            ["Normal", "Mine", "heading 1", "No Spacing", "heading 2", "heading 3", "heading 4", "heading 5", "heading 6", "Title", "Subtitle", "Emphasis", "Strong", "Quote", "List Paragraph"],
            gallery.Select(s => s.Name));
        Assert.All(gallery.Take(3), s => Assert.False(s.BuiltIn));
        Assert.All(gallery.Skip(3), s => Assert.True(s.BuiltIn));
        Assert.Same(styles.Find("Normal"), styles.DefaultParagraphStyle);
        Assert.Same(styles.Find("Heading1"), styles.FindByName("Heading 1"));
    }

    [Fact]
    public void StylesXml_KeepsSourceForUnknownChildren()
    {
        var xml = XDocument.Parse($"""
            <w:styles {Ns}><w:style w:type="paragraph" w:styleId="Odd"><w:name w:val="Odd"/><w:rsid w:val="00AB12CD"/><w:custom w:val="x"/><w:pPr><w:pBdr/><w:jc w:val="center"/></w:pPr></w:style></w:styles>
            """);
        var element = xml.Root!.Elements().Single();

        var style = StylesXml.Read(xml).Find("Odd");

        Assert.Same(element, style.Source);
        Assert.NotNull(style.Source.Element(FormatXml.W + "rsid"));
        Assert.Equal(ParagraphAlignment.Center, style.ParagraphFormat.Alignment);
    }

    [Fact]
    public void Readers_TolerateMissingParts()
    {
        var styles = StylesXml.Read(null);
        var numbering = NumberingXml.Read(null);
        var theme = DocumentTheme.Read(null);

        Assert.Empty(styles.All);
        Assert.NotNull(styles.DefaultRunFormat);
        Assert.Empty(numbering.Instances);
        Assert.Null(numbering.Level(new NumberingRef(1, 0)));
        Assert.Equal("#4472C4", theme.Color("theme:accent1"));
    }

    private static DocumentStyles Styles(string body) => StylesXml.Read(XDocument.Parse($"<w:styles {Ns}>{body}</w:styles>"));

    private static RunFormat Rpr(string xml) => FormatXml.Run(XElement.Parse($"<w:rPr {Ns}>{xml}</w:rPr>"));

    private static Numbering SingleLevelNumbering(string indent) => NumberingXml.Read(XDocument.Parse($"""
        <w:numbering {Ns}>
          <w:abstractNum w:abstractNumId="7"><w:lvl w:ilvl="0"><w:start w:val="1"/><w:numFmt w:val="decimal"/><w:lvlText w:val="%1."/><w:pPr>{indent}</w:pPr></w:lvl></w:abstractNum>
          <w:num w:numId="1"><w:abstractNumId w:val="7"/></w:num>
        </w:numbering>
        """));

    private static Paragraph P(string styleId, RunFormat run) =>
        new() { Format = new ParagraphFormat { StyleId = styleId }, Inlines = [new TextRun("text") { Format = run }] };

    private static Table TableOf(string styleId, TableLook look, params Paragraph[] rows) => new()
    {
        Format = new TableFormat { StyleId = styleId, Look = look },
        Grid = [2000],
        Rows = rows.Select(p => new TableRow([new TableCell([p])])).ToList()
    };

    private static TextDocument Doc(DocumentStyles styles, params Block[] blocks)
    {
        var document = TextDocument.FromBlocks(blocks);
        document.Styles = styles;
        return document;
    }

    private static RunFormat RunOf(TextDocument document)
    {
        var paragraph = (Paragraph)document.Blocks[0];
        return document.EffectiveRunFormat(paragraph, paragraph.Inlines[0]);
    }
}
