using System.Xml.Linq;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class NumberingLabelTests
{
    private const string Ns = "xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"";
    private const string Num1 = "<w:num w:numId=\"1\"><w:abstractNumId w:val=\"1\"/></w:num>";

    [Fact]
    public void Labels_MultilevelResetsLowerLevel()
    {
        var document = Doc(Abstract(1, Lvl(0, "decimal", "%1."), Lvl(1, "lowerLetter", "%2)")) + Num1, Item(0), Item(1), Item(1), Item(0), Item(1));

        Assert.Equal(["1.", "a)", "b)", "2.", "a)"], Labels(document));
    }

    [Fact]
    public void Labels_LvlTextReferencesHigherLevels()
    {
        var document = Doc(Abstract(1, Lvl(0, "decimal", "%1."), Lvl(1, "decimal", "%1.%2.")) + Num1, Item(0), Item(1), Item(1), Item(0), Item(1));

        Assert.Equal(["1.", "1.1.", "1.2.", "2.", "2.1."], Labels(document));
    }

    [Fact]
    public void Labels_LvlRestartZero_DoesNotReset()
    {
        var document = Doc(Abstract(1, Lvl(0, "decimal", "%1."), Lvl(1, "lowerLetter", "%2)", extra: "<w:lvlRestart w:val=\"0\"/>")) + Num1,
            Item(0), Item(1), Item(0), Item(1));

        Assert.Equal(["1.", "a)", "2.", "b)"], Labels(document));
    }

    [Fact]
    public void Labels_PlainParagraphBetween_DoesNotReset()
    {
        var document = Doc(Abstract(1, Lvl(0, "decimal", "%1.")) + Num1, Item(0), new Paragraph { Inlines = [new TextRun("plain")] }, Item(0));

        var labels = Labels(document);

        Assert.Equal("1.", labels[0]);
        Assert.Null(labels[1]);
        Assert.Equal("2.", labels[2]);
    }

    [Fact]
    public void Labels_SameAbstractTwoNums_ContinueCounting()
    {
        var document = Doc(Abstract(1, Lvl(0, "decimal", "%1.")) + Num1 + "<w:num w:numId=\"2\"><w:abstractNumId w:val=\"1\"/></w:num>",
            Item(0), Item(0), Item(0, 2));

        Assert.Equal(["1.", "2.", "3."], Labels(document));
    }

    [Fact]
    public void Labels_StartOverride_Restarts()
    {
        var document = Doc(Abstract(1, Lvl(0, "decimal", "%1.")) + Num1 +
                           "<w:num w:numId=\"3\"><w:abstractNumId w:val=\"1\"/><w:lvlOverride w:ilvl=\"0\"><w:startOverride w:val=\"1\"/></w:lvlOverride></w:num>",
            Item(0), Item(0), Item(0, 3), Item(0, 3));

        Assert.Equal(["1.", "2.", "1.", "2."], Labels(document));
    }

    [Fact]
    public void Labels_LettersAndRomans()
    {
        var document = Doc(Abstract(1, Lvl(0, "lowerLetter", "%1.", 27)) + Abstract(2, Lvl(0, "upperRoman", "%1.", 4)) + Num1 +
                           "<w:num w:numId=\"2\"><w:abstractNumId w:val=\"2\"/></w:num>",
            Item(0), Item(0), Item(0, 2), Item(0, 2));

        Assert.Equal(["aa.", "bb.", "IV.", "V."], Labels(document));
    }

    [Fact]
    public void Labels_SymbolBullet_MapsToUnicode()
    {
        var document = Doc(Abstract(1, Lvl(0, "bullet", "&#xF0B7;"), Lvl(1, "bullet", "&#xF0A7;")) + Num1, Item(0), Item(1));

        Assert.Equal(["•", "▪"], Labels(document));
    }

    [Fact]
    public void Labels_NumberingFromParagraphStyle()
    {
        var document = Doc(Abstract(1, Lvl(0, "decimal", "%1.")) + Num1, Item(0), new Paragraph { Format = new ParagraphFormat { StyleId = "Numbered" } });
        document.Styles = StylesXml.Read(XDocument.Parse($"""
            <w:styles {Ns}><w:style w:type="paragraph" w:styleId="Numbered"><w:pPr><w:numPr><w:numId w:val="1"/></w:numPr></w:pPr></w:style></w:styles>
            """));

        Assert.Equal(["1.", "2."], Labels(document));
    }

    [Fact]
    public void ListTemplates_Multilevel_RepeatsDecimalLetterRoman()
    {
        var levels = ListTemplates.Create(ListTemplate.Multilevel).Levels;
        var bullets = ListTemplates.Create(ListTemplate.Bullet).Levels;

        Assert.Equal(9, levels.Count);
        Assert.Equal([NumberFormat.Decimal, NumberFormat.LowerLetter, NumberFormat.LowerRoman, NumberFormat.Decimal], levels.Take(4).Select(l => l.Format));
        Assert.Equal("%2.", levels[1].Text);
        Assert.Equal(["\uF0B7", "o", "\uF0A7", "\uF0B7"], bullets.Take(4).Select(l => l.Text));
        Assert.Equal(["Symbol", "Courier New", "Wingdings", "Symbol"], bullets.Take(4).Select(l => l.LabelFormat.Font));
    }

    private static TextDocument Doc(string numbering, params Paragraph[] paragraphs)
    {
        var document = TextDocument.FromBlocks(paragraphs);
        document.Numbering = NumberingXml.Read(XDocument.Parse($"<w:numbering {Ns}>{numbering}</w:numbering>"));
        return document;
    }

    private static string[] Labels(TextDocument document) => document.AllParagraphs().Select(document.ListLabel).ToArray();

    private static Paragraph Item(int level, int numId = 1) =>
        new() { Format = new ParagraphFormat { Numbering = new NumberingRef(numId, level) }, Inlines = [new TextRun("item")] };

    private static string Abstract(int id, params string[] levels) => $"<w:abstractNum w:abstractNumId=\"{id}\">{string.Concat(levels)}</w:abstractNum>";

    private static string Lvl(int ilvl, string format, string text, int start = 1, string extra = "") =>
        $"<w:lvl w:ilvl=\"{ilvl}\"><w:start w:val=\"{start}\"/><w:numFmt w:val=\"{format}\"/>{extra}<w:lvlText w:val=\"{text}\"/></w:lvl>";
}
