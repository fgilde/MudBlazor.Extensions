namespace MudBlazor.Extensions.Options;

/// <summary>
/// Item selection mode for multi or single select
/// </summary>
public enum ItemSelectionMode
{
    /// <summary>
    /// No selection is allowed.
    /// </summary>
    None,

    /// <summary>
    /// Only a single item can be selected at a time.
    /// </summary>
    Single,

    /// <summary>
    /// Multiple items can be selected without requiring any modifier key.
    /// </summary>
    MultiSelect,

    /// <summary>
    /// Multiple items can be selected, but requires the Ctrl key to be pressed during selection.
    /// </summary>
    MultiSelectWithCtrlKey
}
