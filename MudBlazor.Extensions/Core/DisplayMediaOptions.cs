using System.Text.Json.Serialization;
using MudBlazor.Extensions.Helper.JsonConverter;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// Represents the options for capturing display media, including video and audio constraints, 
/// preferences for capturing the current tab, and settings for system audio and browser surfaces.
/// </summary>
public class DisplayMediaOptions
{
    /// <summary>
    /// Gets or sets the constraints for video capture.
    /// </summary>
    public VideoConstraints Video { get; set; }

    /// <summary>
    /// Gets or sets the constraints for audio capture.
    /// </summary>
    public AudioConstraints Audio { get; set; }

    /// <summary>
    /// Indicates whether the current tab is preferred for capture.
    /// </summary>
    public bool PreferCurrentTab { get; set; }

    /// <summary>
    /// Determines whether the self browser surface should be included or excluded during capture.
    /// </summary>
    public IncludeExclude? SelfBrowserSurface { get; set; }

    /// <summary>
    /// Determines whether the system audio should be included or excluded.
    /// </summary>
    public IncludeExclude? SystemAudio { get; set; }

    /// <summary>
    /// Determines whether surface switching is allowed.
    /// </summary>
    public IncludeExclude? SurfaceSwitching { get; set; }

    /// <summary>
    /// Determines whether monitor-type surfaces should be included or excluded.
    /// </summary>
    public IncludeExclude? MonitorTypeSurfaces { get; set; }

    /// <summary>
    /// Configures the options to include system audio.
    /// </summary>
    /// <returns>The updated <see cref="DisplayMediaOptions"/>.</returns>
    public DisplayMediaOptions WithSystemAudio()
    {
        return this.SetProperties(options =>
        {
            options.Audio = new AudioConstraints
            {
                SuppressLocalAudioPlayback = false,
                AutoGainControl = false,
                EchoCancellation = false,
                NoiseSuppression = false
            };
            options.SystemAudio = IncludeExclude.Include;
        });
    }

    /// <summary>
    /// Configures the options to exclude system audio.
    /// </summary>
    /// <returns>The updated <see cref="DisplayMediaOptions"/>.</returns>
    public DisplayMediaOptions WithoutSystemAudio()
    {
        return this.SetProperties(options =>
        {
            options.Audio = null;
            options.SystemAudio = IncludeExclude.Exclude;
        });
    }

    /// <summary>
    /// Provides default display media options.
    /// </summary>
    public static DisplayMediaOptions Default => new()
    {
        Video = new VideoConstraints
        {
            DisplaySurface = CaptureDisplaySurface.Browser
        },
        Audio = new AudioConstraints
        {
            SuppressLocalAudioPlayback = false,
            AutoGainControl = false,
            EchoCancellation = false,
            NoiseSuppression = false
        },
        PreferCurrentTab = false,
        SelfBrowserSurface = IncludeExclude.Include,
        SurfaceSwitching = IncludeExclude.Include,
        MonitorTypeSurfaces = IncludeExclude.Include
    };

    /// <summary>
    /// Provides display media options without audio.
    /// </summary>
    public static DisplayMediaOptions WithoutAudio => Default.WithoutSystemAudio();

    /// <summary>
    /// Provides display media options configured for best quality.
    /// </summary>
    public static DisplayMediaOptions BestQuality => new()
    {
        Video = new VideoConstraints
        {
            DisplaySurface = CaptureDisplaySurface.Browser,
            Cursor = CaptureCursor.Always,
            LogicalSurface = true,
            Width = 3840,
            Height = 2160,
            FrameRate = 60.0
        },
        Audio = new AudioConstraints
        {
            SuppressLocalAudioPlayback = false,
            AutoGainControl = false,
            EchoCancellation = false,
            NoiseSuppression = false
        },
        SystemAudio = IncludeExclude.Include
    };

    /// <summary>
    /// Provides display media options optimized for low bandwidth usage.
    /// </summary>
    public static DisplayMediaOptions LowBandwidth => new()
    {
        Video = new VideoConstraints
        {
            DisplaySurface = CaptureDisplaySurface.Monitor,
            Cursor = CaptureCursor.Always,
            LogicalSurface = true,
            Width = 640,
            Height = 480,
            FrameRate = 15.0
        }
    };

    /// <summary>
    /// Provides display media options to capture the current tab with audio.
    /// </summary>
    public static DisplayMediaOptions CurrentTabWithAudio => new()
    {
        Video = new VideoConstraints
        {
            DisplaySurface = CaptureDisplaySurface.Browser,
            Cursor = CaptureCursor.Always,
            LogicalSurface = true
        },
        Audio = new AudioConstraints
        {
            EchoCancellation = true,
            AutoGainControl = true,
            NoiseSuppression = true
        },
        PreferCurrentTab = true,
        SystemAudio = IncludeExclude.Include
    };
}
/// <summary>
/// Represents constraints for capturing audio, including settings like device ID, echo cancellation, and sample rate.
/// </summary>
public class AudioConstraints
{
    /// <summary>
    /// Gets or sets the device ID for the audio source.
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the group ID for the audio source.
    /// </summary>
    public string GroupId { get; set; }

