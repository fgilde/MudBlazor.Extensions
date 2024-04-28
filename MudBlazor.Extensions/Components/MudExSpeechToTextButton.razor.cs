using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Css;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Button component for speech-to-text functionality, allowing for asynchronous recording and processing of speech input.
/// </summary>
public partial class MudExSpeechToTextButton: IAsyncDisposable
{
    [Inject] private ISpeechRecognitionService SpeechRecognitionService { get; set; }
    [Inject] private MudExAppearanceService AppearanceService { get; set; }
    
    private bool _initialized;
    private string[] _preInitParameters;
    private string _recordingId;
    private string _icon;
    private RenderFragment Inherited() => builder => base.BuildRenderTree(builder);


    /// <summary>
    /// Indicates whether a recording session is currently active.
    /// </summary>
    public bool IsRecording => !string.IsNullOrEmpty(_recordingId);

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
    /// Event triggered when a speech recognition result is obtained.
    /// </summary>
    [Parameter]
    public EventCallback<SpeechRecognitionResult> OnRecognized { get; set; }

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
    /// Language used for speech recognition, defaults to English (US).
    /// </summary>
    [Parameter]
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Specifies whether the recording should continue listening after capturing a complete phrase.
    /// </summary>
    [Parameter]
    public bool Continuous { get; set; } = true;

    /// <summary>
    /// Specifies whether interim results should be reported during the recognition process.
    /// </summary>
    [Parameter]
    public bool InterimResults { get; set; } = true;

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

    private bool IsOverwritten(string paramName) => _preInitParameters?.Contains(paramName) == true;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (!IsOverwritten(nameof(Icon)))
            Icon = Icons.Material.Filled.Mic;
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
            await StartRecordingAsync();
        await base.OnClickHandler(ev);
    }

    /// <summary>
    /// Starts the recording asynchronously.
    /// </summary>
    public async Task StartRecordingAsync()
    {
        var options = new SpeechRecognitionOptions
        {
            Lang = Language,
            Continuous = Continuous,
            InterimResults = InterimResults
        };
        _recordingId = await SpeechRecognitionService.StartRecordingAsync(options, OnResult, OnStopped);
        if (IsRecording)
        {
            await SetActive();
            await OnRecordingStarted.InvokeAsync();
        }
    }

    /// <summary>
    /// Stops the recording asynchronously.
    /// </summary>
    public async Task StopRecordingAsync()
    {
        if (IsRecording)
        {
            await SpeechRecognitionService.StopRecordingAsync(_recordingId);
        }
    }

    private void OnStopped(string obj)
    {
        _recordingId = null;
        _ = OnRecordingStopped.InvokeAsync();
    }

    private async Task SetActive()
    {
        var currentIcon = Icon;
        Icon = RecordingIcon;
        var size = Variant == Variant.Text ? 50 : 1;
        var borderColors = BorderAnimationColors.ToArray();
        var backgroundColor = Variant != Variant.Filled ? MudExColor.Surface : Color;
        var appearance = MudExStyleBuilder.Default
            .WithAnimation(RecordingAnimation, TimeSpan.FromSeconds(1), AnimationIteration.Infinite, RecordingAnimation != AnimationType.Default)
            .WithoutBorder(RecordingBorderAnimation)
            .WithAnimatedGradientBorder(size, backgroundColor, borderColors, RecordingBorderAnimation)
            .WithBorderColor(RecordingColor, !RecordingBorderAnimation && Variant != Variant.Text)
            .WithBackgroundColor(RecordingColor, Variant == Variant.Filled)
            .WithColor(RecordingColor, Variant != Variant.Filled).AsImportant();
        _ = AppearanceService.ApplyTemporarilyToAsync(appearance, this, () => !IsRecording)
            .ContinueWith(_ =>
            {
                Icon = currentIcon;
                return InvokeAsync(StateHasChanged);
            });
        await InvokeAsync(StateHasChanged);
    }
    
    private void OnResult(SpeechRecognitionResult result)
    {
        OnRecognized.InvokeAsync(result);
    }


    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (SpeechRecognitionService != null) 
            await SpeechRecognitionService.DisposeAsync();
    }
}
