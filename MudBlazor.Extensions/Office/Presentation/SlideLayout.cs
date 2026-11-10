namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A slide layout; its placeholders give slides their default positions and text styles.</summary>
public sealed class SlideLayout
{
    private List<SlideShape> _shapes;

    internal SlideLayout()
    {
    }

    /// <summary>The layout name, for example <c>Title and Content</c>, or null.</summary>
    public string Name { get; internal set; }

    /// <summary>The layout type, for example <c>title</c>, <c>obj</c> or <c>blank</c>, or null.</summary>
    public string Type { get; internal set; }

    /// <summary>The master this layout belongs to.</summary>
    public SlideMaster Master { get; internal set; }

    /// <summary>The layout's shapes that are not placeholders, such as logos, bands and lines; they are drawn behind the slide's shapes.</summary>
    public IReadOnlyList<SlideShape> Shapes => _shapes ??= ShapeList.Where(s => s.Placeholder == null).ToList();

    /// <summary>The layout's own background, or null if it is inherited from the master.</summary>
    public SlideBackground Background { get; internal set; }

    internal bool ShowMasterShapes { get; set; } = true;

    internal List<SlideShape> ShapeList { get; } = [];

    internal string PartName { get; set; }
}
