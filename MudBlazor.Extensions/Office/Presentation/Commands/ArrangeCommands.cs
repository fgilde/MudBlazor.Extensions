namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>The edge or centre line shapes are aligned to.</summary>
public enum ShapeAlignment
{
    /// <summary>Aligns the left edges.</summary>
    Left,

    /// <summary>Aligns the horizontal centres.</summary>
    Center,

    /// <summary>Aligns the right edges.</summary>
    Right,

    /// <summary>Aligns the top edges.</summary>
    Top,

    /// <summary>Aligns the vertical centres.</summary>
    Middle,

    /// <summary>Aligns the bottom edges.</summary>
    Bottom
}

/// <summary>
/// Groups shapes of the same level. The group takes the z-order position of the topmost shape, its frame is the bounding box of the
/// rotated shapes and its child space equals the frame, so the shapes keep their transforms.
/// </summary>
/// <param name="slide">The slide.</param>
/// <param name="shapes">The ids of at least two shapes with the same parent; placeholders and tables cannot be grouped.</param>
public sealed class GroupShapesCommand(Slide slide, IReadOnlyList<uint> shapes) : IDocumentCommand<Presentation>
{
    private List<SlideShape> _container;
    private SlideShape[] _members;
    private SlideShape[] _before;
    private int _index;

    /// <summary>The created group, available after the command was applied.</summary>
    public GroupShape Group { get; private set; }

    /// <inheritdoc />
    public string Description => "Group";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        if (Group == null) Create();
        _before = _container.ToArray();
        _container.RemoveAll(_members.Contains);
        foreach (var member in _members) member.Parent = Group;
        Group.ChildList.AddRange(_members);
        _container.Insert(_index, Group);
        document.Notify(slide, PresentationChangeKind.Content);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        _container.Clear();
        _container.AddRange(_before);
        Group.ChildList.Clear();
        foreach (var member in _members) member.Parent = Group.Parent;
        document.Notify(slide, PresentationChangeKind.Content);
    }

    private void Create()
    {
        var selected = ShapeEdit.Shapes(slide, shapes ?? []);
        if (selected.Count < 2) throw new ArgumentException("Grouping needs at least two shapes.", nameof(shapes));
        if (selected.Select(s => s.Parent).Distinct().Count() > 1) throw new ArgumentException("The shapes must belong to the same group or all be top-level shapes.", nameof(shapes));
        if (SlideShape.Flatten(selected).Any(s => s.Placeholder != null)) throw new InvalidOperationException("Placeholders cannot be grouped.");
        if (SlideShape.Flatten(selected).Any(s => s is TableShape)) throw new InvalidOperationException("Tables cannot be grouped.");
        _container = ShapeEdit.Container(slide, selected[0]);
        _members = _container.Where(selected.Contains).ToArray();
        _index = _container.IndexOf(_members[^1]) - (_members.Length - 1);
        var hull = ShapeEdit.Hull(_members.Select(m => m.Transform ?? default));
        var (left, top) = ((long)Math.Round(hull.Left), (long)Math.Round(hull.Top));
        var frame = new ShapeTransform(left, top, (long)Math.Round(hull.Right) - left, (long)Math.Round(hull.Bottom) - top);
        var id = ShapeEdit.NextId(slide);
        Group = new GroupShape
        {
            Id = id, Name = $"Group {id - 1}", Transform = frame, ChildTransform = frame, Parent = selected[0].Parent, Master = slide.Layout?.Master
        };
    }
}

/// <summary>
/// Dissolves groups. The children take the place of their group and keep their visible position, size, rotation and flip; their
/// transforms are converted into the coordinates of the group's parent.
/// </summary>
/// <param name="slide">The slide.</param>
/// <param name="groups">The ids of the groups.</param>
public sealed class UngroupShapesCommand(Slide slide, IReadOnlyList<uint> groups) : IDocumentCommand<Presentation>
{
    private List<(List<SlideShape> Container, int Index, GroupShape Group, SlideShape[] Children, (SlideShape Shape, ShapeTransform? Transform)[] Before)> _ungrouped;

