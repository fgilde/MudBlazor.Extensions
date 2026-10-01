using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Services;
using Nextended.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>Client-side viewer for OpenDocument text, spreadsheet, presentation and drawing files.</summary>
public partial class MudExFileDisplayOpenDocument : IMudExFileDisplay
{
    private static readonly string[] Extensions = { ".odt", ".ods", ".odp", ".odg", ".ott", ".ots", ".otp", ".otg" };
    private object _loadedSource;
    private OpenDocumentFile _document;
    private OpenDocumentPage _selected;
    private bool _loading;
    private string _errorMessage;

    [Inject] private MudExFileService FileService { get; set; }
    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayOpenDocument);
    /// <inheritdoc />
    public int RenderPriority => 100;
    /// <inheritdoc />
    [Parameter] public IMudExFileDisplayInfos FileDisplayInfos { get; set; }
    /// <summary>Reference to the parent file display.</summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos infos, IMudExFileService fileService)
        => Task.FromResult(Extensions.Contains(Path.GetExtension(infos?.FileName ?? string.Empty), StringComparer.OrdinalIgnoreCase)
                           || MimeType.Matches(infos?.ContentType, "application/vnd.oasis.opendocument.*"));

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos infos)
        => Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            ["Media type"] = _document?.MediaType,
            ["Title"] = _document?.Title,
            ["Creator"] = _document?.Creator,
            ["Pages / sheets"] = _document?.Pages.Count ?? 0
        });

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var infos);
        var changed = infos.SourceChanged(ref _loadedSource);
        await base.SetParametersAsync(parameters);
        if (!changed) return;
        _document?.Dispose();
        _document = null;
        _selected = null;
        _loading = true;
        _errorMessage = null;
        try
        {
            await using var stream = await FileService.ReadStreamAsync(FileDisplayInfos);
            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy);
            _document = OpenDocumentFile.Open(copy.ToArray());
            _selected = _document.Pages.FirstOrDefault();
        }
        catch (Exception e)
        {
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
        await FileDisplayInfos.NotifyMetaChangedAsync();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        _document?.Dispose();
        await base.DisposeAsync();
    }
}
