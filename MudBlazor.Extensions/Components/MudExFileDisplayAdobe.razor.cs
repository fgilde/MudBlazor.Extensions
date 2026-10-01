using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Services;
using Nextended.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Viewer for Adobe Illustrator and PostScript artwork. PDF-compatible Illustrator files are shown with the
/// browser's native PDF preview; EPS, EPSI and legacy Illustrator files use their embedded TIFF or EPSI preview when available.
/// PostScript is never executed in the browser.
/// </summary>
public partial class MudExFileDisplayAdobe : IMudExFileDisplay
{
    private static readonly string[] Extensions = { ".ai", ".ait", ".eps", ".epsf", ".epsi", ".epi", ".ps" };
    private static readonly string[] ContentTypes =
    {
        "application/postscript",
        "application/x-postscript",
        "application/eps",
        "application/x-eps",
        "application/illustrator",
        "application/x-illustrator",
        "application/x-ai",
        "application/vnd.adobe.illustrator",
        "image/eps",
        "image/x-eps"
    };

    private object _loadedSource;
    private MemoryStream _previewStream;
    private IMudExFileDisplayInfos _previewInfos;
    private string _previewUrl;
    private AdobeGraphicsDocument _document;
    private string _message;
    private bool _loading;

    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayAdobe);

    /// <inheritdoc />
    public int RenderPriority => 100;

    /// <inheritdoc />
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the parent <see cref="MudExFileDisplay"/> when this viewer is selected automatically.
    /// </summary>
    [CascadingParameter]
    public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        var extension = Path.GetExtension(fileDisplayInfos?.FileName ?? string.Empty);
        var contentType = fileDisplayInfos?.ContentType?.Split(';', 2)[0].Trim();
        var canHandle = Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
                        || ContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
        return Task.FromResult(canHandle);
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        IDictionary<string, object> result = new Dictionary<string, object>
        {
            { "Format", _document?.Format },
            { "Representation", RepresentationName(_document?.Representation) },
            { "Version", _document?.Version },
            { "Title", _document?.Title },
            { "Creator", _document?.Creator },
            { "Creation date", _document?.CreationDate },
            { "Bounding box", _document?.BoundingBox },
            { "Preview size", PreviewSize(_document) }
        };
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var infos);
        var sourceChanged = infos.SourceChanged(ref _loadedSource);

        await base.SetParametersAsync(parameters);

        if (!sourceChanged)
            return;

        await LoadAsync();
        await FileDisplayInfos.NotifyMetaChangedAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _message = null;
        _document = null;
        ClearPreview();
        StateHasChanged();

        try
        {
            await using var stream = await FileService.ReadStreamAsync(FileDisplayInfos);
            if (stream == null)
                throw new InvalidOperationException(TryLocalize("No file content is available."));

            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy);
            _document = AdobeGraphicsFile.Read(copy.ToArray(), FileDisplayInfos.FileName);

            if (_document.PreviewData != null)
            {
                var extension = _document.Representation switch
                {
                    AdobeGraphicsRepresentation.Pdf => ".pdf",
                    AdobeGraphicsRepresentation.TiffPreview => ".tiff",
                    _ => ".png"
                };
                _previewStream = new MemoryStream(_document.PreviewData, writable: false);
                _previewInfos = new AdobePreviewFileInfos(
                    Path.ChangeExtension(FileDisplayInfos.FileName ?? "artwork", extension),
                    _document.PreviewContentType,
                    _previewStream);
                if (_document.Representation == AdobeGraphicsRepresentation.Pdf)
                    _previewUrl = await FileService.CreateDataUrlAsync(_document.PreviewData, "application/pdf", false);
            }
            else
            {
                _message = TryLocalize("No browser-readable PDF or embedded EPS preview was found. Save Illustrator files with 'Create PDF Compatible File', or export PostScript with a TIFF or EPSI preview.");
            }
        }
        catch (Exception exception)
        {
            _message = exception.Message;
            MudExFileDisplay?.ShowError(exception.Message);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private static string RepresentationName(AdobeGraphicsRepresentation? representation) => representation switch
    {
        AdobeGraphicsRepresentation.Pdf => "PDF-compatible content",
        AdobeGraphicsRepresentation.TiffPreview => "Embedded TIFF preview",
        AdobeGraphicsRepresentation.EpsiPreview => "Embedded EPSI preview",
        AdobeGraphicsRepresentation.None => "No embedded preview",
        _ => null
    };

    private static string PreviewSize(AdobeGraphicsDocument document)
        => document?.PreviewWidth is { } width && document.PreviewHeight is { } height
            ? $"{width} × {height} px"
            : null;

    private void ClearPreview()
    {
        _previewInfos = null;
        _previewUrl = null;
        _previewStream?.Dispose();
        _previewStream = null;
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        ClearPreview();
        await base.DisposeAsync();
    }

    private sealed record AdobePreviewFileInfos(string FileName, string ContentType, Stream ContentStream) : IMudExFileDisplayInfos
    {
        public string Url => null;
    }
}
