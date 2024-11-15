namespace MudBlazor.Extensions.Core.Capture;

/// <summary>
/// The result of a capture.
/// </summary>
public class CaptureResult
{
    /// <summary>
    /// Id of the capture.
    /// </summary>
    public string CaptureId { get; set; }

    /// <summary>
    /// The options used for the capture.
    /// </summary>
    public CaptureOptions Options { get; set; }

    /// <summary>
    /// The captured data of the combined result.
    /// </summary>
    public byte[] Bytes => CombinedData?.Bytes;

    /// <summary>
    /// Blob URL of the captured data for combined.
    /// </summary>
    public string BlobUrl => CombinedData?.BlobUrl;

    /// <summary>
    /// Captured data of the screen.
    /// </summary>
    public CaptureData CaptureData { get; set; }

    /// <summary>
    /// The captured data of the camera.
    /// </summary>
    public CaptureData CameraData { get; set; }

    /// <summary>
    /// recorded audio data.
    /// </summary>
    public CaptureData AudioData { get; set; }

    /// <summary>
    /// recorded system audio data.
    /// </summary>
    public CaptureData SystemAudioData { get; set; }

    /// <summary>
    /// Combined data of all captures.
    /// </summary>
    public CaptureData CombinedData { get; set; }

    /// <summary>
    /// All data of the capture.
    /// </summary>
    public CaptureData[] AllData => new[] { CombinedData, CaptureData, CameraData, AudioData, SystemAudioData };
}

public class CaptureData
{
    public string ContentType { get; set; }
    public byte[] Bytes { get; set; }
    public string BlobUrl { get; set; }
}