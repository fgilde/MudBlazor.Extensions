namespace MudBlazor.Extensions.Core;

public class CaptureResult
{
    public string CaptureId { get; set; }
    public CaptureOptions Options { get; set; }
    public byte[] Bytes => CombinedData?.Bytes;
    public string BlobUrl => CombinedData?.BlobUrl;

    public CaptureData CaptureData { get; set; }
    public CaptureData CameraData { get; set; }
    public CaptureData AudioData { get; set; }
    public CaptureData SystemAudioData { get; set; }
    public CaptureData CombinedData { get; set; }
}

public class CaptureData
{
    public string ContentType { get; set; }
    public byte[] Bytes { get; set; }
    public string BlobUrl { get; set; }
}