using System.Globalization;
using System.Net.Http.Headers;
using Nextended.Core;
using Nextended.Core.Contracts;

namespace MudBlazor.Extensions.Components;

public class OneDriveFileInfo: IUploadableFile
{
    private string _url;
    public string Id { get; set; }
    public string AccessToken { get; set; }
    public string ApiPath { get; set; }

    public async Task EnsureDataLoadedAsync(HttpClient client = null)
    {
        if (Data == null || Data?.Length == 0)
        {
            client ??= new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            Extension ??= System.IO.Path.GetExtension(Url);
            ContentType ??= await MimeType.ReadMimeTypeFromUrlAsync(Url, client);
            FileName ??= System.IO.Path.GetFileName(Url);
            Data = await client.GetByteArrayAsync(DownloadUrl);
        }
    }

    public long Size { get; set; }
    public string FileName { get; set; }
    public string DownloadUrl { get; set; }
    public string Extension { get; set; }
    public string ContentType { get; set; }
    public string WebViewLink { get; set; }
    public string WebContentLink { get; set; }
    public byte[] Data { get; set; }

    public string Url
    {
        get => _url ??= WebContentLink?.Replace("&export=download", string.Empty, true, CultureInfo.InvariantCulture);
        set => _url = value;
    }

    public string Path { get; set; }
}
