namespace MudBlazor.Extensions.Office.Notebook;

internal sealed class AddElementCommand(NotebookPage page, NoteElement element) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    public string Description => "Insert";

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

    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Adds a text block on top of the other elements. Text without any character adds nothing.</summary>
/// <param name="page">The page.</param>
/// <param name="x">The left edge in page coordinates.</param>
/// <param name="y">The top edge in page coordinates.</param>
/// <param name="width">The width in pixels; the height follows the text.</param>
/// <param name="paragraphs">The paragraphs, checked like those of <see cref="SetTextCommand"/>.</param>
public sealed class AddTextCommand(NotebookPage page, double x, double y, double width, IReadOnlyList<NoteParagraph> paragraphs) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <summary>The created text block, available after the command was applied; null if the text was empty.</summary>
    public TextElement Text { get; private set; }

    /// <inheritdoc />
    public string Description => "Insert text";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        var text = NotebookEdit.Text(paragraphs, nameof(paragraphs));
        Text = NotebookEdit.IsEmpty(text) ? null
            : new TextElement { X = NotebookEdit.Number(x, nameof(x)), Y = NotebookEdit.Number(y, nameof(y)), Width = NotebookEdit.Positive(width, nameof(width)), Paragraphs = text };
        if (Text != null) elements.Add(Text);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Adds a shape on top of the other elements. A line or arrow runs from the top left to the bottom right corner of the bounds unless flipped.</summary>
/// <param name="page">The page.</param>
/// <param name="kind">The kind of shape.</param>
/// <param name="bounds">The rectangle in page coordinates.</param>
/// <param name="stroke">The outline colour as <c>#RRGGBB</c>.</param>
/// <param name="strokeWidth">The outline width in pixels.</param>
/// <param name="fill">The fill colour as <c>#RRGGBB</c>, or null for no fill.</param>
/// <param name="flipX">True if a line or arrow starts at the right edge.</param>
/// <param name="flipY">True if a line or arrow starts at the bottom edge.</param>
public sealed class AddShapeCommand(NotebookPage page, NoteShapeKind kind, NoteBounds bounds, string stroke = "#000000", double strokeWidth = 2, string fill = null,
    bool flipX = false, bool flipY = false) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <summary>The created shape, available after the command was applied.</summary>
    public ShapeElement Shape { get; private set; }

    /// <inheritdoc />
    public string Description => "Insert shape";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        NotebookEdit.Check(bounds, nameof(bounds));
        Shape = new ShapeElement
        {
            Kind = kind, X = bounds.X, Y = bounds.Y, Width = bounds.Width, Height = bounds.Height, Stroke = stroke, StrokeWidth = strokeWidth, Fill = fill,
            FlipX = flipX, FlipY = flipY
        };
        elements.Add(Shape);
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
        ArgumentNullException.ThrowIfNull(ids);
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
        ArgumentNullException.ThrowIfNull(positions);
        foreach (var (id, (x, y)) in positions)
        {
            var element = NotebookEdit.Find<NoteElement>(elements, id);
            if ((NotebookEdit.Number(x, nameof(positions)), NotebookEdit.Number(y, nameof(positions))) == (element.X, element.Y)) continue;
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
        ArgumentNullException.ThrowIfNull(bounds);
        foreach (var (id, target) in bounds)
        {
            var element = NotebookEdit.Find<NoteElement>(elements, id);
            if (NotebookEdit.Check(target, nameof(bounds)) == element.Bounds || element is OpaqueElement) continue;
            if (element is TableElement) NotebookEdit.Positive(target.Width, nameof(bounds));
            var resized = NotebookEdit.Replace(elements, element);
            (resized.X, resized.Y, resized.Width, resized.Height) = target;
            if (resized is InkElement ink)
            {
                var pad = ink.StrokeWidth / 2;
                var (width, height) = (element.Width - 2 * pad, element.Height - 2 * pad);
                var (sx, sy) = (width > 0 ? Math.Max(0, target.Width - 2 * pad) / width : 1, height > 0 ? Math.Max(0, target.Height - 2 * pad) / height : 1);
                NotebookEdit.FitInk(ink, ink.Points.Select(p => p with { X = target.X + pad + (p.X - pad) * sx, Y = target.Y + pad + (p.Y - pad) * sy }).ToList());
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
        ArgumentNullException.ThrowIfNull(ids);
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
        NotebookEdit.Number(offset, nameof(offset));
        ArgumentNullException.ThrowIfNull(ids);
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
    private const double MaxSide = 10000;

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
        NotebookEdit.Positive(maxWidth, nameof(maxWidth));
        NotebookEdit.Number(x, nameof(x));
        NotebookEdit.Number(y, nameof(y));
        var bytes = data.ToArray();
        document.CheckRoom(bytes.Length);
        var (width, height) = OfficeImageSize.Read(bytes, type) is { } size && size.Width > 0 && size.Height > 0 && double.IsFinite(size.Width) && double.IsFinite(size.Height)
            ? size : (FallbackWidth, FallbackHeight);
        var scale = Math.Min(1, Math.Min(maxWidth, MaxSide) / width);
        scale = Math.Min(scale, MaxSide / height);
        Image = new ImageElement
        {
            X = x, Y = y, Width = width * scale, Height = height * scale, MediaId = document.AddMedia(bytes, type), ContentType = type
        };
        elements.Add(Image);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Sets the alternative text of a picture, the text screen readers read and shown while the picture cannot be loaded.</summary>
/// <param name="page">The page.</param>
/// <param name="imageId">The id of an <see cref="ImageElement"/>.</param>
/// <param name="altText">The new text; null is an empty text.</param>
public sealed class SetImageAltTextCommand(NotebookPage page, string imageId, string altText) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => "Alt text";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        var image = NotebookEdit.Find<ImageElement>(elements, imageId);
        if ((altText ?? "") != image.AltText) NotebookEdit.Replace(elements, image).AltText = altText ?? "";
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}