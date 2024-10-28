using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using Nextended.Core.Attributes;
using Nextended.Core.Extensions;
using OneOf;

namespace MudBlazor.Extensions.Services;

/// <summary>
/// Capture service for capturing audio and video.
/// </summary>
[RegisterAs(typeof(ICaptureService), RegisterAsImplementation = true, ServiceLifetime = ServiceLifetime.Transient)]
internal class MudExCaptureService : ICaptureService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ConcurrentBag<string> _captures = new();
    private readonly ConcurrentDictionary<string, Snackbar> _toasts = new();
    private readonly ISnackbar _snackBarService;

    public MudExCaptureService(IJSRuntime jsRuntime, IServiceProvider serviceProvider)
    {
        _jsRuntime = jsRuntime;
        _snackBarService = serviceProvider.GetService<ISnackbar>();
    }

    RenderFragment CaptureInfo(TimeSpan? maxRecordingTime) => builder =>
    {
        int seq = 0;
        builder.OpenComponent(seq++, typeof(CaptureInfoComponent));
        builder.AddAttribute(seq++, "MaxRecordingTime", maxRecordingTime);
        builder.CloseComponent();
    };


    internal void ShowRecordingInfo(string captureId, TimeSpan? maxRecordingTime)
    {
        _snackBarService.Configuration.PositionClass = Defaults.Classes.Position.TopCenter;

        var toast =_snackBarService.Add(CaptureInfo(maxRecordingTime), Severity.Error, config =>
        {
            config.SnackbarVariant = Variant.Outlined;
            config.Icon = Icons.Material.Filled.Stop;
            config.BackgroundBlurred = true;
            config.RequireInteraction = true;
            config.ShowCloseIcon = false;
            config.Onclick = _ => StopCaptureAsync(captureId);
        }, key: captureId);
        _toasts.TryAdd(captureId, toast);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_captures.Select(StopCaptureAsync));
    }

    /// <inheritdoc />
    public async Task<string> StartCaptureAsync(CaptureOptions options, Action<CaptureResult> callback, Action<string> stoppedCallback = null)
    {
        if (!options.Valid())
        {
            return null;
        }
        var callbackReference = DotNetObjectReference.Create(new JsRecordingCallbackWrapper<CaptureResult>(
            captureResult => callback?.Invoke(Prepare(captureResult, options)), s =>
        {
            _captures.TryTake(out s);
            if (_toasts.TryRemove(s, out var toast))
                _snackBarService.Remove(toast);
            
            stoppedCallback?.Invoke(s);
        }));

        var result = await _jsRuntime.InvokeAsync<string>("MudExCapture.startCapture", options, callbackReference);
        _captures.Add(result);
        if (options.MaxCaptureTime is { TotalSeconds: > 0 })
            _ = Task.Delay(options.MaxCaptureTime.Value).ContinueWith(_ => StopCaptureAsync(result));
        
        ShowRecordingInfo(result, options.MaxCaptureTime);
        return result;
    }

    private CaptureResult Prepare(CaptureResult captureResult, CaptureOptions options)
    {
        //var datas = new [] { captureResult.CaptureData, captureResult.CameraData, captureResult.AudioData, captureResult.SystemAudioData }.Where(data => data != null);

        return captureResult;
    }

    /// <inheritdoc />
    public Task StopCaptureAsync(string captureId) =>
        _jsRuntime.InvokeVoidAsync("MudExCapture.stopCapture", captureId).AsTask();

    /// <inheritdoc />
    public Task StopAllCapturesAsync() =>
        _jsRuntime.InvokeVoidAsync("MudExCapture.stopAllCaptures").AsTask();

    /// <inheritdoc />
    public Task<IEnumerable<AudioDevice>> GetAudioDevicesAsync() =>
        _jsRuntime.InvokeAsync<IEnumerable<AudioDevice>>("MudExCapture.getAvailableAudioDevices").AsTask();

    /// <inheritdoc />
    public Task<IEnumerable<VideoDevice>> GetVideoDevicesAsync() =>
        _jsRuntime.InvokeAsync<IEnumerable<VideoDevice>>("MudExCapture.getAvailableVideoDevices").AsTask();

    /// <inheritdoc />
    public async Task<MediaStreamTrack> SelectCaptureSourceAsync(DisplayMediaOptions? displayMediaOptions = null, OneOf<ElementReference, string> elementForPreview = default)
    {
        object toPass = null;
        if (elementForPreview is { IsT0: true, AsT0: { Context: not null, Id: not null } })
        {
            toPass = elementForPreview.AsT0;
        }
        else if (elementForPreview.IsT1 && !string.IsNullOrWhiteSpace(elementForPreview.AsT1))
            toPass = elementForPreview.AsT1;
        
        return await _jsRuntime.InvokeAsync<MediaStreamTrack>("MudExCapture.selectCaptureSource", displayMediaOptions, toPass);
    }
}