using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core.Capture;
using MudBlazor.Extensions.Core.W3C;
using OneOf;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// Service for capturing the screen or camera and audio.
/// </summary>
public interface ICaptureService
{
    /// <summary>
    /// Shows a dialog where the user can specify or edit the capture options.
    /// </summary>
    Task<CaptureOptions> EditCaptureOptionsAsync(CaptureOptionsEditMode editMode, CaptureOptions? options = null);
    
    /// <summary>
    /// Starts capturing the screen or camera and audio.
    /// </summary>
    /// <returns>Id to stop recording</returns>
    Task<CaptureId> StartCaptureAsync(CaptureOptions options, Action<CaptureResult> callback, Action<CaptureId> stoppedCallback = null);

    /// <summary>
    /// Stops the preview capture for the specified track.
    /// </summary>
    Task StopCaptureAsync(MediaStreamTrack track);

    /// <summary>
    /// Stops the capture with the specified ID.
    /// </summary>
    /// <param name="captureId">The id for the capture recording to stop</param>
    /// <returns></returns>
    Task StopCaptureAsync(CaptureId captureId);

    /// <summary>
    /// Stops all captures.
    /// </summary>
    /// <returns></returns>
    Task StopAllCapturesAsync();

    /// <summary>
    /// Gets the available audio devices.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<AudioDevice>> GetAudioDevicesAsync();

    /// <summary>
    /// Gets the available video devices.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<VideoDevice>> GetVideoDevicesAsync();

    /// <summary>
    /// Selects the capture source.
    /// </summary>
    /// <param name="displayMediaOptions">The media options for capturing</param>
    /// <param name="elementForPreview">Specify an element to show preview. This should be a ElementReference of a videoElement or a selector for a videoElement</param>
    /// <returns></returns>
    Task<MediaStreamTrack> SelectCaptureSourceAsync(DisplayMediaOptions? displayMediaOptions = null, OneOf<ElementReference, string> elementForPreview = default);
}