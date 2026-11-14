namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>The theme of a slide master: its colour scheme and fonts.</summary>
public sealed class PresentationTheme
{
    internal static readonly string[] Slots = ["dk1", "lt1", "dk2", "lt2", "accent1", "accent2", "accent3", "accent4", "accent5", "accent6", "hlink", "folHlink"];

    internal static readonly PresentationTheme Default = FromPalette(null, ThemeColors.Office, null, null);

    private PresentationTheme(string name, string[] colors, string majorFont, string minorFont)
    {
        Name = name;
        Colors = Array.AsReadOnly(colors);
        MajorFont = majorFont;
        MinorFont = minorFont;
        Palette = ThemeColors.SchemeOrder.Select(n => colors[Array.IndexOf(Slots, n)][1..]).ToArray();
    }

    /// <summary>The theme name from the file, or null.</summary>
    public string Name { get; }

    /// <summary>The twelve scheme colours as <c>#RRGGBB</c> in the order dk1, lt1, dk2, lt2, accent1 to accent6, hlink, folHlink.</summary>
    public IReadOnlyList<string> Colors { get; }

    /// <summary>The heading font, or null.</summary>
    public string MajorFont { get; }

    /// <summary>The body font, or null.</summary>
    public string MinorFont { get; }

    internal string[] Palette { get; }

    internal static PresentationTheme FromPalette(string name, string[] palette, string majorFont, string minorFont) =>
        new(name, Slots.Select(n => "#" + palette[Array.IndexOf(ThemeColors.SchemeOrder, n)]).ToArray(), majorFont, minorFont);

    internal static string[] Normalize(IReadOnlyList<string> colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        if (colors.Count != Slots.Length) throw new ArgumentException($"A theme needs {Slots.Length} colours.", nameof(colors));
        return colors.Select(c => c is { Length: 7 } && c[0] == '#' && ThemeColors.Hex(c[1..]) is { } hex
            ? "#" + hex
            : throw new ArgumentException($"'{c}' is not a #RRGGBB colour.", nameof(colors))).ToArray();
    }

    internal PresentationTheme With(string[] colors = null, string majorFont = null, string minorFont = null) =>
        new(Name, colors ?? Colors.ToArray(), majorFont ?? MajorFont, minorFont ?? MinorFont);

    internal bool Matches(PresentationTheme other) => Colors.SequenceEqual(other.Colors) && MajorFont == other.MajorFont && MinorFont == other.MinorFont;
}
