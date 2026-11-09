using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A file display that can edit and save the file it shows.
/// </summary>
public interface IMudExFileEditor : IMudExFileDisplay
{
    /// <summary>
    /// If true the content cannot be changed
    /// </summary>
    bool ReadOnly { get; set; }

    /// <summary>
    /// True if there are unsaved changes
    /// </summary>
    bool IsDirty { get; }

    /// <summary>
    /// Formats the editor can save to. The first one is the original format of the file.
    /// </summary>
    IReadOnlyList<MudExFileFormat> SaveFormats { get; }

    /// <summary>
    /// Should return true if this editor can edit a file with the given information
    /// </summary>
    /// <param name="fileDisplayInfos">Infos of the file</param>
    /// <param name="fileService">The file service</param>
    Task<bool> CanEditFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService) => CanHandleFileAsync(fileDisplayInfos, fileService);

    /// <summary>
    /// Exports the current content
    /// </summary>
    /// <param name="format">Target format, or null for the original format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<MudExFileContent> ExportAsync(MudExFileFormat format = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the current content
    /// </summary>
    /// <param name="saveAs">If true the user is asked for a new target</param>
    /// <returns>True if the content was saved</returns>
    Task<bool> SaveAsync(bool saveAs = false);
}
