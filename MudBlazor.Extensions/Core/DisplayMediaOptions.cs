using System.Text.Json.Serialization;
using MudBlazor.Extensions.Helper.JsonConverter;

namespace MudBlazor.Extensions.Core;

public class DisplayMediaOptions
{
    public VideoConstraints Video { get; set; }
    public AudioConstraints Audio { get; set; }
    public bool PreferCurrentTab { get; set; }
    public IncludeExclude? SelfBrowserSurface { get; set; }
    public IncludeExclude? SystemAudio { get; set; }
    public IncludeExclude? SurfaceSwitching { get; set; }
    public IncludeExclude? MonitorTypeSurfaces { get; set; }

    public static DisplayMediaOptions Default => new DisplayMediaOptions
    {
        Video = new VideoConstraints
        {
            DisplaySurface = CaptureDisplaySurface.Browser,
        },
        Audio = new AudioConstraints()
        {
            SuppressLocalAudioPlayback = false,
            AutoGainControl = false,
            EchoCancellation = false,
            NoiseSuppression = false
        },
        PreferCurrentTab = false,
        SelfBrowserSurface = IncludeExclude.Include,
        SystemAudio = IncludeExclude.Include,
        SurfaceSwitching = IncludeExclude.Include,
        MonitorTypeSurfaces = IncludeExclude.Include
    };

    public static DisplayMediaOptions WithoutAudio => new DisplayMediaOptions
    {
        Video = new VideoConstraints
        {
            DisplaySurface = CaptureDisplaySurface.Browser,
        },
        PreferCurrentTab = false,
        SelfBrowserSurface = IncludeExclude.Include,
        SurfaceSwitching = IncludeExclude.Include,
        MonitorTypeSurfaces = IncludeExclude.Include
    };

    public static DisplayMediaOptions BestQuality => new DisplayMediaOptions
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
        Audio = new AudioConstraints()
        {
            SuppressLocalAudioPlayback = false,
            AutoGainControl = false,
            EchoCancellation = false,
            NoiseSuppression = false
        },
        SystemAudio = IncludeExclude.Include
    };

    
    public static DisplayMediaOptions LowBandwidth => new DisplayMediaOptions
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


    public static DisplayMediaOptions CurrentTabWithAudio => new DisplayMediaOptions
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



public class AudioConstraints
{
    public string DeviceId { get; set; }
    public string GroupId { get; set; }

    public bool? SuppressLocalAudioPlayback { get; set; }
    public bool? EchoCancellation { get; set; }
    public bool? AutoGainControl { get; set; }
    public bool? NoiseSuppression { get; set; }
    public double? SampleRate { get; set; }
    public double? SampleSize { get; set; }
    public double? ChannelCount { get; set; }
    public double? Latency { get; set; }
    public CaptureVoiceIsolationMode? VoiceIsolation { get; set; }
}

public class VideoConstraints
{
    public string DeviceId { get; set; }
    public string GroupId { get; set; }

    // Display capture specific
    public CaptureDisplaySurface? DisplaySurface { get; set; }
    public bool? LogicalSurface { get; set; }
    public CaptureCursor? Cursor { get; set; }

    // Basic video properties
    public double? Width { get; set; }
    public double? Height { get; set; }
    public double? AspectRatio { get; set; }
    public double? FrameRate { get; set; }

    // Camera specific properties
    public CaptureFacingMode? FacingMode { get; set; }
    public CaptureResizeMode? ResizeMode { get; set; }

    // Camera adjustments
    public double? Brightness { get; set; }
    public double? ColorTemperature { get; set; }
    public double? Contrast { get; set; }
    public double? Saturation { get; set; }
    public double? Sharpness { get; set; }

    // Camera exposure
    public double? ExposureCompensation { get; set; }
    public CaptureExposureMode? ExposureMode { get; set; }
    public double? ExposureTime { get; set; }
    public double? ISO { get; set; }

    // Camera focus
    public CaptureFocusMode? FocusMode { get; set; }
    public double? FocusDistance { get; set; }
    public CapturePoint[] PointsOfInterest { get; set; }

    // Camera movement
    public double? Pan { get; set; }
    public double? Tilt { get; set; }
    public double? Zoom { get; set; }

    // Additional features
    public bool? Torch { get; set; }
    public CaptureWhiteBalanceMode? WhiteBalanceMode { get; set; }
}

public class CapturePoint
{
    public double X { get; set; }
    public double Y { get; set; }
}

[JsonConverter(typeof(LowercaseEnumConverter))]
public enum IncludeExclude
{
    Include,
    Exclude
}

[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureDisplaySurface
{
    Monitor,
    Window,
    Application,
    Browser
}

[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureCursor
{
    Always,
    Motion,
    Never
}

[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureExposureMode
{
    None,
    Manual,
    Single,
    Continuous
}

[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureFocusMode
{
    None,
    Manual,
    Single,
    Continuous
}

[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureFacingMode
{
    User,
    Environment,
    Left,
    Right
}

[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureResizeMode
{
    None,
    CropAndScale
}

[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureWhiteBalanceMode
{
    None,
    Manual,
    Single,
    Continuous
}

[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureVoiceIsolationMode
{
    Off,
    Standard,
    High
}
