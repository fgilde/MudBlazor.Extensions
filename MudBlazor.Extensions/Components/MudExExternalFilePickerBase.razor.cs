using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core;
using Nextended.Core.Contracts;

namespace MudBlazor.Extensions.Components;

public abstract partial class MudExExternalFilePickerBase<T, TFile> : MudExJsRequiredBaseComponent<T>, IMudExExternalFilePicker
    where T : MudExJsRequiredBaseComponent<T>
    where TFile : IUploadableFile
{
    protected TaskCompletionSource<TFile[]> PickTaskCompletionSource;
    protected TaskCompletionSource<bool> InitializationCompletionSource = new();
    private string _clientId;
    private string _apiKey;
    private string[] _allowedMimeTypes;
    private bool _multiSelect;
    private bool _autoLoadFileDataBytes = true;

    public abstract string Image { get; }

    [Parameter] public EventCallback<TFile[]> FilesSelected { get; set; }
    
    [Parameter] public string ClientId { get => _clientId; set => Set(ref _clientId, value, _ => UpdateJsOptions()); }
    [Parameter] public string ApiKey { get => _apiKey; set => Set(ref _apiKey, value, _ => UpdateJsOptions()); }
    [Parameter] public string[] AllowedMimeTypes { get => _allowedMimeTypes; set => Set(ref _allowedMimeTypes, value, _ => UpdateJsOptions()); }
    [Parameter] public bool MultiSelect { get => _multiSelect; set => Set(ref _multiSelect, value, _ => UpdateJsOptions()); }
    [Parameter] public bool AutoLoadFileDataBytes { get => _autoLoadFileDataBytes; set => Set(ref _autoLoadFileDataBytes, value, _ => UpdateJsOptions()); }


    [Parameter] public Variant Variant { get; set; } = Variant.Text;
    [Parameter] public Color Color { get; set; } = Color.Primary;
    [Parameter] public Size Size { get; set; } = Size.Small;
    [Parameter] public string StartIcon { get; set; } = MudExIcons.Brands.DropBox;
    [Parameter] public RenderFragment ChildContent { get; set; }

    protected bool IsReady { get; set; }
    protected bool IsLoading { get; set; }

    /// <inheritdoc />
    public virtual async Task<IUploadableFile[]> PickAsync(CancellationToken cancellation = default)
    {
        await InitializationCompletionSource.Task;
        PickTaskCompletionSource = new TaskCompletionSource<TFile[]>();
        cancellation.Register(() => PickTaskCompletionSource.TrySetCanceled());
        await ShowPicker(cancellation);
        return (await PickTaskCompletionSource.Task).Cast<IUploadableFile>().ToArray();
    }

    /// <summary>
    /// Gets the JavaScript arguments to pass to the component.
    /// </summary>
    public override object[] GetJsArguments() => new[] { ElementReference, CreateDotNetObjectReference(), Options() };

    /// <summary>
    /// Callback method for JavaScript to call when the picker is ready
    /// </summary>
    [JSInvokable]
    public virtual void OnReady()
    {
        IsReady = true;
        CallStateHasChanged();
        InitializationCompletionSource.TrySetResult(IsReady);
    }

    /// <summary>
    /// Callback method for JavaScript to call when files are selected
    /// </summary>
    /// <param name="files"></param>
    /// <returns></returns>
    [JSInvokable]
    public virtual async Task OnFilesSelected(TFile[] files)
    {
        if (AutoLoadFileDataBytes)
            await Task.WhenAll(files.Select(f => f.EnsureDataLoadedAsync()));
        PickTaskCompletionSource.TrySetResult(files);
        await FilesSelected.InvokeAsync(files);
        IsLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Returns the options for the js class
    /// </summary>
    /// <returns></returns>
    protected virtual object JsOptions()
    {
        return null;
    }

    private object Options()
    {
        //AllowedMimeTypes.Select(mt => MimeType.GetExtension(mt))
        return new
        {
            OnReadyCallback = nameof(OnReady),
            OnFilesSelectedCallback = nameof(OnFilesSelected),
            ClientId,
            ApiKey,
            AllowedMimeTypes,
            MultiSelect,
            AutoLoadFileDataBytes,
            JsOptions = JsOptions()
        };
    }

    protected virtual void UpdateJsOptions()
    {
        if (JsReference != null)
            JsReference.InvokeVoidAsync("setOptions", Options());
    }

    /// <summary>
    /// Protected method to show the picker
    /// </summary>
    protected abstract Task ShowPicker(CancellationToken cancellation = default);

    private async Task Pick() => await PickAsync();

}
