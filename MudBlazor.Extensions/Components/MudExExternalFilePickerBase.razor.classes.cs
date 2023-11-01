
namespace MudBlazor.Extensions.Components;


/// <summary>
/// Initial render behavior controls rendering while initialization running
/// </summary>
public enum FilePickerInitialRenderBehaviour
{
    /// <summary>
    /// Render normal user can click while not initialized and action is then waiting for initialization
    /// </summary>
    Normal,

    /// <summary>
    /// Render loading while not initialized
    /// </summary>
    Loading,

    /// <summary>
    /// Hide while not initialized
    /// </summary>
    Hidden
}

/// <summary>
/// View Mode of the picker action
/// </summary>
public enum PickerActionViewMode
{
    /// <summary>
    /// Use simple MudButton to render the action
    /// </summary>
    Button,

    /// <summary>
    /// Use image to render the action
    /// </summary>
    Image,

    /// <summary>
    /// ChildContent is used directly to render the action
    /// </summary>
    Custom
}
