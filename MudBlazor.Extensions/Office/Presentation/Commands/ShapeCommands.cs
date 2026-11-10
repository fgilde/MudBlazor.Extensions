namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A change of the z-order of shapes.</summary>
public enum ZOrderChange
{
    /// <summary>Moves the shapes to the top.</summary>
    BringToFront,

    /// <summary>Moves the shapes one step up.</summary>
    BringForward,

    /// <summary>Moves the shapes one step down.</summary>
    SendBackward,

    /// <summary>Moves the shapes to the bottom.</summary>
    SendToBack
}

/// <summary>Moves, resizes, rotates or flips shapes. Placeholders that inherit their position get an explicit transform.</summary>
/// <param name="slide">The slide.</param>
/// <param name="absolute">The new transforms in slide coordinates by shape id; children of groups are converted to their group's child coordinates.</param>
public sealed class SetTransformsCommand(Slide slide, IReadOnlyDictionary<uint, ShapeTransform> absolute) : IDocumentCommand<Presentation>
{
    private List<(SlideShape Shape, ShapeTransform? Transform)> _before;

    /// <inheritdoc />
    public string Description => "Move";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        foreach (var transform in absolute.Values) ShapeEdit.Validate(transform);
        var targets = absolute.Select(e => (Shape: ShapeEdit.Find<SlideShape>(slide, e.Key), Target: e.Value))
            .OrderBy(e => ShapeEdit.Ancestors(e.Shape).Count()).ToList();
        _before = targets.Select(e => (e.Shape, e.Shape.Transform)).ToList();
        foreach (var (shape, target) in targets) shape.Transform = ShapeEdit.ToLocal(shape, target);
        document.Notify(slide, PresentationChangeKind.Content);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        foreach (var (shape, transform) in _before) shape.Transform = transform;
        document.Notify(slide, PresentationChangeKind.Content);
    }
}

/// <summary>Sets the fill and outline of shapes. Shapes other than <see cref="AutoShape"/> are left as they are.</summary>
/// <param name="slide">The slide.</param>
/// <param name="shapes">The shape ids.</param>
/// <param name="fill">The new fill, or null to keep it.</param>
/// <param name="line">The new outline, or null to keep it.</param>
public sealed class SetShapeStyleCommand(Slide slide, IReadOnlyList<uint> shapes, ShapeFill fill, ShapeLine line) : IDocumentCommand<Presentation>
{
    private List<(AutoShape Shape, ShapeFill Fill, ShapeLine Line)> _before;

    /// <inheritdoc />
    public string Description => "Format shape";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        ShapeEdit.Validate(line);
        _before = shapes.Select(id => ShapeEdit.Find<SlideShape>(slide, id)).OfType<AutoShape>().Select(s => (s, s.Fill, s.Line)).ToList();
        foreach (var (shape, _, _) in _before)
        {
            shape.Fill = fill ?? shape.Fill;
            shape.Line = line ?? shape.Line;
        }

        document.Notify(slide, PresentationChangeKind.Content);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        foreach (var (shape, oldFill, oldLine) in _before) (shape.Fill, shape.Line) = (oldFill, oldLine);
        document.Notify(slide, PresentationChangeKind.Content);
    }
}

/// <summary>Changes the preset geometry of a shape; adjust values are reset when the geometry changes.</summary>
/// <param name="slide">The slide.</param>
/// <param name="shape">The id of an <see cref="AutoShape"/>.</param>
/// <param name="preset">A preset name from <see cref="PresetGeometry.Supported"/>.</param>
public sealed class SetGeometryCommand(Slide slide, uint shape, string preset) : IDocumentCommand<Presentation>
{
    private (AutoShape Shape, string Geometry, IReadOnlyDictionary<string, long> Adjustments) _before;

    /// <inheritdoc />
    public string Description => "Change shape";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        if (!PresetGeometry.IsSupported(preset)) throw new ArgumentException($"The preset geometry '{preset}' is not supported.", nameof(preset));
        var target = ShapeEdit.Find<AutoShape>(slide, shape);
        _before = (target, target.Geometry, target.Adjustments);
        if (target.Geometry != preset) target.Adjustments = new Dictionary<string, long>();
        target.Geometry = preset;
        document.Notify(slide, PresentationChangeKind.Content);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        (_before.Shape.Geometry, _before.Shape.Adjustments) = (_before.Geometry, _before.Adjustments);
        document.Notify(slide, PresentationChangeKind.Content);
    }
}

