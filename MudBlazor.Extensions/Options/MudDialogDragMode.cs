namespace MudBlazor.Extensions
{
    /// <summary>
    /// Defines the dragging modes for a Mud Dialog.
    /// </summary>
    public enum MudDialogDragMode
    {
        /// <summary>
        /// No dragging is allowed.
        /// </summary>
        None,

        /// <summary>
        /// Simple dragging mode, allowing basic move operations.
        /// </summary>
        Simple,

        /// <summary>
        /// Dragging mode allowing movement without restricting to certain bounds.
        /// </summary>
        WithoutBounds,

        /// <summary>
        /// Snap dragging mode (not currently implemented).
        /// </summary>
        [Obsolete("Not finished yet. Will be implemented later")]
        SnapDrag
    }

}