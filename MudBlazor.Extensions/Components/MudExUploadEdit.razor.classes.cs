using Nextended.Core;
using Nextended.Core.Contracts;

namespace MudBlazor.Extensions.Components;


public class UploadableFile: IUploadableFile
{
    public string FileName { get; set; }
    public string Extension { get; set; }
    public string ContentType { get; set; }
    public byte[] Data { get; set; }
    public string Url { get; set; }

    public static async Task<UploadableFile> FromUrlAsync(string url, CancellationToken cancellationToken = default) => new()
    {
        Extension = Path.GetExtension(url),
        ContentType = await MimeType.ReadMimeTypeFromUrlAsync(url, cancellationToken),
        FileName = Path.GetFileName(url),
        Url = url
    };
}


public enum SelectItemsMode
{
    None,
    Single,
    MultiSelect,
    MultiSelectWithCtrlKey
}

public enum MimeTypeRestrictionType
{
    WhiteList,
    BlackList
}

