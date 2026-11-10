using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A reference to a placeholder of the slide layout.</summary>
/// <param name="Type">The placeholder type, for example <c>title</c>, <c>ctrTitle</c>, <c>body</c> or <c>obj</c> (the default).</param>
/// <param name="Index">The placeholder index, or null.</param>
public sealed record PlaceholderRef(string Type, uint? Index);

/// <summary>A shape on a slide, layout or master.</summary>
public abstract class SlideShape
{
    private protected SlideShape()
    {
    }

    /// <summary>The shape id, unique per slide.</summary>
    public uint Id { get; internal set; }

    /// <summary>The shape name.</summary>
    public string Name { get; internal set; } = "";

    /// <summary>The shape's own transform in its parent's coordinates; null if it is inherited from the placeholder.</summary>
    public ShapeTransform? Transform { get; internal set; }

    /// <summary>The transform in slide coordinates, with placeholder inheritance and group transforms applied.</summary>
    public ShapeTransform ResolvedTransform
    {
        get
        {
            var transform = Transform ?? InheritsFrom?.ResolvedTransform ?? default;
            for (var group = Parent; group != null; group = group.Parent) transform = group.ToParentSpace(transform);
            return transform;
        }
    }

    /// <summary>The placeholder this shape fills, or null.</summary>
    public PlaceholderRef Placeholder { get; internal set; }

    /// <summary>The group containing this shape, or null for a top-level shape.</summary>
    public GroupShape Parent { get; internal set; }

    internal SlideShape InheritsFrom { get; set; }

    internal SlideMaster Master { get; set; }

    internal XElement Source { get; set; }

    internal virtual SlideShape Clone(GroupShape parent)
    {
        var copy = (SlideShape)MemberwiseClone();
        copy.Parent = parent;
        copy.Source = Source == null ? null : new XElement(Source);
        return copy;
    }

    internal static IEnumerable<SlideShape> Flatten(IEnumerable<SlideShape> shapes) =>
        shapes.SelectMany(s => s is GroupShape g ? Flatten(g.Children).Prepend(s) : new[] { s });
}
