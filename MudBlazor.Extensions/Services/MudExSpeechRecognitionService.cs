using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.W3C;
using Nextended.Core.Attributes;

namespace MudBlazor.Extensions.Services
{
    [RegisterAs(typeof(ISpeechRecognitionService) , RegisterAsImplementation = true, ServiceLifetime = ServiceLifetime.Transient)]
    internal class MudExSpeechRecognitionService : ISpeechRecognitionService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ConcurrentBag<string> _recordings = new();
        private readonly MudExCaptureNotificationService _captureNotifier;

        public MudExSpeechRecognitionService(IJSRuntime jsRuntime, MudExCaptureNotificationService captureNotifier)
        {
            _jsRuntime = jsRuntime;
            _captureNotifier = captureNotifier;
        }

        public async ValueTask DisposeAsync()
        {
            await Task.WhenAll(_recordings.Select(r => StopRecordingAsync(r)));
        }

        public async Task<string> StartRecordingAsync(SpeechRecognitionOptions options, Action<SpeechRecognitionResult> callback, Action<string> stoppedCallback = null, CancellationToken ct = default)
        {
            var callbackReference = DotNetObjectReference.Create(new JsRecordingCallbackWrapper<SpeechRecognitionResult>(callback, s =>
            {
                _recordings.TryTake(out s);
                _captureNotifier.RemoveRecordingInfo(s);
                stoppedCallback?.Invoke(s);
            }));
            var result = await _jsRuntime.InvokeAsync<string>("MudExSpeechRecognition.startRecording", ct, options, callbackReference);
            _recordings.Add(result);
            if (options.MaxCaptureTime is { TotalSeconds: > 0 })
                _ = Task.Delay(options.MaxCaptureTime.Value, ct).ContinueWith(_ => StopRecordingAsync(result), TaskScheduler.Default);
            if (options.ShowNotificationWhileRecording)
                _captureNotifier.ShowRecordingInfo(result, options.MaxCaptureTime, (s, _) => StopRecordingAsync(s));
            return result;
        }

        public Task<string> StartRecordingAsync(Action<SpeechRecognitionResult> callback, Action<string> stoppedCallback = null, CancellationToken ct = default)
            => StartRecordingAsync(null, callback, stoppedCallback, ct);

        public Task StopRecordingAsync(string recordingId, CancellationToken ct = default)
            => _jsRuntime.InvokeVoidAsync("MudExSpeechRecognition.stopRecording", ct, recordingId).AsTask();

        public Task StopAllRecordingsAsync(CancellationToken ct = default)
            => _jsRuntime.InvokeVoidAsync("MudExSpeechRecognition.stopAllRecordings", ct).AsTask();

        public Task<IEnumerable<AudioDevice>> GetAudioDevicesAsync(CancellationToken ct = default)
            => _jsRuntime.InvokeAsync<IEnumerable<AudioDevice>>("MudExSpeechRecognition.getAvailableAudioDevices", ct).AsTask();
    }
}
