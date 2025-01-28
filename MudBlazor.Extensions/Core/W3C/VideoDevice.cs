namespace MudBlazor.Extensions.Core.W3C;

public class VideoDevice : HardwareDeviceInfo
{
    public static VideoDevice Default = new() { Label = "Default", DeviceId = "default" };
    public VideoConstraints ToConstraints() => new() { DeviceId = DeviceId };
}