using System.Globalization;
using System.Text.Json.Serialization;

namespace MudBlazor.Extensions.Core;

public class CaptureOptions
{
    public string ContentType { get; set; } = "video/webm; codecs=vp9";
    public bool CaptureScreen { get; set; } = true; // Bildschirmaufnahme
    public string ScreenSource { get; set; } // Optional: Gibt die Bildschirmquelle an, wenn vorhanden
    public string VideoDeviceId { get; set; } // Kamera-ID
    public List<string> AudioDevices { get; set; } = new(); // Liste der Audio-Device-IDs
    public string VideoDevicePosition { get; set; } = "top-right"; // Position des Overlays
    public double VideoDeviceSize { get; set; } = 0.2; // Größe des Overlays als Faktor des Hauptvideos
}
