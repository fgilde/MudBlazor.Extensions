namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>Adds a slide based on a layout. The slide gets the layout's placeholders as empty shapes; date, footer and slide number are left out.</summary>
/// <param name="index">The position of the new slide, from 0 to the slide count.</param>
/// <param name="layout">The layout.</param>
public sealed class AddSlideCommand(int index, SlideLayout layout) : IDocumentCommand<Presentation>
{
    private SectionState _sections;

    /// <summary>The created slide, available after the command was applied.</summary>
    public Slide Slide { get; private set; }

    /// <inheritdoc />
    public string Description => "New slide";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (index < 0 || index > document.SlideList.Count) throw new ArgumentOutOfRangeException(nameof(index));
        Slide ??= Create(document);
        _sections = new SectionState(document);
        document.SlideList.Insert(index, Slide);
        _sections.Inserted(index, 1);
        document.Notify(Slide, PresentationChangeKind.SlidesAdded);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        document.SlideList.Remove(Slide);
        _sections.Restore();
        document.Notify(Slide, PresentationChangeKind.SlidesRemoved);
    }

    private Slide Create(Presentation document)
    {
        var slide = new Slide { Layout = layout, Owner = document };
        var id = 2u;
        foreach (var placeholder in SlideEdit.ContentPlaceholders(layout)) slide.ShapeList.Add(SlideEdit.EmptyPlaceholder(placeholder, id++));
        return slide;
    }
}

/// <summary>Copies slides; the copies are inserted as a block after the last of them, in show order.</summary>
/// <param name="slides">The slides.</param>
public sealed class DuplicateSlidesCommand(IReadOnlyList<Slide> slides) : IDocumentCommand<Presentation>
{
    private SectionState _sections;

    /// <summary>The copies in show order, available after the command was applied.</summary>
    public IReadOnlyList<Slide> Copies { get; private set; }

    /// <inheritdoc />
    public string Description => "Duplicate slide";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        var ordered = SlideEdit.Ordered(document, slides);
        Copies ??= ordered.Select(Copy).ToList();
        var at = document.SlideList.IndexOf(ordered[^1]) + 1;
        _sections = new SectionState(document);
        document.SlideList.InsertRange(at, Copies);
        _sections.Inserted(at, Copies.Count);
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

        _sections.Restore();
    }

    private static Slide Copy(Slide source)
    {
        var copy = new Slide
        {
            Layout = source.Layout, Notes = source.Notes, HasNotesPart = source.HasNotesPart, Hidden = source.Hidden, CopyOf = source.CopyOf ?? source,
            ReservedId = source.ReservedId, Owner = source.Owner, Background = source.Background, ShowMasterShapes = source.ShowMasterShapes, ColorMap = source.ColorMap,
            Transition = source.Transition
        };
        copy.ShapeList.AddRange(source.ShapeList.Select(s => s.Clone(null)));
        copy.AnimationList.AddRange(source.AnimationList);
        return copy;
    }
}

/// <summary>Deletes slides. Deleting every slide is allowed.</summary>
/// <param name="slides">The slides.</param>
public sealed class DeleteSlidesCommand(IReadOnlyList<Slide> slides) : IDocumentCommand<Presentation>
{
    private List<(int Index, Slide Slide)> _removed;
    private SectionState _sections;

    /// <inheritdoc />
    public string Description => "Delete slide";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        _removed = SlideEdit.Ordered(document, slides).Select(s => (document.SlideList.IndexOf(s), s)).ToList();
        _sections = new SectionState(document);
        for (var i = _removed.Count - 1; i >= 0; i--)
        {
            _sections.Removing(_removed[i].Index);
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

        _sections.Restore();
    }
}

/// <summary>Moves slides as a block to a new position; they keep their relative show order.</summary>
/// <param name="slides">The slides.</param>
/// <param name="targetIndex">The position in the current slide list before which the slides are placed, from 0 to the slide count.</param>
public sealed class MoveSlidesCommand(IReadOnlyList<Slide> slides, int targetIndex) : IDocumentCommand<Presentation>
{
    private Slide[] _before;
    private SectionState _sections;

    /// <inheritdoc />
    public string Description => "Move slide";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        var ordered = SlideEdit.Ordered(document, slides);
        if (targetIndex < 0 || targetIndex > document.SlideList.Count) throw new ArgumentOutOfRangeException(nameof(targetIndex));
        var at = targetIndex - ordered.Count(s => document.SlideList.IndexOf(s) < targetIndex);
        var result = document.SlideList.Except(ordered).ToList();
        result.InsertRange(at, ordered);
        if (result.SequenceEqual(document.SlideList)) return;
        _before = document.SlideList.ToArray();
        _sections = new SectionState(document);
        for (var i = ordered.Count - 1; i >= 0; i--)
        {
            var index = document.SlideList.IndexOf(ordered[i]);
            _sections.Removing(index);
            document.SlideList.RemoveAt(index);
        }

        document.SlideList.InsertRange(at, ordered);
        _sections.Inserted(at, ordered.Count);
        foreach (var slide in ordered) document.Notify(slide, PresentationChangeKind.SlideOrder);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        document.SlideList.Clear();
        document.SlideList.AddRange(_before);
        _sections.Restore();
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

    internal static IEnumerable<AutoShape> ContentPlaceholders(SlideLayout layout) =>
        layout.ShapeList.OfType<AutoShape>().Where(s => s.Placeholder != null && !IsFooter(s.Placeholder));

    internal static ShapeTransform? InheritedTransform(SlideShape shape) => shape is AutoShape ? null : shape.InheritsFrom?.ResolvedTransform ?? shape.Transform;

    internal static bool IsFooter(PlaceholderRef placeholder) => placeholder.Type is "dt" or "ftr" or "sldNum" or "hdr";

    internal static AutoShape EmptyPlaceholder(AutoShape placeholder, uint id, TextBody text = null) => new()
    {
        Id = id, Name = placeholder.Name, Placeholder = placeholder.Placeholder, InheritsFrom = placeholder, Master = placeholder.Master,
        Geometry = placeholder.Geometry, Adjustments = placeholder.Adjustments, Text = text ?? new TextBody { Paragraphs = [new TextParagraph()] }
    };
}
