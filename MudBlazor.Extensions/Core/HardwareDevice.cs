using System.ComponentModel;
using System.Text.Json.Serialization;

namespace MudBlazor.Extensions.Core;

public class HardwareDeviceInfo
{
    public string DeviceId { get; set; }
    public string GroupId { get; set; }
    public string Label { get; set; }
    public DeviceKind Kind { get; set; }
    public override string ToString() => Label;
}

public class AudioDevice : HardwareDeviceInfo
{}

public class VideoDevice : HardwareDeviceInfo
{}

public class MediaStreamTrack : HardwareDeviceInfo
{
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