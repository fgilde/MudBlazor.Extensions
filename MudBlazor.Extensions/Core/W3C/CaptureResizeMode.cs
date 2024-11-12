using System.Text.Json.Serialization;
using MudBlazor.Extensions.Helper.JsonConverter;

namespace MudBlazor.Extensions.Core.W3C;

/// <summary>
/// Defines resize modes for video capture.
/// </summary>
[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureResizeMode
{
    /// <summary>
    /// No specific resize mode.
    /// </summary>
    None,

    /// <summary>
    /// Crop the captured video and scale it to fit.
    /// </summary>
    CropAndScale
}