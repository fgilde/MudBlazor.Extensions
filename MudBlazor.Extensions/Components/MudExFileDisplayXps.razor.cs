using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Services;
using Nextended.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>Client-side XPS and OpenXPS viewer for fixed pages, paths, text and embedded raster images.</summary>
public partial class MudExFileDisplayXps : IMudExFileDisplay
{
    private object _loadedSource;
    private XpsFile _document;
    private XpsPage _selected;
    private bool _loading;
    private string _errorMessage;
    [Inject] private MudExFileService FileService { get; set; }
    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayXps);
    /// <inheritdoc />
    public int RenderPriority => 100;
    /// <inheritdoc />
    [Parameter] public IMudExFileDisplayInfos FileDisplayInfos { get; set; }
    /// <summary>Reference to the parent file display.</summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos infos, IMudExFileService service)
    {
        var ext = Path.GetExtension(infos?.FileName ?? string.Empty);
        return Task.FromResult(ext.Equals(".xps", StringComparison.OrdinalIgnoreCase) || ext.Equals(".oxps", StringComparison.OrdinalIgnoreCase)
            || MimeType.Matches(infos?.ContentType, "application/vnd.ms-xpsdocument", "application/oxps"));
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos infos)
        => Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object> { ["Pages"] = _document?.Pages.Count ?? 0, ["Format"] = Path.GetExtension(infos?.FileName ?? "").TrimStart('.').ToUpperInvariant() });

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var infos);
        var changed = infos.SourceChanged(ref _loadedSource);
        await base.SetParametersAsync(parameters);
        if (!changed) return;
        _document?.Dispose(); _document = null; _selected = null; _loading = true; _errorMessage = null;
        try
        {
            await using var stream = await FileService.ReadStreamAsync(FileDisplayInfos);
            using var copy = new MemoryStream(); await stream.CopyToAsync(copy);
            _document = XpsFile.Open(copy.ToArray()); _selected = _document.Pages.First();
        }
        catch (Exception e) { _errorMessage = e.Message; MudExFileDisplay?.ShowError(e.Message); }
        finally { _loading = false; StateHasChanged(); }
        await FileDisplayInfos.NotifyMetaChangedAsync();
    }

    private int Index => _selected == null ? -1 : _document.Pages.IndexOf(_selected);
    private bool CanGoBack => Index > 0;
    private bool CanGoForward => Index >= 0 && Index < _document.Pages.Count - 1;
    private void Move(int delta) { var next = Index + delta; if (next >= 0 && next < _document.Pages.Count) _selected = _document.Pages[next]; }
    /// <inheritdoc />
    public override async ValueTask DisposeAsync() { _document?.Dispose(); await base.DisposeAsync(); }
}
