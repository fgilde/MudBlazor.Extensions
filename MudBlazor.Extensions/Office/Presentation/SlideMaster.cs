using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A slide master with its theme, text styles, shapes and layouts.</summary>
public sealed class SlideMaster
{
    private List<SlideShape> _shapes;

    internal SlideMaster()
    {
    }

    /// <summary>The master name, or null.</summary>
    public string Name { get; internal set; }

    /// <summary>The layouts of this master in file order.</summary>
    public IReadOnlyList<SlideLayout> Layouts => LayoutList;

    /// <summary>The master's shapes that are not placeholders, such as logos, bands and lines; they are drawn behind layout and slide shapes.</summary>
    public IReadOnlyList<SlideShape> Shapes => _shapes ??= ShapeList.Where(s => s.Placeholder == null).ToList();

    /// <summary>The master's background, or null if it has none.</summary>
    public SlideBackground Background { get; internal set; }

    /// <summary>The theme of this master and its layouts and slides.</summary>
    public PresentationTheme Theme { get; internal set; } = PresentationTheme.Default;

    internal PresentationTheme LoadedTheme { get; set; } = PresentationTheme.Default;

    internal SlideBackground LoadedBackground { get; set; }

    internal string ThemePart { get; set; }

    internal XElement FormatScheme { get; set; }

    internal List<SlideLayout> LayoutList { get; } = [];

    internal List<SlideShape> ShapeList { get; } = [];

    internal string PartName { get; set; }

    internal IReadOnlyDictionary<string, string> ColorMap { get; set; } = new Dictionary<string, string>();

    internal XElement TextStyles { get; set; }

    internal XElement DefaultTextStyle { get; set; }
}
