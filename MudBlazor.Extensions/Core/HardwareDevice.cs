using System.ComponentModel;
using System.Text.Json.Serialization;

namespace MudBlazor.Extensions.Core;

public class HardwareDeviceInfo: IEquatable<HardwareDeviceInfo>
{
    #region Equality

    public bool Equals(HardwareDeviceInfo other) => DeviceId == other.DeviceId;

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == this.GetType() && Equals((HardwareDeviceInfo)obj);
    }

    public override int GetHashCode() => (DeviceId != null ? DeviceId.GetHashCode() : 0);

    #endregion

    public string DeviceId { get; set; }
    public string GroupId { get; set; }
    public string Label { get; set; }
    public DeviceKind Kind { get; set; }
    public override string ToString() => Label;
}

public class AudioDevice : HardwareDeviceInfo
{
    public static AudioDevice Default = new() { Label = "Default", DeviceId = "default" };
}

public class VideoDevice : HardwareDeviceInfo
{
    public static VideoDevice Default = new() { Label = "Default", DeviceId = "default" };
}

public class MediaStreamTrack : HardwareDeviceInfo
{
    public string AccessId { get; set; }
    public bool Enabled { get; set; }
    public string Id { get; set; }
    public bool Muted { get; set; }
    public string ReadyState { get; set; }
    public MediaStreamTrackVideoStats Stats { get; set; }   
}

public class MediaStreamTrackVideoStats
{
    public int DeliveredFrames { get; set; }
    public int DiscardedFrames { get; set; }
    public int TotalFrames { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeviceKind
{
    [Description("videoinput")]
    VideoInput,
    [Description("audioinput")]
    AudioInput,
    [Description("audiooutput")]
    AudioOutput,
    [Description("video")]
    Video
}