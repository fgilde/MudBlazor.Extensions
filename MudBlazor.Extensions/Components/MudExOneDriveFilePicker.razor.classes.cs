namespace MudBlazor.Extensions.Components;

/// <summary>
/// File info from one drive
/// </summary>
public class OneDriveFileInfo: UploadableFile
{
    /// <summary>
    /// File Id
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// AccessToken
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// ApiPath
    /// </summary>
    public string ApiPath { get; set; }

    /// <summary>
    /// DownloadUrl
    /// </summary>
    public string DownloadUrl { get; set; }
}
