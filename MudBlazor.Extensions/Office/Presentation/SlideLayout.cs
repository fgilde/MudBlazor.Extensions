namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A slide layout; its placeholders give slides their default positions and text styles.</summary>
public sealed class SlideLayout
{
    internal SlideLayout()
    {
    }

    /// <summary>The layout name, for example <c>Title and Content</c>, or null.</summary>
    public string Name { get; internal set; }

    /// <summary>The layout type, for example <c>title</c>, <c>obj</c> or <c>blank</c>, or null.</summary>
    public string Type { get; internal set; }

    /// <summary>The master this layout belongs to.</summary>
    public SlideMaster Master { get; internal set; }

    /// <summary>The layout's shapes, including its placeholders.</summary>
    public IReadOnlyList<SlideShape> Shapes => ShapeList;

    internal List<SlideShape> ShapeList { get; } = [];

    internal string PartName { get; set; }
}
