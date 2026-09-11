namespace MudBlazor.Extensions.Core.FileManager;

/// <summary>
/// The panels a <see cref="Components.MudExFileManager"/> shows.
/// </summary>
/// <remarks>
/// Which panels exist is decided up front by this flag set, not at runtime. That is deliberate: the dock
/// layout adopts the rendered DOM nodes, so panels must be declared in a stable order and may only ever be
/// appended. A panel whose flag is set but which has nothing to show renders a placeholder instead of
/// disappearing - see <see cref="Components.MudExFileManager"/> for why.
/// </remarks>
[Flags]
public enum MudExFileManagerPanels
{
    /// <summary>Only the file area.</summary>
    None = 0,

    /// <summary>The structure tree beside the file area.</summary>
    Tree = 1 << 0,

    /// <summary>The file area itself. Included in <see cref="All"/> for symmetry; it always renders.</summary>
    Files = 1 << 1,

    /// <summary>A preview of the selected file, rendered with <see cref="Components.MudExFileDisplay"/>.</summary>
    Preview = 1 << 2,

    /// <summary>Metadata of the selected entry, rendered with <see cref="Components.MudExFileMetaView"/>.</summary>
    Meta = 1 << 3,

    /// <summary>Everything.</summary>
    All = Tree | Files | Preview | Meta
}

/// <summary>
/// How the file area presents an entry.
/// </summary>
public enum MudExFileManagerPreviewContent
{
    /// <summary>An icon plus the entry's metadata. Cheap, and always usable.</summary>
    Icon,

    /// <summary>
    /// The file itself: the picture for an image, the first lines for anything textual, an icon for the rest.
    /// Costs one read per entry, so it is capped by
    /// <see cref="Components.MudExFileManager.MaxContentPreviews"/>.
    /// </summary>
    Content
}
