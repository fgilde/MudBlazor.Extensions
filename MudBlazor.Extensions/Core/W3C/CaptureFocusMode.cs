using System.Text.Json.Serialization;
using MudBlazor.Extensions.Helper.JsonConverter;

namespace MudBlazor.Extensions.Core.W3C;

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