using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlideEditor
{
    private static readonly (string Id, string Text, long Width, long Height)[] SlideSizes =
    [
        ("pp-slide-size-standard", "Standard (4:3)", 9144000, 6858000),
        ("pp-slide-size-widescreen", "Widescreen (16:9)", 12192000, 6858000)
    ];

    private SlideDesign CurrentDesign => Matching((master, design) => SameColors(master, design) && SameFonts(master, design) && Equals(master.Background, design.Background));

    private SlideDesign CurrentColors => Matching(SameColors);

    private SlideDesign CurrentFonts => Matching(SameFonts);

    private SlideDesign Matching(Func<SlideMaster, SlideDesign, bool> match) =>
        CurrentMaster is { } master ? SlideDesigns.BuiltIn.FirstOrDefault(design => match(master, design)) : null;

    private static bool SameColors(SlideMaster master, SlideDesign design) => master.Theme.Colors.SequenceEqual(design.Colors, StringComparer.OrdinalIgnoreCase);

    private static bool SameFonts(SlideMaster master, SlideDesign design) => master.Theme.MajorFont == design.MajorFont && master.Theme.MinorFont == design.MinorFont;

    private Task ApplyDesignAsync(SlideDesign design) => RunCommandAsync(new ApplyDesignCommand(design));

    private Task ApplyColorsAsync(SlideDesign design) => RunCommandAsync(new ApplyThemeColorsCommand(design.Colors));

    private Task ApplyFontsAsync(SlideDesign design) => RunCommandAsync(new ApplyThemeFontsCommand(design.MajorFont, design.MinorFont));

    private Task SetSlideSizeAsync(long width, long height) => RunCommandAsync(new SetSlideSizeCommand(width, height, true));
}
