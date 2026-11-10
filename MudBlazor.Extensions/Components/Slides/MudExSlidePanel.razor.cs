using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>The slide panel of the slide editor: numbered thumbnails that are rendered once they scroll into view, multi-selection and a context menu.</summary>
public partial class MudExSlidePanel
{
    internal const int EagerThumbnails = 8;
    private readonly HashSet<string> _visible = [];
    private Presentation _presentation;
    private Slide _anchor;
    private MudMenu _menu;

    /// <summary>The presentation whose slides are listed.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    /// <summary>The image URLs of the presentation.</summary>
    [Parameter] public SlideImageUrls Images { get; set; }

    /// <summary>The slide shown in the editor.</summary>
    [Parameter] public Slide Current { get; set; }

    /// <summary>The selected slides.</summary>
    [Parameter] public IReadOnlyList<Slide> Selection { get; set; }

    /// <summary>Raised when the user clicks a slide.</summary>
    [Parameter] public EventCallback<SlideSelection> SelectionChanged { get; set; }

    /// <summary>False until the images are ready; no thumbnail is rendered before.</summary>
    [Parameter] public bool ShowThumbnails { get; set; } = true;

    /// <summary>If true only the first thumbnails are rendered at once, the others after the browser reported them visible.</summary>
    [Parameter] public bool LazyThumbnails { get; set; }

    /// <summary>If true slides can only be selected, not changed.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Raised for keys pressed in the panel.</summary>
    [Parameter] public EventCallback<KeyboardEventArgs> KeyDown { get; set; }

    /// <summary>Raised by the context menu entry New Slide.</summary>
    [Parameter] public EventCallback NewSlideRequested { get; set; }

    /// <summary>Raised by the context menu entry Duplicate Slide.</summary>
    [Parameter] public EventCallback DuplicateRequested { get; set; }

    /// <summary>Raised by the context menu entry Delete Slide.</summary>
    [Parameter] public EventCallback DeleteRequested { get; set; }

    /// <summary>Raised by the context menu entries Hide Slide (true) and Show Slide (false).</summary>
    [Parameter] public EventCallback<bool> HideRequested { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!ReferenceEquals(Presentation, _presentation))
        {
            _presentation = Presentation;
            _visible.Clear();
            _anchor = null;
        }

        _visible.UnionWith(Presentation?.Slides.Take(EagerThumbnails).Select(s => s.Key) ?? []);
    }

    internal void MarkVisible(IEnumerable<string> keys)
    {
        var before = _visible.Count;
        _visible.UnionWith(keys);
        if (_visible.Count != before)
            StateHasChanged();
    }

    private static string ItemClass(Slide slide, bool selected) =>
        "mud-ex-slide-panel-item" + (selected ? " mud-ex-slide-panel-selected" : "") + (slide.Hidden ? " mud-ex-slide-panel-hidden" : "");

    private Task ClickAsync(Slide slide, MouseEventArgs e)
    {
        var slides = Presentation.SlideList;
        var selection = Selection ?? [];
        var current = slide;
        IReadOnlyList<Slide> selected;
        if (!ReadOnly && e.ShiftKey && (_anchor ?? Current) is { } anchor && slides.Contains(anchor))
        {
            var (from, to) = (slides.IndexOf(anchor), slides.IndexOf(slide));
            selected = slides.Skip(Math.Min(from, to)).Take(Math.Abs(to - from) + 1).ToList();
        }
        else if (!ReadOnly && (e.CtrlKey || e.MetaKey))
        {
            _anchor = slide;
            selected = selection.Contains(slide) && selection.Count > 1
                ? selection.Where(s => s != slide).ToList()
                : selection.Append(slide).Distinct().OrderBy(slides.IndexOf).ToList();
            if (!selected.Contains(slide))
                current = selected.Contains(Current) ? Current : selected[0];
        }
        else
        {
            _anchor = slide;
            selected = [slide];
        }

        return SelectionChanged.InvokeAsync(new SlideSelection(current, selected));
    }

    private async Task ContextMenuAsync(Slide slide, MouseEventArgs e)
    {
        if (ReadOnly || _menu == null)
            return;
        if (Selection?.Contains(slide) != true)
        {
            _anchor = slide;
            await SelectionChanged.InvokeAsync(new SlideSelection(slide, [slide]));
        }

        await _menu.OpenMenuAsync(e);
    }
}

/// <summary>The slide selection of the slide panel.</summary>
/// <param name="Current">The slide to show in the editor.</param>
/// <param name="Slides">The selected slides in show order.</param>
public sealed record SlideSelection(Slide Current, IReadOnlyList<Slide> Slides);
