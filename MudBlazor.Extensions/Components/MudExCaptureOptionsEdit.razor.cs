using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Capture;
using MudBlazor.Extensions.Core.Css;
using MudBlazor.Extensions.Core.W3C;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Interop;
using MudBlazor.Services;
using Nextended.Core;
using Nextended.Core.Extensions;
using YamlDotNet.Core.Tokens;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component edit capture options
/// </summary>
public partial class MudExCaptureOptionsEdit : IObjectEditorWithCustomPropertyRenderDataFor<CaptureOptions>
{
    private MudExDimension _previewSize = new(400, 300);

    [Inject] private ICaptureService CaptureService { get; set; }
    [Inject] private IDialogService DialogService { get; set; }
    [Inject] private IResizeObserver ResizeObserver { get; set; }

    private bool _resizeObserved = false;
    private bool _isDragging = false;
    private MudExDimension _dragStartPoint;
    private MudExPosition _elementStartPosition;
    private List<AudioDevice> _audioDevices = new();
    private List<VideoDevice> _videoDevices = new();
    private ElementReference _previewScreen;
    private ElementReference _previewCamera;
    private ElementReference _overlayContainer;
    private CaptureOptions _value;
    private DisplayMediaOptions _displayMediaOptions = new();
    private MediaStreamTrack _screenTrack;
    private MediaStreamTrack _cameraTrack;
    private bool _captureScreen;
    private Action<DialogOptionsEx> dialogOptions = o =>
    {
        o.MaxWidth = MaxWidth.Medium;
        o.Animation = AnimationType.FadeIn;
        o.FullWidth = true;
        o.MaxHeight = MaxHeight.Medium;
        o.FullHeight = true;
        o.Resizeable = true;
        o.DragMode = MudDialogDragMode.Simple;
    };


    /// <inheritdoc />
    [Parameter]
    public CaptureOptions Value
    {
        get => GetValue();
        set
        {
            _value = value;
            _displayMediaOptions = value.ScreenCapture is { IsT2: true, AsT2: not null } ? value.ScreenCapture.AsT2 : DisplayMediaOptions.Default;
        }
    }

    private CaptureOptions GetValue()
    {
        _value.CaptureMediaOptions = null;
        _value.ScreenSource = null;
        _value.ScreenCapture = default;
        if (_captureScreen)
        {
            if (_screenTrack is not null)
                _value.ScreenSource = _screenTrack;
            else if (_displayMediaOptions is not null)
                _value.ScreenCapture = _displayMediaOptions;
            else
                _value.ScreenCapture = _captureScreen;
        }
        if (_value.VideoDevice?.DeviceId is null)
            _value.VideoDevice = null;
        if (_value.AudioDevices != null && _value.AudioDevices.All(d => d.DeviceId == null))
            _value.AudioDevices = null;

        return _value;
    }

    /// <inheritdoc />
    [Parameter]
    public EventCallback<CaptureOptions> ValueChanged { get; set; }

    /// <summary>
    /// Set to true to have a read only mode
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Dense
    /// </summary>
    [Parameter] public bool Dense { get; set; } = true;

    /// <inheritdoc />
    public IRenderData GetRenderData(ObjectEditPropertyMeta meta)
    {
        return RenderData.For<MudExObjectEditPicker<CaptureOptions>, CaptureOptions>(edit => edit.Value, edit =>
        {
            //edit.PickerVariant = PickerVariant.Dialog;
            edit.BindWidthToPicker = false;
            edit.AllowOpenOnReadOnly = true;
        });
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(
            CaptureService.GetAudioDevicesAsync().ContinueWith(task =>
            {
                _audioDevices = task.Result.ToList();
                if(_audioDevices.All(device => device?.DeviceId == null))
                    _audioDevices.Insert(0, AudioDevice.Default);
            }),
            CaptureService.GetVideoDevicesAsync().ContinueWith(task =>
            {
                _videoDevices = task.Result.ToList();
                if(_videoDevices.All(device => device?.DeviceId == null))
                    _videoDevices.Insert(0, VideoDevice.Default);
            }),
            base.OnInitializedAsync()
        );
        // _videoDevices.Insert(0, VideoDevice.Default);
    }

