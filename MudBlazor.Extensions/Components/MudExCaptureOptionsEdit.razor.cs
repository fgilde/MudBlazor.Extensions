using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component edit capture options
/// </summary>
public partial class MudExCaptureOptionsEdit : IObjectEditorWithCustomPropertyRenderDataFor<CaptureOptions>
{
    [Inject] private ICaptureService CaptureService { get; set; }

    private List<AudioDevice> _audioDevices = new();
    private List<VideoDevice> _videoDevices = new();

    /// <inheritdoc />
    [Parameter]
    public CaptureOptions Value { get; set; }

    /// <inheritdoc />
    [Parameter]
    public EventCallback<CaptureOptions> ValueChanged { get; set; }

    /// <summary>
    /// Set to true to have a read only mode
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }


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
    private void VideoDeviceChanged(IEnumerable<MudExListItem<VideoDevice>> obj)
        => SetAndStateChange(o => o.VideoDevice = obj?.Select(x => x.Value).FirstOrDefault());

    private void SetAndStateChange(Action<CaptureOptions> action)
    {
        (Value ??=new CaptureOptions()).SetProperties(action);
        CallStateHasChanged();
    }


}