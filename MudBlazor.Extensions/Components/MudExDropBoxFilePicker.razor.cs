using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using Nextended.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// DropBox external file picker component
/// </summary>
public partial class MudExDropBoxFilePicker
{
    private long? _maxFileSize;

    /// <summary>
    /// Max file size in bytes
    /// </summary>
    [Parameter] public long? MaxFileSize { get => _maxFileSize; set => Set(ref _maxFileSize, value, _ => UpdateJsOptions()); }

    /// <inheritdoc />
    public override string Image => MudExIcons.Custom.Brands.ColorFull.DropBox;
    
    /// <inheritdoc />
    protected override string[] ExternalJsFiles => new[] { "https://www.dropbox.com/static/api/2/dropins.js" };

    /// <inheritdoc />
    protected override object JsOptions()
    {
        return new
        {
            AllowedExtensions = AllowedMimeTypes?.Any() == true ? AllowedMimeTypes.Select(MimeType.GetExtension).ToArray() : null,
            MaxFileSize
        };
    }
}