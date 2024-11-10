using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Css;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Interop;
using MudBlazor.Services;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component edit capture options
/// </summary>
public partial class MudExCaptureOptionsEdit : IObjectEditorWithCustomPropertyRenderDataFor<CaptureOptions>
{
    private MudExDimension _previewSize = new(400, 300);

    [Inject] private ICaptureService CaptureService { get; set; }
    [Inject] private IDialogService DialogService { get; set; }
    [Inject] IResizeObserver ResizeObserver { get; set; }

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

    private bool _recordSystemAudio
    {
        get => _displayMediaOptions?.SystemAudio == IncludeExclude.Include;
        set
        {
            (_displayMediaOptions ??= DisplayMediaOptions.Default).SystemAudio = value ? IncludeExclude.Include : IncludeExclude.Exclude;
            Value.ScreenCapture = _displayMediaOptions;
        }
    }

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
            edit.AllowOpenOnReadOnly = true;
        });
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(
            CaptureService.GetAudioDevicesAsync().ContinueWith(task => _audioDevices = task.Result.ToList()),
            CaptureService.GetVideoDevicesAsync().ContinueWith(task => _videoDevices = task.Result.ToList()),
            base.OnInitializedAsync()
        );
       // _videoDevices.Insert(0, VideoDevice.Default);
    }

    protected override async Task OnFinishedRenderAsync()
    {
        await ResizeObserver.Observe(_overlayContainer);
        ResizeObserver.OnResized += OnOverlayResized;
        await base.OnFinishedRenderAsync();
    }

    private void OnOverlayResized(IDictionary<ElementReference, BoundingClientRect> changes)
    {
        var change = changes.FirstOrDefault(c => c.Key.Id == _overlayContainer.Id);
        if (change.Value != null)
        {
            ResizeObserver.Unobserve(_overlayContainer);
            change.Value.WindowWidth = _previewSize.Width;
            change.Value.WindowHeight = _previewSize.Height;
            Value.OverlaySize = change.Value.ToDimension(CssUnit.Percentage);
            StateHasChanged();
            Task.Delay(200).ContinueWith(_ => ResizeObserver.Observe(_overlayContainer));
        }
    }

    private Task<IEnumerable<string>> SearchAudioContentType(string value, CancellationToken token)
        => Task.FromResult(MimeType.AudioTypes.Concat(new[] { "audio/webm" }).Distinct().Where(x => string.IsNullOrEmpty(value) || x.Contains(value, StringComparison.InvariantCultureIgnoreCase)));

    private Task<IEnumerable<string>> SearchVideoContentType(string value, CancellationToken token)
        => Task.FromResult(MimeType.VideoTypes.Concat(new[] { "video/webm" }).Distinct().Where(x => string.IsNullOrEmpty(value) || x.Contains(value, StringComparison.InvariantCultureIgnoreCase)));

    private void AudioDevicesChanged(IEnumerable<MudExListItem<AudioDevice>> obj)
        => SetAndStateChange(o => o.AudioDevices = obj?.Select(x => x.Value).ToList());

    private void SetAndStateChange(Action<CaptureOptions> action)
    {
        (Value ??= new CaptureOptions()).SetProperties(action);
        CallStateHasChanged();
    }


    private string GetStyleStr()
    {
        return Get<MudExStyleBuilder>()
            .WithWidth("100%")
            .WithHeight("100%")
            .AddRaw(Style).Style;
    }

    private async Task ChangeMediaOptions(MouseEventArgs obj)
    {
        var dialogOptionsEx = DialogOptionsEx.DefaultDialogOptions.CloneOptions().SetProperties(o =>
        {
            o.MaxWidth = MaxWidth.Medium;
            o.FullWidth = true;
            o.MaxHeight = MaxHeight.Medium;
            o.FullHeight = true;
            o.Resizeable = true;
            o.DragMode = MudDialogDragMode.Simple;
        });

        var dialogResult = await DialogService.EditObject(_displayMediaOptions, TryLocalize("Change media options"), Icons.Material.Filled.VideoCameraFront, dialogOptionsEx);
        if (dialogResult.Cancelled)
            return;
        _recordSystemAudio = _displayMediaOptions.SystemAudio == IncludeExclude.Include;
    }


    private string StopPreviewButtonStyle()
    {
        return MudExStyleBuilder.Default
            .WithPosition(Core.Css.Position.Absolute)
            .WithMarginLeft(_previewSize.Width)
            .WithZIndex(1)
            .WithTransform("scale(0.8)")
            .Style;
    }

    private string GetMainCapturePreviewStyle()
    {
        return MudExStyleBuilder.Default
            .WithHeight("100%")
            .WithWidth("100%")
            //.WithOpacity(0, track == null)
            .Style;
    }

    private string GetOverlayPreviewStyle()
    {
        return MudExStyleBuilder.Default
            .WithSize(Value.OverlaySize)
            .WithPosition(Value.GetOverlayPosition(_previewSize))
            //.WithOpacity(0, track == null)
            .Style;
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

        _screenTrack = await CaptureService.SelectCaptureSourceAsync(_displayMediaOptions, _previewScreen);
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
                Value.ScreenCapture = true;
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
        _elementStartPosition = Value.GetOverlayPosition(_previewSize);
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

    private async Task VideoDeviceChanged(VideoDevice obj)
    {
        await StopPreviewCameraTrack();
        if(obj != null)
            _cameraTrack = await CaptureService.SelectCaptureSourceAsync(new DisplayMediaOptions { Video = new VideoConstraints { DeviceId = obj.DeviceId } }, _previewCamera);
        SetAndStateChange(o => o.VideoDevice = obj);
    }


}