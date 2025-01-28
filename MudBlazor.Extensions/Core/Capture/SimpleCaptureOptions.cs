using MudBlazor.Extensions.Core.W3C;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Core.Capture;

/// <summary>
/// This class represents the simple capture options that can then converted to real <see cref="CaptureOptions"/>.
/// </summary>
public class SimpleCaptureOptions
{
    /// <summary>
    /// Set to true to record the screen.
    /// </summary>
    public bool RecordScreen { get; set; }

    /// <summary>
    /// Set to true to record audio.
    /// </summary>
    public bool RecordAudio { get; set; }

    /// <summary>
    /// Set to true to record the camera.
    /// </summary>
    public bool RecordCamera { get; set; }

    /// <summary>
    /// Is true when anything can be recorded.
    /// </summary>
    /// <returns></returns>
    public bool Valid() => RecordScreen || RecordAudio || RecordCamera;

    /// <summary>
    /// Converts the simple options to real <see cref="CaptureOptions"/>.
    /// </summary>
    public CaptureOptions ToCaptureOptions()
    {
        var options = new CaptureOptions
        {
            ScreenCapture = RecordScreen,
            VideoDevice = RecordCamera ? VideoDevice.Default.ToConstraints() : null,
            AudioDevices = RecordAudio ? new List<AudioConstraints> { AudioDevice.Default.ToConstraints() } : null
        };

        return options;
    }

    /// <summary>
    /// Creates a new instance of <see cref="SimpleCaptureOptions"/> from the specified <see cref="CaptureOptions"/>.
    /// </summary>
    public static SimpleCaptureOptions From(CaptureOptions? options)
    {
        var result = new SimpleCaptureOptions
        {
            RecordScreen = options != null && (options.CaptureScreen || options.ScreenSource != null || options.CaptureMediaOptions != null),
            RecordCamera = options is { VideoDevice.DeviceId: not null },
            RecordAudio = options != null && options.AudioDevices.EmptyIfNull().Any(d => !string.IsNullOrEmpty(d.DeviceId))
        };
        return result;
    }
}