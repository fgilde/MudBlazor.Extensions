using System.Text.Json.Serialization;
using MudBlazor.Extensions.Helper.JsonConverter;

namespace MudBlazor.Extensions.Core.W3C;

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