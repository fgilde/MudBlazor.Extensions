using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using Nextended.Core;
using Nextended.Core.Extensions;

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

    /// <summary>
    /// Callback method for JavaScript to call when the picker is ready
    /// </summary>
    [JSInvokable]
    public override async Task OnReady()
    {
        await Task.Delay(500);
        await base.OnReady();
    }

    /// <inheritdoc />
    protected override object JsOptions()
    {
        return new
        {
            AllowedExtensions = AllowedMimeTypes?.Any() == true ? AllowedMimeTypes.Select(s => MimeType.GetExtension(s).EnsureStartsWith(".")).ToArray() : null,
            MaxFileSize
        };
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        ApiKey ??= MudExConfiguration.DropBoxApiKey;
        base.OnInitialized();
    }
}