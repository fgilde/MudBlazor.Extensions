using System.Text.Json.Serialization;
using MudBlazor.Extensions.Helper.JsonConverter;

namespace MudBlazor.Extensions.Core.W3C;

/// <summary>
/// Defines options to include or exclude a feature.
/// </summary>
[JsonConverter(typeof(LowercaseEnumConverter))]
public enum IncludeExclude
{
    /// <summary>
    /// Include the feature.
    /// </summary>
    Include,

    /// <summary>
    /// Exclude the feature.
    /// </summary>
    Exclude
}