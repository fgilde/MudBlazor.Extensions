using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Core.W3C;

/// <summary>
/// Represents the options for capturing display media, including video and audio constraints, 
/// preferences for capturing the current tab, and settings for system audio and browser surfaces.
/// </summary>
public class DisplayMediaOptions
{
    /// <summary>
    /// Indicates whether the current tab is preferred for capture.
    /// </summary>
    public bool PreferCurrentTab { get; set; }

    /// <summary>
    /// Determines whether the self browser surface should be included or excluded during capture.
    /// </summary>
    public IncludeExclude? SelfBrowserSurface { get; set; }

    /// <summary>
    /// Determines whether the system audio should be included or excluded.
    /// </summary>
    public IncludeExclude? SystemAudio { get; set; }

    /// <summary>
    /// Determines whether surface switching is allowed.
    /// </summary>
    public IncludeExclude? SurfaceSwitching { get; set; }

    /// <summary>
    /// Determines whether monitor-type surfaces should be included or excluded.
    /// </summary>
    public IncludeExclude? MonitorTypeSurfaces { get; set; }
    /// <summary>
    /// Gets or sets the constraints for video capture.
    /// </summary>
    public VideoConstraints Video { get; set; }

    /// <summary>
    /// Gets or sets the constraints for audio capture.
    /// </summary>
    public AudioConstraints Audio { get; set; }

    #region Static factory

    /// <summary>
    /// Configures the options to include system audio.
    /// </summary>
    /// <returns>The updated <see cref="DisplayMediaOptions"/>.</returns>
    public DisplayMediaOptions WithSystemAudio()
    {
        return this.SetProperties(options =>
        {
            options.Audio = new AudioConstraints
            {
                SuppressLocalAudioPlayback = false,
                AutoGainControl = false,
                EchoCancellation = false,
                NoiseSuppression = false
            };
            options.SystemAudio = IncludeExclude.Include;
        });
    }

    /// <summary>
    /// Configures the options to exclude system audio.
    /// </summary>
    /// <returns>The updated <see cref="DisplayMediaOptions"/>.</returns>
    public DisplayMediaOptions WithoutSystemAudio()
    {
        return this.SetProperties(options =>
        {
            options.Audio = null;
            options.SystemAudio = IncludeExclude.Exclude;
        });
    }

    /// <summary>
    /// Provides default display media options.
    /// </summary>
    public static DisplayMediaOptions Default => new()
    {
        Video = new VideoConstraints
        {
            DisplaySurface = CaptureDisplaySurface.Browser
        },
        Audio = new AudioConstraints
        {
            SuppressLocalAudioPlayback = false,
            AutoGainControl = false,
            EchoCancellation = false,
            NoiseSuppression = false
        },
        PreferCurrentTab = false,
        SelfBrowserSurface = IncludeExclude.Include,
        SurfaceSwitching = IncludeExclude.Include,
        MonitorTypeSurfaces = IncludeExclude.Include
    };

    /// <summary>
    /// Provides display media options without audio.
    /// </summary>
    public static DisplayMediaOptions WithoutAudio => Default.WithoutSystemAudio();

    /// <summary>
    /// Provides display media options configured for best quality.
    /// </summary>
    public static DisplayMediaOptions BestQuality => new()
    {
        Video = new VideoConstraints
        {
            DisplaySurface = CaptureDisplaySurface.Browser,
            Cursor = CaptureCursor.Always,
            LogicalSurface = true,
            Width = 3840,
            Height = 2160,
            FrameRate = 60.0
        },
        Audio = new AudioConstraints
        {
            SuppressLocalAudioPlayback = false,
            AutoGainControl = false,
            EchoCancellation = false,
            NoiseSuppression = false
        },
        SystemAudio = IncludeExclude.Include
    };

    /// <summary>
    /// Provides display media options optimized for low bandwidth usage.
    /// </summary>
    public static DisplayMediaOptions LowBandwidth => new()
    {
        Video = new VideoConstraints
        {
            DisplaySurface = CaptureDisplaySurface.Monitor,
            Cursor = CaptureCursor.Always,
            LogicalSurface = true,
            Width = 640,
            Height = 480,
            FrameRate = 15.0
        }
    };

    /// <summary>
    /// Provides display media options to capture the current tab with audio.
    /// </summary>
    public static DisplayMediaOptions CurrentTabWithAudio => new()
    {
        Video = new VideoConstraints
        {
            DisplaySurface = CaptureDisplaySurface.Browser,
            Cursor = CaptureCursor.Always,
            LogicalSurface = true
        },
        Audio = new AudioConstraints
        {
            EchoCancellation = true,
            AutoGainControl = true,
            NoiseSuppression = true
        },
        PreferCurrentTab = true,
        SystemAudio = IncludeExclude.Include
    };

    #endregion
}