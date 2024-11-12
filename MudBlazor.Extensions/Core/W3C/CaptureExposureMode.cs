using System.Text.Json.Serialization;
using MudBlazor.Extensions.Helper.JsonConverter;

namespace MudBlazor.Extensions.Core.W3C;

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