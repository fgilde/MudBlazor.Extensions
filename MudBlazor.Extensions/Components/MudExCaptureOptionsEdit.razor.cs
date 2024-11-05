using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component edit capture options
/// </summary>
public partial class MudExCaptureOptionsEdit : IObjectEditorWithCustomPropertyRenderDataFor<CaptureOptions>
{
    [Inject] private ICaptureService CaptureService { get; set; }
    [Inject] private IDialogService DialogService { get; set; }

    private List<AudioDevice> _audioDevices = new();
    private List<VideoDevice> _videoDevices = new();
    private ElementReference _preview;
    private CaptureOptions _value;
    private DisplayMediaOptions _displayMediaOptions = new();
    private MediaStreamTrack _track;
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
            if (_track is not null)
                _value.ScreenSource = _track;
            else if (_displayMediaOptions is not null)
                _value.ScreenCapture = _displayMediaOptions;
            else
                _value.ScreenCapture = _captureScreen;
        }
        if(_value.VideoDevice?.DeviceId is null)
            _value.VideoDevice = null;
        if(_value.AudioDevices != null && _value.AudioDevices.All(d => d.DeviceId == null))
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

    private string GetPreviewStyle()
    {
        return MudExStyleBuilder.Default
            .WithHeight("100%")
            .WithWidth("100%")
            .WithOpacity(0, _track == null)
            .Style;
    }

    private async Task SelectScreen()
    {
        await StopPreviewTrack();
        _track = await CaptureService.SelectCaptureSourceAsync(_displayMediaOptions, _preview);
        Value.ScreenCapture = _track;
    }

    private async Task StopPreviewTrack()
    {
        if (_track is not null)
        {
            await CaptureService.StopCaptureAsync(_track);
            _track = null;
            if (_displayMediaOptions is not null)
                Value.ScreenCapture = _displayMediaOptions;
            else
                Value.ScreenCapture = true;
        }
    }
}