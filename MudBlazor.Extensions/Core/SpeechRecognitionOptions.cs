using System.Globalization;
using System.Text.Json.Serialization;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// Defines configuration options for speech recognition services.
/// </summary>
public class SpeechRecognitionOptions
{
    /// <summary>
    /// Gets or sets the culture information for the speech recognition, derived from the language code.
    /// </summary>
    /// <remarks>
    /// This property is marked with attributes to be ignored during JSON serialization.
    /// </remarks>
    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public CultureInfo CultureInfo
    {
        get => !string.IsNullOrEmpty(Lang) ? CultureInfo.GetCultureInfo(Lang) : CultureInfo.InvariantCulture;
        set => Lang = value.Name;
    }

    /// <summary>
    /// If set, the recording will stop after the specified time.
    /// </summary>
    public TimeSpan? MaxCaptureTime { get; set; }

    /// <summary>
    /// If this is true a notification toast will be shown while recording.
    /// </summary>
    public bool ShowNotificationWhileRecording { get; set; }

    /// <summary>
    /// DeviceId for audio input, used for selecting the appropriate microphone.
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    /// Language code used for speech recognition, corresponding to the specific culture.
    /// </summary>
    public string Lang { get; set; }

    /// <summary>
    /// Specifies whether only final results should be returned, ignoring interim results.
    /// </summary>
    public bool FinalResultsOnly { get; set; }

    /// <summary>
    /// Indicates if the recognition should process continuously rather than stopping after a single utterance.
    /// </summary>
    public bool Continuous { get; set; }

    /// <summary>
    /// Indicates whether interim results should be included in the recognition results.
    /// </summary>
    public bool InterimResults { get; set; }
}