using System.Text.Json.Serialization;
using MudBlazor.Extensions.Helper.JsonConverter;

namespace MudBlazor.Extensions.Core.W3C;

/// <summary>
/// Specifies the facing mode for the camera (e.g., front-facing or rear-facing).
/// </summary>
[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureFacingMode
{
    /// <summary>
    /// The camera is user-facing.
    /// </summary>
    User,

    /// <summary>
    /// The camera is environment-facing (e.g., rear camera).
    /// </summary>
    Environment,

    /// <summary>
    /// The camera faces the left side.
    /// </summary>
    Left,

    /// <summary>
    /// The camera faces the right side.
    /// </summary>
    Right
}