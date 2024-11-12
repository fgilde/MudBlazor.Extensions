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
            await Task.WhenAll(_recordings.Select(StopRecordingAsync));
        }

        public async Task<string> StartRecordingAsync(SpeechRecognitionOptions options, Action<SpeechRecognitionResult> callback, Action<string> stoppedCallback = null)
        {
            var callbackReference = DotNetObjectReference.Create(new JsRecordingCallbackWrapper<SpeechRecognitionResult>(callback, s =>
            {
                _recordings.TryTake(out s);
                _captureNotifier.RemoveRecordingInfo(s);
                stoppedCallback?.Invoke(s);
            }));
            var result = await _jsRuntime.InvokeAsync<string>("MudExSpeechRecognition.startRecording", options, callbackReference);
            _recordings.Add(result);
            if (options.MaxCaptureTime is { TotalSeconds: > 0 })
                _ = Task.Delay(options.MaxCaptureTime.Value).ContinueWith(_ => StopRecordingAsync(result));
            if (options.ShowNotificationWhileRecording)
                _captureNotifier.ShowRecordingInfo(result, options.MaxCaptureTime, (s, _) => StopRecordingAsync(s));
            return result;
        }

        public Task<string> StartRecordingAsync(Action<SpeechRecognitionResult> callback, Action<string> stoppedCallback = null) => StartRecordingAsync(null, callback, stoppedCallback);

        public Task StopRecordingAsync(string recordingId) => _jsRuntime.InvokeVoidAsync("MudExSpeechRecognition.stopRecording", recordingId).AsTask();

        public Task StopAllRecordingsAsync() => _jsRuntime.InvokeVoidAsync("MudExSpeechRecognition.stopAllRecordings").AsTask();
        public Task<IEnumerable<AudioDevice>> GetAudioDevicesAsync() => _jsRuntime.InvokeAsync<IEnumerable<AudioDevice>>("MudExSpeechRecognition.getAvailableAudioDevices").AsTask();


    }
}