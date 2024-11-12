using System.Text.Json.Serialization;
using MudBlazor.Extensions.Core.W3C;
using Nextended.Core.Extensions;
using OneOf;

namespace MudBlazor.Extensions.Core.Capture;

/// <summary>
/// Options for capturing video and audio.
/// </summary>
public class CaptureOptions
{
    private List<AudioDevice> _audioDevices = new();

    /// <summary>
    /// If this is true a notification toast will be shown while recording.
    /// </summary>
    public bool ShowNotificationWhileRecording { get; set; } = true;

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
    public List<AudioDevice> AudioDevices
    {
        get => _audioDevices;
        set => _audioDevices = value;
    }

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
        return AudioDevices.EmptyIfNull().Any(d => !string.IsNullOrEmpty(d.DeviceId)) || VideoDevice?.DeviceId != null || CaptureScreen;
    }

    /// <summary>
    /// returns the position for the overlay based on given containerDimension.
    /// </summary>
    public MudExPosition GetOverlayPosition(MudExDimension containerDimension, MudExDimension? overlayDimension = null)
    {
        overlayDimension ??= OverlaySize;
        overlayDimension = overlayDimension.Value.ToAbsolute(containerDimension);
        return GetOverlayPosition(containerDimension.Width, containerDimension.Height, overlayDimension.Value.Width, overlayDimension.Value.Height);
    }

    /// <summary>
    /// returns the position for the overlay based on given reference sizes.
    /// </summary>
    public MudExPosition GetOverlayPosition(double canvasWidth, double canvasHeight, double overlayWidth, double overlayHeight)
    {
        return OverlayPosition switch
        {
            DialogPosition.Center => new MudExPosition((canvasWidth - overlayWidth) / 2, (canvasHeight - overlayHeight) / 2),
            DialogPosition.CenterLeft => new MudExPosition(20, (canvasHeight - overlayHeight) / 2),
            DialogPosition.CenterRight => new MudExPosition(canvasWidth - overlayWidth - 20, (canvasHeight - overlayHeight) / 2),
            DialogPosition.TopCenter => new MudExPosition((canvasWidth - overlayWidth) / 2, 20),
            DialogPosition.TopLeft => new MudExPosition(20, 20),
            DialogPosition.TopRight => new MudExPosition(canvasWidth - overlayWidth - 20, 20),
            DialogPosition.BottomCenter => new MudExPosition((canvasWidth - overlayWidth) / 2, canvasHeight - overlayHeight - 20),
            DialogPosition.BottomLeft => new MudExPosition(20, canvasHeight - overlayHeight - 20),
            DialogPosition.Custom => OverlayCustomPosition,
            DialogPosition.BottomRight or _ => new MudExPosition(canvasWidth - overlayWidth - 20, canvasHeight - overlayHeight - 20),
        };
    }

    #region Static factory methods

    /// <summary>
    /// Creates a new instance of <see cref="CaptureOptions"/> setting to capture the screen only.
    /// </summary>
    public static CaptureOptions ScreenOnly => new() { CaptureScreen = true};

    /// <summary>
    /// Creates a new instance of <see cref="CaptureOptions"/> setting to capture the camera only.
    /// </summary>
    public static CaptureOptions CameraOnly => new() { VideoDevice = VideoDevice.Default };

    /// <summary>
    /// Creates a new instance of <see cref="CaptureOptions"/> setting to capture audio only.
    /// </summary>
    public static CaptureOptions AudioOnly => new() { AudioDevices = new List<AudioDevice> { AudioDevice.Default } };

    /// <summary>
    /// Creates a new instance of <see cref="CaptureOptions"/> setting to capture the screen and camera.
    /// </summary>
    public static CaptureOptions ScreenAndCamera => new() { CaptureScreen = true, VideoDevice = VideoDevice.Default };

    /// <summary>
    /// Creates a new instance of <see cref="CaptureOptions"/> setting to capture the screen and audio.
    /// </summary>
    public static CaptureOptions ScreenAndAudio => new() { CaptureScreen = true, AudioDevices = new List<AudioDevice> { AudioDevice.Default } };

    /// <summary>
    /// Creates a new instance of <see cref="CaptureOptions"/> setting to capture the camera and audio.
    /// </summary>
    public static CaptureOptions CameraAndAudio => new() { VideoDevice = VideoDevice.Default, AudioDevices = new List<AudioDevice> { AudioDevice.Default } };

    /// <summary>
    /// Creates a new instance of <see cref="CaptureOptions"/> setting to capture the screen, camera and audio.
    /// </summary>
    public static CaptureOptions ScreenCameraAndAudio => new() { CaptureScreen = true, VideoDevice = VideoDevice.Default, AudioDevices = new List<AudioDevice> { AudioDevice.Default } };

    /// <summary>
    /// Creates a new instance of <see cref="CaptureOptions"/> setting to capture the screen and camera with the screen as overlay over the camera.
    /// </summary>
    public static CaptureOptions CameraAndScreen => ScreenAndCamera.SetProperties(o => o.OverlaySource = OverlaySource.CapturedScreen);

    /// <summary>
    /// Creates a new instance of <see cref="CaptureOptions"/> setting to capture the screen, camera and audio with the screen as overlay over the camera.
    /// </summary>
    public static CaptureOptions CameraScreenAndAudio => ScreenCameraAndAudio.SetProperties(o => o.OverlaySource = OverlaySource.CapturedScreen);
    
    #endregion


}