    /// <inheritdoc />
    public string Description => "Ungroup";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        var selected = (groups ?? []).Select(id => ShapeEdit.Find<GroupShape>(slide, id)).ToHashSet();
        _ungrouped = [];
        foreach (var group in SlideShape.Flatten(slide.Shapes).OfType<GroupShape>().Where(selected.Contains).ToList())
        {
            var container = ShapeEdit.Container(slide, group);
            var index = container.IndexOf(group);
            var children = group.ChildList.ToArray();
            var descendants = SlideShape.Flatten(children).ToList();
            var visible = descendants.ToDictionary(s => s, s => s.ResolvedTransform);
            var skewed = children.OfType<GroupShape>().Where(c => Skewed(group, c)).ToList();
            _ungrouped.Add((container, index, group, children, descendants.Select(s => (s, s.Transform)).ToArray()));
            foreach (var child in children)
            {
                child.Transform = group.ToParentSpace(child.Transform ?? child.InheritsFrom?.ResolvedTransform ?? default);
                child.Parent = group.Parent;
            }

            group.ChildList.Clear();
            container.RemoveAt(index);
            container.InsertRange(index, children);
            foreach (var shape in SlideShape.Flatten(skewed.SelectMany(g => g.Children)))
                shape.Transform = ShapeEdit.ToLocal(shape, visible[shape]);
        }

        if (_ungrouped.Count > 0) document.Notify(slide, PresentationChangeKind.Content);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        for (var i = _ungrouped.Count - 1; i >= 0; i--)
        {
            var (container, index, group, children, before) = _ungrouped[i];
            container.RemoveRange(index, children.Length);
            container.Insert(index, group);
            foreach (var (shape, transform) in before) shape.Transform = transform;
            foreach (var child in children) child.Parent = group;
            group.ChildList.AddRange(children);
        }

        document.Notify(slide, PresentationChangeKind.Content);
    }

    private static bool Skewed(GroupShape group, GroupShape child) => group.Scale.X != group.Scale.Y && (child.Transform?.Rotation ?? 0) % 180 != 0;
}

/// <summary>
/// Aligns shapes by the axis-aligned bounding boxes of the rotated shapes, to the selection or to the slide. A single shape is always
/// aligned to the slide.
/// </summary>
/// <param name="slide">The slide.</param>
/// <param name="shapes">The shape ids.</param>
/// <param name="alignment">The edge or centre line.</param>
/// <param name="relativeToSlide">True to align to the slide instead of the selection.</param>
public sealed class AlignShapesCommand(Slide slide, IReadOnlyList<uint> shapes, ShapeAlignment alignment, bool relativeToSlide) : IDocumentCommand<Presentation>
{
    private SetTransformsCommand _move;

    /// <inheritdoc />
    public string Description => "Align";

    /// <inheritdoc />
    public void Apply(Presentation document) => (_move ??= new SetTransformsCommand(slide, Targets(document))).Apply(document);

    /// <inheritdoc />
    public void Revert(Presentation document) => _move.Revert(document);

    private Dictionary<uint, ShapeTransform> Targets(Presentation document)
    {
        var selected = ShapeEdit.Outermost(slide, shapes ?? []);
        if (selected.Count == 0) return [];
        var area = relativeToSlide || selected.Count == 1
            ? (0, 0, document.SlideWidth, document.SlideHeight)
            : ShapeEdit.Hull(selected.Select(s => s.ResolvedTransform));
        return selected.ToDictionary(s => s.Id, s =>
        {
            var t = s.ResolvedTransform;
            var b = ShapeEdit.Bounds(t);
            var (dx, dy) = alignment switch
            {
                ShapeAlignment.Left => (area.Left - b.Left, 0d),
                ShapeAlignment.Center => ((area.Left + area.Right - b.Left - b.Right) / 2, 0d),
                ShapeAlignment.Right => (area.Right - b.Right, 0d),
                ShapeAlignment.Top => (0d, area.Top - b.Top),
                ShapeAlignment.Middle => (0d, (area.Top + area.Bottom - b.Top - b.Bottom) / 2),
                _ => (0d, area.Bottom - b.Bottom)
            };
            return t with { X = t.X + (long)Math.Round(dx), Y = t.Y + (long)Math.Round(dy) };
        });
    }
}

/// <summary>
/// Distributes shapes with equal gaps between their rotated bounding boxes. Without <paramref name="relativeToSlide"/> the outer shapes
/// keep their place; with it they move to the slide edges.
/// </summary>
/// <param name="slide">The slide.</param>
/// <param name="shapes">The ids of at least three shapes, or two when distributing on the slide.</param>
/// <param name="horizontal">True to distribute along the x axis, false along the y axis.</param>
/// <param name="relativeToSlide">True to distribute across the whole slide.</param>
public sealed class DistributeShapesCommand(Slide slide, IReadOnlyList<uint> shapes, bool horizontal, bool relativeToSlide) : IDocumentCommand<Presentation>
{
    private SetTransformsCommand _move;

