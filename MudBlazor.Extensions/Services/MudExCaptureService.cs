using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using Nextended.Core.Attributes;

namespace MudBlazor.Extensions.Services;

/// <summary>
/// Capture service for capturing audio and video.
/// </summary>
[RegisterAs(typeof(ICaptureService), RegisterAsImplementation = true, ServiceLifetime = ServiceLifetime.Transient)]
internal class MudExCaptureService : ICaptureService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ConcurrentBag<string> _captures = new();

    public MudExCaptureService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_captures.Select(StopCaptureAsync));
    }

    /// <inheritdoc />
    public async Task<string> StartCaptureAsync(CaptureOptions options, Action<CaptureResult> callback, Action<string> stoppedCallback = null)
    {
        var callbackReference = DotNetObjectReference.Create(new JsRecordingCallbackWrapper<CaptureResult>(
            captureResult => callback?.Invoke(Prepare(captureResult)), s =>
        {
            _captures.TryTake(out s);
            stoppedCallback?.Invoke(s);
        }));

        var result = await _jsRuntime.InvokeAsync<string>("MudExCapture.startCapture", options, callbackReference);
        _captures.Add(result);
        return result;
    }

    private CaptureResult Prepare(CaptureResult captureResult)
    {
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
    public async Task<MediaStreamTrack> SelectCaptureSourceAsync() =>
        await _jsRuntime.InvokeAsync<MediaStreamTrack>("MudExCapture.selectCaptureSource");

}