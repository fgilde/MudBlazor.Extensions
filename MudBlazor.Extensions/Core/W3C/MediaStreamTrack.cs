namespace MudBlazor.Extensions.Core.W3C;

/// <summary>
/// MediaStreamTrack
/// </summary>
public class MediaStreamTrack : HardwareDeviceInfo
{
    /// <summary>
    /// AccessId
    /// </summary>
    public string AccessId { get; set; }

    /// <summary>
    /// Is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Track Id
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Is muted
    /// </summary>
    public bool Muted { get; set; }

    /// <summary>
    /// Ready state
    /// </summary>
    public string ReadyState { get; set; }

    /// <summary>
    /// Stats
    /// </summary>
    public MediaStreamTrackVideoStats Stats { get; set; }
}