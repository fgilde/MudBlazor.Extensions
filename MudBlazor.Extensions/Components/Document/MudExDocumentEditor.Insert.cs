using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Document;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components;

public partial class MudExDocumentEditor
{
    internal const int MaxPictureBytes = 16 * 1024 * 1024;

    private static readonly Dictionary<string, string> PictureTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".gif"] = "image/gif", [".bmp"] = "image/bmp"
    };

    private static readonly string[] Symbols =
    [
        "\u00A9", "\u00AE", "\u2122", "\u00A7", "\u00B6", "\u2020", "\u2021", "\u2022", "\u2026", "\u2013", "\u2014", "\u20AC", "\u00A3", "\u00A5",
        "\u00B0", "\u00B1", "\u00D7", "\u00F7", "\u2260", "\u2264", "\u2265", "\u221E", "\u03B1", "\u03B2", "\u03C0", "\u00B5", "\u2192", "\u2190"
    ];

    private const string TocInstruction = "TOC \\o \"1-3\" \\h \\z \\u";

    private readonly string _pictureInput = $"mud-ex-doc-picture-{Guid.NewGuid():N}";

    private static string PictureAccept => string.Join(",", PictureTypes.Keys);

    internal Task InsertPageBreakAsync() => RunAsync(range =>
    {
        if (Live(range))
            _document.Execute(new InsertBreakCommand(range.Normalize(_document).End, BreakKind.Page), range);
    });

    internal Task InsertTableAsync((int Rows, int Columns) size) => RunAsync(range =>
    {
        if (Live(range) && size.Rows >= 1 && size.Columns is >= 1 and <= MudExDocumentTableDialog.MaxColumns)
            _document.Execute(new InsertTableCommand(range.Normalize(_document).End, size.Rows, size.Columns), range);
    });

    internal async Task OpenTableDialogAsync()
    {
        var (range, format) = await SelectionFormatAsync();
        if (format == null)
            return;
        var reference = await DialogService.ShowExAsync<MudExDocumentTableDialog>(TryLocalize("Insert Table"), new DialogParameters { { nameof(MudExDocumentTableDialog.Localizer), Localizer } },
            DialogOptionsEx.EditorDialogOptions(DialogOptions));
        var result = await reference.Result;
        if (await ReselectAsync(range) && result is { Canceled: false, Data: ValueTuple<int, int> size })
            await InsertTableAsync(size);
        await FocusSurfaceAsync();
    }

    internal Task InsertTextAsync(string text) => RunAsync(range =>
    {
        if (Live(range) && !string.IsNullOrEmpty(text))
            _document.Execute(new ReplaceTextCommand(range, text), range);
    });

    internal Task InsertDateAsync() => InsertTextAsync(DateTime.Today.ToString("d", CultureInfo.CurrentCulture));

    internal Task InsertFieldAsync(string instruction, string display) => RunAsync(range =>
    {
        if (Live(range))
            _document.Execute(new InsertFieldCommand(range, instruction, display), range);
    });

    internal Task PickPictureAsync() => IsReadOnly ? Task.CompletedTask : JsRuntime.InvokeVoidAsync("MudExEventHelper.clickElementById", _pictureInput).AsTask();

    private async Task OnPictureChosenAsync(InputFileChangeEventArgs e)
    {
        try
        {
            var file = e.File;
            if (file == null || IsReadOnly)
                return;
            var type = PictureTypes.GetValueOrDefault(Path.GetExtension(file.Name) ?? "") ?? (PictureTypes.ContainsValue(file.ContentType) ? file.ContentType : null);
            if (type == null)
            {
                await ShowErrorAsync(TryLocalize("Only PNG, JPEG, GIF and BMP pictures can be inserted."));
                return;
            }

            await using var stream = file.OpenReadStream(MaxPictureBytes);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            await InsertPictureAsync(buffer.ToArray(), type);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.Message);
        }
    }

    internal Task InsertPictureAsync(byte[] data, string contentType) => RunAsync(range =>
    {
        if (Live(range) && data is { Length: > 0 and <= MaxPictureBytes })
            _document.Execute(new InsertImageCommand(range, data, contentType), range);
    });

    internal async Task OpenLinkDialogAsync()
    {
        (DocumentRange Range, string Text, string Address) info = default;
        await RunAsync(range => info = Live(range) ? LinkInfo(range) : default);
        if (info.Range.Start == null)
            return;
        var reference = await DialogService.ShowExAsync<MudExNotebookLinkDialog>(TryLocalize("Link"), new DialogParameters
        {
            { nameof(MudExNotebookLinkDialog.Text), info.Text },
            { nameof(MudExNotebookLinkDialog.Address), info.Address },
            { nameof(MudExNotebookLinkDialog.Localizer), Localizer }
        }, DialogOptionsEx.EditorDialogOptions(DialogOptions));
        var result = await reference.Result;
        if (await ReselectAsync(info.Range) && result is { Canceled: false, Data: ValueTuple<string, string> link })
            await ApplyLinkAsync(info.Range, info.Text, link.Item1, link.Item2);
        await FocusSurfaceAsync();
    }

    internal Task RemoveLinkAsync() => RunAsync(range =>
    {
        if (!Live(range))
            return;
        var info = LinkInfo(range);
        if (!range.IsEmpty || info.Address != null)
            _document.Execute(new SetHyperlinkCommand(info.Range, null), range);
    });

    internal Task ApplyLinkAsync(DocumentRange range, string old, string text, string address) => RunAsync(_ =>
    {
        if (!Live(range) || new Hyperlink(address?.Trim(), null, null).SafeTarget is not { } safe || safe.StartsWith('#'))
            return;
        address = address.Trim();
        text = (text ?? "").ReplaceLineEndings(" ");
        if (range.IsEmpty && text.Length == 0)
            text = address;
        using (_document.Undo.BeginTransaction("Insert link"))
        {
            var target = range;
            if (text.Length > 0 && text != old)
            {
                var (start, _) = range.Normalize(_document);
                _document.Execute(new ReplaceTextCommand(range, text), range);
                target = new DocumentRange(start, start with { Offset = start.Offset + text.Length });
            }

            _document.Execute(new SetHyperlinkCommand(target, address), range);
        }
    });

    private (DocumentRange Range, string Text, string Address) LinkInfo(DocumentRange range)
    {
        var (start, end) = range.Normalize(_document);
        var (paragraph, from) = _document.Resolve(start);
        var link = (range.IsEmpty ? InlineEdit.InsertionStyle(paragraph, from).Link : null) ?? InlineEdit.At(paragraph, from)?.Link;
        var address = link?.SafeTarget is { } target && !target.StartsWith('#') ? target : null;
        if (address != null && range.IsEmpty)
        {
            var (first, last, position) = (-1, -1, 0);
            foreach (var inline in paragraph.Inlines)
            {
                if (ReferenceEquals(inline.Link, link) && inline.Length > 0)
                    (first, last) = (first < 0 ? position : first, position + inline.Length);
                position += inline.Length;
            }

            range = new DocumentRange(start with { Offset = first }, end with { Offset = last });
        }

        return (range, _document.TextOf(range), address);
    }
}
