using MudBlazor.Extensions.Helper;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Services;
using Nextended.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Viewer for PowerPoint presentations (.pptx). Reads the OOXML package with <see cref="PptxPresentation"/> and
/// renders the slides as scalable html - text, pictures, solid fills, groups and speaker notes. Runs fully
/// client-side and needs neither Office nor an online service, unlike <see cref="MudExFileDisplayOfficeLive"/>.
/// </summary>
public partial class MudExFileDisplayPresentation : IMudExFileDisplay
{
    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayPresentation);

    /// <inheritdoc />
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <summary>
    /// Shows the speaker notes below the slide
    /// </summary>
    [Parameter]
    public bool ShowNotes { get; set; } = true;

    private object _loadedSource;
    private PptxPresentation _presentation;
    private PptxSlide _selected;
    private string _errorMessage;

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        var fileName = fileDisplayInfos?.FileName ?? string.Empty;
        var isMatch = fileName.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase)
                      || MimeType.Matches(fileDisplayInfos?.ContentType, "application/vnd.openxmlformats-officedocument.presentationml*");
        return Task.FromResult(isMatch);
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "Slides", _presentation?.Slides.Count ?? 0 },
            { "Slide size", _presentation == null ? null : $"{_presentation.SlideWidth / 914400.0:F2} x {_presentation.SlideHeight / 914400.0:F2} in" }
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
            _presentation = null;
            _errorMessage = null;
            await LoadPresentationAsync();
            // A viewer knows its own values only now, and its renders do not reach the host.
            await FileDisplayInfos.NotifyMetaChangedAsync();
        }
    }

    private async Task LoadPresentationAsync()
    {
        try
        {
            var stream = await FileService.ReadStreamAsync(FileDisplayInfos);
            if (stream == null)
                return;

            // ZipArchive needs a seekable stream and keeps it open while the presentation is displayed.
            var seekable = new MemoryStream();
            await stream.CopyToAsync(seekable);
            await stream.DisposeAsync();
            seekable.Position = 0;

            _presentation = PptxPresentation.Open(seekable);
            _selected = _presentation.Slides.FirstOrDefault();
        }
        catch (Exception e)
        {
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
        }
        StateHasChanged();
    }

    private string AspectRatio => _presentation == null
        ? "16 / 9"
        : $"{_presentation.SlideWidth} / {_presentation.SlideHeight}";

    private int SelectedIndex => _selected == null ? -1 : _presentation.Slides.IndexOf(_selected);

    private bool CanGoBack => SelectedIndex > 0;

    private bool CanGoForward => SelectedIndex >= 0 && SelectedIndex < _presentation.Slides.Count - 1;

    private void Move(int offset)
    {
        var index = SelectedIndex + offset;
        if (index >= 0 && index < _presentation.Slides.Count)
            _selected = _presentation.Slides[index];
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        _presentation?.Dispose();
        _presentation = null;
        return base.DisposeAsync();
    }
}
