namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>Adds an element on top of the others.</summary>
/// <param name="page">The page.</param>
/// <param name="element">A new element whose id is not used on the page yet.</param>
public sealed class AddElementCommand(NotebookPage page, NoteElement element) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => "Insert";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        ArgumentNullException.ThrowIfNull(element);
        if (document.SectionList.SelectMany(s => s.PageList).Any(p => p.ElementList.Contains(element)))
            throw new ArgumentException("The element is already on a page.", nameof(element));
        if (!NoteValues.IsId(element.Id) || elements.Any(e => e.Id == element.Id))
            throw new ArgumentException($"The element id {element.Id} is invalid or already used on the page.", nameof(element));
        NotebookEdit.Check(element.Bounds, nameof(element));
        elements.Add(element);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Deletes elements.</summary>
/// <param name="page">The page.</param>
/// <param name="ids">The element ids.</param>
public sealed class DeleteElementsCommand(NotebookPage page, IReadOnlyList<string> ids) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => "Delete";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        foreach (var element in ids.Distinct().Select(id => NotebookEdit.Find<NoteElement>(elements, id)).ToList()) elements.Remove(element);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Moves elements; this is one step however many elements move.</summary>
/// <param name="page">The page.</param>
/// <param name="positions">The new top left corners in page coordinates by element id.</param>
public sealed class MoveElementsCommand(NotebookPage page, IReadOnlyDictionary<string, (double X, double Y)> positions) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => "Move";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        foreach (var (id, (x, y)) in positions)
        {
            var element = NotebookEdit.Find<NoteElement>(elements, id);
            if (NotebookEdit.Finite(x, nameof(positions)) == element.X && NotebookEdit.Finite(y, nameof(positions)) == element.Y) continue;
            var moved = NotebookEdit.Replace(elements, element);
            (moved.X, moved.Y) = (x, y);
        }
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>
/// Moves and resizes elements. Freehand strokes scale their points, tables scale their columns to the new width. Elements of an unknown type
/// (<see cref="OpaqueElement"/>) keep their size.
/// </summary>
/// <param name="page">The page.</param>
/// <param name="bounds">The new rectangles in page coordinates by element id.</param>
public sealed class ResizeElementsCommand(NotebookPage page, IReadOnlyDictionary<string, NoteBounds> bounds) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => "Resize";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        foreach (var (id, target) in bounds)
        {
            var element = NotebookEdit.Find<NoteElement>(elements, id);
            if (element is OpaqueElement || NotebookEdit.Check(target, nameof(bounds)) == element.Bounds) continue;
            var resized = NotebookEdit.Replace(elements, element);
            (resized.X, resized.Y, resized.Width, resized.Height) = target;
            if (resized is InkElement ink)
            {
                var (sx, sy) = (element.Width > 0 ? target.Width / element.Width : 1, element.Height > 0 ? target.Height / element.Height : 1);
                ink.Points = ink.Points.Select(p => p with { X = p.X * sx, Y = p.Y * sy }).ToList();
            }
            else if (resized is TableElement table && table.ColumnWidths.Sum() is > 0 and var sum)
                table.ColumnWidths = table.ColumnWidths.Select(w => w * target.Width / sum).ToList();
        }
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Brings elements to the front or sends them to the back; they keep their order among themselves.</summary>
/// <param name="page">The page.</param>
/// <param name="ids">The element ids.</param>
/// <param name="toFront">True to bring the elements to the front, false to send them to the back.</param>
public sealed class ReorderElementsCommand(NotebookPage page, IReadOnlyList<string> ids, bool toFront) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => toFront ? "Bring to front" : "Send to back";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        var selected = ids.Select(id => NotebookEdit.Find<NoteElement>(elements, id)).ToHashSet();
        var ordered = elements.Where(selected.Contains).ToList();
        elements.RemoveAll(selected.Contains);
        elements.InsertRange(toFront ? elements.Count : 0, ordered);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Copies elements with new ids, shifted by an offset, on top of the others.</summary>
/// <param name="page">The page.</param>
/// <param name="ids">The element ids.</param>
/// <param name="offset">The shift of the copies to the right and down in pixels.</param>
public sealed class DuplicateElementsCommand(NotebookPage page, IReadOnlyList<string> ids, double offset) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <summary>The copies in z-order, available after the command was applied.</summary>
    public IReadOnlyList<NoteElement> Copies { get; private set; } = [];

    /// <inheritdoc />
    public string Description => "Duplicate";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        NotebookEdit.Finite(offset, nameof(offset));
        var selected = ids.Select(id => NotebookEdit.Find<NoteElement>(elements, id)).ToHashSet();
        Copies = elements.Where(selected.Contains).Select(e =>
        {
            var copy = e.Clone();
            (copy.Id, copy.X, copy.Y) = (NoteValues.NewId(), e.X + offset, e.Y + offset);
            return copy;
        }).ToList();
        elements.AddRange(Copies);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Adds a picture on top of the other elements, at its natural size but at most <paramref name="maxWidth"/> wide.</summary>
/// <param name="page">The page.</param>
/// <param name="data">The image bytes.</param>
/// <param name="contentType">The content type: PNG, JPEG, GIF, WebP, BMP or SVG.</param>
/// <param name="x">The left edge in page coordinates.</param>
/// <param name="y">The top edge in page coordinates.</param>
/// <param name="maxWidth">The largest width in pixels; a wider image is scaled down keeping its aspect ratio.</param>
public sealed class AddImageCommand(NotebookPage page, byte[] data, string contentType, double x, double y, double maxWidth) : IDocumentCommand<Notebook>
{
    private const double FallbackWidth = 320;
    private const double FallbackHeight = 240;

    private ElementEdit _edit;

    /// <summary>The created image, available after the command was applied.</summary>
    public ImageElement Image { get; private set; }

    /// <inheritdoc />
    public string Description => "Insert picture";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        ArgumentNullException.ThrowIfNull(data);
        var type = NotebookMediaTypes.Normalize(contentType) ?? throw new ArgumentException($"The content type {contentType} is not supported.", nameof(contentType));
        if (!double.IsFinite(maxWidth) || maxWidth <= 0) throw new ArgumentOutOfRangeException(nameof(maxWidth));
        var (width, height) = OfficeImageSize.Read(data, type) is { } size && size.Width > 0 && size.Height > 0 ? size : (FallbackWidth, FallbackHeight);
        var scale = Math.Min(1, maxWidth / width);
        Image = new ImageElement
        {
            X = NotebookEdit.Finite(x, nameof(x)), Y = NotebookEdit.Finite(y, nameof(y)), Width = width * scale, Height = height * scale,
            MediaId = document.AddMedia(data, type), ContentType = type
        };
        elements.Add(Image);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}
