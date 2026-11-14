using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>The layouts of a slide master as previews: the layout's background and shapes with an outline for each placeholder.</summary>
public partial class MudExSlideLayoutGallery
{
    private readonly Dictionary<SlideLayout, Slide> _previews = [];

    /// <summary>The presentation the layouts belong to.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    /// <summary>The layouts to show.</summary>
    [Parameter] public IReadOnlyList<SlideLayout> Layouts { get; set; }

    /// <summary>The image URLs of the presentation.</summary>
    [Parameter] public SlideImageUrls Images { get; set; }

    /// <summary>The highlighted layout, usually the layout of the current slide, or null.</summary>
    [Parameter] public SlideLayout Selected { get; set; }

    /// <summary>Raised when the user picks a layout.</summary>
    [Parameter] public EventCallback<SlideLayout> LayoutChosen { get; set; }

    private Slide PreviewOf(SlideLayout layout) => _previews.TryGetValue(layout, out var slide) ? slide : _previews[layout] = new Slide { Layout = layout };

    private static IEnumerable<SlideShape> Placeholders(SlideLayout layout) =>
        layout.ShapeList.Where(s => s.Placeholder is { Type: not ("dt" or "ftr" or "sldNum" or "hdr") });

    private string Box(SlideShape shape)
    {
        var t = shape.ResolvedTransform;
        string Percent(long value, long total) => MudExSlideShapeView.Num(value * 100d / total);
        return $"left:{Percent(t.X, Presentation.SlideWidth)}%;top:{Percent(t.Y, Presentation.SlideHeight)}%;" +
               $"width:{Percent(t.Width, Presentation.SlideWidth)}%;height:{Percent(t.Height, Presentation.SlideHeight)}%;";
    }
}
