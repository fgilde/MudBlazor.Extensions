using System.Globalization;
using System.Text.Json.Serialization;

namespace MudBlazor.Extensions.Core;

public class CaptureOptions
{
    public string ContentType { get; set; } = "video/webm; codecs=vp9";
    public string AudioContentType { get; set; } = "audio/webm";
    public bool CaptureScreen { get; set; } = true; // Bildschirmaufnahme
    public string ScreenSource { get; set; } // Optional: Gibt die Bildschirmquelle an, wenn vorhanden
    public string VideoDeviceId { get; set; } // Kamera-ID
    public List<string> AudioDevices { get; set; } = new(); // Liste der Audio-Device-IDs

    public DialogPosition VideoDeviceOverlayPosition { get; set; } = DialogPosition.CenterLeft;
    public double VideoDeviceOverlaySize { get; set; } = 0.2; // Größe des Overlays als Faktor des Hauptvideos
}
