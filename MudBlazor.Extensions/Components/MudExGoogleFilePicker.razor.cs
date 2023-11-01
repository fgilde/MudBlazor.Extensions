using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Google Drive file picker component
/// </summary>
public partial class MudExGoogleFilePicker : IMudExExternalFilePicker
{
    private string _appId;
    private bool _allowUpload = true;
    private bool _allowFolderSelect;
    private bool _allowFolderNavigation;

    /// <summary>
    /// Google Drive App Id
    /// </summary>
    [Parameter] public string AppId { get => _appId; set => Set(ref _appId, value, _ => UpdateJsOptions()); }

    /// <summary>
    /// Option to allow upload
    /// </summary>
    [Parameter] public bool AllowUpload { get => _allowUpload; set => Set(ref _allowUpload, value, _ => UpdateJsOptions()); }

    /// <summary>
    /// Option to allow folder select
    /// </summary>
    [Parameter] public bool AllowFolderSelect { get => _allowFolderSelect; set => Set(ref _allowFolderSelect, value, _ => UpdateJsOptions()); }

    /// <summary>
    /// Option to allow folder navigation
    /// </summary>
    [Parameter] public bool AllowFolderNavigation { get => _allowFolderNavigation; set => Set(ref _allowFolderNavigation, value, _ => UpdateJsOptions()); }

    /// <inheritdoc />
    public override string Image => MudExIcons.Custom.Brands.ColorFull.GoogleDrive;

    /// <inheritdoc />
    protected override object JsOptions()
    {
        return new
        {
            AppId,
            AllowedMimeTypes = AllowedMimeTypes?.Any() == true ? string.Join(",", AllowedMimeTypes) : null,
            AllowUpload,
            AllowFolderSelect,
            AllowFolderNavigation
        };
    }
}