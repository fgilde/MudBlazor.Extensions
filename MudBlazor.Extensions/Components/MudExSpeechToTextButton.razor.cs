using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Core.W3C;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Button component for speech-to-text functionality, allowing for asynchronous recording and processing of speech input.
/// </summary>
public partial class MudExSpeechToTextButton: IAsyncDisposable
{
    [Inject] private ISpeechRecognitionService SpeechRecognitionService { get; set; }
    [Inject] private MudExAppearanceService AppearanceService { get; set; }

    private AudioDevice[] _devices;
    private AudioDevice _selectedDevice = null;
    private bool _initialized;
    private string[] _preInitParameters;
    private string _recordingId;
    private string _icon;
    private RenderFragment Inherited() => builder => base.BuildRenderTree(builder);

    private IStringLocalizer<MudExSpeechToTextButton> FallbackLocalizer => ServiceProvider.GetService<IStringLocalizer<MudExSpeechToTextButton>>();


    /// <summary>
    /// Gets or sets the <see cref="IServiceProvider"/> to be used for dependency injection.
    /// </summary>
    [Inject]
    protected IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// Gets the <see cref="IStringLocalizer"/> to be used for localizing strings.
    /// </summary>
    protected IStringLocalizer LocalizerToUse => Localizer ?? FallbackLocalizer;
    
    /// <summary>
    /// Returns the device id to use for recording.
    /// </summary>
    public AudioDevice UsedDevice => (_selectedDevice ?? AudioDevice);

    /// <summary>
    /// If set, the recording will stop after the specified time.
    /// </summary>
    [Parameter] public TimeSpan? MaxCaptureTime { get; set; }

    /// <summary>
    /// If this is true a notification toast will be shown while recording.
    /// </summary>
    [Parameter] public bool ShowNotificationWhileRecording { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="IStringLocalizer"/> to be used for localizing strings.
    /// </summary>
    [Parameter, SafeCategory("Common")]
    public IStringLocalizer Localizer { get; set; }

    /// <summary>
    /// Indicates whether a recording session is currently active.
    /// </summary>
    public bool IsRecording => !string.IsNullOrEmpty(_recordingId);

    /// <summary>
    /// Sets the device for the audio input device to use for recording.
    /// Leave empty to use the default device.
    /// </summary>
    [Parameter] public AudioDevice  AudioDevice { get; set; }

    /// <summary>
    /// Specify if and how the user is able to select the audio input device.
    /// </summary>
    [Parameter] public DeviceSelectionType DeviceSelection { get; set; }

    /// <summary>
    /// if <see cref="DeviceSelection"/> is set to <see cref="DeviceSelectionType.SelectionList"/> this variant is used for the list of devices to choose from.
    /// </summary>
    [Parameter] public Variant DeviceListVariant { get; set; } = Variant.Outlined;

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

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (DeviceSelection == DeviceSelectionType.SelectionList && _devices == null)
            await FillDevices();
    }

    private async Task FillDevices()
    {
        _devices = (await SpeechRecognitionService.GetAudioDevicesAsync()).ToArray();
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

    private bool _devicePopoverOpen;
    private void OnBlur()
    {
        _devicePopoverOpen = false;
        InvokeAsync(StateHasChanged);
    }


    /// <inheritdoc />
    protected override async Task OnClickHandler(MouseEventArgs ev)
    {
        if (IsRecording)
            await StopRecordingAsync();
        else
        {
            if (await SelectDeviceIfRequiredAsync())
                return;
            await StartRecordingAsync();
        }

        await base.OnClickHandler(ev);
    }

    private async Task<bool> SelectDeviceIfRequiredAsync()
    {
        await FillDevices();
        if (DeviceSelection == DeviceSelectionType.PopupEveryTime ||
            (DeviceSelection == DeviceSelectionType.PopupOnlyOnce && _selectedDevice == null))
        {
            if(_devices?.Any() != true)
                return false;
            if (_devices.Length == 1)
            {
                _selectedDevice = _devices[0];
                return false;
            }
            _devicePopoverOpen = true;
            await InvokeAsync(StateHasChanged);
            return true;
        }

        return false;
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
            InterimResults = InterimResults,
            Device = UsedDevice,
            ShowNotificationWhileRecording = ShowNotificationWhileRecording,
            MaxCaptureTime = MaxCaptureTime
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

    private async Task AudioDeviceSelected(AudioDevice arg)
    {
        _selectedDevice = arg;        
        if (_devicePopoverOpen)
        {
            _devicePopoverOpen = false;
            await StartRecordingAsync();
        }
        else
        {
            await InvokeAsync(StateHasChanged);
        }
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

/// <summary>
/// Specifies the type of device selection to use for audio input.
/// </summary>
public enum DeviceSelectionType
{
    /// <summary>
    /// No device selection is used and the default system device is used.
    /// </summary>
    None,
    
    /// <summary>
    /// If no device is selected, a popup is shown to select the device.
    /// </summary>
    PopupOnlyOnce,
    
    /// <summary>
    /// Always show a popup to select the device.
    /// </summary>
    PopupEveryTime,
    
    /// <summary>
    /// Show a selection list of devices to choose from.
    /// </summary>
    SelectionList
}