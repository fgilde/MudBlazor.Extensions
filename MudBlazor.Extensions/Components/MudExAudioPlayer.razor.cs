using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Css;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;
using Nextended.Core;
using Nextended.Core.Extensions;
using BlazorJS;
using BlazorJS.Attributes;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Blazor wrapper component for AudioMotionAnalyzer
/// </summary>
public partial class MudExAudioPlayer : IMudExFileDisplay
{
    const string CATEGORY_PLAYER = "Player";
    
    private string _id = $"mud-ex-audio-player-{Guid.NewGuid().ToFormattedId()}";
    private List<ElementReference> _audioElements = new();
    private ElementReference _visualizer;
    private IJSObjectReference _audioMotion;

    private ElementReference _audioElement
    {
        set => _audioElements.Add(value);
    }

    [Inject] private MudExFileService FileService { get; set; }

    /// <summary>
    /// The name of the component
    /// </summary>
    public string Name => nameof(MudExAudioPlayer);

    /// <summary>
    /// Src url of the audio file
    /// </summary>
    [Parameter] public string Src { get; set; }

    /// <summary>
    /// Content type of the audio file
    /// </summary>
    [Parameter] public string ContentType { get; set; }

    /// <summary>
    /// The file display infos
    /// </summary>
    [Parameter] public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Cascading parameter to get the parent MudExFileDisplay if present
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    [Parameter, SafeCategory(CATEGORY_PLAYER)] public bool AnimateAudioElementBorderColor { get; set; }
    [Parameter, SafeCategory(CATEGORY_PLAYER)] public MudExColor AudioElementBorderColor { get; set; } = MudExColor.Primary;
    [Parameter, SafeCategory(CATEGORY_PLAYER)] public MudExColor AudioElementBackgroundColor { get; set; } = MudExColor.Surface;
    [Parameter, SafeCategory(CATEGORY_PLAYER)] public BorderStyle AudioElementBorderStyle { get; set; } = BorderStyle.Solid;
    [Parameter, SafeCategory(CATEGORY_PLAYER)] public MudExSize<double> AudioElementBorderSize { get; set; } = "1px";


    #region Audio Motion
    

    [Parameter, ForJs, SafeCategory("AudioProcessing")]
    public int Sensitivity { get; set; } = 100;


    [Parameter, ForJs("channelLayout"), SafeCategory("AudioProcessing")]
    public ChannelLayout ChannelLayout { get; set; } = ChannelLayout.Single;

    [Parameter, ForJs(IgnoreOnParams = true)]
    public bool Stereo
    {
        get => ChannelLayout != ChannelLayout.Single;
        set => ChannelLayout = value ? ChannelLayout.DualVertical : ChannelLayout.Single;
    }


    [Parameter, ForJs("frequencyScale"), SafeCategory("AudioProcessing")]
    public FrequencyScale FrequencyScale { get; set; } = FrequencyScale.Log;

    [Parameter, ForJs("fftSize"), SafeCategory("AudioProcessing")]
    public int FFTSize { get; set; } = 8192;

    [Parameter, ForJs("maxDecibels"), SafeCategory("AudioProcessing")]
    public int MaxDecibels { get; set; } = -25;

    [Parameter, ForJs("minDecibels"), SafeCategory("AudioProcessing")]
    public int MinDecibels { get; set; } = -85;

    [Parameter, ForJs("maxFreq"), SafeCategory("AudioProcessing")]
    public int MaxFrequency { get; set; } = 22000;

    [Parameter, ForJs("minFreq"), SafeCategory("AudioProcessing")]
    public int MinFrequency { get; set; } = 20;

    [Parameter, ForJs("smoothing"), SafeCategory("AudioProcessing")]
    public double Smoothing { get; set; } = 0.5;

    [Parameter, ForJs("volume"), SafeCategory("AudioProcessing")]
    public double Volume { get; set; } = 1;

    [Parameter, ForJs("weightingFilter"), SafeCategory("AudioProcessing")]
    public WeightingFilter WeightingFilter { get; set; }

    [Parameter, ForJs, SafeCategory("ControlAndModes")]
    public VisualizationMode Mode { get; set; } = VisualizationMode.DescreteFrequencies;
    
