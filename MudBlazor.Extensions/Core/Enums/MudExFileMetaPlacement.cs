namespace MudBlazor.Extensions.Core.Enums;

/// <summary>
/// Where <see cref="Components.MudExFileMetaView"/> is rendered relative to the viewer content of
/// <see cref="Components.MudExFileDisplay"/> when <c>ShowFileMeta</c> is enabled.
/// </summary>
public enum MudExFileMetaPlacement
{
    /// <summary>
    /// The meta view is rendered below the viewer content.
    /// </summary>
    Below,

    /// <summary>
    /// The meta view is rendered above the viewer content.
    /// </summary>
    Above,

    /// <summary>
    /// The meta view is rendered to the right of the viewer content.
    /// </summary>
    Right
}
