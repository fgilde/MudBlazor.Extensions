using System.ComponentModel;
using System.Text;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

public partial class MudExNotebookEditor
{
    private const double PasteOffset = 10;

    private Clip _clipboard;

    /// <summary>
    /// Called by the browser on paste with the clipboard content. Elements or pages copied in this editor are pasted as such while the clipboard still
    /// holds their marker (or, where the browser could not write one, their text); otherwise a picture is added, and text becomes a text block at the
    /// last click on the page.
    /// </summary>
    /// <param name="text">The <c>text/plain</c> part as UTF-8, or null if the browser could not read it.</param>
    /// <param name="image">The first image of the clipboard, or null.</param>
    /// <param name="contentType">The content type of <paramref name="image"/>.</param>
    /// <param name="clip">The marker found in the <c>text/html</c> part, or null.</param>
    /// <returns>A task that completes when the content is inserted.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnPasteAsync(IJSStreamReference text, IJSStreamReference image, string contentType, string clip)
    {
        string plain;
        byte[] bytes;
        try
        {
            plain = text == null ? null : Encoding.UTF8.GetString(await ReadAsync(text));
            bytes = image == null ? null : await ReadAsync(image);
        }
        catch (Exception e)
        {
            await ShowErrorAsync(e.Message);
            return;
        }

        await InvokeAsync(() => PasteAsync(plain, bytes, contentType, clip));
    }

    internal async Task PasteAsync(string text, byte[] image, string contentType, string marker = null)
    {
        if (IsReadOnly)
            return;
        await CommitTextAsync();
        if (_clipboard is { } clip && image == null && (marker != null ? marker == clip.Id : text == null || !clip.Marked && Normalize(text) == clip.Text))
            await PasteClipAsync(clip);
        else if (image != null)
        {
            var (x, y) = InsertPoint;
            await InsertPictureAsync(image, contentType, x, y);
        }
        else if (!string.IsNullOrEmpty(text))
            await PasteTextAsync(text);
        StateHasChanged();
    }

    private Task PasteFromRibbonAsync() => _js.Js is { } js ? js.InvokeVoidAsync("readClipboard").AsTask() : PasteAsync(null, null, null);

    internal async Task CopyElementsAsync(bool cut)
    {
        if (!CanEditPage || SelectedElements is not { Count: > 0 } elements)
            return;
        await CommitTextAsync();
        var page = _page;
        var media = elements.OfType<ImageElement>().Select(i => i.MediaId).Distinct()
            .ToDictionary(id => id, id => (_notebook.GetMedia(id), _notebook.GetMediaType(id)));
        var text = PlainText(elements);
        var clip = _clipboard = new Clip(page, elements, media, null, Normalize(text)) { Pastes = cut ? -1 : 0 };
        if (cut)
            await RunCommandAsync(new DeleteElementsCommand(page, elements.Select(e => e.Id).ToList()));
        clip.Marked = await WriteClipboardAsync(text, clip.Id);
    }

    internal async Task CopyPagesAsync()
    {
        if (_page == null)
            return;
        await CommitTextAsync();
        await CommitTitleAsync();
        var text = string.IsNullOrEmpty(_page.Title) ? TryLocalize("Untitled page") : _page.Title;
        var clip = _clipboard = new Clip(null, null, null, [_page], Normalize(text));
        clip.Marked = await WriteClipboardAsync(text, clip.Id);
    }

    private async Task PasteClipAsync(Clip clip)
    {
        if (clip.Pages != null)
        {
            await PastePagesAsync(clip.Pages);
            return;
        }

        if (!CanEditPage)
            return;
        var page = _page;
        var offset = ReferenceEquals(clip.Origin, page) ? ++clip.Pastes * PasteOffset : 0;
        var copies = new List<NoteElement>();
        await RunAsync(() =>
        {
            using (_notebook.Undo.BeginTransaction("Paste"))
                foreach (var element in clip.Elements)
                {
                    if (element is ImageElement image && _notebook.GetMedia(image.MediaId) == null && clip.Media.TryGetValue(image.MediaId, out var medium) && medium.Data != null)
                        _notebook.AddMedia(medium.Data, medium.ContentType);
                    var copy = element.Clone();
                    copy.Id = NoteValues.NewId();
                    copy.X += offset;
                    copy.Y += offset;
                    _notebook.Execute(new AddElementCommand(page, copy));
                    copies.Add(copy);
                }
        });
        if (_canvas != null && copies.Count > 0)
            await _canvas.SelectAsync(copies.Select(c => c.Id));
    }

    private Task PastePagesAsync(IReadOnlyList<NotebookPage> pages) => RunAsync(() =>
    {
        var alive = pages.Where(p => _notebook.Sections.Any(s => s.Pages.Contains(p))).ToList();
        if (alive.Count == 0 || _section == null)
            return;
        var duplicate = new DuplicatePagesCommand(alive);
        using (_notebook.Undo.BeginTransaction("Paste"))
        {
            _notebook.Execute(duplicate);
            _notebook.Execute(new MovePagesCommand(duplicate.Copies, _section, PageIndex + 1));
        }

        Show(_section, duplicate.Copies[0]);
    });

    private async Task PasteTextAsync(string text)
    {
        if (!CanEditPage)
            return;
        var (x, y) = InsertPoint;
        var block = new TextElement
        {
            Id = NoteValues.NewId(), X = x, Y = y, Width = MudExNotebookCanvas.NewTextWidth,
            Paragraphs = Normalize(text).Split('\n').Select(line => new NoteParagraph { Runs = [new NoteRun(line)] }).ToList()
        };
        await RunCommandAsync(new AddElementCommand(_page, block));
        if (_canvas != null)
            await _canvas.SelectAsync([block.Id]);
    }

    private async Task<bool> WriteClipboardAsync(string text, string marker)
    {
        if (_js.Js is not { } js)
            return false;
        try { return await js.InvokeAsync<bool>("writeClipboard", text, marker); }
        catch (Exception e) when (e is JSException or JSDisconnectedException or TaskCanceledException) { return false; }
    }

    private static async Task<byte[]> ReadAsync(IJSStreamReference data)
    {
        await using (data)
        {
            await using var stream = await data.OpenReadStreamAsync(MudExNotebookCanvas.MaxPictureBytes);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            return buffer.ToArray();
        }
    }

    private static string PlainText(IEnumerable<NoteElement> elements) =>
        string.Join("\r\n", elements.SelectMany(e => e switch
        {
            TextElement text => text.Paragraphs,
            TableElement table => table.Rows.SelectMany(r => r).SelectMany(c => c.Paragraphs),
            _ => []
        }).Select(p => string.Concat(p.Runs.Select(r => r.Text)).ReplaceLineEndings("\r\n")));

    private static string Normalize(string text) => (text ?? "").ReplaceLineEndings("\n").TrimEnd('\n');

    private sealed class Clip(NotebookPage origin, IReadOnlyList<NoteElement> elements, IReadOnlyDictionary<string, (byte[] Data, string ContentType)> media,
        IReadOnlyList<NotebookPage> pages, string text)
    {
        public NotebookPage Origin { get; } = origin;

        public IReadOnlyList<NoteElement> Elements { get; } = elements;

        public IReadOnlyDictionary<string, (byte[] Data, string ContentType)> Media { get; } = media;

        public IReadOnlyList<NotebookPage> Pages { get; } = pages;

        public string Text { get; } = text;

        public string Id { get; } = Guid.NewGuid().ToString("N");

        public bool Marked { get; set; }

        public int Pastes { get; set; }
    }
}