    [Parameter, ForJs("maxFPS"), SafeCategory("Appearance")]
    public int MaxFPS { get; set; } = 0;

    [Parameter, ForJs("reflexRatio"), SafeCategory("Appearance")]
    public double ReflexRatio { get; set; } = 0;

    [Parameter, ForJs("roundBars"), SafeCategory("Appearance")]
    public bool RoundBars { get; set; } = false;

    [Parameter, ForJs("showBgColor"), SafeCategory("Appearance")]
    public bool ShowBgColor { get; set; } = true;

    [Parameter, ForJs("showFPS"), SafeCategory("Appearance")]
    public bool ShowFPS { get; set; } = false;

    [Parameter, ForJs("showPeaks"), SafeCategory("Appearance")]
    public bool ShowPeaks { get; set; } = true;

    [Parameter, ForJs("showScaleX"), SafeCategory("Appearance")]
    public bool ShowScaleX { get; set; } = true;

    [Parameter, ForJs("showScaleY"), SafeCategory("Appearance")]
    public bool ShowScaleY { get; set; } = false;

    [Parameter, ForJs("spinSpeed"), SafeCategory("Appearance")]
    public double SpinSpeed { get; set; } = 0;

    [Parameter, ForJs("splitGradient"), SafeCategory("Appearance")]
    public bool SplitGradient { get; set; } = false;

    [Parameter, ForJs("start"), SafeCategory("Appearance")]
    public bool IsActive { get; set; } = true;

    [Parameter, ForJs("trueLeds"), SafeCategory("Appearance")]
    public bool TrueLeds { get; set; } = false;

    [Parameter, ForJs("useCanvas"), SafeCategory("Appearance")]
    public bool UseCanvas { get; set; } = true;


    [Parameter, ForJs("colorMode"), SafeCategory("Appearance")]
    public ColorMode ColorMode { get; set; } = ColorMode.Gradient;

    [Parameter, ForJs("gradient"), SafeCategory("Appearance")]
    public AudioMotionGradient Gradient { get; set; } = AudioMotionGradient.Classic;

    [Parameter, ForJs("alphaBars"), SafeCategory("Appearance")]
    public bool AlphaBars { get; set; } = false;

    [Parameter, ForJs("ansiBands"), SafeCategory("Appearance")]
    public bool AnsiBands { get; set; } = false;

    [Parameter, ForJs("barSpace"), SafeCategory("Appearance")]
    public double BarSpacing { get; set; } = 0.1;

    [Parameter, ForJs("bgAlpha"), SafeCategory("Appearance")]
    public double BgAlpha { get; set; } = 0.7;

    [Parameter, ForJs("fillAlpha"), SafeCategory("Appearance")]
    public double FillAlpha { get; set; } = 1;

    [Parameter, ForJs("ledBars"), SafeCategory("Appearance")]
    public bool LedBars { get; set; } = false;

    [Parameter, ForJs("linearAmplitude"), SafeCategory("Appearance")]
    public bool LinearAmplitude { get; set; } = false;

    [Parameter, ForJs("linearBoost"), SafeCategory("Appearance")]
    public double LinearBoost { get; set; } = 1;

    [Parameter, ForJs("lineWidth"), SafeCategory("Appearance")]
    public int LineWidth { get; set; } = 0;

    [Parameter, ForJs("loRes"), SafeCategory("Appearance")]
    public bool LoRes { get; set; } = false;

    [Parameter, ForJs("lumiBars"), SafeCategory("Appearance")]
    public bool LumiBars { get; set; } = false;

    [Parameter, ForJs("mirror"), SafeCategory("Appearance")]
    public MirrorMode Mirror { get; set; } = MirrorMode.None;

    [Parameter, ForJs("noteLabels"), SafeCategory("Appearance")]
    public bool NoteLabels { get; set; } = false;

    [Parameter, ForJs("outlineBars"), SafeCategory("Appearance")]
    public bool OutlineBars { get; set; } = false;

    [Parameter, ForJs("overlay"), SafeCategory("Appearance")]
    public bool Overlay { get; set; } = false;

