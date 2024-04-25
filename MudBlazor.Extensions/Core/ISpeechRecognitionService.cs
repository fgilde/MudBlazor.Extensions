using MudBlazor.Extensions.Attribute;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// Defines a service interface for speech recognition functionalities, allowing for asynchronous recording and processing of speech input.
/// </summary>
[HasDocumentation("ISpeechRecognitionService.md")]
public interface ISpeechRecognitionService : IAsyncDisposable
{
    /// <summary>
    /// Starts recording speech asynchronously with specified options, registering callbacks for recognition results and stop events.
    /// </summary>
    /// <param name="options">Configuration options for speech recognition.</param>
    /// <param name="callback">Callback invoked with the speech recognition result.</param>
    /// <param name="stoppedCallback">Optional callback invoked when recording is stopped.</param>
    /// <returns>A Task representing the asynchronous operation, containing the recording ID.</returns>
    Task<string> StartRecordingAsync(SpeechRecognitionOptions options, Action<SpeechRecognitionResult> callback, Action<string> stoppedCallback = null);

    /// <summary>
    /// Starts recording speech asynchronously, registering callbacks for recognition results and stop events.
    /// </summary>
    /// <param name="callback">Callback invoked with the speech recognition result.</param>
    /// <param name="stoppedCallback">Optional callback invoked when recording is stopped.</param>
    /// <returns>A Task representing the asynchronous operation, containing the recording ID.</returns>
    Task<string> StartRecordingAsync(Action<SpeechRecognitionResult> callback, Action<string> stoppedCallback = null);

    /// <summary>
    /// Stops the recording asynchronously using the specified recording ID.
    /// </summary>
    /// <param name="recordingId">The ID of the recording to be stopped.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    Task StopRecordingAsync(string recordingId);

    /// <summary>
    /// Stops all ongoing recordings asynchronously.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    Task StopAllRecordingsAsync();
}

