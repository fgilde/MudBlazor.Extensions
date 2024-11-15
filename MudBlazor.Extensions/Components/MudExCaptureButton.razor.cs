using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
using MudBlazor.Extensions.Core.Capture;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Button component for capturing audio and video.
/// </summary>
public partial class MudExCaptureButton : IAsyncDisposable
{
    [Inject] private ICaptureService CaptureService { get; set; }
    [Inject] private MudExAppearanceService AppearanceService { get; set; }

    private bool _initialized;
    private string[] _preInitParameters;
    private CaptureId _recordingId;
    
    private RenderFragment Inherited() => builder => base.BuildRenderTree(builder);

    /// <summary>
    /// Indicates whether a recording session is currently active.
    /// </summary>
    public bool IsRecording => _recordingId != null && !string.IsNullOrEmpty(_recordingId);
    
    /// <summary>
    /// If this is true border animation is applied when recording is active, but <see cref="RecordingAnimation"/> has no effect if this is turned on.
    /// </summary>
    [Parameter]
    public bool RecordingBorderAnimation { get; set; } = true;

    /// <summary>
    /// This Animation is playing while recording is active, but only if <see cref="RecordingBorderAnimation"/> is false.
    /// </summary>
    [Parameter] 
    public AnimationType RecordingAnimation { get; set; } = AnimationType.Default;

    /// <summary>
    /// Event triggered when a recording session starts.
    /// </summary>
    [Parameter]
    public EventCallback OnRecordingStarted { get; set; }

    /// <summary>
    /// Event triggered when a recording session stops.
    /// </summary>
    [Parameter]
    public EventCallback OnRecordingStopped { get; set; }

    /// <summary>
    /// Event triggered capture result is obtained.
    /// </summary>
    [Parameter]
    public EventCallback<CaptureResult> OnDataCaptured { get; set; }

    /// <summary>
    /// If this is true a spectrum is shown while recording is active but then the <see cref="RecordingIcon"/> has no effect and will not be used.
    /// </summary>
    [Parameter]
    public bool ShowSpectrumOnRecording { get; set; }

    /// <summary>
    /// Icon displayed when the recording is active. Defaults to a 'Stop' icon.
    /// </summary>
    [Parameter]
    public string RecordingIcon { get; set; } = Icons.Material.Filled.Stop;

    /// <summary>
    /// Color of the icon when the recording is active. Defaults to error color.
    /// </summary>
    [Parameter]
    public MudExColor RecordingColor { get; set; } = MudExColor.Error;

    /// <summary>
    /// The options to use for the capture.
    /// If no options are provided, the user will be prompted to set them.
    /// The complexity of the options edit can be controlled with <see cref="EditMode"/>.
    /// </summary>
    [Parameter]
    public CaptureOptions CaptureOptions { get; set; }

    /// <summary>
    /// The editing mode to use for the capture options edit.
    /// This is only used when <see cref="CaptureOptions"/> is null or <see cref="AlwaysEditOptions"/> is true and the user should set the options.
    /// </summary>
    [Parameter]
    public CaptureOptionsEditMode EditMode { get; set; } = CaptureOptionsEditMode.Full;

    /// <summary>
    /// Is this is true the user is always prompted to edit the options before starting a recording.
    /// </summary>
    [Parameter] public bool AlwaysEditOptions { get; set; }

    /// <summary>
    /// If this is true the options are remembered after the user edited them and then no dialog is shown again.
    /// </summary>
    [Parameter] public bool RememberEditedOptions { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of colors to use for the gradient.
    /// </summary>
    [Parameter]
    public IEnumerable<MudExColor> BorderAnimationColors { get; set; } = new[] { MudExColor.Primary, MudExColor.Secondary, MudExColor.Tertiary, MudExColor.Error };

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (!_initialized)
            _preInitParameters = parameters.ToDictionary().Select(x => x.Key).ToArray();
        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
    }


    private bool IsOverwritten(string paramName) => _preInitParameters?.Contains(paramName) == true;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (!IsOverwritten(nameof(Icon)))
            Icon = Icons.Material.Filled.VideoCall;
        if (!IsOverwritten(nameof(Variant)))
            Variant = Variant.Outlined;
        if (!IsOverwritten(nameof(Color)))
            Color = Color.Primary;
        _initialized = true;
    }


    /// <inheritdoc />
    protected override async Task OnClickHandler(MouseEventArgs ev)
    {
        if (IsRecording)
            await StopRecordingAsync();
        else
        {
            await StartRecordingAsync();
        }

        await base.OnClickHandler(ev);
    }
    
    /// <summary>
    /// Starts the recording asynchronously.
    /// </summary>
    public async Task StartRecordingAsync()
    {
        var options = CaptureOptions != null && !AlwaysEditOptions ? CaptureOptions : await CaptureService.EditCaptureOptionsAsync(EditMode, CaptureOptions);
        if (RememberEditedOptions)
            CaptureOptions = options;

        if (options != null && options.Valid())
        {
            _recordingId = await CaptureService.StartCaptureAsync(options, OnResult, OnStopped);
            if (IsRecording)
            {
                await SetActive();
                await OnRecordingStarted.InvokeAsync();
            }
        }
    }

    /// <summary>
    /// Stops the recording asynchronously.
    /// </summary>
    public async Task StopRecordingAsync()
    {
        if (IsRecording)
        {
            await CaptureService.StopCaptureAsync(_recordingId);
        }
    }

    private void OnStopped(CaptureId captureId)
    {
        _recordingId = null;
        _ = OnRecordingStopped.InvokeAsync();
    }

    private async Task SetActive()
    {
        var currentIcon = Icon;
        Icon = ShowSpectrumOnRecording ? null : RecordingIcon;
        ChildContent = ShowSpectrumOnRecording ? Spectrum() : null;
        var size = Variant == Variant.Text ? 50 : 1;
        var borderColors = BorderAnimationColors.ToArray();
        var backgroundColor = Variant != Variant.Filled ? MudExColor.Surface : Color;
        var appearance = MudExStyleBuilder.Default
            .WithAnimation(RecordingAnimation, TimeSpan.FromSeconds(1), AnimationIteration.Infinite, RecordingAnimation != AnimationType.Default)
            .WithoutBorder(RecordingBorderAnimation)
            .WithAnimatedGradientBorder(size, backgroundColor, borderColors, RecordingBorderAnimation)
            .WithBorderColor(RecordingColor, !RecordingBorderAnimation && Variant != Variant.Text)
            .WithBackgroundColor(RecordingColor, Variant == Variant.Filled)
            .WithPadding(0, ShowSpectrumOnRecording)
            .WithColor(RecordingColor, Variant != Variant.Filled).AsImportant();
        _ = AppearanceService.ApplyTemporarilyToAsync(appearance, this, () => !IsRecording)
            .ContinueWith(_ =>
            {
                Icon = currentIcon;
                ChildContent = null;
                return InvokeAsync(StateHasChanged);
            });
        await InvokeAsync(StateHasChanged);
    }
    
    private void OnResult(CaptureResult captureResult)
    {
        OnDataCaptured.InvokeAsync(captureResult);
    }


    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (CaptureService != null) 
            await CaptureService.StopAllCapturesAsync();
    }


    private string SizeStr()
    {
        return Size switch
        {
            Size.Small => "29px;",
            Size.Medium => "36px;",
            Size.Large => "38px;",
            _ => "36px;"
        };
    }
}