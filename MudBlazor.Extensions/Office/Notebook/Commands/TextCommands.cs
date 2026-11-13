namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>
/// Replaces the paragraphs of a text block, typically once per editing session. A text without any characters deletes the block in the same step.
/// Colours, fonts, sizes and links are checked like those read from a file.
/// </summary>
/// <param name="page">The page.</param>
/// <param name="elementId">The id of a <see cref="TextElement"/>.</param>
/// <param name="paragraphs">The new paragraphs.</param>
/// <exception cref="ArgumentException">A run has an invalid colour, font, size or link, or an indent is out of range.</exception>
public sealed class SetTextCommand(NotebookPage page, string elementId, IReadOnlyList<NoteParagraph> paragraphs) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => "Edit text";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        var text = NotebookEdit.Find<TextElement>(elements, elementId);
        var value = NotebookEdit.Text(paragraphs, nameof(paragraphs));
        if (NotebookEdit.IsEmpty(value)) elements.Remove(text);
        else if (!value.SequenceEqual(text.Paragraphs)) NotebookEdit.Replace(elements, text).Paragraphs = value;
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);
}

/// <summary>Sets outline and fill of shapes and the colour and width of freehand strokes. Other elements are left as they are.</summary>
/// <param name="page">The page.</param>
/// <param name="ids">The element ids.</param>
/// <param name="stroke">The outline or stroke colour as <c>#RRGGBB</c>, or null to keep it.</param>
/// <param name="strokeWidth">The outline or stroke width in pixels, or null to keep it.</param>
/// <param name="fill">The fill colour of shapes as <c>#RRGGBB</c>, an empty string for no fill, or null to keep it.</param>
public sealed class SetShapeStyleCommand(NotebookPage page, IReadOnlyList<string> ids, string stroke, double? strokeWidth, string fill) : IDocumentCommand<Notebook>
{
    private ElementEdit _edit;

    /// <inheritdoc />
    public string Description => "Format shape";

    /// <inheritdoc />
    public void Apply(Notebook document) => _edit = ElementEdit.Run(document, page, _edit, elements =>
    {
        if (stroke != null) NotebookEdit.Color(stroke, nameof(stroke));
        if (fill is { Length: > 0 }) NotebookEdit.Color(fill, nameof(fill));
        if (strokeWidth is { } width && (!double.IsFinite(width) || width < 0)) throw new ArgumentOutOfRangeException(nameof(strokeWidth));
        foreach (var element in ids.Distinct().Select(id => NotebookEdit.Find<NoteElement>(elements, id)).ToList())
            switch (element)
            {
                case ShapeElement shape when (stroke ?? shape.Stroke, strokeWidth ?? shape.StrokeWidth, fill == null ? shape.Fill : NullIfEmpty(fill)) is var style
                                             && style != (shape.Stroke, shape.StrokeWidth, shape.Fill):
                    var styled = NotebookEdit.Replace(elements, shape);
                    (styled.Stroke, styled.StrokeWidth, styled.Fill) = style;
                    break;
                case InkElement ink when (stroke ?? ink.Color, strokeWidth ?? ink.StrokeWidth) is var inkStyle && inkStyle != (ink.Color, ink.StrokeWidth):
                    var colored = NotebookEdit.Replace(elements, ink);
                    (colored.Color, colored.StrokeWidth) = inkStyle;
                    break;
            }
    });

    /// <inheritdoc />
    public void Revert(Notebook document) => _edit.Revert(document);

    private static string NullIfEmpty(string value) => value.Length == 0 ? null : value;
}
