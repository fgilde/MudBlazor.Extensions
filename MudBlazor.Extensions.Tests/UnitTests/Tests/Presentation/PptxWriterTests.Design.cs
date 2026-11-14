using System.Xml.Linq;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public partial class PptxWriterTests
{
    private const string ThemeFormat =
        "<a:fmtScheme name=\"Keep\"><a:fillStyleLst><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill>" +
        "<a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill></a:fillStyleLst><a:lnStyleLst><a:ln w=\"6350\"/><a:ln w=\"12700\"/><a:ln w=\"19050\"/></a:lnStyleLst>" +
        "<a:effectStyleLst><a:effectStyle><a:effectLst/></a:effectStyle><a:effectStyle><a:effectLst/></a:effectStyle><a:effectStyle><a:effectLst/></a:effectStyle></a:effectStyleLst>" +
        "<a:bgFillStyleLst><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill>" +
        "<a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill></a:bgFillStyleLst></a:fmtScheme>";

    private const string ThemeTail =
        "<a:objectDefaults><a:lnDef><a:spPr/><a:bodyPr/><a:lstStyle/><a:style><a:lnRef idx=\"1\"><a:schemeClr val=\"accent1\"/></a:lnRef>" +
        "<a:fillRef idx=\"0\"><a:schemeClr val=\"accent1\"/></a:fillRef><a:effectRef idx=\"0\"><a:schemeClr val=\"accent1\"/></a:effectRef>" +
        "<a:fontRef idx=\"minor\"><a:schemeClr val=\"tx1\"/></a:fontRef></a:style></a:lnDef></a:objectDefaults><a:extraClrSchemeLst/>" +
        "<a:custClrLst><a:custClr name=\"Brand\"><a:srgbClr val=\"FF00AA\"/></a:custClr></a:custClrLst>" +
        "<a:extLst><a:ext uri=\"{05A4C25C-085E-4340-85A3-A5531E510DB2}\"><thm15:themeFamily xmlns:thm15=\"http://schemas.microsoft.com/office/thememl/2012/main\" " +
        "name=\"Keep\" id=\"{62F939B6-93AF-4DB8-9C6B-D6C7DFDC589F}\" vid=\"{4A3C46E8-61CC-4603-A589-7422A47A8E4A}\"/></a:ext></a:extLst>";

    private static readonly string[] ThemeParts = ["/ppt/theme/theme1.xml", "/ppt/theme/theme3.xml"];

    private static PptxBuilder DesignBuilder()
    {
        var builder = DesignTests.Builder();
        builder.ThemeFormat = ThemeFormat;
        builder.ThemeTail = ThemeTail;
        builder.MasterBackground = "<p:bg><p:bgRef idx=\"1001\"><a:schemeClr val=\"bg1\"/></p:bgRef></p:bg>";
        return builder;
    }

    private static XDocument WithoutBackground(XDocument master)
    {
        var copy = new XDocument(master);
        copy.Root!.Element(P + "cSld")!.Element(P + "bg")?.Remove();
        return copy;
    }

    [Fact]
    public void Writer_ApplyDesign_KeepsFmtSchemeCustClrExtLst()
    {
        var source = DesignBuilder().Build();
        var presentation = Load(source);
        var design = SlideDesigns.BuiltIn[4];

        presentation.Execute(new ApplyDesignCommand(design));
        var output = Save(presentation);

        AssertConsistent(output);
        string[] slots = ["dk1", "lt1", "dk2", "lt2", "accent1", "accent2", "accent3", "accent4", "accent5", "accent6", "hlink", "folHlink"];
        foreach (var part in ThemeParts)
        {
            var before = Xml(source, part).Root!;
            var after = Xml(output, part).Root!;
            foreach (var name in new[] { "objectDefaults", "extraClrSchemeLst", "custClrLst", "extLst" })
                Assert.True(XNode.DeepEquals(before.Element(A + name), after.Element(A + name)), $"{part} {name}");
            var elements = after.Element(A + "themeElements")!;
            Assert.True(XNode.DeepEquals(before.Element(A + "themeElements")!.Element(A + "fmtScheme"), elements.Element(A + "fmtScheme")), part);
            Assert.Equal((string?)before.Attribute("name"), (string?)after.Attribute("name"));
            var scheme = elements.Element(A + "clrScheme")!;
            Assert.Equal("Test", (string?)scheme.Attribute("name"));
            Assert.Equal(slots, scheme.Elements().Select(e => e.Name.LocalName));
            Assert.Equal(design.Colors, scheme.Elements().Select(e => "#" + (string?)e.Element(A + "srgbClr")!.Attribute("val")));
            var fonts = elements.Element(A + "fontScheme")!;
            Assert.Equal(design.MajorFont, (string?)fonts.Element(A + "majorFont")!.Element(A + "latin")!.Attribute("typeface"));
            Assert.Equal(design.MinorFont, (string?)fonts.Element(A + "minorFont")!.Element(A + "latin")!.Attribute("typeface"));
            Assert.Equal(3, fonts.Element(A + "majorFont")!.Elements().Count());
        }

        foreach (var part in new[] { "/ppt/slideMasters/slideMaster1.xml", "/ppt/slideMasters/slideMaster2.xml" })
        {
            var master = Xml(output, part);
            var background = master.Root!.Element(P + "cSld")!.Elements().First();
            Assert.Equal(P + "bg", background.Name);
            Assert.Equal([A + "gradFill", A + "effectLst"], background.Element(P + "bgPr")!.Elements().Select(e => e.Name));
            Assert.True(XNode.DeepEquals(WithoutBackground(Xml(source, part)), WithoutBackground(master)), part);
        }

        Assert.Equal(Parts(source)["/ppt/slideLayouts/slideLayout1.xml"], Parts(output)["/ppt/slideLayouts/slideLayout1.xml"]);
    }

    [Fact]
    public void Writer_ApplyDesign_Undo_ThemePartContentIdentical()
    {
        var source = DesignBuilder().Build();
        var presentation = Load(source);

        presentation.Execute(new ApplyDesignCommand(SlideDesigns.BuiltIn[1]));
        var applied = Save(presentation);
        presentation.Undo.Undo();
        var output = Save(presentation);

        Assert.False(Parts(applied)["/ppt/theme/theme1.xml"].SequenceEqual(Parts(source)["/ppt/theme/theme1.xml"]));
        var before = Parts(source);
        var after = Parts(output);
        Assert.Equal(before.Keys.Order(), after.Keys.Order());
        foreach (var (name, bytes) in before) Assert.True(bytes.SequenceEqual(after[name]), name);
    }

    [Fact]
    public void Writer_ApplyDesign_RoundTrip_SchemeColorsResolveToNewValues()
    {
        var presentation = DesignBuilder().Load();
        var design = SlideDesigns.BuiltIn[4];
        presentation.Execute(new ApplyDesignCommand(SlideDesigns.BuiltIn[0]));
        presentation.Execute(new ApplyThemeColorsCommand(design.Colors));
        presentation.Execute(new ApplyThemeFontsCommand(design.MajorFont, design.MinorFont, [presentation.Masters[1]]));
        presentation.Execute(new ApplyDesignCommand(design, [presentation.Masters[0]]));

        var output = Save(presentation);
        AssertConsistent(output);
        var reloaded = Load(output);

        Assert.Equal(presentation.Masters.Select(DesignTests.Theme), reloaded.Masters.Select(DesignTests.Theme));
        Assert.Equal(design.Background, reloaded.Masters[0].Background);
        Assert.Equal(SlideDesigns.BuiltIn[0].Background, reloaded.Masters[1].Background);
        var shape = (AutoShape)reloaded.Slides[0].Shapes[0];
        Assert.Equal(design.Colors[4], shape.EffectiveFormat(shape.Text!.Paragraphs[0].Runs[0]).Color);
        Assert.Equal(design.Colors[1], ColorResolver.Resolve(SlideBackground.EffectiveBackground(reloaded.Slides[1]).Color, reloaded.Masters[1]));
        Assert.Equal("#123456", reloaded.Layouts[0].Background.Color);
    }

    [Fact]
    public void Writer_CreateWithDesign_EditedDeckStaysConsistent()
    {
        var presentation = Presentation.Create(SlideDesigns.BuiltIn[4]);
        presentation.Execute(new AddSlideCommand(1, presentation.Layouts[1]));
        presentation.Execute(new ApplyDesignCommand(SlideDesigns.BuiltIn[1]));

        var output = Save(presentation);

        AssertConsistent(output);
        Assert.Equal(DesignTests.Theme(presentation.Masters[0]), DesignTests.Theme(Load(output).Masters[0]));
    }
}
