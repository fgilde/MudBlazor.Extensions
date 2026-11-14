namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>Applies a design to slide masters: theme colours, theme fonts and the master background. Layouts and slides with their own background keep it.</summary>
/// <param name="design">The design.</param>
/// <param name="masters">The masters to change, or null for all masters.</param>
public sealed class ApplyDesignCommand(SlideDesign design, IReadOnlyList<SlideMaster> masters = null) : IDocumentCommand<Presentation>
{
    private readonly ThemeChanges _changes = new();

    /// <inheritdoc />
    public string Description => "Apply design";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        ArgumentNullException.ThrowIfNull(design);
        var colors = PresentationTheme.Normalize(design.Colors);
        ThemeChanges.ValidateFont(design.MajorFont);
        ThemeChanges.ValidateFont(design.MinorFont);
        if (ThemeXml.Background(design.Background) == null) throw new ArgumentException("The background must be a solid colour, a gradient or no fill.", nameof(design));
        _changes.Apply(document, masters, theme => theme.With(colors, design.MajorFont, design.MinorFont), design.Background);
    }

    /// <inheritdoc />
    public void Revert(Presentation document) => _changes.Revert(document);
}

/// <summary>Sets the theme colours of slide masters.</summary>
/// <param name="colors">The twelve colours as <c>#RRGGBB</c> in the order of <see cref="PresentationTheme.Colors"/>.</param>
/// <param name="masters">The masters to change, or null for all masters.</param>
public sealed class ApplyThemeColorsCommand(IReadOnlyList<string> colors, IReadOnlyList<SlideMaster> masters = null) : IDocumentCommand<Presentation>
{
    private readonly ThemeChanges _changes = new();

    /// <inheritdoc />
    public string Description => "Theme colors";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        var normalized = PresentationTheme.Normalize(colors);
        _changes.Apply(document, masters, theme => theme.With(normalized), null);
    }

    /// <inheritdoc />
    public void Revert(Presentation document) => _changes.Revert(document);
}

/// <summary>Sets the theme fonts of slide masters.</summary>
/// <param name="major">The heading font.</param>
/// <param name="minor">The body font.</param>
/// <param name="masters">The masters to change, or null for all masters.</param>
public sealed class ApplyThemeFontsCommand(string major, string minor, IReadOnlyList<SlideMaster> masters = null) : IDocumentCommand<Presentation>
{
    private readonly ThemeChanges _changes = new();

    /// <inheritdoc />
    public string Description => "Theme fonts";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        ThemeChanges.ValidateFont(major);
        ThemeChanges.ValidateFont(minor);
        _changes.Apply(document, masters, theme => theme.With(majorFont: major, minorFont: minor), null);
    }

    /// <inheritdoc />
    public void Revert(Presentation document) => _changes.Revert(document);
}

internal sealed class ThemeChanges
{
    private readonly List<(SlideMaster Master, PresentationTheme Theme, SlideBackground Background)> _before = [];

    internal static void ValidateFont(string font)
    {
        if (string.IsNullOrWhiteSpace(font)) throw new ArgumentException("A font name is required.", nameof(font));
    }

    internal void Apply(Presentation document, IReadOnlyList<SlideMaster> masters, Func<PresentationTheme, PresentationTheme> change, SlideBackground background)
    {
        if (masters?.Any(m => !document.MasterList.Contains(m)) == true) throw new ArgumentException("The masters must belong to the presentation.", nameof(masters));
        _before.Clear();
        foreach (var master in (masters ?? document.MasterList).Distinct())
        {
            var theme = change(master.Theme);
            var fill = background ?? master.Background;
            if (theme.Matches(master.Theme) && Equals(fill, master.Background)) continue;
            _before.Add((master, master.Theme, master.Background));
            master.Theme = theme;
            master.Background = fill;
        }

        Notify(document);
    }

    internal void Revert(Presentation document)
    {
        foreach (var (master, theme, background) in _before)
        {
            master.Theme = theme;
            master.Background = background;
        }

        Notify(document);
    }

    private void Notify(Presentation document)
    {
        if (_before.Count == 0) return;
        document.Notify(null, PresentationChangeKind.Theme);
        foreach (var slide in document.SlideList) document.Notify(slide, PresentationChangeKind.Theme);
    }
}
