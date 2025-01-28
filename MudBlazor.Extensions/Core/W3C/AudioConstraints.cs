using MudBlazor.Extensions.Attribute;

namespace MudBlazor.Extensions.Core.W3C;

/// <summary>
/// Represents constraints for capturing audio, including settings like device ID, echo cancellation, and sample rate.
/// </summary>
public class AudioConstraints
{
    /// <summary>
    /// Gets or sets the device ID for the audio source.
    /// </summary>
    [SafeCategory("Device Settings")]
    public string DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the group ID for the audio source.
    /// </summary>
    [SafeCategory("Device Settings")]
    public string GroupId { get; set; }

    /// <summary>
    /// Indicates whether local audio playback should be suppressed.
    /// </summary>
    [SafeCategory("Playback Settings")]
    public bool? SuppressLocalAudioPlayback { get; set; }

    /// <summary>
    /// Indicates whether echo cancellation is enabled.
    /// </summary>
    [SafeCategory("Audio Processing")]
    public bool? EchoCancellation { get; set; }

    /// <summary>
    /// Indicates whether auto gain control is enabled.
    /// </summary>
    [SafeCategory("Audio Processing")]
    public bool? AutoGainControl { get; set; }

    /// <summary>
    /// Indicates whether noise suppression is enabled.
    /// </summary>
    [SafeCategory("Audio Processing")]
    public bool? NoiseSuppression { get; set; }

    /// <summary>
    /// Gets or sets the desired sample rate for audio capture.
    /// </summary>
    [SafeCategory("Audio Settings")]
    public double? SampleRate { get; set; }

    /// <summary>
    /// Gets or sets the desired sample size for audio capture.
    /// </summary>
    [SafeCategory("Audio Settings")]
    public double? SampleSize { get; set; }

    /// <summary>
    /// Gets or sets the number of audio channels.
    /// </summary>
    [SafeCategory("Audio Settings")]
    public double? ChannelCount { get; set; }

    /// <summary>
    /// Gets or sets the desired latency for audio capture.
    /// </summary>
    [SafeCategory("Audio Settings")]
    public double? Latency { get; set; }

    /// <summary>
    /// Gets or sets the voice isolation mode.
    /// </summary>
    [SafeCategory("Audio Processing")]
    public CaptureVoiceIsolationMode? VoiceIsolation { get; set; }
}
