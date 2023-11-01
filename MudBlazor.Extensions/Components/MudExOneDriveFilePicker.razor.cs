using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// OneDrive file picker component
/// </summary>
public partial class MudExOneDriveFilePicker 
{
    private bool _allowFolderSelect;

    /// <summary>
    /// Option to allow folder select
    /// </summary>
    [Parameter] public bool AllowFolderSelect { get => _allowFolderSelect; set => Set(ref _allowFolderSelect, value, _ => UpdateJsOptions()); }

    /// <inheritdoc />
    public override string Image => MudExIcons.Custom.Brands.ColorFull.OneDrive;

    /// <inheritdoc />
    protected override string[] ExternalJsFiles => new[] { "https://js.live.net/v7.2/OneDrive.js" };

    /// <inheritdoc />
    protected override object JsOptions()
    {
        return new
        {
            AllowedMimeTypes = AllowedMimeTypes?.Any() == true ? string.Join(",", AllowedMimeTypes) : null,
            AllowFolderSelect
        };
    }

    private async Task Pick() => await PickAsync();
}