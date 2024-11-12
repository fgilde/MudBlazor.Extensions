using System.Text.Json.Serialization;
using MudBlazor.Extensions.Helper.JsonConverter;

namespace MudBlazor.Extensions.Core.W3C;

/// <summary>
/// Specifies how the cursor should be captured during video recording.
/// </summary>
[JsonConverter(typeof(LowercaseEnumConverter))]
public enum CaptureCursor
{
    /// <summary>
    /// Always capture the cursor.
    /// </summary>
    Always,

    /// <summary>
    /// Capture the cursor only when it moves.
    /// </summary>
    Motion,

    /// <summary>
    /// Never capture the cursor.
    /// </summary>
    Never
}