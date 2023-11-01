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
    private bool _alwaysLoadPath = true;

    /// <summary>
    /// Google Drive App Id
    /// </summary>
    [Parameter] public string AppId { get => _appId; set => Set(ref _appId, value, _ => UpdateJsOptions()); }

    /// <summary>
    /// Option to allow upload
    /// </summary>
    [Parameter] public bool AllowUpload { get => _allowUpload; set => Set(ref _allowUpload, value, _ => UpdateJsOptions()); }

    /// <summary>
    /// Option to always load path
    /// </summary>
    [Parameter] public bool AlwaysLoadPath { get => _alwaysLoadPath; set => Set(ref _alwaysLoadPath, value, _ => UpdateJsOptions()); }

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
            AlwaysLoadPath,
            AllowedMimeTypes = AllowedMimeTypes?.Any() == true ? string.Join(",", AllowedMimeTypes) : null,
            AllowUpload,
            AllowFolderSelect,
            AllowFolderNavigation
        };
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        ClientId ??= MudExConfiguration.GoogleDriveClientId;
        base.OnInitialized();
    }
}