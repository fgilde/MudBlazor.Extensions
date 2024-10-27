using System.Globalization;
using System.Text.Json.Serialization;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// Options for capturing video and audio.
/// </summary>
public class CaptureOptions
{
    public string ContentType { get; set; } = "video/webm; codecs=vp9";
    public string AudioContentType { get; set; } = "audio/webm";
    public bool CaptureScreen { get; set; } = true; // Bildschirmaufnahme
    public string ScreenSource { get; set; } // Optional: Gibt die Bildschirmquelle an, wenn vorhanden
    public string VideoDeviceId { get; set; } // Kamera-ID
    public List<string> AudioDevices { get; set; } = new(); // Liste der Audio-Device-IDs

    /// <summary>
    /// The source for the overlay default is camera as overlay over screen.
    /// </summary>
    public OverlaySource OverlaySource { get; set; } = OverlaySource.CapturedScreen;

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

}

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
