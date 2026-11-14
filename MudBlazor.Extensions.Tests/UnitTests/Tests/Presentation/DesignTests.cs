using System.Text.RegularExpressions;
using MudBlazor.Extensions.Office;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public class DesignTests
{
    private static readonly string[] Fonts = ["Aptos", "Calibri", "Segoe UI", "Georgia", "Cambria", "Consolas"];

    private static readonly string[] Palette = ["#101010", "#FAFAFA", "#202020", "#E0E0E0", "#AA0000", "#00AA00", "#0000AA", "#AA00AA", "#00AAAA", "#AAAA00", "#0000FF", "#800080"];

    private static readonly Dictionary<string, Func<Presentation, IDocumentCommand<Presentation>>> Cases = new()
    {
        ["Design"] = _ => new ApplyDesignCommand(SlideDesigns.BuiltIn[1]),
        ["DesignOneMaster"] = p => new ApplyDesignCommand(SlideDesigns.BuiltIn[4], [p.Masters[1]]),
        ["Colors"] = _ => new ApplyThemeColorsCommand(Palette),
        ["Fonts"] = p => new ApplyThemeFontsCommand("Georgia", "Calibri", [p.Masters[0]])
    };

    public static TheoryData<string> CommandNames => new(Cases.Keys);

    internal static PptxBuilder Builder() =>
        new PptxBuilder { SecondMaster = true, LayoutBackground = "<p:bg><p:bgPr><a:solidFill><a:srgbClr val=\"123456\"/></a:solidFill><a:effectLst/></p:bgPr></p:bg>" }
            .Slide(Shape(2, "Text", Xfrm(0, 0, 100, 100), "<a:p><a:r><a:rPr><a:solidFill><a:schemeClr val=\"accent1\"/></a:solidFill></a:rPr><a:t>One</a:t></a:r></a:p>"))
            .Slide(Placeholder(2, "Title 1", "type=\"title\"", "", "<a:p><a:r><a:t>Two</a:t></a:r></a:p>"), layout: 2);

    internal static string Theme(SlideMaster master) =>
        $"{master.Theme.Name} {string.Join(",", master.Theme.Colors)} {master.Theme.MajorFont}/{master.Theme.MinorFont} " +
        $"{master.Background?.Kind} {master.Background?.Color} {master.Background?.ImagePart} {master.Background?.Angle} [{string.Join(",", master.Background?.Stops ?? [])}]";

    private static string Snapshot(Presentation presentation) =>
        SlideCommandTests.Snapshot(presentation) + string.Join("\n", presentation.Masters.Select(Theme));

    private static double Luminance(string color)
    {
        var channels = Enumerable.Range(0, 3).Select(i => Convert.ToInt32(color.Substring(1 + i * 2, 2), 16) / 255.0)
            .Select(c => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4)).ToArray();
        return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2];
    }

    private static double Contrast(string a, string b)
    {
        var (x, y) = (Luminance(a), Luminance(b));
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }

    [Fact]
    public void BuiltIn_HasSixNamedDesigns_WithRequiredContrast()
    {
        Assert.Equal(["Paper", "Slate", "Ocean", "Forest", "Sunset", "Mono"], SlideDesigns.BuiltIn.Select(d => d.Name));
        foreach (var design in SlideDesigns.BuiltIn)
        {
            Assert.Equal(12, design.Colors.Count);
            Assert.All(design.Colors, c => Assert.Matches(new Regex("^#[0-9A-F]{6}$"), c));
            Assert.Contains(design.MajorFont, Fonts);
            Assert.Contains(design.MinorFont, Fonts);
            Assert.True(Contrast(design.Colors[0], design.Colors[1]) >= 7, design.Name);

            var master = Presentation.Create(design).Masters[0];
            var backgrounds = (design.Background.Kind == ShapeFillKind.Gradient ? design.Background.Stops.Select(s => s.Color) : [design.Background.Color])
                .Select(c => ColorResolver.Resolve(c, master)).ToList();
            Assert.Equal(design.Name == "Sunset" ? ShapeFillKind.Gradient : ShapeFillKind.Solid, design.Background.Kind);
            Assert.Equal(design.Name == "Sunset" ? 2 : 0, design.Background.Stops.Count);
            foreach (var accent in design.Colors.Skip(4))
            foreach (var background in backgrounds)
                Assert.True(Contrast(accent, background) >= 3, $"{design.Name} {accent} on {background}");
            var dark = Luminance(backgrounds[0]) < Luminance(ColorResolver.Resolve("scheme:tx1", master));
            Assert.Equal(design.Name == "Slate", dark);
        }
    }

    [Fact]
    public void ApplyDesign_TwoMasters_ChangesBothThemes()
    {
        var presentation = Builder().Load();
        var design = SlideDesigns.BuiltIn[2];

        presentation.Execute(new ApplyDesignCommand(design));

        Assert.Equal(2, presentation.Masters.Count);
        foreach (var master in presentation.Masters)
        {
            Assert.Equal(design.Colors, master.Theme.Colors);
            Assert.Equal(design.MajorFont, master.Theme.MajorFont);
            Assert.Equal(design.MinorFont, master.Theme.MinorFont);
            Assert.Equal(design.Background, master.Background);
            Assert.Equal("Test", master.Theme.Name);
        }

        Assert.Equal("#123456", presentation.Layouts[0].Background.Color);
        presentation.Undo.Undo();
        presentation.Execute(new ApplyDesignCommand(design, [presentation.Masters[1]]));
        Assert.Equal("#000000", presentation.Masters[0].Theme.Colors[0]);
        Assert.Equal(design.Colors, presentation.Masters[1].Theme.Colors);
    }

    [Fact]
    public void ApplyDesign_UndoRedo_RestoresColorsFontsBackground()
    {
        var presentation = Builder().Load();
        var before = Snapshot(presentation);
        var shape = (AutoShape)presentation.Slides[0].Shapes[0];
        var run = shape.Text!.Paragraphs[0].Runs[0];
        Assert.Equal("#4472C4", shape.EffectiveFormat(run).Color);
        var design = SlideDesigns.BuiltIn[1];

        presentation.Execute(new ApplyDesignCommand(design));
        var applied = Snapshot(presentation);
        Assert.Equal(design.Colors[4], shape.EffectiveFormat(run).Color);
        Assert.Equal(design.MajorFont, ((AutoShape)presentation.Slides[1].Shapes[0]).EffectiveFormat(((AutoShape)presentation.Slides[1].Shapes[0]).Text!.Paragraphs[0].Runs[0]).Font);

        presentation.Undo.Undo();
        Assert.Equal(before, Snapshot(presentation));
        Assert.Equal("#4472C4", shape.EffectiveFormat(run).Color);
        presentation.Undo.Redo();
        Assert.Equal(applied, Snapshot(presentation));
        Assert.Equal(design.Colors[4], shape.EffectiveFormat(run).Color);

        presentation.Execute(new ApplyDesignCommand(design));
        presentation.Execute(new ApplyThemeColorsCommand(design.Colors.Select(c => c.ToLowerInvariant()).ToList()));
        presentation.Execute(new ApplyThemeFontsCommand(design.MajorFont, design.MinorFont));
        presentation.Undo.Undo();
        Assert.Equal(before, Snapshot(presentation));
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public void ApplyDesign_BumpsVersionOfAllSlides_RaisesThemeChange()
    {
        var presentation = Builder().Load();
        var versions = presentation.Slides.Select(s => s.Version).ToList();
        PresentationChangedEventArgs? args = null;
        presentation.Changed += (_, e) => args = e;

        presentation.Execute(new ApplyDesignCommand(SlideDesigns.BuiltIn[0], [presentation.Masters[1]]));

        Assert.Equal(versions.Select(v => v + 1), presentation.Slides.Select(s => s.Version));
        Assert.True(args!.Kind.HasFlag(PresentationChangeKind.Theme));
        Assert.Equal(presentation.Slides.ToHashSet(), args.Slides.ToHashSet());
        args = null;
        presentation.Undo.Undo();
        Assert.Equal(versions.Select(v => v + 2), presentation.Slides.Select(s => s.Version));
        Assert.True(args!.Kind.HasFlag(PresentationChangeKind.Theme));
    }

    [Theory]
    [MemberData(nameof(CommandNames))]
    public void Command_ApplyThenRevert_RestoresModel(string name)
    {
        var presentation = Builder().Load();
        var before = Snapshot(presentation);
        var command = Cases[name](presentation);

        command.Apply(presentation);
        var applied = Snapshot(presentation);
        Assert.NotEqual(before, applied);
        command.Revert(presentation);
        Assert.Equal(before, Snapshot(presentation));
        command.Apply(presentation);
        Assert.Equal(applied, Snapshot(presentation));
    }

    [Fact]
    public void ThemeCommands_InvalidInput_ThrowWithoutChange()
    {
        var presentation = Builder().Load();
        var before = Snapshot(presentation);
        var other = Builder().Load();
        var design = SlideDesigns.BuiltIn[0];

        Assert.Throws<ArgumentException>(() => presentation.Execute(new ApplyThemeColorsCommand(Palette.Take(11).ToList())));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new ApplyThemeColorsCommand([.. Palette[..11], "red"])));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new ApplyThemeFontsCommand(" ", "Calibri")));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new ApplyDesignCommand(design, [other.Masters[0]])));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new ApplyDesignCommand(design with { Background = new SlideBackground(ShapeFillKind.Picture, null, "/ppt/media/x.png") })));
        Assert.Throws<ArgumentNullException>(() => presentation.Execute(new ApplyDesignCommand(null!)));
        Assert.Equal(before, Snapshot(presentation));
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public void Create_WithDesign_UsesItsThemeAndBackground()
    {
        foreach (var design in SlideDesigns.BuiltIn)
        {
            var presentation = Presentation.Create(design);
            var master = Assert.Single(presentation.Masters);
            Assert.Equal(design.Colors, master.Theme.Colors);
            Assert.Equal(design.MajorFont, master.Theme.MajorFont);
            Assert.Equal(design.MinorFont, master.Theme.MinorFont);
            Assert.Equal(design.Background, master.Background);
            Assert.Single(presentation.Slides);
            Assert.False(presentation.Undo.CanUndo);
            Assert.False(presentation.Slides[0].Modified);
        }

        Assert.Equal("#4472C4", Presentation.Create().Masters[0].Theme.Colors[4]);
    }
}
