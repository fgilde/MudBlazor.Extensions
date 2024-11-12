using System.Text.Json.Serialization;
using MudBlazor.Extensions.Helper.JsonConverter;

namespace MudBlazor.Extensions.Core.W3C;

/// <summary>
/// Specifies voice isolation modes for audio capture.
/// </summary>
[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureVoiceIsolationMode
{
    /// <summary>
    /// No voice isolation.
    /// </summary>
    Off,

    /// <summary>
    /// Standard voice isolation.
    /// </summary>
    Standard,

    /// <summary>
    /// High level of voice isolation.
    /// </summary>
    High
}