    protected override async Task OnFinishedRenderAsync()
    {
        ResizeObserver.OnResized += OnOverlayResized;
        await base.OnFinishedRenderAsync();
    }
    

    private Task<IEnumerable<string>> SearchAudioContentType(string value, CancellationToken token)
        => Task.FromResult(MimeType.AudioTypes.Concat(new[] { "audio/webm" }).Distinct().Where(x => string.IsNullOrEmpty(value) || x.Contains(value, StringComparison.InvariantCultureIgnoreCase)));

    private Task<IEnumerable<string>> SearchVideoContentType(string value, CancellationToken token)
        => Task.FromResult(MimeType.VideoTypes.Concat(new[] { "video/webm" }).Distinct().Where(x => string.IsNullOrEmpty(value) || x.Contains(value, StringComparison.InvariantCultureIgnoreCase)));

    private void SetAndStateChange(Action<CaptureOptions> action)
    {
        (Value ??= new CaptureOptions()).SetProperties(action);
        CallStateHasChanged();
    }


    private string GetStyleStr()
    {
        return Get<MudExStyleBuilder>()
            .WithMinWidth("500px")
            .WithMinHeight("500px")
            .WithWidth("100%")
            .WithHeight("100%")
            .AddRaw(Style).Style;
    }

    private async Task ChangeAudioConstraintsAsync(AudioDevice device)
    {
        var constraintToEdit = Value?.AudioDevices?.FirstOrDefault(d => d.DeviceId == device.DeviceId);
        if(constraintToEdit != null)
            await DialogService.EditObject(constraintToEdit, TryLocalize("Edit Audio Constraints"), Icons.Material.Filled.Edit, 
                DialogOptionsEx.DefaultDialogOptions.CloneOptions().SetProperties(dialogOptions), 
                meta => meta.Properties(m => m.DeviceId, m => m.GroupId).Ignore());
    }

    private async Task ChangeVideoConstraintsAsync(MouseEventArgs obj)
    {
        Value.VideoDevice ??= new VideoConstraints();
        
        var res = await DialogService.EditObject(Value.VideoDevice, TryLocalize("Edit Video Constraints"), Icons.Material.Filled.Edit, 
            DialogOptionsEx.DefaultDialogOptions.CloneOptions().SetProperties(dialogOptions), 
            meta => meta.Properties(m => m.DeviceId, m => m.GroupId).Ignore());
        if (!res.Cancelled)
            await VideoDeviceChanged(Value.VideoDevice);
    }

    private Task ChangeMediaOptionsAsync(MouseEventArgs obj)
    {
        var dialogOptionsEx = DialogOptionsEx.DefaultDialogOptions.CloneOptions().SetProperties(dialogOptions);
        return DialogService.EditObject(_displayMediaOptions, TryLocalize("Change media options"), Icons.Material.Filled.VideoCameraFront, dialogOptionsEx);
    }

    private Task SelectScreenIf()
    {
        if(_captureScreen && Value.OverlaySource == OverlaySource.VideoDevice)
            return SelectScreen();
        return Task.CompletedTask;
    }

    private string StopPreviewButtonStyle()
    {
        return MudExStyleBuilder.Default
            .WithPosition(Core.Css.Position.Absolute)
            .WithMarginTop(-20)
            .WithMarginLeft(_previewSize.Width)
            .WithZIndex(1)
            .WithTransform("scale(0.8)")
            .Style;
    }

    private string GetMainCapturePreviewStyle()
    {
        return MudExStyleBuilder.Default
            .WithHeight("100%", _captureScreen)
            .WithWidth("100%", _captureScreen)
            .WithCursor(Cursor.Pointer, _captureScreen && Value.OverlaySource == OverlaySource.VideoDevice)
            .WithDisplay(Display.None, !_captureScreen)
            .Style;
    }

