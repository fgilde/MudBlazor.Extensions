using System.Reflection;
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
using Nextended.Blazor.Models;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Blazor wrapper component for AudioMotionAnalyzer
/// </summary>
public partial class MudExAudioPlayer : IMudExFileDisplay, IMudExComponent
{
    private const string CategoryPlayer = "Player";
    private bool _mouseWheelNeedsAlt;
    private string _id = $"mud-ex-audio-player-{Guid.NewGuid().ToFormattedId()}";
    private MudExColor _bg = MudExColor.Surface;
    private MudExColor[] _borderColors = new[] { MudExColor.Primary, MudExColor.Secondary, MudExColor.Warning, MudExColor.Error };
    
    /// <summary>
    /// Reference to the audio element
    /// </summary>
    public ElementReference AudioElement { get; private set; }

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
    [Parameter, SafeCategory(CategoryPlayer)] public bool AutoShowHideAudioElement { get; set; } = true;

    /// <summary>
    /// The file display infos
    /// </summary>
    [Parameter, IgnoreOnObjectEdit] public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Cascading parameter to get the parent MudExFileDisplay if present
    /// </summary>
    [CascadingParameter, IgnoreOnObjectEdit] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <summary>
    /// Set animation for the audio element border color
    /// </summary>
    [Parameter, SafeCategory(CategoryPlayer)] public AnimateBorderColor AnimateAudioElementBorderColor { get; set; } = AnimateBorderColor.OnPlay;
    
    /// <summary>
    /// Set animation for the visualizer border color
    /// </summary>
    [Parameter, SafeCategory(CategoryPlayer)] public AnimateBorderColor AnimateVisualizerBorderColor { get; set; } = AnimateBorderColor.OnPlay;
    
    /// <summary>
    /// Set default audio element border color
    /// </summary>
    [Parameter, SafeCategory(CategoryPlayer)] public MudExColor AudioElementBorderColor { get; set; } = MudExColor.Info;

    /// <summary>
    /// Set the audio element background color use null to use color from visualizer gradient
    /// </summary>
    [Parameter, SafeCategory(CategoryPlayer)] public MudExColor AudioElementBackgroundColor { get; set; }
    
    /// <summary>
    /// Set the visualizer background color use null to use color from visualizer gradient
    /// </summary>
    [Parameter, SafeCategory(CategoryPlayer)] public MudExColor VisualizerBackgroundColor { get; set; }

    /// <summary>
    /// Audio element border style
    /// </summary>
    [Parameter, SafeCategory(CategoryPlayer)] public BorderStyle AudioElementBorderStyle { get; set; } = BorderStyle.Solid;
    
    /// <summary>
    /// Width of the audio element
    /// </summary>
    [Parameter, SafeCategory(CategoryPlayer)] public MudExSize<double> AudioElementWidth { get; set; } = "50%";
    
    /// <summary>
    /// Border size of the audio element
    /// </summary>
    [Parameter, SafeCategory(CategoryPlayer)] public MudExSize<double> AudioElementBorderSize { get; set; } = "2px";
    
    /// <summary>
    /// Border size of the visualizer element
    /// </summary>
    [Parameter, SafeCategory(CategoryPlayer)] public MudExSize<double> VisualizerElementBorderSize { get; set; } = "3px";



    /// <inheritdoc />
    public bool CanHandleFile(IMudExFileDisplayInfos fileDisplayInfos) =>
        MimeType.AudioTypes.Contains(fileDisplayInfos.ContentType, StringComparer.InvariantCultureIgnoreCase) || MimeType.Matches(fileDisplayInfos.ContentType, "audio/*");


    public async Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        await UpdateMetaInfos(null, true);
        if (Meta != null)
        {
            return new Dictionary<string, object>
            {
                { "Duration", Meta.Properties?.Duration.ToString() },
                { "AudioSampleRate", Meta.Properties?.AudioSampleRate },
                { "BitRate", Meta.Properties?.AudioBitrate },
                { "Channels", Meta.Properties?.AudioChannels },
                { "Codecs", string.Join(",", Meta.Properties?.Codecs?.Select(c => c.Description) ?? Array.Empty<string>()) },
                { "Title", Meta.Tag?.Title },
                { "Album", Meta.Tag?.Album },
                { "Year", Meta.Tag?.Year },
                { "Artists", Meta.Tag?.JoinedPerformers },
                { "AlbumArtists", Meta.Tag?.JoinedAlbumArtists },
                { "Genre", Meta.Tag?.JoinedGenres },
                { "Composers", Meta.Tag?.JoinedComposers },
            };
        }
        return null;
    }

    private byte[] _dataBytes = null;
    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var updateRequired = parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var fileDisplayInfos) && (FileDisplayInfos != fileDisplayInfos 
            || (!string.IsNullOrEmpty(fileDisplayInfos.Url) && fileDisplayInfos.Url != Src)
            || (fileDisplayInfos.ContentStream != null && fileDisplayInfos.ContentStream.Length != (_dataBytes?.Length ?? 0))
            );
        await base.SetParametersAsync(parameters);
        if (updateRequired || Src == null)
        {
            try
            {
                Meta = null;
                _dataBytes = fileDisplayInfos?.ContentStream?.ToByteArray();
                Src = fileDisplayInfos?.Url ?? await FileService.CreateDataUrlAsync(_dataBytes ?? throw new ArgumentException("No stream and no url available"), fileDisplayInfos.ContentType, MudExFileDisplay == null || MudExFileDisplay.StreamUrlHandling == StreamUrlHandling.BlobUrl);
                ContentType = fileDisplayInfos.ContentType;
                if (MudExFileDisplay != null)
                    ShowMessage(fileDisplayInfos.FileName);
            }
            catch (Exception e)
            {
                MudExFileDisplay?.ShowError(e.Message);
            }

            await InvokeAsync(StateHasChanged);
            _= Task.Delay(400).ContinueWith(_ => UpdateMetaInfos());
        }
    }
    
    /// <inheritdoc />
    protected override void HandleIsPlayingChanged(bool value)
    {
        base.HandleIsPlayingChanged(value);
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override void HandleOnGradientChanged(AudioMotionGradient value)
    {
        base.HandleOnGradientChanged(value);
        _bg = !string.IsNullOrEmpty(value.BgColor) ? new MudExColor(value.BgColor) : MudExColor.Surface;
        _borderColors = value.ColorStops.Select(s => new MudExColor(s.Color)).ToArray();
        StateHasChanged();
    }


    /// <inheritdoc />
    protected override void HandleOnPresetApplied(AuralizerPreset preset, PresetApplySettings settings)
    {        
        base.HandleOnPresetApplied(preset, settings);
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

        KeepState = true;
        ApplyBackgroundImageFromTrack = false;
        InitialRender = InitialRender.WithRandomData;

        Height = Width = "100%";
        ShowBgColor = true;
        ShowScaleX = false;
        ShowScaleY = true;
        _mouseWheelNeedsAlt = true;
    }

    /// <inheritdoc />
    protected override Task HandleMouseWheel(WheelEventArgs arg)
    {
        if ((_mouseWheelNeedsAlt && arg.AltKey) || !_mouseWheelNeedsAlt)
        {
            return base.HandleMouseWheel(arg);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
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