    /// <summary>
    /// Indicates whether local audio playback should be suppressed.
    /// </summary>
    public bool? SuppressLocalAudioPlayback { get; set; }

    /// <summary>
    /// Indicates whether echo cancellation is enabled.
    /// </summary>
    public bool? EchoCancellation { get; set; }

    /// <summary>
    /// Indicates whether auto gain control is enabled.
    /// </summary>
    public bool? AutoGainControl { get; set; }

    /// <summary>
    /// Indicates whether noise suppression is enabled.
    /// </summary>
    public bool? NoiseSuppression { get; set; }

    /// <summary>
    /// Gets or sets the desired sample rate for audio capture.
    /// </summary>
    public double? SampleRate { get; set; }

    /// <summary>
    /// Gets or sets the desired sample size for audio capture.
    /// </summary>
    public double? SampleSize { get; set; }

    /// <summary>
    /// Gets or sets the number of audio channels.
    /// </summary>
    public double? ChannelCount { get; set; }

    /// <summary>
    /// Gets or sets the desired latency for audio capture.
    /// </summary>
    public double? Latency { get; set; }

    /// <summary>
    /// Gets or sets the voice isolation mode.
    /// </summary>
    public CaptureVoiceIsolationMode? VoiceIsolation { get; set; }
}

/// <summary>
/// Represents constraints for capturing video, including settings like resolution, frame rate, and device-specific properties.
/// </summary>
public class VideoConstraints
{
    /// <summary>
    /// Gets or sets the device ID for the video source.
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the group ID for the video source.
    /// </summary>
    public string GroupId { get; set; }

    /// <summary>
    /// Gets or sets the display surface type (e.g., monitor, window).
    /// </summary>
    public CaptureDisplaySurface? DisplaySurface { get; set; }

    /// <summary>
    /// Indicates whether to capture the logical surface of the display.
    /// </summary>
    public bool? LogicalSurface { get; set; }

    /// <summary>
    /// Specifies how the cursor should be captured (e.g., always, motion only).
    /// </summary>
    public CaptureCursor? Cursor { get; set; }

    /// <summary>
    /// Gets or sets the desired width of the video.
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    /// Gets or sets the desired height of the video.
    /// </summary>
    public double? Height { get; set; }

    /// <summary>
    /// Gets or sets the desired aspect ratio of the video.
    /// </summary>
    public double? AspectRatio { get; set; }

    /// <summary>
    /// Gets or sets the desired frame rate of the video.
    /// </summary>
    public double? FrameRate { get; set; }

    /// <summary>
    /// Specifies the facing mode (e.g., user-facing or environment-facing).
    /// </summary>
    public CaptureFacingMode? FacingMode { get; set; }

    /// <summary>
    /// Specifies the resize mode for the video (e.g., crop and scale).
    /// </summary>
    public CaptureResizeMode? ResizeMode { get; set; }

    /// <summary>
    /// Gets or sets the desired brightness level for the video.
    /// </summary>
    public double? Brightness { get; set; }

    /// <summary>
    /// Gets or sets the desired color temperature for the video.
    /// </summary>
    public double? ColorTemperature { get; set; }

    /// <summary>
    /// Gets or sets the desired contrast level for the video.
    /// </summary>
    public double? Contrast { get; set; }

    /// <summary>
    /// Gets or sets the desired saturation level for the video.
    /// </summary>
    public double? Saturation { get; set; }

    /// <summary>
    /// Gets or sets the desired sharpness level for the video.
    /// </summary>
    public double? Sharpness { get; set; }

    /// <summary>
    /// Gets or sets the exposure compensation for the video.
    /// </summary>
    public double? ExposureCompensation { get; set; }

    /// <summary>
    /// Specifies the exposure mode (e.g., manual, single, continuous).
    /// </summary>
    public CaptureExposureMode? ExposureMode { get; set; }

    /// <summary>
    /// Gets or sets the desired exposure time for the video.
    /// </summary>
    public double? ExposureTime { get; set; }

    /// <summary>
    /// Gets or sets the ISO sensitivity for the video.
    /// </summary>
    public double? ISO { get; set; }

    /// <summary>
    /// Specifies the focus mode (e.g., manual, continuous).
    /// </summary>
    public CaptureFocusMode? FocusMode { get; set; }

    /// <summary>
    /// Gets or sets the focus distance for the video.
    /// </summary>
    public double? FocusDistance { get; set; }

    /// <summary>
    /// Gets or sets points of interest for the camera to focus on.
    /// </summary>
    public CapturePoint[] PointsOfInterest { get; set; }

