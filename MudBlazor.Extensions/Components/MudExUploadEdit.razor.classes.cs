using Nextended.Core;
using Nextended.Core.Contracts;

namespace MudBlazor.Extensions.Components;


/// <summary>
/// Represents a file that can be uploaded. Implements the IUploadableFile interface.
/// </summary>
public class UploadableFile : IUploadableFile
{
    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets the file extension.
    /// </summary>
    public string Extension { get; set; }

    /// <summary>
    /// Gets or sets the content type of the file.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the data of the file.
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// Gets or sets the URL of the file.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Creates an instance of UploadableFile from a given URL.
    /// </summary>
    public static async Task<UploadableFile> FromUrlAsync(string url, CancellationToken cancellationToken = default) => new()
    {
        Extension = Path.GetExtension(url),
        ContentType = await MimeType.ReadMimeTypeFromUrlAsync(url, cancellationToken),
        FileName = Path.GetFileName(url),
        Url = url
    };
}

/// <summary>
/// Specifies the mode of selecting items.
/// </summary>
public enum SelectItemsMode
{
    /// <summary>
    /// No selection allowed.
    /// </summary>
    None,

    /// <summary>
    /// Allows single item selection.
    /// </summary>
    Single,

    /// <summary>
    /// Allows multi item selection.
    /// </summary>
    MultiSelect,

    /// <summary>
    /// Allows multi item selection with Ctrl key.
    /// </summary>
    MultiSelectWithCtrlKey
}

/// <summary>
/// Specifies the type of MIME type restriction.
/// </summary>
public enum MimeTypeRestrictionType
{
    /// <summary>
    /// Only MIME types in the list are allowed.
    /// </summary>
    WhiteList,

    /// <summary>
    /// MIME types in the list are not allowed.
    /// </summary>
    BlackList
}