/// <summary>
/// Adds a shape on top of the others or at a position. The shape and its children get new ids that are unique on the slide.
/// </summary>
/// <param name="slide">The slide.</param>
/// <param name="shape">A new top-level shape, for example from <see cref="AutoShape.Create"/>, that is on no slide.</param>
/// <param name="index">The z-order position, or null for the top.</param>
public sealed class InsertShapeCommand(Slide slide, SlideShape shape, int? index = null) : IDocumentCommand<Presentation>
{
    /// <inheritdoc />
    public string Description => "Insert shape";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        ArgumentNullException.ThrowIfNull(shape);
        var placed = document.SlideList.Append(slide).Select(s => s.ShapeList).Concat(document.LayoutList.Select(l => l.ShapeList)).Concat(document.MasterList.Select(m => m.ShapeList));
        if (shape.Parent != null || placed.Any(shapes => SlideShape.Flatten(shapes).Contains(shape)))
            throw new ArgumentException("The shape is already part of a slide, layout or master.", nameof(shape));
        foreach (var s in SlideShape.Flatten([shape]))
        {
            if (s.Transform is { } transform) ShapeEdit.Validate(transform);
            if (s is AutoShape auto) ShapeEdit.Validate(auto.Line);
        }

        var at = index ?? slide.ShapeList.Count;
        if (at < 0 || at > slide.ShapeList.Count) throw new ArgumentOutOfRangeException(nameof(index));
        ShapeEdit.AssignIds(shape, ShapeEdit.NextId(slide));
        shape.Master ??= slide.Layout?.Master;
        slide.ShapeList.Insert(at, shape);
        document.Notify(slide, PresentationChangeKind.Content);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        slide.ShapeList.Remove(shape);
        document.Notify(slide, PresentationChangeKind.Content);
    }
}

/// <summary>Adds a picture on top of the other shapes. The image is stored as a new media part when the presentation is saved.</summary>
/// <param name="slide">The slide.</param>
/// <param name="image">The image bytes.</param>
/// <param name="contentType">The image content type: PNG, JPEG, GIF, BMP, TIFF, SVG, WebP, EMF or WMF.</param>
/// <param name="transform">The position and size in slide coordinates.</param>
public sealed class InsertPictureCommand(Slide slide, byte[] image, string contentType, ShapeTransform transform) : IDocumentCommand<Presentation>
{
    private static readonly Dictionary<string, (string Extension, string ContentType)> MediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ("png", "image/png"), ["image/jpeg"] = ("jpg", "image/jpeg"), ["image/jpg"] = ("jpg", "image/jpeg"), ["image/pjpeg"] = ("jpg", "image/jpeg"),
        ["image/gif"] = ("gif", "image/gif"), ["image/bmp"] = ("bmp", "image/bmp"), ["image/x-bmp"] = ("bmp", "image/bmp"), ["image/tiff"] = ("tiff", "image/tiff"),
        ["image/svg+xml"] = ("svg", "image/svg+xml"), ["image/webp"] = ("webp", "image/webp"), ["image/x-emf"] = ("emf", "image/x-emf"), ["image/emf"] = ("emf", "image/x-emf"),
        ["image/x-wmf"] = ("wmf", "image/x-wmf"), ["image/wmf"] = ("wmf", "image/x-wmf")
    };

    private (byte[] Data, string ContentType) _media;

    /// <summary>The created picture, available after the command was applied.</summary>
    public PictureShape Picture { get; private set; }

    /// <inheritdoc />
    public string Description => "Insert picture";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        ArgumentNullException.ThrowIfNull(image);
        ShapeEdit.Validate(transform);
        var type = MediaType(contentType);
        if (Picture == null)
        {
            var id = ShapeEdit.NextId(slide);
            _media = (image.ToArray(), type.ContentType);
            Picture = new PictureShape
            {
                Id = id, Name = $"Picture {id - 1}", Transform = transform, ImagePart = MediaPart(document, type.ContentType), ContentType = type.ContentType,
                Master = slide.Layout?.Master
            };
        }

        document.PendingMedia[Picture.ImagePart] = _media;
        slide.ShapeList.Add(Picture);
        document.Notify(slide, PresentationChangeKind.Content);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        slide.ShapeList.Remove(Picture);
        document.Notify(slide, PresentationChangeKind.Content);
    }

    internal static string MediaPart(Presentation document, string contentType)
    {
        var extension = MediaType(contentType).Extension;
        for (var i = 1;; i++)
        {
            var part = $"/ppt/media/image{i}.{extension}";
            if (!document.PendingMedia.ContainsKey(part) && document.Package?.Opc.Entry(part) == null) return part;
        }
    }

    private static (string Extension, string ContentType) MediaType(string contentType) =>
        MediaTypes.TryGetValue(contentType ?? "", out var type) ? type : throw new ArgumentException($"The image type '{contentType}' is not supported.", nameof(contentType));
}

