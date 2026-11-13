namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>
/// Gives slides another layout. The placeholders of a slide are matched to those of the new layout by type, in order for the same type, and then take their
/// position from the layout (tables, charts and pictures get it as their own position). Missing placeholders are added empty. Placeholders without a match are
/// removed when empty and otherwise keep their position.
/// </summary>
/// <param name="slides">The slides.</param>
/// <param name="layout">The new layout.</param>
public sealed class SetSlideLayoutCommand(IReadOnlyList<Slide> slides, SlideLayout layout) : IDocumentCommand<Presentation>
{
    private readonly SlideChanges _changes = new();

    /// <inheritdoc />
    public string Description => "Change layout";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (!document.LayoutList.Contains(layout)) throw new ArgumentException("The layout must belong to the presentation.", nameof(layout));
        _changes.Apply(document, SlideEdit.Ordered(document, slides).Where(s => s.Layout != layout), Change);
    }

    /// <inheritdoc />
    public void Revert(Presentation document) => _changes.Revert(document);

    private void Change(Slide slide)
    {
        var free = layout.ShapeList.OfType<AutoShape>().Where(s => s.Placeholder != null).ToList();
        var placeholders = slide.ShapeList.Where(s => s.Placeholder != null).ToList();
        var matches = new Dictionary<SlideShape, AutoShape>();
        foreach (var key in new Func<string, string>[] { type => type, ShapeReader.Category })
        foreach (var shape in placeholders.Where(s => !matches.ContainsKey(s)))
            if (free.Find(p => key(p.Placeholder.Type) == key(shape.Placeholder.Type) && Accepts(p, shape)) is { } target)
            {
                matches[shape] = target;
                free.Remove(target);
            }

        foreach (var shape in placeholders)
            if (matches.TryGetValue(shape, out var target))
            {
                shape.Placeholder = target.Placeholder;
                shape.InheritsFrom = target;
                shape.Transform = SlideEdit.InheritedTransform(shape);
            }
            else if (IsEmpty(shape)) slide.ShapeList.Remove(shape);
            else shape.Transform = shape.ResolvedTransform;

        foreach (var placeholder in free.Where(p => !SlideEdit.IsFooter(p.Placeholder)))
            slide.ShapeList.Add(SlideEdit.EmptyPlaceholder(placeholder, ShapeEdit.NextId(slide)));
        if (slide.Layout?.Master != layout.Master)
            foreach (var shape in SlideShape.Flatten(slide.ShapeList))
                shape.Master = layout.Master;
        slide.Layout = layout;
    }

    private static bool Accepts(AutoShape target, SlideShape shape) =>
        shape is AutoShape || target.Placeholder.Type == shape.Placeholder.Type || target.Placeholder.Type == "obj";

    private static bool IsEmpty(SlideShape shape) =>
        shape is AutoShape auto && string.IsNullOrWhiteSpace(string.Concat(auto.Text?.Paragraphs.SelectMany(p => p.Runs)
            .Where(r => r.FieldType == null || !SlideEdit.IsFooter(shape.Placeholder)).Select(r => r.Text) ?? []));
}

/// <summary>
/// Resets the placeholders of slides to their layout: their own positions and text body settings are removed. Tables, charts and pictures get the layout
/// position as their own position.
/// </summary>
/// <param name="slides">The slides.</param>
public sealed class ResetSlideCommand(IReadOnlyList<Slide> slides) : IDocumentCommand<Presentation>
{
    private readonly SlideChanges _changes = new();

    /// <inheritdoc />
    public string Description => "Reset slide";

    /// <inheritdoc />
    public void Apply(Presentation document) => _changes.Apply(document, SlideEdit.Ordered(document, slides), Reset);

    /// <inheritdoc />
    public void Revert(Presentation document) => _changes.Revert(document);

    private static void Reset(Slide slide)
    {
        foreach (var shape in slide.ShapeList.Where(s => s.Placeholder != null && s.InheritsFrom != null))
        {
            shape.Transform = SlideEdit.InheritedTransform(shape);
            if (shape is not AutoShape { Text: { } text } auto) continue;
            var reset = text with { Anchor = null, InsetLeft = null, InsetTop = null, InsetRight = null, InsetBottom = null, Wrap = null };
            if (reset != text) auto.Text = reset;
        }
    }
}

internal sealed class SlideChanges
{
    private List<(SlideSnapshot Before, SlideSnapshot After)> _changes;

    internal void Apply(Presentation document, IEnumerable<Slide> slides, Action<Slide> change)
    {
        if (_changes == null)
        {
            _changes = [];
            foreach (var slide in slides)
            {
                var before = new SlideSnapshot(slide);
                change(slide);
                var after = new SlideSnapshot(slide);
                if (!after.SameAs(before)) _changes.Add((before, after));
            }
        }
        else
            foreach (var (_, after) in _changes)
                after.Restore();

        foreach (var (before, _) in _changes) document.Notify(before.Slide, PresentationChangeKind.Content);
    }

    internal void Revert(Presentation document)
    {
        foreach (var (before, _) in _changes)
        {
            before.Restore();
            document.Notify(before.Slide, PresentationChangeKind.Content);
        }
    }
}

internal sealed class SlideSnapshot
{
    private readonly SlideLayout _layout;
    private readonly SlideShape[] _shapes;
    private readonly ShapeState[] _states;

    internal SlideSnapshot(Slide slide)
    {
        Slide = slide;
        _layout = slide.Layout;
        _shapes = slide.ShapeList.ToArray();
        _states = SlideShape.Flatten(_shapes).Select(s => new ShapeState(s, s.Placeholder, s.InheritsFrom, s.Transform, s.Master, (s as AutoShape)?.Text)).ToArray();
    }

    internal Slide Slide { get; }

    internal bool SameAs(SlideSnapshot other) => _layout == other._layout && _shapes.SequenceEqual(other._shapes) && _states.SequenceEqual(other._states);

    internal void Restore()
    {
        Slide.Layout = _layout;
        Slide.ShapeList.Clear();
        Slide.ShapeList.AddRange(_shapes);
        foreach (var state in _states)
        {
            var shape = state.Shape;
            (shape.Placeholder, shape.InheritsFrom, shape.Transform, shape.Master) = (state.Placeholder, state.InheritsFrom, state.Transform, state.Master);
            if (shape is AutoShape auto) auto.Text = state.Text;
        }
    }

    private sealed record ShapeState(SlideShape Shape, PlaceholderRef Placeholder, SlideShape InheritsFrom, ShapeTransform? Transform, SlideMaster Master, TextBody Text);
}
