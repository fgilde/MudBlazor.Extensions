namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>
/// Adds a freehand stroke on top of the other elements. The element's bounds are the hull of the points widened by half the stroke width, so a stroke
/// with a single point is a dot of the stroke width.
/// </summary>
/// <param name="page">The page.</param>
/// <param name="pagePoints">The points in page coordinates; pressure is clamped to 0–1.</param>
/// <param name="color">The colour as <c>#RRGGBB</c>.</param>
/// <param name="width">The stroke width in pixels.</param>
/// <param name="tool">The tool the stroke was drawn with.</param>
public sealed class AddStrokeCommand(NotebookPage page, IReadOnlyList<InkPoint> pagePoints, string color, double width, InkTool tool) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <summary>The created stroke, available after the command was applied.</summary>
    public InkElement Stroke { get; private set; }

    /// <inheritdoc />
    public string Description => tool == InkTool.Highlighter ? "Highlight" : "Draw";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        if (pagePoints is not { Count: > 0 } || pagePoints.Any(p => !double.IsFinite(p.X) || !double.IsFinite(p.Y)))
            throw new ArgumentException("A stroke needs at least one point with finite coordinates.", nameof(pagePoints));
        if (!double.IsFinite(width) || width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        NotebookEdit.Color(color, nameof(color));
        var pad = width / 2;
        var (left, top) = (pagePoints.Min(p => p.X) - pad, pagePoints.Min(p => p.Y) - pad);
        Stroke = new InkElement
        {
            X = left, Y = top, Width = pagePoints.Max(p => p.X) - left + pad, Height = pagePoints.Max(p => p.Y) - top + pad, Color = color, StrokeWidth = width,
            Tool = tool, Points = pagePoints.Select(p => new InkPoint(p.X - left, p.Y - top, double.IsFinite(p.Pressure) ? Math.Clamp(p.Pressure, 0, 1) : 0.5)).ToList()
        };
        elements.Add(Stroke);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Deletes the freehand strokes an eraser gesture touched, as one step.</summary>
/// <param name="page">The page.</param>
/// <param name="ids">The ids of <see cref="InkElement"/>s, for example found with <see cref="InkGeometry.HitByEraser"/>.</param>
public sealed class EraseStrokesCommand(NotebookPage page, IReadOnlyList<string> ids) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => "Erase";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        foreach (var stroke in ids.Distinct().Select(id => NotebookEdit.Find<InkElement>(elements, id)).ToList()) elements.Remove(stroke);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}