/// <summary>Deletes shapes, including shapes inside groups.</summary>
/// <param name="slide">The slide.</param>
/// <param name="shapes">The shape ids.</param>
public sealed class DeleteShapesCommand(Slide slide, IReadOnlyList<uint> shapes) : IDocumentCommand<Presentation>
{
    private List<(List<SlideShape> Container, int Index, SlideShape Shape)> _removed;

    /// <inheritdoc />
    public string Description => "Delete";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        _removed = ShapeEdit.Outermost(slide, shapes)
            .Select(s => (Container: ShapeEdit.Container(slide, s), Index: ShapeEdit.Container(slide, s).IndexOf(s), Shape: s))
            .OrderByDescending(e => e.Index).ToList();
        foreach (var (container, index, _) in _removed) container.RemoveAt(index);
        document.Notify(slide, PresentationChangeKind.Content);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        for (var i = _removed.Count - 1; i >= 0; i--) _removed[i].Container.Insert(_removed[i].Index, _removed[i].Shape);
        document.Notify(slide, PresentationChangeKind.Content);
    }
}

/// <summary>Copies shapes, moved by an offset, on top of their group or slide.</summary>
/// <param name="slide">The slide.</param>
/// <param name="shapes">The shape ids.</param>
/// <param name="offset">The offset in EMU, applied to X and Y in slide coordinates.</param>
public sealed class DuplicateShapesCommand(Slide slide, IReadOnlyList<uint> shapes, long offset) : IDocumentCommand<Presentation>
{
    private List<(List<SlideShape> Container, SlideShape Copy)> _copies;

    /// <summary>The copies in z-order, available after the command was applied.</summary>
    public IReadOnlyList<SlideShape> Copies { get; private set; }

    /// <inheritdoc />
    public string Description => "Duplicate";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        if (_copies == null)
        {
            var next = ShapeEdit.NextId(slide);
            _copies = [];
            foreach (var original in ShapeEdit.Outermost(slide, shapes))
            {
                var copy = original.Clone(original.Parent);
                var resolved = original.ResolvedTransform;
                copy.Transform = ShapeEdit.ToLocal(original, resolved with { X = resolved.X + offset, Y = resolved.Y + offset });
                next = ShapeEdit.AssignIds(copy, next);
                _copies.Add((ShapeEdit.Container(slide, original), copy));
            }

            Copies = _copies.Select(c => c.Copy).ToList();
        }

        foreach (var (container, copy) in _copies) container.Add(copy);
        document.Notify(slide, PresentationChangeKind.Content);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        foreach (var (container, copy) in _copies) container.Remove(copy);
        document.Notify(slide, PresentationChangeKind.Content);
    }
}

/// <summary>Changes the z-order of shapes within their group or slide; selected shapes move as a block and keep their relative order.</summary>
/// <param name="slide">The slide.</param>
/// <param name="shapes">The shape ids.</param>
/// <param name="change">The change.</param>
public sealed class ReorderShapesCommand(Slide slide, IReadOnlyList<uint> shapes, ZOrderChange change) : IDocumentCommand<Presentation>
{
    private List<(List<SlideShape> Container, SlideShape[] Order)> _before;

    /// <inheritdoc />
    public string Description => "Arrange";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        var selected = shapes.Select(id => ShapeEdit.Find<SlideShape>(slide, id)).ToHashSet();
        _before = selected.Select(s => ShapeEdit.Container(slide, s)).Distinct().Select(c => (c, c.ToArray())).ToList();
        foreach (var (container, _) in _before) Reorder(container, selected);
        document.Notify(slide, PresentationChangeKind.Content);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        foreach (var (container, order) in _before)
        {
            container.Clear();
            container.AddRange(order);
        }

