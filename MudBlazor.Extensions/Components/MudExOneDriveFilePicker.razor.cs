using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using Nextended.Core.Contracts;
using System;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// OneDrive file picker component
/// </summary>
public partial class MudExOneDriveFilePicker : IMudExExternalFilePicker
{
    private TaskCompletionSource<IUploadableFile[]> _pickTaskCompletionSource;
    private TaskCompletionSource<bool> _initializationCompletionSource = new();
    private string _clientId;
    private string _apiKey;
    private string _appId;
    private string[] _allowedMimeTypes;
    private bool _multiSelect;
    private bool _allowUpload = true;
    private bool _allowFolderSelect;
    private bool _allowFolderNavigation;
    private bool _autoLoadFileDataBytes = true;

    [Parameter] public EventCallback<OneDriveFileInfo[]> FilesSelected { get; set; }
    [Parameter] public string ClientId { get => _clientId; set => Set(ref _clientId, value, _ => UpdateJsOptions()); }
    [Parameter] public string ApiKey { get => _apiKey; set => Set(ref _apiKey, value, _ => UpdateJsOptions()); }
    [Parameter] public string AppId { get => _appId; set => Set(ref _appId, value, _ => UpdateJsOptions()); }
    [Parameter] public string[] AllowedMimeTypes { get => _allowedMimeTypes; set => Set(ref _allowedMimeTypes, value, _ => UpdateJsOptions()); }
    [Parameter] public bool MultiSelect { get => _multiSelect; set => Set(ref _multiSelect, value, _ => UpdateJsOptions()); }
    [Parameter] public bool AllowUpload { get => _allowUpload; set => Set(ref _allowUpload, value, _ => UpdateJsOptions()); }
    [Parameter] public bool AllowFolderSelect { get => _allowFolderSelect; set => Set(ref _allowFolderSelect, value, _ => UpdateJsOptions()); }
    [Parameter] public bool AllowFolderNavigation { get => _allowFolderNavigation; set => Set(ref _allowFolderNavigation, value, _ => UpdateJsOptions()); }
    [Parameter] public bool AutoLoadFileDataBytes { get => _autoLoadFileDataBytes; set => Set(ref _autoLoadFileDataBytes, value, _ => UpdateJsOptions()); }

    [Parameter] public Variant Variant { get; set; } = Variant.Text;
    [Parameter] public Color Color { get; set; } = Color.Primary;
    [Parameter] public Size Size { get; set; } = Size.Small;
    [Parameter] public string StartIcon { get; set; } = Icons.Custom.Brands.Microsoft;
    [Parameter] public RenderFragment ChildContent { get; set; }

    internal bool IsReady { get; private set; }
    internal bool IsLoading { get; private set; }
    public string Image => "https://developers.google.com/drive/images/drive_icon.png";
    public string AccessToken { get; private set; }


    public async Task<IUploadableFile[]> PickAsync(CancellationToken cancellation = default)
    {
        await _initializationCompletionSource.Task;
        _pickTaskCompletionSource = new TaskCompletionSource<IUploadableFile[]>();
        cancellation.Register(() => _pickTaskCompletionSource.TrySetCanceled());
        await ShowPicker(cancellation);
        return await _pickTaskCompletionSource.Task;
    }

    /// <summary>
    /// Gets the JavaScript arguments to pass to the component.
    /// </summary>
    public override object[] GetJsArguments() => new[] { ElementReference, CreateDotNetObjectReference(), Options() };
    
    [JSInvokable]
    public void OnReady()
    {
        IsReady = true;
        CallStateHasChanged();
        _initializationCompletionSource.TrySetResult(IsReady);
    }

    [JSInvokable]
    public void OnAuthorized(string accessToken)
    {
        AccessToken = accessToken;
        IsLoading = true;
        CallStateHasChanged();
    }

    // This method will be called from JavaScript
    [JSInvokable]
    public Task OnFilesSelected(OneDriveFileInfo[] files)
    {
        _pickTaskCompletionSource.TrySetResult(files);
        FilesSelected.InvokeAsync(files);
        IsLoading = false;
        return InvokeAsync(StateHasChanged);
    }

    private object Options()
    {
        return new
        {
            ClientId,
            ApiKey,
            AppId,
            AllowedMimeTypes = AllowedMimeTypes?.Any() == true ? string.Join(",", AllowedMimeTypes) : null,
            MultiSelect,
            AllowUpload,
            AllowFolderSelect,
            AllowFolderNavigation,
            AutoLoadFileDataBytes
        };
    }

    private void UpdateJsOptions()
    {
        if (JsReference != null)
            JsReference.InvokeVoidAsync("setOptions", Options());
    }

    private async Task ShowPicker(CancellationToken cancellation = default) => await JsReference.InvokeVoidAsync("openPicker", cancellation);

    private async Task Pick() => await PickAsync();
}