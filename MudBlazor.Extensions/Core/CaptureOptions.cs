using System.Text.Json.Serialization;
using Nextended.Core.Extensions;
using OneOf;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// Options for capturing video and audio.
/// </summary>
public class CaptureOptions
{
    /// <summary>
    /// The content type for video.
    /// </summary>
    public string ContentType { get; set; } = "video/webm; codecs=vp9";

    /// <summary>
    /// The content type for audio.
    /// </summary>
    public string AudioContentType { get; set; } = "audio/webm";

    [JsonInclude]
    internal bool CaptureScreen
    {
        get => ScreenCapture is { IsT0: true } ? ScreenCapture.AsT0 : CaptureMediaOptions != null || ScreenSource != null;
        set => ScreenCapture = value;
    }

    [JsonInclude]
    internal DisplayMediaOptions CaptureMediaOptions
    {
        get => ScreenCapture.IsT2 ? ScreenCapture.AsT2 : DisplayMediaOptions.Default;
        set => ScreenCapture = value;
    }

    [JsonInclude]
    internal MediaStreamTrack ScreenSource
    {
        get => ScreenCapture.IsT1 ? ScreenCapture.AsT1 : null;
        set => ScreenCapture = value;
    }

    /// <summary>
    /// The screen capture options.
    /// Provide a boolean to capture a screen or a <see cref="MediaStreamTrack"/> to capture a specific preselected screen or a <see cref="DisplayMediaOptions"/> to capture the screen with options.
    /// Set false to not capture the screen.
    /// </summary>
    [JsonIgnore]
    public OneOf<bool, MediaStreamTrack, DisplayMediaOptions> ScreenCapture { get; set; }

    /// <summary>
    /// The video device to record.
    /// </summary>
    public VideoDevice VideoDevice { get; set; }

    /// <summary>
    /// The audio device ids to record audio.
    /// </summary>
    public List<AudioDevice> AudioDevices { get; set; } = new();


    /// <summary>
    /// If set, the recording will stop after the specified time.
    /// </summary>
    public TimeSpan? MaxCaptureTime { get; set; }

    /// <summary>
    /// The source for the overlay default is camera as overlay over screen.
    /// </summary>
    public OverlaySource OverlaySource { get; set; } = OverlaySource.VideoDevice;

    /// <summary>
    /// Position of the overlay.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DialogPosition OverlayPosition { get; set; } = DialogPosition.BottomRight;

    /// <summary>
    /// Size of the overlay.
    /// </summary>
    public MudExDimension OverlaySize { get; set; } = new("20%", "20%");

    /// <summary>
    /// For the Overlay you can set <see cref="OverlayPosition"/> to <see cref="DialogPosition.Custom"/> and set the custom position for the overlay here.
    /// </summary>
    public MudExPosition OverlayCustomPosition { get; set; } = new("0", "0");


    /// <summary>
    /// Returns true when anything to capture is set.
    /// </summary>
    public bool Valid()
    {
        return AudioDevices.EmptyIfNull().Any() || VideoDevice != null || CaptureScreen;
    }
}

/// <summary>
/// The source for the overlay.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OverlaySource
{
    /// <summary>
    /// Use the camera as overlay over screen.
    /// </summary>
    VideoDevice,

    /// <summary>
    /// Use the screen as overlay over camera.
    /// </summary>
    CapturedScreen
}
