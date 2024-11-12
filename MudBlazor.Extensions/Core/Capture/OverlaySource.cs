using System.Text.Json.Serialization;

namespace MudBlazor.Extensions.Core.Capture;


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