using Microsoft.AspNetCore.Components.Forms;
using Nextended.Core;
using Nextended.Core.Contracts;
using System.Net.Mime;

namespace MudBlazor.Extensions.Components;


/// <summary>
/// Represents a file that can be uploaded. Implements the IUploadableFile interface.
/// </summary>
public class UploadableFile : IUploadableFile
{
    private string _contentType;
    private string _extension;
    
    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets the file extension.
    /// </summary>
    public string Extension
    {
        get => _extension ??= !string.IsNullOrEmpty(FileName) ? System.IO.Path.GetExtension(FileName) : _extension;
        set => _extension = value;
    }


    /// <summary>
    /// Gets or sets the content type of the file.
    /// </summary>
    public string ContentType
    {
        get => _contentType ??= !string.IsNullOrEmpty(FileName) ? MimeType.GetMimeType(FileName) : _contentType;
        set => _contentType = value;
    }

    /// <summary>
    /// Gets or sets the data of the file.
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// Gets or sets the URL of the file.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Path if its came from folder or extracted from zip
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Task for loading content
    /// </summary>
    public Func<Task> LoadTask { get; set; }

    /// <summary>
    /// File size
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Ensure data array is filled
    /// </summary>
    public virtual async Task EnsureDataLoadedAsync(HttpClient client = null)
    {
        if (LoadTask == null && (Data == null || Data?.Length == 0) && !string.IsNullOrEmpty(Url))
        {
            client ??= new HttpClient();
            Extension ??= System.IO.Path.GetExtension(Url);
            ContentType ??= await MimeType.ReadMimeTypeFromUrlAsync(Url, client);
            FileName ??= System.IO.Path.GetFileName(Url);
            Data = await client.GetByteArrayAsync(Url);
        }else if (LoadTask != null)
        {
            await LoadTask();
        }
    }

    /// <summary>
    /// Creates an instance of UploadableFile from a given URL.
    /// </summary>
    public static async Task<UploadableFile> FromUrlAsync(string url, CancellationToken cancellationToken = default) => new()
    {
        Extension = System.IO.Path.GetExtension(url),
        ContentType = await MimeType.ReadMimeTypeFromUrlAsync(url, cancellationToken),
        FileName = System.IO.Path.GetFileName(url),
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
public enum RestrictionType
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

public class BrowserFileWithPath
{
    public string RelativePath { get; set; }
    public string Name { get; set; }
    public long Size { get; set; }
    public string ContentType { get; set; }
}

/// <summary>
/// Action for drop zone click
/// </summary>
public enum DropZoneClickAction
{
    None,
    UploadFile,
    UploadFolder,
    AddUrl,
    PickFromGoogleDrive,
    PickFromOneDrive,
    PickFromDropBox,
}

/// <summary>
/// Represents the mode of rendering of the external provider.
/// </summary>
public enum ExternalProviderRendering
{
    ActionButtons,
    ActionButtonsNewLine,
    Images,
    ImagesNewLine,
    IntegratedInDialogAsImages,
    IntegratedInDialogAsButtons,
}