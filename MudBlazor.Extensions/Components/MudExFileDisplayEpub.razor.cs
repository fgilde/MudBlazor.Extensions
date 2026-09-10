using MudBlazor.Extensions.Helper;
using Microsoft.AspNetCore.Components;
using Nextended.Core;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Viewer for EPUB e-books (.epub). Reads the package document with <see cref="EpubBook"/>, lists the chapters in
/// spine order next to the content and renders the selected chapter in a sandboxed iframe with its stylesheets and
/// images inlined. Works client-side, the archive is opened with the BCL <see cref="System.IO.Compression.ZipArchive"/>.
/// </summary>
public partial class MudExFileDisplayEpub : IMudExFileDisplay
{
    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayEpub);

    /// <inheritdoc />
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    private object _loadedSource;
    private EpubBook _book;
    private EpubChapter _selected;
    private string _chapterHtml;
    private string _errorMessage;

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        var fileName = fileDisplayInfos?.FileName ?? string.Empty;
        var isMatch = fileName.EndsWith(".epub", StringComparison.OrdinalIgnoreCase)
                      || MimeType.Matches(fileDisplayInfos?.ContentType, "application/epub+zip");
        return Task.FromResult(isMatch);
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "Title", _book?.Title },
            { "Author", _book?.Author },
            { "Language", _book?.Language },
            { "Chapters", _book?.Chapters.Count ?? 0 }
        });
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        // The same viewer instance serves every file of its kind, and FileDisplayInfos is always
        // the same object - so only the values tell one file from the next.
        parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var infos);
        var sourceChanged = infos.SourceChanged(ref _loadedSource);

        await base.SetParametersAsync(parameters);

        if (sourceChanged)
        {
            _book = null;
            _errorMessage = null;
            await LoadBookAsync();
            // A viewer knows its own values only now, and its renders do not reach the host.
            await FileDisplayInfos.NotifyMetaChangedAsync();
        }
    }

    private async Task LoadBookAsync()
    {
        try
        {
            var stream = await FileService.ReadStreamAsync(FileDisplayInfos);
            if (stream == null)
                return;

            // ZipArchive needs to seek, and it keeps the stream open for as long as the book is displayed.
            var seekable = new MemoryStream();
            await stream.CopyToAsync(seekable);
            await stream.DisposeAsync();
            seekable.Position = 0;

            _book = EpubBook.Open(seekable);
            SelectChapter(_book.Chapters.FirstOrDefault());
        }
        catch (Exception e)
        {
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
        }
        StateHasChanged();
    }

    private void SelectChapter(EpubChapter chapter)
    {
        _selected = chapter;
        _chapterHtml = chapter == null ? null : _book.RenderChapter(chapter);
    }

    private int SelectedIndex => _selected == null ? -1 : _book.Chapters.IndexOf(_selected);

    private bool CanGoBack => SelectedIndex > 0;

    private bool CanGoForward => SelectedIndex >= 0 && SelectedIndex < _book.Chapters.Count - 1;

    private void Move(int offset)
    {
        var index = SelectedIndex + offset;
        if (index >= 0 && index < _book.Chapters.Count)
            SelectChapter(_book.Chapters[index]);
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        _book?.Dispose();
        _book = null;
        return base.DisposeAsync();
    }
}
