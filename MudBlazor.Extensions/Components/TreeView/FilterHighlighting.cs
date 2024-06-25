namespace MudBlazor.Extensions.Components;

/// <summary>
/// Controls how the highlighting of the filter is displayed
/// </summary>
public enum FilterHighlighting
{
    /// <summary>
    /// Only the current active filter is highlighted
    /// </summary>
    ActiveFilterOnly,

    /// <summary>
    /// All filters are highlighted
    /// </summary>
    AllFilters,

    /// <summary>
    /// No highlighting at all
    /// </summary>
    None,
}