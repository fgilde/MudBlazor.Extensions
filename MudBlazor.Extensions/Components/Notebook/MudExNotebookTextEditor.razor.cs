using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Edits a notebook text block in place: a content editable element exactly over the block whose paragraphs are <c>div</c> elements and whose runs
/// are <c>span</c> elements carrying their format as <c>data-*</c> attributes. Lists, to-do items, indents and links are handled in the browser;
/// text from the file or the clipboard is set as text, never as markup. The model changes only when <see cref="Committed"/> is handled.
/// </summary>
public partial class MudExNotebookTextEditor
{
    private readonly JsModule<MudExNotebookTextEditor> _module = new();
    private ElementReference _root;

    /// <summary>The id of the edited block, or null for a block that does not exist yet.</summary>
    [Parameter] public string BlockId { get; set; }

    /// <summary>The paragraphs the editing starts with.</summary>
    [Parameter] public IReadOnlyList<NoteParagraph> Paragraphs { get; set; } = [];

    /// <summary>The left edge of the block in page coordinates.</summary>
    [Parameter] public double X { get; set; }

    /// <summary>The top edge of the block in page coordinates.</summary>
    [Parameter] public double Y { get; set; }

    /// <summary>The width of the block in page coordinates.</summary>
    [Parameter] public double Width { get; set; }

    /// <summary>The zoom factor of the page; 1 is 100 %.</summary>
    [Parameter] public double Zoom { get; set; } = 1;

    /// <summary>Raised with the final text when the user ends editing with Esc, Ctrl+Z or a click outside.</summary>
    [Parameter] public EventCallback<IReadOnlyList<NotebookTextParagraphDto>> Committed { get; set; }

    /// <summary>Raised once, on the first change of the text.</summary>
    [Parameter] public EventCallback TextInput { get; set; }

    /// <summary>Raised, at most a few times per second, with the paragraphs and runs the selection touches.</summary>
    [Parameter] public EventCallback<IReadOnlyList<NotebookTextParagraphDto>> SelectionChanged { get; set; }

    /// <summary>Raised for Ctrl+K with the selected text and the link at the selection, if any.</summary>
    [Parameter] public EventCallback<(string Text, string Link)> LinkRequested { get; set; }

    private string BoxStyle
    {
        get
        {
            var zoom = MudExNotebookPageView.ZoomOf(Zoom);
            return $"left:{NotebookStyles.Num(X * zoom)}px;top:{NotebookStyles.Num(Y * zoom)}px;width:{NotebookStyles.Num(Width)}px;transform:scale({NotebookStyles.Num(zoom)});";
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!firstRender)
            return;
        var options = new { Paragraphs = NotebookTextDom.ToDom(Paragraphs), Id = BlockId };
        await _module.StartAsync(JsRuntime, this, "MudExNotebookText", dotnet => [_root, dotnet, options]);
    }

    internal async Task<NotebookTextParagraphDto[]> ReadAsync()
    {
        if (_module.Js is not { } js)
            return null;
        try { return await js.InvokeAsync<NotebookTextParagraphDto[]>("read"); }
        catch (Exception e) when (e is JSException or JSDisconnectedException or TaskCanceledException) { return null; }
    }

    internal Task FormatAsync(string key, string value) => CallAsync("format", key, value);

    internal Task RequestLinkAsync() => CallAsync("requestLink");

    internal Task LinkAsync(string text, string address) => CallAsync("link", text, address);

    private async Task CallAsync(string method, params object[] arguments)
    {
        if (_module.Js is not { } js)
            return;
        try { await js.InvokeVoidAsync(method, arguments); }
        catch (Exception e) when (e is JSException or JSDisconnectedException or TaskCanceledException) { }
    }

    /// <summary>Called by the browser when the user ends editing.</summary>
    /// <param name="paragraphs">The final paragraphs of the editing DOM.</param>
    /// <returns>A task that completes when the text is committed.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnTextCommitAsync(NotebookTextParagraphDto[] paragraphs) => InvokeAsync(() => Committed.InvokeAsync(paragraphs ?? []));

    /// <summary>Called by the browser on the first change of the text.</summary>
    /// <returns>A task that completes when the change is handed on.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnTextInputAsync() => InvokeAsync(() => TextInput.InvokeAsync());

    /// <summary>Called by the browser after the selection or its format changed.</summary>
    /// <param name="paragraphs">The paragraphs the selection touches with the runs it covers; the run at the caret for an empty selection.</param>
    /// <returns>A task that completes when the selection is handed on.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnSelectionFormatAsync(NotebookTextParagraphDto[] paragraphs) => InvokeAsync(() => SelectionChanged.InvokeAsync(paragraphs ?? []));

    /// <summary>Called by the browser for Ctrl+K.</summary>
    /// <param name="text">The selected text.</param>
    /// <param name="link">The link at the selection, or null.</param>
    /// <returns>A task that completes when the request is handed on.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnLinkRequestAsync(string text, string link) => InvokeAsync(() => LinkRequested.InvokeAsync((text ?? "", link)));

    /// <summary>
    /// Called by the browser when HTML is pasted. Keeps only text with bold, italic, underline, colour, lists and links with an allowed scheme;
    /// scripts, styles and everything else are dropped.
    /// </summary>
    /// <param name="html">The <c>text/html</c> content of the clipboard.</param>
    /// <returns>The pasted paragraphs.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public NotebookTextParagraphDto[] OnPasteHtml(string html) => NotebookTextDom.FromHtml(html);

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await _module.DisposeAsync();
        await base.DisposeAsync();
    }
}
