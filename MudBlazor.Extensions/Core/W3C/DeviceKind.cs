using System.ComponentModel;
using System.Text.Json.Serialization;

namespace MudBlazor.Extensions.Core.W3C;

/// <summary>
/// HardwareDeviceKind
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeviceKind
{
    /// <summary>
    /// Video input device.
    /// </summary>
    [Description("videoinput")]
    VideoInput,

    /// <summary>
    /// Audio input device.
    /// </summary>
    [Description("audioinput")]
    AudioInput,

    /// <summary>
    /// Audio output device.
    /// </summary>
    [Description("audiooutput")]
    AudioOutput,

    /// <summary>
    /// Video device.
    /// </summary>
    [Description("video")]
    Video
}