namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>Adds a slide based on a layout. The slide gets the layout's placeholders as empty shapes; date, footer and slide number are left out.</summary>
/// <param name="index">The position of the new slide, from 0 to the slide count.</param>
/// <param name="layout">The layout.</param>
public sealed class AddSlideCommand(int index, SlideLayout layout) : IDocumentCommand<Presentation>
{
    /// <summary>The created slide, available after the command was applied.</summary>
    public Slide Slide { get; private set; }

    /// <inheritdoc />
    public string Description => "New slide";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (index < 0 || index > document.SlideList.Count) throw new ArgumentOutOfRangeException(nameof(index));
        Slide ??= Create();
        document.SlideList.Insert(index, Slide);
        document.Notify(Slide, PresentationChangeKind.SlidesAdded);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        document.SlideList.Remove(Slide);
        document.Notify(Slide, PresentationChangeKind.SlidesRemoved);
    }

    private Slide Create()
    {
        var slide = new Slide { Layout = layout };
        var id = 2u;
        foreach (var placeholder in layout.ShapeList.OfType<AutoShape>().Where(s => s.Placeholder is { Type: not ("dt" or "ftr" or "sldNum" or "hdr") }))
            slide.ShapeList.Add(new AutoShape
            {
                Id = id++, Name = placeholder.Name, Placeholder = placeholder.Placeholder, InheritsFrom = placeholder, Master = layout.Master,
                Geometry = placeholder.Geometry, Adjustments = placeholder.Adjustments, Text = new TextBody { Paragraphs = [new TextParagraph()] }
            });
        return slide;
    }
}

/// <summary>Copies slides; the copies are inserted as a block after the last of them, in show order.</summary>
/// <param name="slides">The slides.</param>
public sealed class DuplicateSlidesCommand(IReadOnlyList<Slide> slides) : IDocumentCommand<Presentation>
{
    /// <summary>The copies in show order, available after the command was applied.</summary>
    public IReadOnlyList<Slide> Copies { get; private set; }

    /// <inheritdoc />
    public string Description => "Duplicate slide";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        var ordered = SlideEdit.Ordered(document, slides);
        Copies ??= ordered.Select(Copy).ToList();
        document.SlideList.InsertRange(document.SlideList.IndexOf(ordered[^1]) + 1, Copies);
        foreach (var copy in Copies) document.Notify(copy, PresentationChangeKind.SlidesAdded);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        foreach (var copy in Copies)
        {
            document.SlideList.Remove(copy);
            document.Notify(copy, PresentationChangeKind.SlidesRemoved);
        }
    }

    private static Slide Copy(Slide source)
    {
        var copy = new Slide
        {
            Layout = source.Layout, Notes = source.Notes, HasNotesPart = source.HasNotesPart, Hidden = source.Hidden, CopyOf = source.CopyOf ?? source,
            Background = source.Background, ShowMasterShapes = source.ShowMasterShapes
        };
        copy.ShapeList.AddRange(source.ShapeList.Select(s => s.Clone(null)));
        return copy;
    }
}

/// <summary>Deletes slides. Deleting every slide is allowed.</summary>
/// <param name="slides">The slides.</param>
public sealed class DeleteSlidesCommand(IReadOnlyList<Slide> slides) : IDocumentCommand<Presentation>
{
    private List<(int Index, Slide Slide)> _removed;

    /// <inheritdoc />
    public string Description => "Delete slide";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        _removed = SlideEdit.Ordered(document, slides).Select(s => (document.SlideList.IndexOf(s), s)).ToList();
        for (var i = _removed.Count - 1; i >= 0; i--)
        {
            document.SlideList.RemoveAt(_removed[i].Index);
            document.Notify(_removed[i].Slide, PresentationChangeKind.SlidesRemoved);
        }
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        foreach (var (index, slide) in _removed)
        {
            document.SlideList.Insert(index, slide);
            document.Notify(slide, PresentationChangeKind.SlidesAdded);
        }
    }
}

/// <summary>Moves slides as a block to a new position; they keep their relative show order.</summary>
/// <param name="slides">The slides.</param>
/// <param name="targetIndex">The position in the current slide list before which the slides are placed, from 0 to the slide count.</param>
public sealed class MoveSlidesCommand(IReadOnlyList<Slide> slides, int targetIndex) : IDocumentCommand<Presentation>
{
    private Slide[] _before;

    /// <inheritdoc />
    public string Description => "Move slide";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        var ordered = SlideEdit.Ordered(document, slides);
        if (targetIndex < 0 || targetIndex > document.SlideList.Count) throw new ArgumentOutOfRangeException(nameof(targetIndex));
        _before = document.SlideList.ToArray();
        var at = targetIndex - ordered.Count(s => document.SlideList.IndexOf(s) < targetIndex);
        document.SlideList.RemoveAll(ordered.Contains);
        document.SlideList.InsertRange(at, ordered);
        foreach (var slide in ordered) document.Notify(slide, PresentationChangeKind.SlideOrder);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        document.SlideList.Clear();
        document.SlideList.AddRange(_before);
        foreach (var slide in slides) document.Notify(slide, PresentationChangeKind.SlideOrder);
    }
}

/// <summary>Hides slides in the slide show or shows them again.</summary>
/// <param name="slides">The slides.</param>
/// <param name="hidden">True to hide the slides.</param>
public sealed class SetSlideHiddenCommand(IReadOnlyList<Slide> slides, bool hidden) : IDocumentCommand<Presentation>
{
    private List<(Slide Slide, bool Hidden)> _before;

    /// <inheritdoc />
    public string Description => hidden ? "Hide slide" : "Show slide";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        _before = SlideEdit.Ordered(document, slides).Select(s => (s, s.Hidden)).ToList();
        foreach (var (slide, _) in _before)
        {
            slide.Hidden = hidden;
            document.Notify(slide, PresentationChangeKind.SlideOrder);
        }
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        foreach (var (slide, old) in _before)
        {
            slide.Hidden = old;
            document.Notify(slide, PresentationChangeKind.SlideOrder);
        }
    }
}

/// <summary>Sets the speaker notes of a slide. A slide without notes gets a notes slide when the presentation is saved, which needs a notes master.</summary>
/// <param name="slide">The slide.</param>
/// <param name="text">The notes, paragraphs separated by newlines.</param>
public sealed class SetNotesCommand(Slide slide, string text) : IDocumentCommand<Presentation>
{
    private string _before;

    /// <inheritdoc />
    public string Description => "Edit notes";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        if (!document.CanHaveNotes(slide))
            throw new InvalidOperationException("The slide has no notes and the presentation has no notes master.");
        _before = slide.Notes;
        slide.Notes = (text ?? "").ReplaceLineEndings("\n");
        document.Notify(slide, PresentationChangeKind.Notes);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        slide.Notes = _before;
        document.Notify(slide, PresentationChangeKind.Notes);
    }
}

internal static class SlideEdit
{
    internal static List<Slide> Ordered(Presentation presentation, IReadOnlyList<Slide> slides)
    {
        if (slides == null || slides.Count == 0) throw new ArgumentException("No slides given.", nameof(slides));
        if (slides.Any(s => !presentation.SlideList.Contains(s))) throw new ArgumentException("The slides must belong to the presentation.", nameof(slides));
        return slides.Distinct().OrderBy(presentation.SlideList.IndexOf).ToList();
    }
}
