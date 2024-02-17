using AuralizeBlazor;
using AuralizeBlazor.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Css;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Blazor wrapper component for AudioMotionAnalyzer
/// </summary>
public partial class MudExAudioPlayer : IMudExFileDisplay, IMudExComponent
{
    const string CATEGORY_PLAYER = "Player";
    bool _mouseWheelNeedsAlt;
    private string _id = $"mud-ex-audio-player-{Guid.NewGuid().ToFormattedId()}";
    private ElementReference _visualizer;
    private IJSObjectReference _audioMotion;
    private ElementReference _audioElement;

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
    /// If true the audio player will be shown and hidden automatically depending on mouse hover
    /// </summary>
    [Parameter, SafeCategory(CATEGORY_PLAYER)] public bool AutoShowHideAudioElement { get; set; } = true;

    /// <summary>
    /// The file display infos
    /// </summary>
    [Parameter, IgnoreOnObjectEdit] public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Cascading parameter to get the parent MudExFileDisplay if present
    /// </summary>
    [CascadingParameter, IgnoreOnObjectEdit] public MudExFileDisplay MudExFileDisplay { get; set; }

    [Parameter, SafeCategory(CATEGORY_PLAYER)] public AnimateBorderColor AnimateAudioElementBorderColor { get; set; } = AnimateBorderColor.OnPlay;
    [Parameter, SafeCategory(CATEGORY_PLAYER)] public AnimateBorderColor AnimateVisualizerBorderColor { get; set; } = AnimateBorderColor.OnPlay;
    [Parameter, SafeCategory(CATEGORY_PLAYER)] public MudExColor AudioElementBorderColor { get; set; } = MudExColor.Info;

    /// <summary>
    /// Set the audio element background color use null to use color from visualizer gradient
    /// </summary>
    [Parameter, SafeCategory(CATEGORY_PLAYER)] public MudExColor AudioElementBackgroundColor { get; set; }
    [Parameter, SafeCategory(CATEGORY_PLAYER)] public MudExColor VisualizerBackgroundColor { get; set; }

    [Parameter, SafeCategory(CATEGORY_PLAYER)] public BorderStyle AudioElementBorderStyle { get; set; } = BorderStyle.Solid;
    [Parameter, SafeCategory(CATEGORY_PLAYER)] public BorderStyle VisualizerElementBorderStyle { get; set; } = BorderStyle.Solid;
    [Parameter, SafeCategory(CATEGORY_PLAYER)] public MudExSize<double> AudioElementWidth { get; set; } = "80%";
    [Parameter, SafeCategory(CATEGORY_PLAYER)] public MudExSize<double> AudioElementBorderSize { get; set; } = "2px";
    [Parameter, SafeCategory(CATEGORY_PLAYER)] public MudExSize<double> VisualizerElementBorderSize { get; set; } = "3px";



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
                if (MudExFileDisplay != null)
                    ShowMessage(fileDisplayInfos.FileName);
            }
            catch (Exception e)
            {
                MudExFileDisplay?.ShowError(e.Message);
            }
            StateHasChanged();
        }
    }

    protected override void HandleIsPlayingChanged(bool value)
    {
        base.HandleIsPlayingChanged(value);
        StateHasChanged();
    }

    private MudExColor _bg = MudExColor.Surface;
    MudExColor[] _borderColors = new[] { MudExColor.Primary, MudExColor.Secondary, MudExColor.Warning, MudExColor.Error, };
    protected override void HandleOnGradientChanged(AudioMotionGradient value)
    {
        base.HandleOnGradientChanged(value);
        _bg = !string.IsNullOrEmpty(value.BgColor) ? new MudExColor(value.BgColor) : MudExColor.Surface;
        _borderColors = value.ColorStops.Select(s => new MudExColor(s.Color)).ToArray();
        StateHasChanged();
    }

    protected override void HandleOnPresetApplied(AuralizerPreset preset)
    {
        base.HandleOnPresetApplied(preset);
        if (MudExFileDisplay != null)
            ApplyInitialSettingsForFileView(false);
    }

    private void ApplyInitialSettingsForFileView(bool withPreset)
    {
        if (withPreset)
        {
            InitialPreset = AuralizerPreset.BarkScaleLinearAmplitude;
            Presets = AuralizerPreset.All;
        }

        Height = Width = "100%";
        ShowBgColor = true;
        ShowScaleX = false;
        ShowScaleY = true;
        _mouseWheelNeedsAlt = true;
    }

    protected override Task HandleMouseWheel(WheelEventArgs arg)
    {
        if ((_mouseWheelNeedsAlt && arg.AltKey) || !_mouseWheelNeedsAlt)
        {
            return base.HandleMouseWheel(arg);
        }
        return Task.CompletedTask;
    }

    protected override void OnInitialized()
    {
        Presets = AuralizerPreset.All;

        ClickAction = VisualizerAction.TogglePlayPause;
        DoubleClickAction = VisualizerAction.ToggleFullscreen;
        ContextMenuAction = VisualizerAction.TogglePictureInPicture;
        
        if (MudExFileDisplay != null)
            ApplyInitialSettingsForFileView(true);
        ChildContent = Player();
        base.OnInitialized();
    }

    private MudExColor BgFor(MudExColor c, MudExColor fallback)
    {
        if (c.Is(Color.Inherit) || c.Is(Color.Default) || c.Is(Color.Transparent))
        {
            return fallback;
        }

        return c;
    }

    private string AudioElementStyleStr()
    {
        var animate = (IsPlaying && AnimateAudioElementBorderColor == AnimateBorderColor.OnPlay) || AnimateAudioElementBorderColor == AnimateBorderColor.Always;
        return MudExStyleBuilder.Default
            .WithPosition(Core.Css.Position.Relative)
            .WithBorderRadius("27px")
            .WithBottom(65)
            .WithWidth(AudioElementWidth)
            .WithMarginLeft("auto")
            .WithMarginRight("auto")
            .WithBorderWidth(AudioElementBorderSize)
            .WithBorderStyle(AudioElementBorderStyle)
            .WithBorderColor(AudioElementBorderColor, !animate)
            .WithAnimatedGradientBorder("10px", BgFor(AudioElementBackgroundColor, _bg), _borderColors, animate)
            .Build();
    }    
    

    private string ContainerStyle()
    {
        var animate = (IsPlaying && AnimateVisualizerBorderColor == AnimateBorderColor.OnPlay) || AnimateVisualizerBorderColor == AnimateBorderColor.Always;
        return MudExStyleBuilder.Default
            .WithAnimatedGradientBorder(VisualizerElementBorderSize, BgFor(VisualizerBackgroundColor, _bg), _borderColors, animate)
            .WithBackgroundColor(BgFor(VisualizerBackgroundColor, _bg), !animate)
            .WithHeight(Height)
            .WithWidth(Width)
            .Build();
    }
}