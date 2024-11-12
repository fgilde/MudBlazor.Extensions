namespace MudBlazor.Extensions.Core.W3C;

/// <summary>
/// MediaStreamTrackVideoStats
/// </summary>
public class MediaStreamTrackVideoStats
{
    /// <summary>
    /// Delivered frames
    /// </summary>
    public int DeliveredFrames { get; set; }

    /// <summary>
    /// Discarded frames
    /// </summary>
    public int DiscardedFrames { get; set; }

    /// <summary>
    /// Total frames
    /// </summary>
    public int TotalFrames { get; set; }
}