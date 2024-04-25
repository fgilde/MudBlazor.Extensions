using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Button component for speech-to-text functionality, allowing for asynchronous recording and processing of speech input.
/// </summary>
public partial class MudExSpeechToTextButton: IAsyncDisposable
{
    [Inject] private ISpeechRecognitionService SpeechRecognitionService { get; set; }
    [Inject] private MudExStyleBuilder StyleBuilder { get; set; }
    
    private bool _initialized;
    private string[] _preInitParameters;
    private string _recordingId;
    private Color _color;
    private string _tmpClass;
    private string _icon;
    private RenderFragment Inherited() => builder => base.BuildRenderTree(builder);


    /// <summary>
    /// Indicates whether a recording session is currently active.
    /// </summary>
    public bool IsRecording => !string.IsNullOrEmpty(_recordingId);

    /// <summary>
    /// Gets or sets a value indicating whether animations are active during recording.
    /// </summary>
    [Parameter]
    public bool ActiveAnimation { get; set; } = true;

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
    public string IconActive { get; set; } = Icons.Material.Filled.Stop;

    /// <summary>
    /// Color of the icon when the recording is active. Defaults to warning color.
    /// </summary>
    [Parameter]
    public Color ColorActive { get; set; } = Color.Warning;

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
        _ = SetInActive();
        _ = OnRecordingStopped.InvokeAsync();
    }

    private async Task SetActive()
    {
        if (ActiveAnimation)
        {
            var borderColors = Variant != Variant.Filled ? new MudExColor[] { Color, ColorActive } : new[] { ColorActive, MudExColor.Error, MudExColor.Info };
            var backgroundColor = Variant != Variant.Filled ? MudExColor.Surface : Color;
            Class += _tmpClass = await StyleBuilder.WithAnimatedGradientBorder(1, backgroundColor, borderColors)
                .BuildAsClassRuleAsync();
        }

        _icon = Icon;
        _color = Color;
        Color = ColorActive;
        Icon = IconActive;
        await InvokeAsync(StateHasChanged);
    }

    private async Task SetInActive()
    {
        if (_tmpClass != null)
        {
            Class = Class?.Replace(_tmpClass, string.Empty);
            await StyleBuilder.RemoveClassRuleAsync(_tmpClass);
        }

        Color = _color;
        Icon = _icon;
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