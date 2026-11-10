using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlideCanvas
{
    private const long TextBoxWidth = 914400 * 3;
    private const long TextBoxHeight = 369332;

    private TextSession _text;
    private MudExSlideTextEditor _textEditor;
    private bool _insertTextBox;

    /// <summary>Raised on the first input of a text editing session, before the text is committed.</summary>
    [Parameter] public EventCallback TextInput { get; set; }

    /// <summary>True while the text of a shape is edited.</summary>
    public bool IsEditingText => _text != null;

    /// <summary>
    /// Called by the browser when a shape was double-clicked: selects it and starts editing its text with the caret at the clicked point.
    /// </summary>
    /// <param name="id">The id of the shape.</param>
    /// <param name="x">The horizontal client position in pixels.</param>
    /// <param name="y">The vertical client position in pixels.</param>
    /// <returns>A task that completes when editing started.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnShapeDoubleClickAsync(uint id, double x, double y) => InvokeAsync(() => BeginTextAsync(id, null, [x, y]));

    /// <summary>Called by the browser when the user clicked the slide to insert a text box there.</summary>
    /// <param name="x">The horizontal position in slide coordinates (EMU).</param>
    /// <param name="y">The vertical position in slide coordinates (EMU).</param>
    /// <returns>A task that completes when the text box is inserted and edited.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnInsertAsync(double x, double y) => InvokeAsync(async () =>
    {
        if (!_insertTextBox || ReadOnly || Slide == null)
            return;
        _insertTextBox = false;
        CommitText();
        var shape = new AutoShape
        {
            IsTextBox = true,
            Fill = new ShapeFill(ShapeFillKind.None),
            Transform = new ShapeTransform(Math.Clamp((long)Math.Round(x), 0, Presentation.SlideWidth), Math.Clamp((long)Math.Round(y), 0, Presentation.SlideHeight),
                TextBoxWidth, TextBoxHeight),
            Text = new TextBody { Wrap = true, Paragraphs = [new TextParagraph()] }
        };
        var insert = new InsertShapeCommand(Slide, shape);
        insert.Apply(Presentation);
        shape.Name = $"TextBox {shape.Id - 1}";
        _text = new TextSession(Slide, shape, insert, null, null);
        await SelectAsync([shape.Id]);
    });

    internal Task InsertTextBoxAsync()
    {
        if (ReadOnly || Slide == null)
            return Task.CompletedTask;
        _insertTextBox = true;
        return CommitTextAsync();
    }

    internal Task CommitTextAsync()
    {
        CommitText();
        StateHasChanged();
        return RaiseSelectionAsync();
    }

    internal async Task BeginTextAsync(uint id, string text = null, double[] caret = null)
    {
        if (ReadOnly || Find(id) is not AutoShape shape || ReferenceEquals(_text?.Shape, shape))
            return;
        CommitText();
        _text = new TextSession(Slide, shape, null, text, caret);
        await SelectAsync([id]);
    }

    private void CommitText()
    {
        if (_text is not { } session)
            return;
        _text = null;
        if (!SlideShape.Flatten(session.Slide.Shapes).Contains(session.Shape))
            return;
        var body = session.Draft == null ? session.Shape.Text : SlideTextDom.FromDom(session.Shape.Text, session.Draft);
        var empty = body?.Paragraphs.All(p => p.Runs.Count == 0) != false;
        if (session.Insert != null)
        {
            if (empty)
            {
                session.Insert.Revert(Presentation);
                Change(_selection.Where(i => i != session.Shape.Id));
                return;
            }

            using (Presentation.Undo.BeginTransaction("Insert text box"))
            {
                Presentation.Undo.Record(session.Insert);
                Presentation.Execute(new SetTextCommand(session.Slide, session.Shape.Id, body));
            }
        }
        else if (!ReferenceEquals(body, session.Shape.Text) && !(session.Shape.Text == null && empty))
            Presentation.Execute(new SetTextCommand(session.Slide, session.Shape.Id, body));
    }

    internal async Task FormatTextAsync(string key, string value)
    {
        if (ReadOnly || Slide == null)
            return;
        if (_text != null)
        {
            if (_textEditor != null)
                await _textEditor.FormatAsync(key, value);
            return;
        }

        var shapes = SelectedShapes.SelectMany(s => SlideShape.Flatten([s])).OfType<AutoShape>().Where(s => s.Text != null).ToList();
        if (shapes.Count == 0)
            return;
        value ??= SlideTextDom.All(shapes, key) ? "0" : "1";
        using (Presentation.Undo.BeginTransaction("Format text"))
        {
            foreach (var shape in shapes)
            {
                var body = SlideTextDom.Format(shape.Text, key, value);
                if (!ReferenceEquals(body, shape.Text))
                    Presentation.Execute(new SetTextCommand(Slide, shape.Id, body));
            }
        }

        StateHasChanged();
    }

    private async Task OnTextChangedAsync(IReadOnlyList<SlideTextParagraphDto> paragraphs)
    {
        if (_text is not { } session)
            return;
        session.Draft = paragraphs;
        if (session.Typed)
            return;
        session.Typed = true;
        await TextInput.InvokeAsync();
    }

    private Task OnTextCommittedAsync(IReadOnlyList<SlideTextParagraphDto> paragraphs)
    {
        if (_text == null)
            return Task.CompletedTask;
        _text.Draft = paragraphs;
        return CommitTextAsync();
    }

    private async Task<bool> HandleTextKeyAsync(string key, bool ctrl)
    {
        var single = _selection.Count == 1 && Find(_selection[0]) is AutoShape;
        var align = ctrl ? key.ToLowerInvariant() switch { "l" => "left", "e" => "center", "r" => "right", "j" => "justify", _ => null } : null;
        switch (key)
        {
            case "Escape" when _insertTextBox:
                _insertTextBox = false;
                StateHasChanged();
                return true;
            case "b" or "B" or "i" or "I" or "u" or "U" when ctrl && _selection.Count > 0:
                await FormatTextAsync(key.ToLowerInvariant(), null);
                return true;
            case not null when align != null && _selection.Count > 0:
                await FormatTextAsync("align", align);
                return true;
            case "F2" when single:
                await BeginTextAsync(_selection[0]);
                return true;
            case { Length: 1 } when !ctrl && single:
                await BeginTextAsync(_selection[0], key);
                return true;
            default:
                return false;
        }
    }

    private sealed class TextSession(Slide slide, AutoShape shape, InsertShapeCommand insert, string text, double[] caret)
    {
        public Slide Slide { get; } = slide;

        public AutoShape Shape { get; } = shape;

        public InsertShapeCommand Insert { get; } = insert;

        public string Text { get; } = text;

        public double[] Caret { get; } = caret;

        public IReadOnlyList<SlideTextParagraphDto> Draft { get; set; }

        public bool Typed { get; set; }
    }
}