    /// <summary>
    /// Gets or sets the pan level for the camera.
    /// </summary>
    public double? Pan { get; set; }

    /// <summary>
    /// Gets or sets the tilt level for the camera.
    /// </summary>
    public double? Tilt { get; set; }

    /// <summary>
    /// Gets or sets the zoom level for the camera.
    /// </summary>
    public double? Zoom { get; set; }

    /// <summary>
    /// Indicates whether the camera's torch (flashlight) is enabled.
    /// </summary>
    public bool? Torch { get; set; }

    /// <summary>
    /// Specifies the white balance mode (e.g., manual, continuous).
    /// </summary>
    public CaptureWhiteBalanceMode? WhiteBalanceMode { get; set; }
}

/// <summary>
/// Represents a point in 2D space, used for specifying areas of interest in video capture.
/// </summary>
public class CapturePoint
{
    /// <summary>
    /// The X-coordinate of the point.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// The Y-coordinate of the point.
    /// </summary>
    public double Y { get; set; }
}


/// <summary>
/// Defines options to include or exclude a feature.
/// </summary>
[JsonConverter(typeof(LowercaseEnumConverter))]
public enum IncludeExclude
{
    /// <summary>
    /// Include the feature.
    /// </summary>
    Include,

    /// <summary>
    /// Exclude the feature.
    /// </summary>
    Exclude
}


/// <summary>
/// Defines types of display surfaces for video capture.
/// </summary>
[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureDisplaySurface
{
    /// <summary>
    /// Capture the entire monitor display.
    /// </summary>
    Monitor,

    /// <summary>
    /// Capture a specific window.
    /// </summary>
    Window,

    /// <summary>
    /// Capture an application view.
    /// </summary>
    Application,

    /// <summary>
    /// Capture the browser display.
    /// </summary>
    Browser
}

/// <summary>
/// Specifies how the cursor should be captured during video recording.
/// </summary>
[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureCursor
{
    /// <summary>
    /// Always capture the cursor.
    /// </summary>
    Always,

    /// <summary>
    /// Capture the cursor only when it moves.
    /// </summary>
    Motion,

    /// <summary>
    /// Never capture the cursor.
    /// </summary>
    Never
}

/// <summary>
/// Defines the exposure modes for a camera.
/// </summary>
[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureExposureMode
{
    /// <summary>
    /// No specific exposure mode.
    /// </summary>
    None,

    /// <summary>
    /// Manual exposure control.
    /// </summary>
    Manual,

    /// <summary>
    /// Single-shot exposure adjustment.
    /// </summary>
    Single,

    /// <summary>
    /// Continuous exposure adjustment.
    /// </summary>
    Continuous
}

/// <summary>
/// Specifies focus modes for camera devices.
/// </summary>
[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureFocusMode
{
    /// <summary>
    /// No specific focus mode.
    /// </summary>
    None,

    /// <summary>
    /// Manual focus control.
    /// </summary>
    Manual,

    /// <summary>
    /// Single focus adjustment.
    /// </summary>
    Single,

    /// <summary>
    /// Continuous focus adjustment.
    /// </summary>
    Continuous
}

/// <summary>
/// Specifies the facing mode for the camera (e.g., front-facing or rear-facing).
/// </summary>
[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureFacingMode
{
    /// <summary>
    /// The camera is user-facing.
    /// </summary>
    User,

    /// <summary>
    /// The camera is environment-facing (e.g., rear camera).
    /// </summary>
    Environment,

    /// <summary>
    /// The camera faces the left side.
    /// </summary>
    Left,

    /// <summary>
    /// The camera faces the right side.
    /// </summary>
    Right
}

/// <summary>
/// Defines resize modes for video capture.
/// </summary>
[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureResizeMode
{
    /// <summary>
    /// No specific resize mode.
    /// </summary>
    None,

    /// <summary>
    /// Crop the captured video and scale it to fit.
    /// </summary>
    CropAndScale
}

/// <summary>
/// Specifies white balance modes for camera devices.
/// </summary>
[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureWhiteBalanceMode
{
    /// <summary>
    /// No specific white balance mode.
    /// </summary>
    None,

    /// <summary>
    /// Manual white balance adjustment.
    /// </summary>
    Manual,

    /// <summary>
    /// Single-shot white balance adjustment.
    /// </summary>
    Single,

    /// <summary>
    /// Continuous white balance adjustment.
    /// </summary>
    Continuous
}

/// <summary>
/// Specifies voice isolation modes for audio capture.
/// </summary>
[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureVoiceIsolationMode
{
    /// <summary>
    /// No voice isolation.
    /// </summary>
    Off,

    /// <summary>
    /// Standard voice isolation.
    /// </summary>
    Standard,

    /// <summary>
    /// High level of voice isolation.
    /// </summary>
    High
}
