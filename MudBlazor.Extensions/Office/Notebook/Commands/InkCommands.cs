namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>
/// Adds a freehand stroke on top of the other elements. The element's bounds are the hull of the points widened by half the stroke width, so a stroke
/// with a single point is a dot of the stroke width.
/// </summary>
/// <param name="page">The page.</param>
/// <param name="pagePoints">The points in page coordinates; pressure is clamped to 0–1. Every value must be finite.</param>
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
        if (pagePoints is not { Count: > 0 }) throw new ArgumentException("A stroke needs at least one point.", nameof(pagePoints));
        var points = pagePoints.Select(p => new InkPoint(NotebookEdit.Number(p.X, nameof(pagePoints)), NotebookEdit.Number(p.Y, nameof(pagePoints)),
            Math.Clamp(NotebookEdit.Number(p.Pressure, nameof(pagePoints)), 0, 1))).ToList();
        Stroke = new InkElement { Color = NotebookEdit.Color(color, nameof(color)), StrokeWidth = NotebookEdit.Positive(width, nameof(width)), Tool = tool };
        NotebookEdit.FitInk(Stroke, points);
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
        ArgumentNullException.ThrowIfNull(ids);
        foreach (var stroke in ids.Distinct().Select(id => NotebookEdit.Find<InkElement>(elements, id)).ToList()) elements.Remove(stroke);
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}
