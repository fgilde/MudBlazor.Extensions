using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Shows one notebook page as a free surface of absolutely positioned elements. The surface size is computed from the model: the larger of the
/// viewport and the content plus a 400 px margin to the right and bottom, so it grows and shrinks with the content and never with its own CSS
/// layout. An element renders again only when its id or <see cref="NoteElement.Version"/> changes.
/// </summary>
public partial class MudExNotebookPageView
{
    private object _rendered;
    private (double Width, double Height) _size;
    private NotebookImageUrls _ownImages;
    private Notebook _ownImagesOf;

    /// <summary>The notebook the page belongs to.</summary>
    [Parameter] public Notebook Notebook { get; set; }

    /// <summary>The page to show. It renders again only when its <see cref="NotebookPage.Version"/> or its computed size changes.</summary>
    [Parameter] public NotebookPage Page { get; set; }

    /// <summary>The zoom factor; 1 is 100 %.</summary>
    [Parameter] public double Zoom { get; set; } = 1;

    /// <summary>True if the elements take pointer events, as in the editor.</summary>
    [Parameter] public bool Interactive { get; set; }

    /// <summary>The width of the visible area in CSS pixels; the page is at least this wide.</summary>
    [Parameter] public double ViewportWidth { get; set; }

    /// <summary>The height of the visible area in CSS pixels; the page is at least this high.</summary>
    [Parameter] public double ViewportHeight { get; set; }

    /// <summary>
    /// The rendered heights of text and table elements by element id, in CSS pixels at 100 % zoom. Elements without a measured height use an
    /// estimate from their line count and font size.
    /// </summary>
    [Parameter] public IReadOnlyDictionary<string, double> MeasuredHeights { get; set; }

    /// <summary>The image URLs of the notebook; if null, the view uses data URIs.</summary>
    [Parameter] public NotebookImageUrls Images { get; set; }

    private double EffectiveZoom => ZoomOf(Zoom);

    internal static double ZoomOf(double zoom) => zoom > 0 && double.IsFinite(zoom) ? zoom : 1;

    private NotebookImageUrls ImageUrls => Images ?? _ownImages;

    internal static double HeightOf(NoteElement element, IReadOnlyDictionary<string, double> measured)
    {
        if (element.Height > 0 || element is not (TextElement or TableElement)) return element.Height;
        return measured != null && measured.TryGetValue(element.Id, out var height) && double.IsFinite(height) && height >= 0
            ? height
            : NotebookStyles.EstimateHeight(element);
    }

    internal static (double Width, double Height) SizeOf(NotebookPage page, IReadOnlyDictionary<string, double> measured, double minWidth, double minHeight)
    {
        double right = 0, bottom = 0;
        foreach (var element in page.Elements)
        {
            right = Math.Max(right, element.X + element.Width);
            bottom = Math.Max(bottom, element.Y + HeightOf(element, measured));
        }

        return (Math.Max(Finite(minWidth), right + NotebookStyles.PageMargin), Math.Max(Finite(minHeight), bottom + NotebookStyles.PageMargin));
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Images == null && Notebook != null && !ReferenceEquals(_ownImagesOf, Notebook)) (_ownImages, _ownImagesOf) = (new NotebookImageUrls(Notebook), Notebook);
        _size = Page == null ? default : SizeOf(Page, MeasuredHeights, ViewportWidth / EffectiveZoom, ViewportHeight / EffectiveZoom);
        _rendered ??= Key;
    }

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        var key = Key;
        if (Equals(key, _rendered)) return false;
        _rendered = key;
        return true;
    }

    private object Key => (Notebook, Page, Page?.Version, Page?.LoadError, EffectiveZoom, Interactive, (ImageUrls, ImageUrls?.Version, _size, Class, Style));

    private static double Finite(double value) => double.IsFinite(value) ? value : 0;
}
