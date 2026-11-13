using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

public partial class MudExNotebookCanvas
{
    private NotebookTextEditRequest _text;
    private MudExNotebookTextEditor _textEditor;
    private NotebookTextState _sessionState;

    /// <summary>Raised when a text editing session starts: a click on the free page in the text tool, a click into a text block or typing on a selected one.</summary>
    [Parameter] public EventCallback<NotebookTextEditRequest> TextEditRequested { get; set; }

    /// <summary>Raised on the first input of a text editing session, before the text is committed.</summary>
    [Parameter] public EventCallback TextInput { get; set; }

    /// <summary>Raised for Ctrl+K in a text editing session with the selected text and the link at the selection; answer with <see cref="SetLinkAsync"/>.</summary>
    [Parameter] public EventCallback<(string Text, string Link)> LinkRequested { get; set; }

    /// <summary>True while a text block is edited.</summary>
    public bool IsEditingText => _text != null;

    internal NotebookTextEditRequest TextEdit => _text;

    internal bool CanFormatText => Editable && (_text != null || SelectedElements.OfType<TextElement>().Any());

    internal NotebookTextState TextState => !Editable ? null
        : _text != null ? _sessionState ?? NotebookTextDom.State(OriginalOf(_text))
        : NotebookTextDom.State(SelectedElements.OfType<TextElement>().SelectMany(t => t.Paragraphs).ToList());

    /// <summary>
    /// Sets a link on the text selected when <see cref="LinkRequested"/> was raised. Only <c>http</c>, <c>https</c> and <c>mailto</c> addresses are
    /// accepted.
    /// </summary>
    /// <param name="text">The text to show; the address if empty.</param>
    /// <param name="address">The link target.</param>
    /// <returns>A task that completes when the link is set.</returns>
    public Task SetLinkAsync(string text, string address) =>
        _textEditor != null && NoteValues.IsSafeLink(address?.Trim()) ? _textEditor.LinkAsync(text ?? "", address.Trim()) : Task.CompletedTask;

    internal Task RequestLinkAsync() => _textEditor?.RequestLinkAsync() ?? Task.CompletedTask;

    /// <summary>Called by the browser when the check box of a to-do item was clicked outside a text editing session.</summary>
    /// <param name="id">The id of the text block.</param>
    /// <param name="paragraph">The index of the paragraph.</param>
    /// <returns>A task that completes when the check box is toggled.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnTodoToggleAsync(string id, int paragraph) => InvokeAsync(() =>
    {
        if (!Editable || _text?.ElementId == id || Find(id) is not TextElement text || paragraph < 0 || paragraph >= text.Paragraphs.Count
            || text.Paragraphs[paragraph].List != NoteListKind.Todo)
            return;
        var paragraphs = text.Paragraphs.ToList();
        paragraphs[paragraph] = paragraphs[paragraph] with { Checked = !paragraphs[paragraph].Checked };
        Execute(new SetTextCommand(Page, id, paragraphs));
    });

    internal async Task FormatTextAsync(string key, string value)
    {
        if (!Editable)
            return;
        if (_text != null)
        {
            if (_textEditor != null)
                await _textEditor.FormatAsync(key, value);
            return;
        }

        var blocks = SelectedElements.OfType<TextElement>().ToList();
        if (blocks.Count == 0)
            return;
        var all = blocks.SelectMany(b => b.Paragraphs).ToList();
        if (value == null && key is "b" or "i" or "u" or "s")
            value = NotebookTextDom.State(all) is { } state && key switch { "b" => state.Bold, "i" => state.Italic, "u" => state.Underline, _ => state.Strike } ? "0" : "1";
        if (key == "list" && all.All(p => NotebookTextDom.ListName(p.List) == value))
            value = "none";
        using (Notebook.Undo.BeginTransaction("Format text"))
            foreach (var block in blocks)
                if (NotebookTextDom.Format(block.Paragraphs, key, value) is var formatted && !ReferenceEquals(formatted, block.Paragraphs))
                    Notebook.Execute(new SetTextCommand(Page, block.Id, formatted));
        StateHasChanged();
    }

    internal async Task CommitTextAsync()
    {
        if (_text is not { } session)
            return;
        var live = _textEditor == null ? null : await _textEditor.ReadAsync();
        if (ReferenceEquals(_text, session))
            await EndTextEditAsync(live == null ? null : NotebookTextDom.FromDom(OriginalOf(session), live));
    }

    internal Task EndTextEditAsync(IReadOnlyList<NoteParagraph> paragraphs)
    {
        if (_text is not { } session)
            return Task.CompletedTask;
        _text = null;
        _sessionState = null;
        if (paragraphs != null && Editable)
        {
            if (!session.IsNew && Find(session.ElementId) is TextElement)
                Notebook.Execute(new SetTextCommand(Page, session.ElementId, paragraphs));
            else if (session.IsNew && !NotebookEdit.IsEmpty(paragraphs))
                Notebook.Execute(new AddElementCommand(Page, new TextElement { Id = session.ElementId, X = session.X, Y = session.Y, Width = NewTextWidth, Paragraphs = paragraphs }));
        }

        _raiseSelection = true;
        StateHasChanged();
        return RaiseSelectionAsync();
    }

    private Task BeginTextAsync(NotebookTextEditRequest request)
    {
        _text = request;
        _sessionState = null;
        StateHasChanged();
        return TextEditRequested.InvokeAsync(request);
    }

    private Task OnTextCommittedAsync(IReadOnlyList<NotebookTextParagraphDto> paragraphs) =>
        _text is { } session ? EndTextEditAsync(NotebookTextDom.FromDom(OriginalOf(session), paragraphs)) : Task.CompletedTask;

    private async Task OnTextSelectedAsync(IReadOnlyList<NotebookTextParagraphDto> paragraphs)
    {
        if (_text == null || NotebookTextDom.State(NotebookTextDom.FromDom([], paragraphs)) is var state && state == _sessionState)
            return;
        _sessionState = state;
        await SelectionChanged.InvokeAsync(_selection);
    }

    private IReadOnlyList<NoteParagraph> OriginalOf(NotebookTextEditRequest session) => !session.IsNew && Find(session.ElementId) is TextElement text ? text.Paragraphs : [];

    private double SessionWidth(NotebookTextEditRequest session) => !session.IsNew && Find(session.ElementId) is { } element ? element.Width : NewTextWidth;

    private static NotebookTextEditRequest Request(TextElement text) => new(text.Id, text.X, text.Y, false);
}