    /// <inheritdoc />
    public string Description => "Distribute";

    /// <inheritdoc />
    public void Apply(Presentation document) => (_move ??= new SetTransformsCommand(slide, Targets(document))).Apply(document);

    /// <inheritdoc />
    public void Revert(Presentation document) => _move.Revert(document);

    private Dictionary<uint, ShapeTransform> Targets(Presentation document)
    {
        var selected = ShapeEdit.Outermost(slide, shapes ?? []);
        if (selected.Count < (relativeToSlide ? 2 : 3))
            throw new ArgumentException($"Distributing needs at least {(relativeToSlide ? 2 : 3)} shapes.", nameof(shapes));
        var spans = selected.Select(s => (Shape: s, Bounds: ShapeEdit.Bounds(s.ResolvedTransform)))
            .Select(e => (e.Shape, Start: horizontal ? e.Bounds.Left : e.Bounds.Top, End: horizontal ? e.Bounds.Right : e.Bounds.Bottom))
            .OrderBy(e => e.Start + e.End).ToList();
        var start = relativeToSlide ? 0 : spans.Min(e => e.Start);
        var end = relativeToSlide ? horizontal ? document.SlideWidth : document.SlideHeight : spans.Max(e => e.End);
        var gap = (end - start - spans.Sum(e => e.End - e.Start)) / (spans.Count - 1);
        var targets = new Dictionary<uint, ShapeTransform>();
        foreach (var (shape, from, to) in spans)
        {
            var t = shape.ResolvedTransform;
            var delta = (long)Math.Round(start - from);
            targets[shape.Id] = horizontal ? t with { X = t.X + delta } : t with { Y = t.Y + delta };
            start += to - from + gap;
        }

        return targets;
    }
}

/// <summary>Rotates each shape around its own centre. Tables, charts and other graphic frames are skipped; they cannot be rotated.</summary>
/// <param name="slide">The slide.</param>
/// <param name="shapes">The shape ids.</param>
/// <param name="degrees">The clockwise angle in degrees; negative values rotate counter-clockwise.</param>
public sealed class RotateShapesCommand(Slide slide, IReadOnlyList<uint> shapes, double degrees) : IDocumentCommand<Presentation>
{
    private SetTransformsCommand _move;

    /// <inheritdoc />
    public string Description => "Rotate";

    /// <inheritdoc />
    public void Apply(Presentation document) =>
        (_move ??= new SetTransformsCommand(slide, ShapeEdit.Outermost(slide, shapes ?? []).Where(ShapeEdit.CanRotate).ToDictionary(s => s.Id, s => s.ResolvedTransform with
        {
            Rotation = ShapeEdit.Angle(s.ResolvedTransform.Rotation + degrees)
        }))).Apply(document);

    /// <inheritdoc />
    public void Revert(Presentation document) => _move.Revert(document);
}

/// <summary>Mirrors each shape in place, horizontally or vertically. Tables, charts and other graphic frames are skipped; they cannot be flipped.</summary>
/// <param name="slide">The slide.</param>
/// <param name="shapes">The shape ids.</param>
/// <param name="horizontal">True to mirror left to right, false to mirror top to bottom.</param>
public sealed class FlipShapesCommand(Slide slide, IReadOnlyList<uint> shapes, bool horizontal) : IDocumentCommand<Presentation>
{
    private SetTransformsCommand _move;

    /// <inheritdoc />
    public string Description => "Flip";

    /// <inheritdoc />
    public void Apply(Presentation document) =>
        (_move ??= new SetTransformsCommand(slide, ShapeEdit.Outermost(slide, shapes ?? []).Where(ShapeEdit.CanRotate).ToDictionary(s => s.Id, s =>
        {
            var t = s.ResolvedTransform with { Rotation = ShapeEdit.Angle(-s.ResolvedTransform.Rotation) };
            return horizontal ? t with { FlipH = !t.FlipH } : t with { FlipV = !t.FlipV };
        }))).Apply(document);

    /// <inheritdoc />
    public void Revert(Presentation document) => _move.Revert(document);
}
