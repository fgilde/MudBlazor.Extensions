namespace MudBlazor.Extensions.Core.W3C;

/// <summary>
/// Represents an audio device.
/// </summary>
public class AudioDevice : HardwareDeviceInfo
{
    /// <summary>
    /// Default audio device.
    /// </summary>
    public static AudioDevice Default = new() { Label = "Default", DeviceId = "default" };

    public AudioConstraints ToConstraints() => new() { DeviceId = DeviceId };
}