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

    /// <summary>
    /// Saves a copy in the given format. The document, its file and its dirty state stay unchanged.
    /// </summary>
    /// <param name="format">Target format, usually one of <see cref="SaveFormats"/></param>
    /// <returns>True if the copy was saved</returns>
    Task<bool> SaveCopyAsync(MudExFileFormat format);

    /// <summary>
    /// Name of the current document
    /// </summary>
    string FileName { get; }

    /// <summary>
    /// MIME type of the current document
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Raised after the document or its file changed: loaded, opened, created or saved
    /// </summary>
    event Action DocumentChanged;

    /// <summary>
    /// Size in bytes of the current document when it was loaded or last saved, or null if unknown
    /// </summary>
    long? FileSize { get; }

    /// <summary>
    /// If true the user may open a local file into the editor
    /// </summary>
    bool AllowOpen { get; }

    /// <summary>
    /// If true the user may replace the content with a new empty document
    /// </summary>
    bool AllowNew { get; }

    /// <summary>
    /// Lets the user pick a local file and loads it. Unsaved changes are confirmed first. Saving afterwards overwrites the picked file.
    /// </summary>
    /// <returns>True if a file was opened</returns>
    Task<bool> OpenAsync();

    /// <summary>
    /// Replaces the content with a new empty document. Unsaved changes are confirmed first.
    /// </summary>
    /// <returns>True if a new document was created</returns>
    Task<bool> NewAsync();
}
