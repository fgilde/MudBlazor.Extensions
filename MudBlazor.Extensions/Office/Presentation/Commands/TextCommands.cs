namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>
/// Replaces the text of a shape. Build the body with <c>with</c> from the current one and keep unchanged paragraphs and runs, so their
/// formatting stays exactly as in the file.
/// </summary>
/// <param name="slide">The slide.</param>
/// <param name="shape">The id of an <see cref="AutoShape"/>.</param>
/// <param name="body">The new text.</param>
public sealed class SetTextCommand(Slide slide, uint shape, TextBody body) : IDocumentCommand<Presentation>
{
    private (AutoShape Shape, TextBody Text) _before;

    /// <inheritdoc />
    public string Description => "Edit text";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        ArgumentNullException.ThrowIfNull(body);
        var target = ShapeEdit.Find<AutoShape>(slide, shape);
        _before = (target, target.Text);
        target.Text = body;
        document.Notify(slide, PresentationChangeKind.Content);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        _before.Shape.Text = _before.Text;
        document.Notify(slide, PresentationChangeKind.Content);
    }
}
