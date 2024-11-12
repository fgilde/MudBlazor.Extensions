using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Capture;
using MudBlazor.Extensions.Core.W3C;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Core.Attributes;
using Nextended.Core.Extensions;
using OneOf;

namespace MudBlazor.Extensions.Services;

/// <summary>
/// Capture service for capturing audio and video.
/// </summary>
[RegisterAs(typeof(ICaptureService), RegisterAsImplementation = true, ServiceLifetime = ServiceLifetime.Transient)]
internal class MudExCaptureService : ICaptureService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly MudExCaptureNotificationService _captureNotifier;
    private readonly ConcurrentDictionary<string, DotNetObjectReference<JsRecordingCallbackWrapper<CaptureResult>>> _captures = new();

    private readonly IDialogService _dialogService;
    private readonly IServiceProvider _serviceProvider;


    private string TryLocalize(string text, params object[] args) => _serviceProvider?.GetService<IStringLocalizer<ICaptureService>>()?.TryLocalize(text, args) ?? string.Format(text, args);

    public MudExCaptureService(IJSRuntime jsRuntime, MudExCaptureNotificationService captureNotifier, IDialogService dialogService, IServiceProvider serviceProvider)
    {
        _jsRuntime = jsRuntime;
        _captureNotifier = captureNotifier;
        _dialogService = dialogService;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_captures.Select(pair => StopCaptureAsync(new CaptureId(pair.Key))));
    }

    public async Task<CaptureOptions> QuickEditCaptureOptionsAsync(CaptureOptions? options = null)
    {
        var simple = SimpleCaptureOptions.From(options);

        var dialogOptionsEx = new DialogOptionsEx()
        {
            DialogAppearance = MudExAppearance.FromCss(await _serviceProvider.GetService<MudExStyleBuilder>().WithHeight("auto").AsImportant().BuildAsClassRuleAsync()),
            Animation = AnimationType.Pulse,
            AnimationDuration = TimeSpan.FromMilliseconds(200),
            ShowAtCursor = true,
            CursorPositionOrigin = Origin.TopCenter,
            NoHeader = true,
        };

        var parameters = new DialogParameters
        {
            { nameof(MudExObjectEditDialog<CaptureOptions>.GlobalResetSettings), new GlobalResetSettings() {AllowReset = false} },
            { nameof(MudExObjectEditDialog<CaptureOptions>.DefaultPropertyResetSettings), new PropertyResetSettings() {AllowReset = false} },
            { nameof(MudExObjectEditDialog<CaptureOptions>.AllowSearch),false },
            { nameof(MudExObjectEditDialog<CaptureOptions>.SaveButtonText), TryLocalize("Start Recording") },
            { nameof(MudExObjectEditDialog<CaptureOptions>.SaveButtonColor), Color.Error },
            { nameof(MudExObjectEditDialog<CaptureOptions>.SaveButtonIcon), Icons.Material.Filled.RecordVoiceOver },
            { nameof(MudExObjectEditDialog<CaptureOptions>.ShowCancelButton), false },
            { nameof(MudExObjectEditDialog<CaptureOptions>.ShowToolbar), false }
        };

        var result = await _dialogService.EditObject(simple, TryLocalize("Capture"),
            (captureOptions, _) => Task.FromResult(captureOptions?.Valid() == true ? "" : TryLocalize("Please select at least one source to record")), 
            dialogOptionsEx, meta =>
            {
                meta.AllProperties.WithLabelResolver(i => TryLocalize(string.Join(" ", i.Name.SplitByUpperCase())));
            }, parameters);

        if (result.Cancelled)
            return null;
        return result.Result.ToCaptureOptions();
    }

    /// <inheritdoc />
    public async Task<CaptureOptions> EditCaptureOptionsAsync(CaptureOptionsEditMode mode, CaptureOptions? options = null)
    {
        if (mode == CaptureOptionsEditMode.Simple)
            return await QuickEditCaptureOptionsAsync(options);
        options ??= new CaptureOptions();
        var dialogOptionsEx = DialogOptionsEx.DefaultDialogOptions.CloneOptions().SetProperties(o =>
        {
            o.DialogAppearance = MudExAppearance.FromCss(MudExCss.Classes.Dialog.FullHeightWithMargin);
            o.MaxWidth = MaxWidth.Medium;
            o.FullWidth = true;
            o.MaxHeight = MaxHeight.Medium;
            o.FullHeight = true;
            o.Resizeable = true;
            o.DragMode = MudDialogDragMode.Simple;
        });

        var parameters = new DialogParameters
        {
            { nameof(MudExObjectEditDialog<CaptureOptions>.DialogIcon), Icons.Material.Filled.VideoCameraFront },
            { nameof(MudExObjectEditDialog<CaptureOptions>.SaveButtonText), TryLocalize("Start Recording") },
            { nameof(MudExObjectEditDialog<CaptureOptions>.SaveButtonColor), Color.Error },
            { nameof(MudExObjectEditDialog<CaptureOptions>.SaveButtonIcon), Icons.Material.Filled.RecordVoiceOver },
        };

        var result = await _dialogService.EditObject(options, TryLocalize("Capture"), (captureOptions, _) => Task.FromResult(captureOptions?.Valid() == true ? "" : TryLocalize("Please select at least one source to record")), dialogOptionsEx, null, parameters);

        if (result.Cancelled)
            return null;
        return result.Result;
    }

    /// <inheritdoc />
    public async Task<CaptureId> StartCaptureAsync(CaptureOptions options, Action<CaptureResult> callback, Action<CaptureId> stoppedCallback = null)
    {
        if (!options.Valid())
        {
            return null;
        }
        var callbackReference = DotNetObjectReference.Create(new JsRecordingCallbackWrapper<CaptureResult>(
            captureResult => callback?.Invoke(Prepare(captureResult, options)), s =>
            {
                _captures.TryRemove(s, out _);
                _captureNotifier.RemoveRecordingInfo(s);
                stoppedCallback?.Invoke(new CaptureId(s));
            }));

        var result = new CaptureId(await _jsRuntime.InvokeAsync<string>("MudExCapture.startCapture", options, callbackReference));
        _captures.TryAdd(result, callbackReference);
        if (options.MaxCaptureTime is { TotalSeconds: > 0 })
            _ = Task.Delay(options.MaxCaptureTime.Value).ContinueWith(_ => StopCaptureAsync(result));

        if (options.ShowNotificationWhileRecording)
            _captureNotifier.ShowRecordingInfo(result, options.MaxCaptureTime, (s, _) => StopCaptureAsync(new CaptureId(s)));
        return result;
    }

    public Task StopCaptureAsync(MediaStreamTrack track)
    {
         return _jsRuntime.InvokeAsync<string>("MudExCapture.stopPreviewCapture", track.Id).AsTask();
    }

    private CaptureResult Prepare(CaptureResult captureResult, CaptureOptions options)
    {
        // TODO: Think about to build the combined result in blazor maybe with FFMpegBlazor
        return captureResult;
    }

    /// <inheritdoc />
    public Task StopCaptureAsync(CaptureId captureId)
    {
        string id = captureId;
        var callback = _captures.GetOrAdd(id, _ => null);
        return _jsRuntime.InvokeVoidAsync("MudExCapture.stopCapture", id, callback).AsTask();
    }

    /// <inheritdoc />
    public Task StopAllCapturesAsync() =>
        _jsRuntime.InvokeVoidAsync("MudExCapture.stopAllCaptures").AsTask();

    /// <inheritdoc />
    public Task<IEnumerable<AudioDevice>> GetAudioDevicesAsync() =>
        _jsRuntime.InvokeAsync<IEnumerable<AudioDevice>>("MudExCapture.getAvailableAudioDevices").AsTask();

    /// <inheritdoc />
    public Task<IEnumerable<VideoDevice>> GetVideoDevicesAsync() =>
        _jsRuntime.InvokeAsync<IEnumerable<VideoDevice>>("MudExCapture.getAvailableVideoDevices").AsTask();

    /// <inheritdoc />
    public async Task<MediaStreamTrack> SelectCaptureSourceAsync(DisplayMediaOptions? displayMediaOptions = null, OneOf<ElementReference, string> elementForPreview = default)
    {
        object toPass = null;
        if (elementForPreview is { IsT0: true, AsT0: { Context: not null, Id: not null } })
        {
            toPass = elementForPreview.AsT0;
        }
        else if (elementForPreview.IsT1 && !string.IsNullOrWhiteSpace(elementForPreview.AsT1))
            toPass = elementForPreview.AsT1;

        return await _jsRuntime.InvokeAsync<MediaStreamTrack>("MudExCapture.selectCaptureSource", displayMediaOptions, toPass);
    }
}