        document.Notify(slide, PresentationChangeKind.Content);
    }

    private void Reorder(List<SlideShape> list, HashSet<SlideShape> selected)
    {
        switch (change)
        {
            case ZOrderChange.BringToFront or ZOrderChange.SendToBack:
                var moved = list.Where(selected.Contains).ToList();
                list.RemoveAll(selected.Contains);
                list.InsertRange(change == ZOrderChange.BringToFront ? list.Count : 0, moved);
                break;
            case ZOrderChange.BringForward:
                for (var i = list.Count - 2; i >= 0; i--)
                    if (selected.Contains(list[i]) && !selected.Contains(list[i + 1]))
                        (list[i], list[i + 1]) = (list[i + 1], list[i]);
                break;
            case ZOrderChange.SendBackward:
                for (var i = 1; i < list.Count; i++)
                    if (selected.Contains(list[i]) && !selected.Contains(list[i - 1]))
                        (list[i], list[i - 1]) = (list[i - 1], list[i]);
                break;
        }
    }
}

internal static class ShapeEdit
{
    private const long MaxLineWidth = 20116800;

    private static readonly HashSet<string> Dashes =
        ["solid", "dot", "dash", "lgDash", "dashDot", "lgDashDot", "lgDashDotDot", "sysDash", "sysDot", "sysDashDot", "sysDashDotDot"];

    internal static void Validate(ShapeTransform transform)
    {
        if (transform.Width < 0 || transform.Height < 0 || !double.IsFinite(transform.Rotation))
            throw new ArgumentException("A transform needs a width and height of at least 0 and a finite rotation.", nameof(transform));
    }

    internal static void Validate(ShapeLine line)
    {
        if (line != null && (line.Width is < 0 or > MaxLineWidth || line.Dash != null && !Dashes.Contains(line.Dash)))
            throw new ArgumentException($"A line needs a width from 0 to {MaxLineWidth} EMU and a preset dash name.", nameof(line));
    }

    internal static T Find<T>(Slide slide, uint id) where T : SlideShape =>
        SlideShape.Flatten(slide.Shapes).FirstOrDefault(s => s.Id == id) switch
        {
            T shape => shape,
            null => throw new ArgumentException($"The slide has no shape with id {id}.", nameof(id)),
            _ => throw new ArgumentException($"The shape {id} is no {typeof(T).Name}.", nameof(id))
        };

    internal static List<SlideShape> Outermost(Slide slide, IEnumerable<uint> ids)
    {
        var selected = ids.Select(id => Find<SlideShape>(slide, id)).ToHashSet();
        return SlideShape.Flatten(slide.Shapes).Where(s => selected.Contains(s) && !Ancestors(s).Any(selected.Contains)).ToList();
    }

    internal static IEnumerable<GroupShape> Ancestors(SlideShape shape)
    {
        for (var group = shape.Parent; group != null; group = group.Parent) yield return group;
    }

    internal static List<SlideShape> Container(Slide slide, SlideShape shape) => shape.Parent?.ChildList ?? slide.ShapeList;

    internal static uint NextId(Slide slide) => Math.Max(slide.ReservedId, SlideShape.Flatten(slide.Shapes).Select(s => s.Id).DefaultIfEmpty(1u).Max()) + 1;

    internal static uint AssignIds(SlideShape shape, uint next)
    {
        foreach (var s in SlideShape.Flatten([shape])) s.Id = next++;
        return next;
    }

    internal static SlideShape Detach(Presentation document, Slide slide, SlideShape shape)
    {
        var copy = shape.Clone(null);
        copy.Transform = shape.ResolvedTransform;
        var part = document.PartOf(slide);
        foreach (var s in SlideShape.Flatten([copy]).Where(s => s.Source != null)) s.SourcePart ??= part;
        return copy;
    }

    internal static ShapeTransform ToLocal(SlideShape shape, ShapeTransform absolute) =>
        Ancestors(shape).Reverse().Aggregate(absolute, (transform, group) => group.FromParentSpace(transform));
}
