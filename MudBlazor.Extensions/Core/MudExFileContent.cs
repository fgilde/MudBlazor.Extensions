namespace MudBlazor.Extensions.Core;

/// <summary>
/// Describes a file format an editor can save to.
/// </summary>
/// <param name="Name">Display name of the format</param>
/// <param name="Extension">File extension of the format without the leading dot</param>
/// <param name="ContentType">MIME type of the format</param>
public sealed record MudExFileFormat(string Name, string Extension, string ContentType);

/// <summary>
/// The content of a file that is about to be saved. Disposing it disposes the stream.
/// </summary>
public sealed class MudExFileContent : IAsyncDisposable
{
    /// <summary>
    /// Name of the file
    /// </summary>
    public string FileName { get; init; }

    /// <summary>
    /// MIME type of the file
    /// </summary>
    public string ContentType { get; init; }

    /// <summary>
    /// Stream with the file content
    /// </summary>
    public Stream Stream { get; init; }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Stream?.DisposeAsync() ?? ValueTask.CompletedTask;
}

/// <summary>
/// Where a file ended up after saving.
/// </summary>
public enum MudExFileSaveTarget
{
    /// <summary>
    /// Saved by the host application
    /// </summary>
    Host,

    /// <summary>
    /// Written to a local file picked by the user
    /// </summary>
    Local,

    /// <summary>
    /// Handed to the browser as a download
    /// </summary>
    Download
}

/// <summary>
/// Result of saving a file locally.
/// </summary>
/// <param name="HandleId">Id of the local file handle for later saves, or null when no handle is kept (download)</param>
/// <param name="FileName">Name of the saved file</param>
/// <param name="Target">Where the file was saved</param>
public sealed record MudExLocalFileSaveResult(string HandleId, string FileName, MudExFileSaveTarget Target);

internal sealed record MudExLocalFileSaveDto(string HandleId, string FileName, string Target);