    [Parameter, ForJs("peakLine"), SafeCategory("Appearance")]
    public bool PeakLine { get; set; } = false;

    [Parameter, ForJs("radial"), SafeCategory("Appearance")]
    public bool Radial { get; set; } = false;

    [Parameter, ForJs("reflexAlpha"), SafeCategory("Appearance")]
    public double ReflexAlpha { get; set; } = 0.15;

    [Parameter, ForJs("reflexBright"), SafeCategory("Appearance")]
    public double ReflexBright { get; set; } = 1;

    [Parameter, ForJs("reflexFit"), SafeCategory("Appearance")]
    public bool ReflexFit { get; set; }


    #endregion


    /// <inheritdoc />
    public bool CanHandleFile(IMudExFileDisplayInfos fileDisplayInfos) =>
        MimeType.AudioTypes.Contains(fileDisplayInfos.ContentType, StringComparer.InvariantCultureIgnoreCase) || MimeType.Matches(fileDisplayInfos.ContentType, "audio/*");

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var updateRequired = (parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var fileDisplayInfos) && (FileDisplayInfos != fileDisplayInfos || (!string.IsNullOrEmpty(fileDisplayInfos.Url) && fileDisplayInfos.Url != Src)));
        await base.SetParametersAsync(parameters);
        if (updateRequired || Src == null)
        {
            try
            {
                Src = fileDisplayInfos?.Url ?? await FileService.CreateDataUrlAsync(fileDisplayInfos?.ContentStream?.ToByteArray() ?? throw new ArgumentNullException("No stream and no url available"), fileDisplayInfos.ContentType, MudExFileDisplay == null || MudExFileDisplay.StreamUrlHandling == StreamUrlHandling.BlobUrl);
                ContentType = fileDisplayInfos?.ContentType;
            }
            catch (Exception e)
            {
                MudExFileDisplay?.ShowError(e.Message);
            }
            StateHasChanged();
        }
    }

    private async Task UpdateJsOptions()
    {
        if (JsReference != null)
            await JsReference.InvokeVoidAsync("setOptions", JsOptions());
    }

    protected override async Task OnJsOptionsChanged()
    {
        await UpdateJsOptions();
    }

    private object JsOptions()
    {
        return this.AsJsObject(new
        {
            audioMotion = _audioMotion,
            audioElements = _audioElements.ToArray(),
            visualizer = _visualizer
        });
    }

    /// <summary>
    /// Gets the JavaScript arguments to pass to the component.
    /// </summary>
    public override object[] GetJsArguments() => new[] { ElementReference, CreateDotNetObjectReference(), JsOptions() };

    /// <inheritdoc />
    public override async Task ImportModuleAndCreateJsAsync()
    {
        if(!await JsRuntime.IsNamespaceAvailableAsync("AudioMotionAnalyzer"))
        {
            //_audioMotion = await JsRuntime.ImportModuleAsync(JsImportHelper.JsPath("/js/libs/audioMotion-analyzer.min.js"));
            _audioMotion = await JsRuntime.ImportModuleAsync("https://cdn.skypack.dev/audiomotion-analyzer?min");
        }
        await base.ImportModuleAndCreateJsAsync();
    }


    private string AudioElementStyleStr()
    {
        return MudExStyleBuilder.Default
            .WithMargin("20px")
            .WithPosition(Core.Css.Position.Absolute)
            .WithBorderRadius("27px")
            .WithBottom(0)
            .WithBorderWidth(AudioElementBorderSize)
            .WithBorderStyle(AudioElementBorderStyle)
            .WithBorderColor(AudioElementBorderColor, !AudioElementBorderColor.Is(Color.Inherit) && !AudioElementBorderColor.Is(Color.Default) && !(AudioElementBorderColor.Is(Color.Transparent) && AnimateAudioElementBorderColor))
            .WithAnimatedGradientBorder("10px", AudioElementBackgroundColor, new []{MudExColor.Primary, MudExColor.Secondary, MudExColor.Warning, MudExColor.Error, }, AnimateAudioElementBorderColor)
            .Build();
    }
}