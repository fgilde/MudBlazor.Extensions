using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Edits the text of a shape in place: a content editable element over the shape's text area whose paragraphs and runs are built and
/// formatted by the browser. Text from the file is set as text, never as markup. The model changes only when <see cref="Committed"/> is handled.
/// </summary>
public partial class MudExSlideTextEditor
{
    private readonly JsModule<MudExSlideTextEditor> _module = new();
    private ElementReference _root;

    /// <summary>The presentation the shape belongs to.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    /// <summary>The shape whose text is edited.</summary>
    [Parameter] public AutoShape Shape { get; set; }

    /// <summary>Text typed to start editing; it replaces the whole text. Null keeps the text and puts the caret at the end or at <see cref="Caret"/>.</summary>
    [Parameter] public string Text { get; set; }

    /// <summary>The point in client pixels where the caret is placed, for example where the shape was double-clicked; null for the end of the text.</summary>
    [Parameter] public double[] Caret { get; set; }

    /// <summary>Raised with the current text after each input and formatting change, but not during an IME composition.</summary>
    [Parameter] public EventCallback<IReadOnlyList<SlideTextParagraphDto>> TextChanged { get; set; }

    /// <summary>Raised with the final text when the user ends editing with Esc or a click outside.</summary>
    [Parameter] public EventCallback<IReadOnlyList<SlideTextParagraphDto>> Committed { get; set; }

    /// <summary>True for the text of a table cell: Tab and Shift+Tab end editing and raise <see cref="Tabbed"/> instead of inserting a tab.</summary>
    [Parameter] public bool Cell { get; set; }

    /// <summary>Raised for a table cell with the final text when the user pressed Tab, with true for Shift+Tab.</summary>
    [Parameter] public EventCallback<(SlideTextParagraphDto[] Paragraphs, bool Back)> Tabbed { get; set; }

    /// <summary>Raised, at most a few times per second, with the paragraphs and runs the selection touches after the selection or its format changed.</summary>
    [Parameter] public EventCallback<IReadOnlyList<SlideTextParagraphDto>> SelectionChanged { get; set; }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!firstRender)
            return;
        var options = new { Paragraphs = SlideTextDom.ToDom(Shape, Presentation), SlideWidth = Presentation.SlideWidth, Text, Caret, Cell };
        await _module.StartAsync(JsRuntime, this, "MudExSlideText", dotnet => [_root, dotnet, options]);
    }

    internal async Task<SlideTextParagraphDto[]> ReadAsync()
    {
        if (_module.Js is not { } js)
            return null;
        try { return await JsPayload.ReadAsync<SlideTextParagraphDto[]>(null, await js.InvokeAsync<IJSStreamReference>("readData")); }
        catch (Exception e) when (e is JSException or JSDisconnectedException or TaskCanceledException or IOException or System.Text.Json.JsonException) { return null; }
    }

    internal async Task FormatAsync(string key, string value)
    {
        if (_module.Js is not { } js)
            return;
        try { await js.InvokeVoidAsync("format", key, value); }
        catch (Exception e) when (e is JSException or JSDisconnectedException) { }
    }

    /// <summary>Called by the browser with the current text after an input.</summary>
    /// <param name="paragraphs">The paragraphs of the editing DOM, or null if they come as <paramref name="stream"/>.</param>
    /// <param name="stream">The paragraphs as JSON when they are too large for one message.</param>
    /// <returns>A task that completes when the text is stored.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnTextInputAsync(SlideTextParagraphDto[] paragraphs, IJSStreamReference stream)
    {
        var list = await JsPayload.ReadAsync(paragraphs, stream);
        await InvokeAsync(() => TextChanged.InvokeAsync(list ?? []));
    }

    /// <summary>Called by the browser when the user ends editing.</summary>
    /// <param name="paragraphs">The final paragraphs of the editing DOM, or null if they come as <paramref name="stream"/>.</param>
    /// <param name="stream">The paragraphs as JSON when they are too large for one message.</param>
    /// <returns>A task that completes when the text is committed.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnTextCommitAsync(SlideTextParagraphDto[] paragraphs, IJSStreamReference stream)
    {
        var list = await JsPayload.ReadAsync(paragraphs, stream);
        await InvokeAsync(() => Committed.InvokeAsync(list ?? []));
    }

    /// <summary>Called by the browser when the user pressed Tab in a table cell.</summary>
    /// <param name="paragraphs">The final paragraphs of the editing DOM, or null if they come as <paramref name="stream"/>.</param>
    /// <param name="stream">The paragraphs as JSON when they are too large for one message.</param>
    /// <param name="back">True for Shift+Tab.</param>
    /// <returns>A task that completes when the text is committed and the next cell is edited.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnTextTabAsync(SlideTextParagraphDto[] paragraphs, IJSStreamReference stream, bool back)
    {
        var list = await JsPayload.ReadAsync(paragraphs, stream);
        await InvokeAsync(() => Tabbed.InvokeAsync((list ?? [], back)));
    }

    /// <summary>Called by the browser after the selection or its format changed.</summary>
    /// <param name="paragraphs">The paragraphs the selection touches with the runs it covers; the run at the caret for an empty selection.</param>
    /// <returns>A task that completes when the selection is handed on.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnSelectionFormatAsync(SlideTextParagraphDto[] paragraphs) => InvokeAsync(() => SelectionChanged.InvokeAsync(paragraphs ?? []));

    /// <summary>Called by the browser when HTML is pasted. Keeps only text with bold, italic, underline and colour.</summary>
    /// <param name="html">The <c>text/html</c> content of the clipboard, or null if it comes as <paramref name="stream"/>.</param>
    /// <param name="stream">The HTML as a JSON string when it is too large for one message.</param>
    /// <returns>The pasted paragraphs.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task<SlideTextParagraphDto[]> OnPasteHtml(string html, IJSStreamReference stream) => SlideTextDom.FromHtml(await JsPayload.ReadAsync(html, stream));

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await _module.DisposeAsync();
        await base.DisposeAsync();
    }
}
