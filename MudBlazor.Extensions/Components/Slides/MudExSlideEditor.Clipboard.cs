using System.ComponentModel;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlideEditor
{
    private const long PasteOffset = 914400 / 8;
    private const long MaxPasteBytes = 50L * 1024 * 1024;

    private Clip _clipboard;

    /// <summary>
    /// Called by the browser on paste with the clipboard content. Shapes or slides copied in this editor are pasted as such while the clipboard
    /// still holds their marker (or, where the browser could not write one, their text); otherwise an image becomes a picture and text a text box.
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
            plain = text == null ? null : System.Text.Encoding.UTF8.GetString(await ReadAsync(text));
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
        if (_canvas != null)
            await _canvas.CommitTextAsync();
        if (_clipboard is { } clip && image == null && (marker != null ? marker == clip.Id : text == null || !clip.Marked && Normalize(text) == clip.Text))
            await PasteClipAsync(clip);
        else if (image != null)
            await InsertPictureAsync(image, contentType);
        else if (!string.IsNullOrEmpty(text))
            await PasteTextAsync(text);
        StateHasChanged();
    }

    private async Task PasteFromRibbonAsync()
    {
        if (_js != null)
        {
            try
            {
                await _js.InvokeVoidAsync("readClipboard");
                return;
            }
            catch (Exception e) when (e is JSException or JSDisconnectedException)
            {
            }
        }

        await PasteAsync(null, null, null);
    }

    private async Task CopyShapesAsync(bool cut)
    {
        if (_current == null || SelectedShapes.Count == 0)
            return;
        if (_canvas != null)
            await _canvas.CommitTextAsync();
        var slide = _current;
        var selected = SelectedShapes.ToList();
        var copies = ShapeEdit.Outermost(slide, selected).Select(s => ShapeEdit.Detach(_presentation, slide, s)).ToList();
        var text = PlainText(copies);
        var clip = _clipboard = new Clip(slide, copies, null, Normalize(text)) { Pastes = cut ? -1 : 0 };
        if (cut)
            await RunAsync(() => _presentation.Execute(new DeleteShapesCommand(slide, selected)));
        clip.Marked = await WriteClipboardAsync(text, clip.Id);
    }

    private async Task CopySlidesAsync()
    {
        if (_selection.Count == 0)
            return;
        if (_canvas != null)
            await _canvas.CommitTextAsync();
        var text = PlainText(_selection.SelectMany(s => s.Shapes));
        var clip = _clipboard = new Clip(null, null, _selection.Select(DuplicateSlidesCommand.Snapshot).ToList(), Normalize(text));
        clip.Marked = await WriteClipboardAsync(text, clip.Id);
    }

    private async Task PasteClipAsync(Clip clip)
    {
        if (clip.Slides != null)
        {
            await PasteSlidesAsync(clip.Slides);
            return;
        }

        var slide = _current;
        if (slide == null)
            return;
        var offset = ReferenceEquals(clip.Origin, slide) ? ++clip.Pastes * PasteOffset : 0;
        var master = slide.Layout?.Master;
        var ids = new List<uint>();
        await RunAsync(() =>
        {
            using (_presentation.Undo.BeginTransaction("Paste"))
                foreach (var shape in clip.Shapes)
                {
                    var copy = shape.Clone(null);
                    if (PlaceholderXml.Collides(slide, copy))
                        PlaceholderXml.ToTextBox(copy);
                    var at = copy.Transform ?? default;
                    copy.Transform = at with { X = at.X + offset, Y = at.Y + offset };
                    foreach (var part in SlideShape.Flatten([copy]))
                        part.Master = master ?? part.Master;
                    _presentation.Execute(new InsertShapeCommand(slide, copy));
                    ids.Add(copy.Id);
                }
        });
        if (_canvas != null)
            await _canvas.SelectAsync(slide, ids);
    }

    private Task PasteSlidesAsync(IReadOnlyList<Slide> snapshots) => RunAsync(() =>
    {
        var paste = new DuplicateSlidesCommand(snapshots, _current == null ? _presentation.Slides.Count : IndexOf(_current) + 1);
        using (_presentation.Undo.BeginTransaction("Paste"))
            _presentation.Execute(paste);
        Show(paste.Copies[0], paste.Copies);
    });

    private async Task PasteTextAsync(string text)
    {
        var slide = _current;
        if (slide == null)
            return;
        var lines = Normalize(text).Split('\n');
        var width = Math.Min(6 * 914400L, _presentation.SlideWidth * 4 / 5);
        var height = Math.Min(MudExSlideCanvas.TextBoxHeight * lines.Length, _presentation.SlideHeight);
        var shape = AutoShape.CreateTextBox(new ShapeTransform((_presentation.SlideWidth - width) / 2, (_presentation.SlideHeight - height) / 2, width, height),
            new TextBody { Wrap = true, Paragraphs = lines.Select(l => new TextParagraph { Runs = l.Length == 0 ? [] : [new TextRun(l)] }).ToList() });
        await RunAsync(() =>
        {
            _presentation.Execute(new InsertShapeCommand(slide, shape));
            shape.Name = $"TextBox {shape.Id - 1}";
        });
        if (_canvas != null && shape.Id != 0)
            await _canvas.SelectAsync(slide, [shape.Id]);
    }

    private async Task<bool> WriteClipboardAsync(string text, string marker)
    {
        if (_js == null)
            return false;
        try { return await _js.InvokeAsync<bool>("writeClipboard", text, marker); }
        catch (Exception e) when (e is JSException or JSDisconnectedException) { return false; }
    }

    private static async Task<byte[]> ReadAsync(IJSStreamReference data)
    {
        await using (data)
        {
            await using var stream = await data.OpenReadStreamAsync(MaxPasteBytes);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            return buffer.ToArray();
        }
    }

    private static string PlainText(IEnumerable<SlideShape> shapes) =>
        string.Join("\r\n", SlideShape.Flatten(shapes).SelectMany(s => s switch
            {
                AutoShape { Text: { } text } => [text],
                TableShape table => table.Rows.SelectMany(r => r.Cells).Where(c => !c.IsMergedAway && c.Text != null).Select(c => c.Text),
                _ => []
            }).SelectMany(t => t.Paragraphs)
            .Select(p => string.Concat(p.Runs.Select(r => r.Text)).ReplaceLineEndings("\r\n")));

    private static string Normalize(string text) => (text ?? "").ReplaceLineEndings("\n").TrimEnd('\n');

    private sealed class Clip(Slide origin, IReadOnlyList<SlideShape> shapes, IReadOnlyList<Slide> slides, string text)
    {
        public Slide Origin { get; } = origin;

        public IReadOnlyList<SlideShape> Shapes { get; } = shapes;

        public IReadOnlyList<Slide> Slides { get; } = slides;

        public string Text { get; } = text;

        public string Id { get; } = Guid.NewGuid().ToString("N");

        public bool Marked { get; set; }

        public int Pastes { get; set; }
    }
}
