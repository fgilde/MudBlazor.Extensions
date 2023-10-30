using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using Nextended.Core;
using Nextended.Core.Contracts;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Google Drive file picker component
/// </summary>
public partial class MudExDropBoxFilePicker : IMudExExternalFilePicker
{
    private TaskCompletionSource<IUploadableFile[]> _pickTaskCompletionSource;
    private TaskCompletionSource<bool> _initializationCompletionSource = new();
    private string _apiKey;
    private string[] _allowedMimeTypes;
    private bool _multiSelect;
    private long? _maxFileSize;

    [Parameter] public EventCallback<DropBoxFileInfo[]> FilesSelected { get; set; }
    [Parameter] public string ApiKey { get => _apiKey; set => Set(ref _apiKey, value, _ => UpdateJsOptions()); }
    [Parameter] public string[] AllowedMimeTypes { get => _allowedMimeTypes; set => Set(ref _allowedMimeTypes, value, _ => UpdateJsOptions()); }
    [Parameter] public bool MultiSelect { get => _multiSelect; set => Set(ref _multiSelect, value, _ => UpdateJsOptions()); }
    [Parameter] public long? MaxFileSize { get => _maxFileSize; set => Set(ref _maxFileSize, value, _ => UpdateJsOptions()); }
    [Parameter] public bool AutoLoadFileDataBytes { get; set; } = true;

    [Parameter] public Variant Variant { get; set; } = Variant.Text;
    [Parameter] public Color Color { get; set; } = Color.Primary;
    [Parameter] public Size Size { get; set; } = Size.Small;
    [Parameter] public string StartIcon { get; set; } = MudExIcons.Brands.DropBox;
    [Parameter] public RenderFragment ChildContent { get; set; }

    internal bool IsReady { get; private set; }
    internal bool IsLoading { get; private set; }
    public string Image => "https://developers.google.com/drive/images/drive_icon.png";
    

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

    // This method will be called from JavaScript
    [JSInvokable]
    public async Task OnFilesSelected(DropBoxFileInfo[] files)
    {
        if(AutoLoadFileDataBytes)
            await Task.WhenAll(files.Select(f => f.EnsureDataLoadedAsync()));
        _pickTaskCompletionSource.TrySetResult(files);
        FilesSelected.InvokeAsync(files);
        IsLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private object Options()
    {
        //AllowedMimeTypes.Select(mt => MimeType.GetExtension(mt))
        return new
        {
            ApiKey,
            AllowedExtensions = AllowedMimeTypes?.Any() == true ? AllowedMimeTypes.Select(MimeType.GetExtension).ToArray() : null,
            MultiSelect,
            MaxFileSize
        };
    }

    private void UpdateJsOptions()
    {
        if (JsReference != null)
            JsReference.InvokeVoidAsync("setOptions", Options());
    }

    private async Task ShowPicker(CancellationToken cancellation = default) => await JsReference.InvokeVoidAsync("showPicker", cancellation);

    private async Task Pick() => await PickAsync();
}