    private string GetOverlayPreviewStyle()
    {
        return MudExStyleBuilder.Default
            .WithSize(Value.OverlaySize, _captureScreen)
            .WithHeight("100%", !_captureScreen)
            .WithWidth("100%", !_captureScreen)
            .WithPosition(Value.GetOverlayPosition(_previewSize), _captureScreen)
            .WithDisplay(Display.None, _cameraTrack == null)
            .Style;
    }

    private string GetOverlayClass()
    {
        return MudExCssBuilder.Default
            .AddClass("mud-ex-draggable", _captureScreen)
            .AddClass("mud-ex-resizeable", _captureScreen)
            .Class;
    }


    private string GetPreviewContainerStyle()
    {
        //width: 200px; height: 200px; border: 3px dotted gray
        return MudExStyleBuilder.Default
            .WithPosition(Core.Css.Position.Relative)
            .WithSize(_previewSize)
            .WithBorder(3, BorderStyle.Dotted, "gray")
            .Style;
    }

    private async Task SelectScreen()
    {
        if (_isDragging)
        {
            _isDragging = false;
            return;
        }
        await StopPreviewScreenTrack();
        var videoElement = Value.OverlaySource == OverlaySource.CapturedScreen && Value.VideoDevice?.DeviceId != null ? _previewCamera : _previewScreen;
        _screenTrack = await CaptureService.SelectCaptureSourceAsync(_displayMediaOptions, videoElement);
        Value.ScreenCapture = _screenTrack;
    }

    private async Task StopPreviewScreenTrack()
    {
        if (_screenTrack is not null)
        {
            await CaptureService.StopCaptureAsync(_screenTrack);
            _screenTrack = null;
            if (_displayMediaOptions is not null)
                Value.ScreenCapture = _displayMediaOptions;
            else
                Value.ScreenCapture = _captureScreen;
            await SetDummyStream();
        }
    }

    private async Task StopPreviewCameraTrack()
    {
        if (_cameraTrack is not null)
        {
            await CaptureService.StopCaptureAsync(_cameraTrack);
            _cameraTrack = null;
        }
    }


    private void StartDrag(MouseEventArgs e)
    {
        _isDragging = true;
        _dragStartPoint = new MudExDimension(e.ClientX, e.ClientY);
        _elementStartPosition = Value.GetOverlayPosition(_previewSize).ToAbsolute(_previewSize);
        Value.OverlayPosition = DialogPosition.Custom;
    }

    private void OnDrag(MouseEventArgs e)
    {
        if (_isDragging)
        {
            var deltaX = e.ClientX - _dragStartPoint.Width;
            var deltaY = e.ClientY - _dragStartPoint.Height;
            Value.OverlayCustomPosition = new MudExPosition(
                _elementStartPosition.Left + deltaX,
                _elementStartPosition.Top + deltaY);
            ConstrainWithinContainer();
            StateHasChanged();
        }
    }

    private void EndDrag(MouseEventArgs e)
    {
        _isDragging = false;
        Value.OverlayCustomPosition = Value.OverlayCustomPosition.ToRelative(_previewSize);
    }

    private void ConstrainWithinContainer()
    {
        var size = Value.OverlaySize;
        double? left = null;
        double? top = null;
        if (Value.OverlayCustomPosition.Left < 0) left = 0;
        if (Value.OverlayCustomPosition.Top < 0) top = 0;
        if (size.Width.Value + Value.OverlayCustomPosition.Left.Value > _previewSize.Width)
            left = _previewSize.Width - size.Width;
        if (size.Height.Value + Value.OverlayCustomPosition.Top.Value > _previewSize.Height)
            top = _previewSize.Height - size.Height;
        if (left.HasValue || top.HasValue)
            Value.OverlayCustomPosition = new MudExPosition(left ?? Value.OverlayCustomPosition.Left, top ?? Value.OverlayCustomPosition.Top);
    }

