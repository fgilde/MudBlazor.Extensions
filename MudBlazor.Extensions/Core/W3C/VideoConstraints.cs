namespace MudBlazor.Extensions.Core.W3C;

/// <summary>
/// Represents constraints for capturing video, including settings like resolution, frame rate, and device-specific properties.
/// </summary>
public class VideoConstraints
{
    /// <summary>
    /// Gets or sets the device ID for the video source.
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the group ID for the video source.
    /// </summary>
    public string GroupId { get; set; }

    /// <summary>
    /// Gets or sets the display surface type (e.g., monitor, window).
    /// </summary>
    public CaptureDisplaySurface? DisplaySurface { get; set; }

    /// <summary>
    /// Indicates whether to capture the logical surface of the display.
    /// </summary>
    public bool? LogicalSurface { get; set; }

    /// <summary>
    /// Specifies how the cursor should be captured (e.g., always, motion only).
    /// </summary>
    public CaptureCursor? Cursor { get; set; }

    /// <summary>
    /// Gets or sets the desired width of the video.
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    /// Gets or sets the desired height of the video.
    /// </summary>
    public double? Height { get; set; }

    /// <summary>
    /// Gets or sets the desired aspect ratio of the video.
    /// </summary>
    public double? AspectRatio { get; set; }

    /// <summary>
    /// Gets or sets the desired frame rate of the video.
    /// </summary>
    public double? FrameRate { get; set; }

    /// <summary>
    /// Specifies the facing mode (e.g., user-facing or environment-facing).
    /// </summary>
    public CaptureFacingMode? FacingMode { get; set; }

    /// <summary>
    /// Specifies the resize mode for the video (e.g., crop and scale).
    /// </summary>
    public CaptureResizeMode? ResizeMode { get; set; }

    /// <summary>
    /// Gets or sets the desired brightness level for the video.
    /// </summary>
    public double? Brightness { get; set; }

    /// <summary>
    /// Gets or sets the desired color temperature for the video.
    /// </summary>
    public double? ColorTemperature { get; set; }

    /// <summary>
    /// Gets or sets the desired contrast level for the video.
    /// </summary>
    public double? Contrast { get; set; }

    /// <summary>
    /// Gets or sets the desired saturation level for the video.
    /// </summary>
    public double? Saturation { get; set; }

    /// <summary>
    /// Gets or sets the desired sharpness level for the video.
    /// </summary>
    public double? Sharpness { get; set; }

    /// <summary>
    /// Gets or sets the exposure compensation for the video.
    /// </summary>
    public double? ExposureCompensation { get; set; }

    /// <summary>
    /// Specifies the exposure mode (e.g., manual, single, continuous).
    /// </summary>
    public CaptureExposureMode? ExposureMode { get; set; }

    /// <summary>
    /// Gets or sets the desired exposure time for the video.
    /// </summary>
    public double? ExposureTime { get; set; }

    /// <summary>
    /// Gets or sets the ISO sensitivity for the video.
    /// </summary>
    public double? ISO { get; set; }

    /// <summary>
    /// Specifies the focus mode (e.g., manual, continuous).
    /// </summary>
    public CaptureFocusMode? FocusMode { get; set; }

    /// <summary>
    /// Gets or sets the focus distance for the video.
    /// </summary>
    public double? FocusDistance { get; set; }

    /// <summary>
    /// Gets or sets points of interest for the camera to focus on.
    /// </summary>
    public CapturePoint[] PointsOfInterest { get; set; }

    /// <summary>
    /// Gets or sets the pan level for the camera.
    /// </summary>
    public double? Pan { get; set; }

    /// <summary>
    /// Gets or sets the tilt level for the camera.
    /// </summary>
    public double? Tilt { get; set; }

    /// <summary>
    /// Gets or sets the zoom level for the camera.
    /// </summary>
    public double? Zoom { get; set; }

    /// <summary>
    /// Indicates whether the camera's torch (flashlight) is enabled.
    /// </summary>
    public bool? Torch { get; set; }

    /// <summary>
    /// Specifies the white balance mode (e.g., manual, continuous).
    /// </summary>
    public CaptureWhiteBalanceMode? WhiteBalanceMode { get; set; }
}