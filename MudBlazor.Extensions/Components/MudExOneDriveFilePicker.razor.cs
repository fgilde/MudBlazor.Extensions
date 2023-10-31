using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// OneDrive file picker component
/// </summary>
public partial class MudExOneDriveFilePicker 
{
    private bool _allowUpload = true;
    private bool _allowFolderSelect;
    private bool _allowFolderNavigation;

    [Parameter] public bool AllowUpload { get => _allowUpload; set => Set(ref _allowUpload, value, _ => UpdateJsOptions()); }
    [Parameter] public bool AllowFolderSelect { get => _allowFolderSelect; set => Set(ref _allowFolderSelect, value, _ => UpdateJsOptions()); }
    [Parameter] public bool AllowFolderNavigation { get => _allowFolderNavigation; set => Set(ref _allowFolderNavigation, value, _ => UpdateJsOptions()); }

    public override string Image => "https://developers.google.com/drive/images/drive_icon.png";
    public string AccessToken { get; private set; }

  
    [JSInvokable]
    public void OnAuthorized(string accessToken)
    {
        AccessToken = accessToken;
        IsLoading = true;
        CallStateHasChanged();
    }

    protected override object JsOptions()
    {
        return new
        {
            AllowedMimeTypes = AllowedMimeTypes?.Any() == true ? string.Join(",", AllowedMimeTypes) : null,
            AllowUpload,
            AllowFolderSelect,
            AllowFolderNavigation
        };
    }


    protected override async Task ShowPicker(CancellationToken cancellation = default) => await JsReference.InvokeVoidAsync("openPicker", cancellation);

    private async Task Pick() => await PickAsync();
}