    private async Task VideoDeviceChanged(VideoConstraints constraints)
    {
        await StopPreviewCameraTrack();
        if (constraints != null)
        {
            _cameraTrack = await CaptureService.SelectCaptureSourceAsync(new DisplayMediaOptions { Video = constraints }, _previewCamera);
        }
        else
        {
            if (Value.OverlaySource == OverlaySource.CapturedScreen)
                await ToogleOverlaySource();
        }
        SetAndStateChange(o => o.VideoDevice = constraints);
    }

    private async Task VideoDeviceChanged(VideoDevice obj)
    {
        await StopPreviewCameraTrack();
        VideoConstraints constraints = null;
        if (obj != null)
        {
            constraints = Value?.VideoDevice ?? new VideoConstraints();
            constraints.DeviceId = obj.DeviceId;
            await VideoDeviceChanged(constraints);
        }
        else
        {
            await VideoDeviceChanged(constraints);
        }
    }


    private async Task ToogleOverlaySource()
    {
        Value.OverlaySource = Value.OverlaySource == OverlaySource.CapturedScreen ? OverlaySource.VideoDevice : OverlaySource.CapturedScreen;
        await JsRuntime.InvokeVoidAsync("MudExCapture.switchSrcObject", _previewScreen, _previewCamera, true);
    }

    private async Task SetDummyStream()
    {
        var videoElement = Value.OverlaySource == OverlaySource.CapturedScreen && Value.VideoDevice?.DeviceId != null ? _previewCamera : _previewScreen;
        await JsRuntime.InvokeVoidAsync("MudExCapture.setText", videoElement, TryLocalize("Screen"));
    }

    private async Task CaptureScreenChanged(bool @checked)
    {
        _captureScreen = @checked;
        if(!_captureScreen && Value.OverlaySource == OverlaySource.CapturedScreen)
            await ToogleOverlaySource();
        if (_captureScreen && _screenTrack == null)
            await SetDummyStream();
    }

    private async Task DetachResize()
    {
        await Task.Delay(500);
        await ResizeObserver.Unobserve(_overlayContainer);
        _resizeObserved = false;
    }

    private async Task AttachResize()
    {
        await ResizeObserver.Observe(_overlayContainer);
        _resizeObserved = true;
    }

    private void OnOverlayResized(IDictionary<ElementReference, BoundingClientRect> changes)
    {
        var change = changes.FirstOrDefault(c => c.Key.Id == _overlayContainer.Id);
        if (change.Value != null && _captureScreen && _resizeObserved)
        {
            change.Value.WindowWidth = _previewSize.Width;
            change.Value.WindowHeight = _previewSize.Height;
            Value.OverlaySize = change.Value.ToDimension(CssUnit.Percentage);
            StateHasChanged();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await StopPreviewCameraTrack();
        //await StopPreviewScreenTrack();
        await base.DisposeAsync();
    }

    private string OverlayOptionsStyle()
    {
        var hasOverlay = _captureScreen && !string.IsNullOrEmpty(Value?.VideoDevice?.DeviceId);
        return MudExStyleBuilder.Default
            .WithVisibility(Visibility.Hidden, !hasOverlay)
            .Style;
    }

    private string GetAuralizerStyle()
    {
        return MudExStyleBuilder.Default
            .WithVisibility(Visibility.Collapse, string.IsNullOrEmpty(Value?.AudioDevices?.FirstOrDefault()?.DeviceId))
            .WithHeight(50)
            .WithWidth("100%")
            .Style;
    }

    private bool IsAudioDeviceSelected(string deviceId)
    {
        return Value?.AudioDevices?.FirstOrDefault(d => d.DeviceId == deviceId) != null;
    }

    private Task OnAudioDeviceSelected(AudioDevice context, bool isSelected)
    {
        if(isSelected && !IsAudioDeviceSelected(context.DeviceId))
            SetAndStateChange(o => (o.AudioDevices ??= new List<AudioConstraints>()).Add(context.ToConstraints()));
        else if (!isSelected && Value?.AudioDevices != null)
            SetAndStateChange(o => o.AudioDevices.RemoveAll(d => d.DeviceId == context.DeviceId));
        return Task.CompletedTask;
    }
}