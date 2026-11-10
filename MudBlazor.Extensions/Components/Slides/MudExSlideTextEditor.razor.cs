using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Edits the text of a shape in place: a content editable element over the shape's text area whose paragraphs and runs are built and
/// formatted by the browser. Text from the file is set as text, never as markup. The model changes only when <see cref="Committed"/> is handled.
/// </summary>
public partial class MudExSlideTextEditor
{
    private ElementReference _root;
    private IJSObjectReference _module;
    private IJSObjectReference _js;
    private DotNetObjectReference<MudExSlideTextEditor> _dotnet;
    private bool _disposed;

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

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!firstRender)
            return;
        try
        {
            await JsRuntime.InitializeMudBlazorExtensionsCoreAsync();
            var dotnet = DotNetObjectReference.Create(this);
            var options = new
            {
                Paragraphs = SlideTextDom.ToDom(Shape, Presentation),
                SlideWidth = Presentation.SlideWidth,
                Text,
                Caret
            };
            var (module, js) = await JsRuntime.ImportModuleAndCreateJsAsync<MudExSlideTextEditor>("MudExSlideText", _root, dotnet, options);
            (_module, _js, _dotnet) = (module, js, dotnet);
            if (_disposed)
                await ReleaseJsAsync();
        }
        catch (Exception e) when (e is JSException or JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    internal async Task<SlideTextParagraphDto[]> ReadAsync()
    {
        if (_js == null)
            return null;
        try { return await _js.InvokeAsync<SlideTextParagraphDto[]>("read"); }
        catch (Exception e) when (e is JSException or JSDisconnectedException or TaskCanceledException) { return null; }
    }

    internal async Task FormatAsync(string key, string value)
    {
        if (_js == null)
            return;
        try { await _js.InvokeVoidAsync("format", key, value); }
        catch (Exception e) when (e is JSException or JSDisconnectedException) { }
    }

    /// <summary>Called by the browser with the current text after an input.</summary>
    /// <param name="paragraphs">The paragraphs of the editing DOM.</param>
    /// <returns>A task that completes when the text is stored.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnTextInputAsync(SlideTextParagraphDto[] paragraphs) => InvokeAsync(() => TextChanged.InvokeAsync(paragraphs ?? []));

    /// <summary>Called by the browser when the user ends editing.</summary>
    /// <param name="paragraphs">The final paragraphs of the editing DOM.</param>
    /// <returns>A task that completes when the text is committed.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnTextCommitAsync(SlideTextParagraphDto[] paragraphs) => InvokeAsync(() => Committed.InvokeAsync(paragraphs ?? []));

    /// <summary>Called by the browser when HTML is pasted. Keeps only text with bold, italic, underline and colour.</summary>
    /// <param name="html">The <c>text/html</c> content of the clipboard.</param>
    /// <returns>The pasted paragraphs.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public SlideTextParagraphDto[] OnPasteHtml(string html) => SlideTextDom.FromHtml(html);

    private async Task ReleaseJsAsync()
    {
        var (js, module, dotnet) = (_js, _module, _dotnet);
        (_js, _module, _dotnet) = (null, null, null);
        if (js != null)
        {
            try { await js.InvokeVoidAsync("dispose"); } catch { }
            try { await js.DisposeAsync(); } catch { }
        }

        if (module != null)
            try { await module.DisposeAsync(); } catch { }
        dotnet?.Dispose();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        _disposed = true;
        await ReleaseJsAsync();
        await base.DisposeAsync();
    }
}
