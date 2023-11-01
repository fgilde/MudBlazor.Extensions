using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Contracts;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Base class for external file picker components
/// </summary>
public abstract partial class MudExExternalFilePickerBase<T, TFile> : MudExJsRequiredBaseComponent<T>, IMudExExternalFilePicker
    where T : MudExJsRequiredBaseComponent<T>
    where TFile : IUploadableFile
{
    /// <summary>
    /// Pick task awaitable completion source
    /// </summary>
    protected TaskCompletionSource<TFile[]> PickTaskCompletionSource;

    /// <summary>
    /// Initialization task awaitable completion source
    /// </summary>
    protected TaskCompletionSource<bool> InitializationCompletionSource = new();
    private string _clientId;
    private string _apiKey;
    private string[] _allowedMimeTypes;
    private bool _multiSelect;
    private bool _autoLoadFileDataBytes = true;

    /// <summary>
    /// The Image for the provider
    /// </summary>
    public abstract string Image { get; }

    /// <summary>
    /// Callback method for when files are selected
    /// </summary>
    [Parameter] public EventCallback<TFile[]> FilesSelected { get; set; }

    /// <summary>
    /// Client id for the picker
    /// </summary>
    [Parameter] public string ClientId { get => _clientId; set => Set(ref _clientId, value, _ => UpdateJsOptions()); }

    /// <summary>
    /// Api key for the picker
    /// </summary>
    [Parameter] public string ApiKey { get => _apiKey; set => Set(ref _apiKey, value, _ => UpdateJsOptions()); }

    /// <summary>
    /// Allowed mime types
    /// </summary>
    [Parameter] public string[] AllowedMimeTypes { get => _allowedMimeTypes; set => Set(ref _allowedMimeTypes, value, _ => UpdateJsOptions()); }

    /// <summary>
    /// If true, the user can select multiple files
    /// </summary>
    [Parameter] public bool MultiSelect { get => _multiSelect; set => Set(ref _multiSelect, value, _ => UpdateJsOptions()); }

    /// <summary>
    /// If true, the file data bytes are loaded automatically after selection
    /// </summary>
    [Parameter] public bool AutoLoadFileDataBytes { get => _autoLoadFileDataBytes; set => Set(ref _autoLoadFileDataBytes, value, _ => UpdateJsOptions()); }

    /// <summary>
    /// Variant of button
    /// </summary>
    [Parameter] public Variant Variant { get; set; } = Variant.Text;

    /// <summary>
    /// Color of button
    /// </summary>
    [Parameter] public Color Color { get; set; } = Color.Primary;

    /// <summary>
    /// Size of button
    /// </summary>
    [Parameter] public Size Size { get; set; } = Size.Small;

    /// <summary>
    /// Icon for Button
    /// </summary>
    [Parameter] public string StartIcon { get; set; }

    /// <summary>
    /// The child content of the component
    /// </summary>
    [Parameter] public RenderFragment<MudExExternalFilePickerBase<T, TFile>> ChildContent { get; set; }

    /// <summary>
    /// Set to true to remove colors from icons
    /// </summary>
    [Parameter] public bool IconsWithoutColors { get; set; }

    /// <summary>
    /// Initial render behavior controls rendering while initialization running
    /// </summary>
    [Parameter] public FilePickerInitialRenderBehaviour RenderBehaviourWhileInitialization { get; set; } = FilePickerInitialRenderBehaviour.Loading;

    /// <summary>
    /// Initial render behavior controls rendering while initialization running
    /// </summary>
    [Parameter] public PickerActionViewMode ActionViewMode { get; set; } = PickerActionViewMode.Button;

    /// <summary>
    /// The size that is used if ActionViewMode is set to Image
    /// </summary>
    [Parameter] public MudExDimension ImageSize { get; set; } = new(84);

    /// <summary>
    /// AccessToken for the picker service api
    /// </summary>
    public string AccessToken { get; private set; }

    /// <summary>
    /// External required js files
    /// </summary>
    protected virtual string[] ExternalJsFiles => null;

    /// <summary>
    /// If true, the picker is ready
    /// </summary>
    protected bool IsReady { get; set; }

    /// <summary>
    /// If true, the picker is loading
    /// </summary>
    protected bool IsLoading { get; set; }

    /// <summary>
    /// DefaultIcon if no StartIcon is set for the button
    /// </summary>
    protected virtual string DefaultIcon =>
        RenderBehaviourWhileInitialization == FilePickerInitialRenderBehaviour.SwitchIconToColored
            ? !IsReady ? MudExSvg.RemoveFillColors(Image) : Image
            : IconsWithoutColors ? MudExSvg.RemoveFillColors(Image) : Image;

    /// <inheritdoc />
    public virtual async Task<IUploadableFile[]> PickAsync(CancellationToken cancellation = default)
    {
        SetLoading(true);
        await InitializationCompletionSource.Task;
        await Task.Delay(100, cancellation);
        PickTaskCompletionSource = new TaskCompletionSource<TFile[]>();
        cancellation.Register(() => PickTaskCompletionSource.TrySetCanceled());
        await ShowPicker(cancellation);
        SetLoading(false);
        return (await PickTaskCompletionSource.Task).Cast<IUploadableFile>().ToArray();
    }

    /// <summary>
    /// Gets the JavaScript arguments to pass to the component.
    /// </summary>
    public override object[] GetJsArguments() => new[] { ElementReference, CreateDotNetObjectReference(), Options() };

    /// <summary>
    /// Callback method for JavaScript to call when the picker is authorized
    /// </summary>
    [JSInvokable]
    public virtual void OnAuthorized(string accessToken)
    {
        AccessToken = accessToken;
        IsLoading = true;
        CallStateHasChanged();
    }

    /// <summary>
    /// Sets the loading state
    /// </summary>
    [JSInvokable]
    public virtual void SetLoading(bool loading)
    {
        IsLoading = loading;
        CallStateHasChanged();
    }

    /// <summary>
    /// Callback method for JavaScript to call when the picker is ready
    /// </summary>
    [JSInvokable]
    public virtual Task OnReady()
    {
        IsReady = true;
        InitializationCompletionSource.TrySetResult(IsReady);
        CallStateHasChanged();
        return Task.CompletedTask;
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
            ExternalJsFiles,
            OnAuthorizedCallback = nameof(OnAuthorized),
            OnReadyCallback = nameof(OnReady),
            OnFilesSelectedCallback = nameof(OnFilesSelected),
            SetLoadingMethod = nameof(SetLoading),
            ClientId,
            ApiKey,
            AllowedMimeTypes,
            MultiSelect,
            AutoLoadFileDataBytes,
            JsOptions = JsOptions()
        };
    }

    /// <inheritdoc />
    public override async Task ImportModuleAndCreateJsAsync()
    {
        if(JsImportHelper.UseMinified)
            await JsRuntime.InvokeAsync<IJSObjectReference>("import", $"./_content/MudBlazor.Extensions/js/components/MudExExternalFilePickerBase.min.js");
        else
            await JsRuntime.InvokeAsync<IJSObjectReference>("import", $"./_content/MudBlazor.Extensions/js/components/MudExExternalFilePickerBase.js");
        await base.ImportModuleAndCreateJsAsync();
    }

    /// <summary>
    /// Updates the options for the js class
    /// </summary>
    protected virtual void UpdateJsOptions()
    {
        if (JsReference != null)
            JsReference.InvokeVoidAsync("setOptions", Options());
    }

    /// <summary>
    /// Protected method to show the picker
    /// </summary>
    protected virtual async Task ShowPicker(CancellationToken cancellation = default)
    {
        while(JsReference == null)
            await Task.Delay(100, cancellation);
        await JsReference.InvokeVoidAsync("openPicker", cancellation);
    }

    private async Task Pick() => await PickAsync();

}