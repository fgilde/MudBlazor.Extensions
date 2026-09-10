using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Viewer for 3D model files (.stl, .obj, .ply, .glb, .gltf, .3mf). Renders the mesh with three.js in a WebGL
/// canvas, fits the camera to the model's bounding box and offers orbit/zoom/pan plus a wireframe and an
/// auto rotate toggle. three.js and its loaders come from a CDN, nothing is bundled into the package.
/// </summary>
public partial class MudExFileDisplayModel3d : IMudExFileDisplay
{
    private object _loadedSource;
    private static readonly string[] SupportedExtensions = { ".stl", ".obj", ".ply", ".glb", ".gltf", ".3mf" };

    private readonly string _containerId = $"model3d-{Guid.NewGuid().ToFormattedId()}";

    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayModel3d);

    /// <inheritdoc />
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <summary>
    /// Renders the model as a wireframe instead of a shaded surface
    /// </summary>
    [Parameter]
    public bool Wireframe { get; set; }

    /// <summary>
    /// Rotates the model continuously
    /// </summary>
    [Parameter]
    public bool AutoRotate { get; set; }

    private bool _isLoading = true;
    private string _errorMessage;
    private int _triangles;
    private byte[] _pendingBytes;
    private string _pendingFormat;

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
            { "Format", Path.GetExtension(fileDisplayInfos?.FileName ?? string.Empty).TrimStart('.').ToUpperInvariant() },
            { "Triangles", _triangles }
        });
    }

    /// <inheritdoc />
    public override object[] GetJsArguments() => new object[] { ElementReference, CreateDotNetObjectReference(), _containerId };

    /// <inheritdoc />
    public override async Task ImportModuleAndCreateJsAsync()
    {
        await base.ImportModuleAndCreateJsAsync();

        if (_pendingBytes != null)
        {
            await RenderInternalAsync(_pendingBytes, _pendingFormat);
            _pendingBytes = null;
            _pendingFormat = null;
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
            _pendingBytes = null;
            _triangles = 0;
            _errorMessage = null;
            await LoadModelAsync();
            // A viewer knows its own values only now, and its renders do not reach the host.
            await FileDisplayInfos.NotifyMetaChangedAsync();
        }
    }

    private async Task LoadModelAsync()
    {
        try
        {
            await using var stream = await FileService.ReadStreamAsync(FileDisplayInfos);
            if (stream == null)
                return;

            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var format = Path.GetExtension(FileDisplayInfos?.FileName ?? string.Empty).TrimStart('.').ToLowerInvariant();

            if (JsReference != null)
            {
                await RenderInternalAsync(bytes, format);
            }
            else
            {
                // The JS module is only imported after the first render, so keep the bytes until it is there.
                _pendingBytes = bytes;
                _pendingFormat = format;
            }
        }
        catch (Exception e)
        {
            _isLoading = false;
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
        }
        StateHasChanged();
    }

    private Task RenderInternalAsync(byte[] bytes, string format)
        => JsReference.InvokeVoidAsync("renderModel", bytes, format, Wireframe, AutoRotate).AsTask();

    private async Task ToggleWireframeAsync()
    {
        Wireframe = !Wireframe;
        if (JsReference != null)
            await JsReference.InvokeVoidAsync("setWireframe", Wireframe);
    }

    private async Task ToggleAutoRotateAsync()
    {
        AutoRotate = !AutoRotate;
        if (JsReference != null)
            await JsReference.InvokeVoidAsync("setAutoRotate", AutoRotate);
    }

    private async Task ResetViewAsync()
    {
        if (JsReference != null)
            await JsReference.InvokeVoidAsync("resetView");
    }

    /// <summary>
    /// Called from JS when the model is on screen.
    /// </summary>
    [JSInvokable]
    public void OnModelLoaded(int triangles)
    {
        _triangles = triangles;
        _isLoading = false;
        _errorMessage = null;
        StateHasChanged();
    }

    /// <summary>
    /// Called from JS when three.js could not load or render the file.
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
