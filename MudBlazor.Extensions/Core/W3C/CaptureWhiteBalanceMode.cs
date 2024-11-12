using System.Text.Json.Serialization;
using MudBlazor.Extensions.Helper.JsonConverter;

namespace MudBlazor.Extensions.Core.W3C;

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