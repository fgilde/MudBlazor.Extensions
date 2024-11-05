using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using Nextended.Core.Attributes;
using OneOf;

namespace MudBlazor.Extensions.Services;

/// <summary>
/// Capture service for capturing audio and video.
/// </summary>
[RegisterAs(typeof(ICaptureService), RegisterAsImplementation = true, ServiceLifetime = ServiceLifetime.Transient)]
internal class MudExCaptureService : ICaptureService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly MudExCaptureNotificationService _captureNotifier;
    private readonly ConcurrentDictionary<string, DotNetObjectReference<JsRecordingCallbackWrapper<CaptureResult>>> _captures = new();

    public MudExCaptureService(IJSRuntime jsRuntime, MudExCaptureNotificationService captureNotifier)
    {
        _jsRuntime = jsRuntime;
        _captureNotifier = captureNotifier;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_captures.Select(pair => StopCaptureAsync(pair.Key)));
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
                _captures.TryRemove(s, out _);
                _captureNotifier.RemoveRecordingInfo(s);
                stoppedCallback?.Invoke(s);
            }));

        var result = await _jsRuntime.InvokeAsync<string>("MudExCapture.startCapture", options, callbackReference);
        _captures.TryAdd(result, callbackReference);
        if (options.MaxCaptureTime is { TotalSeconds: > 0 })
            _ = Task.Delay(options.MaxCaptureTime.Value).ContinueWith(_ => StopCaptureAsync(result));

        _captureNotifier.ShowRecordingInfo(result, options.MaxCaptureTime, (s, _) => StopCaptureAsync(s));
        return result;
    }

    public Task StopCaptureAsync(MediaStreamTrack track)
    {
         return _jsRuntime.InvokeAsync<string>("MudExCapture.stopPreviewCapture", track.Id).AsTask();
    }

    private CaptureResult Prepare(CaptureResult captureResult, CaptureOptions options)
    {
        // TODO: Think about to build the combined result in blazor maybe with FFMpegBlazor
        return captureResult;
    }

    /// <inheritdoc />
    public Task StopCaptureAsync(string captureId)
    {
        var callback = _captures.GetOrAdd(captureId, _ => null);
        return _jsRuntime.InvokeVoidAsync("MudExCapture.stopCapture", captureId, callback).AsTask();
    }

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