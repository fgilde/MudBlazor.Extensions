namespace MudBlazor.Extensions.Core.FileManager;

/// <summary>
/// What a <see cref="IMudExFileStructureManager"/> is allowed to do right now.
/// </summary>
/// <remarks>
/// Separate from <see cref="IMudExFileStructureWriter"/> on purpose. The interface says what the
/// implementation is able to do, these flags say what is permitted at runtime - a local folder that the user
/// only granted read access to implements writing but must not offer it.
/// </remarks>
[Flags]
public enum MudExFileManagerCapabilities
{
    /// <summary>Nothing, not even reading.</summary>
    None = 0,
    /// <summary>Enumerate the structure and read file content.</summary>
    Read = 1 << 0,
    /// <summary>Create a new directory.</summary>
    CreateDirectory = 1 << 1,
    /// <summary>Rename a file or directory.</summary>
    Rename = 1 << 2,
    /// <summary>Move files or directories into another directory.</summary>
    Move = 1 << 3,
    /// <summary>Delete files or directories.</summary>
    Delete = 1 << 4,
    /// <summary>Add new files to a directory.</summary>
    Upload = 1 << 5,
    /// <summary>
    /// Offer a download of a file. Has no method of its own, it is served by
    /// <see cref="IMudExFileStructureManager.OpenReadAsync"/> and only decides whether the action is shown.
    /// </summary>
    Download = 1 << 6,

    /// <summary>Everything.</summary>
    All = Read | CreateDirectory | Rename | Move | Delete | Upload | Download
}
