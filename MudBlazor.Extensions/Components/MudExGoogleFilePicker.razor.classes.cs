using System.Globalization;
using System.Net.Http.Headers;
using Nextended.Core;
using Nextended.Core.Contracts;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// File info for Google Drive
/// </summary>
public class GoogleFileInfo: IUploadableFile
{
    private string _url;

    /// <summary>
    /// File Id
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Access token
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// Ensure data is loaded
    /// </summary>
    public async Task EnsureDataLoadedAsync(HttpClient client = null)
    {
        if (Data == null || Data?.Length == 0)
        {
            client ??= new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            Extension ??= System.IO.Path.GetExtension(Url);
            ContentType ??= await MimeType.ReadMimeTypeFromUrlAsync(Url, client);
            FileName ??= System.IO.Path.GetFileName(Url);
            Data = await client.GetByteArrayAsync($"https://www.googleapis.com/drive/v3/files/{Id}?alt=media");
        }
    }

    /// <summary>
    /// File size
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// File extension
    /// </summary>
    public string Extension { get; set; }

    /// <summary>
    /// File content type
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Web view link
    /// </summary>
    public string WebViewLink { get; set; }

    /// <summary>
    /// Web content link
    /// </summary>
    public string WebContentLink { get; set; }

    /// <summary>
    /// File data
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// Url
    /// </summary>
    public string Url
    {
        get => _url ??= WebContentLink?.Replace("&export=download", string.Empty, true, CultureInfo.InvariantCulture);
        set => _url = value;
    }

    /// <summary>
    /// Folder path
    /// </summary>
    public string Path { get; set; }
}
