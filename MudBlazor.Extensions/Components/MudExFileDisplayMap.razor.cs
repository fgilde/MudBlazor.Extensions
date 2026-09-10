using MudBlazor.Extensions.Helper;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Services;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Viewer for geographic files (.geojson, .gpx, .kml). GPX and KML are converted to GeoJSON in C# by
/// <see cref="GeoJsonConverter"/>, so the browser side only has to render one format with Leaflet. The map fits
/// the bounds of the data, shows names and descriptions in a popup and uses OpenStreetMap tiles.
/// </summary>
public partial class MudExFileDisplayMap : IMudExFileDisplay
{
    private object _loadedSource;
    private static readonly string[] SupportedExtensions = { ".geojson", ".gpx", ".kml" };

    private readonly string _containerId = $"map-{Guid.NewGuid().ToFormattedId()}";

    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayMap);

    /// <inheritdoc />
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    private bool _isLoading = true;
    private string _errorMessage;
    private string _pendingGeoJson;
    private int _features;

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        var fileName = fileDisplayInfos?.FileName ?? string.Empty;
        return Task.FromResult(SupportedExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "Features", _features }
        });
    }

    /// <inheritdoc />
    public override object[] GetJsArguments() => new object[] { ElementReference, CreateDotNetObjectReference(), _containerId };

    /// <inheritdoc />
    public override async Task ImportModuleAndCreateJsAsync()
    {
        await base.ImportModuleAndCreateJsAsync();

        // Leaflet itself is pulled in by the JS module, which can await the actual script execution.
        if (_pendingGeoJson != null)
        {
            await RenderInternalAsync(_pendingGeoJson);
            _pendingGeoJson = null;
        }
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
            _pendingGeoJson = null;
            _features = 0;
            _errorMessage = null;
            await LoadMapAsync();
            // A viewer knows its own values only now, and its renders do not reach the host.
            await FileDisplayInfos.NotifyMetaChangedAsync();
        }
    }

    private async Task LoadMapAsync()
    {
        try
        {
            var content = await FileService.ReadAsStringFromFileDisplayInfosAsync(FileDisplayInfos);
            if (string.IsNullOrEmpty(content))
                return;

            var geoJson = GeoJsonConverter.ToGeoJson(FileDisplayInfos?.FileName ?? string.Empty, content);
            _features = CountFeatures(geoJson);

            if (JsReference != null)
                await RenderInternalAsync(geoJson);
            else
                _pendingGeoJson = geoJson; // The JS module only exists after the first render
        }
        catch (Exception e)
        {
            _isLoading = false;
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
        }
        StateHasChanged();
    }

    private static int CountFeatures(string geoJson)
    {
        using var doc = JsonDocument.Parse(geoJson);
        if (doc.RootElement.TryGetProperty("features", out var features) && features.ValueKind == JsonValueKind.Array)
            return features.GetArrayLength();
        return doc.RootElement.TryGetProperty("type", out _) ? 1 : 0;
    }

    private Task RenderInternalAsync(string geoJson)
        => JsReference.InvokeVoidAsync("renderGeoJson", geoJson).AsTask();

    /// <summary>
    /// Called from JS once the map has drawn the data.
    /// </summary>
    [JSInvokable]
    public void OnMapRendered()
    {
        _isLoading = false;
        _errorMessage = null;
        StateHasChanged();
    }

    /// <summary>
    /// Called from JS when Leaflet could not render the file.
    /// </summary>
    [JSInvokable]
    public void OnError(string message)
    {
        _isLoading = false;
        _errorMessage = message;
        MudExFileDisplay?.ShowError(message);
        StateHasChanged();
    }
}
