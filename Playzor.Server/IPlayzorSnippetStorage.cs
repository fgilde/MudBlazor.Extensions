namespace Playzor.Server;

/// <summary>
/// Server side storage behind the snippet endpoints. A snippet is a zip archive, one entry per
/// file — the same shape the editor sends and expects.
/// </summary>
public interface IPlayzorSnippetStorage
{
    /// <summary>Stores a snippet archive and returns the id it can be read back with.</summary>
    Task<string> SaveAsync(Stream zipArchive, CancellationToken cancellationToken = default);

    /// <summary>Reads a stored snippet archive, or null when there is none with that id.</summary>
    Task<Stream?> LoadAsync(string snippetId, CancellationToken cancellationToken = default);

    /// <summary>Names of the ready made samples.</summary>
    Task<IReadOnlyList<string>> GetSampleNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads a sample archive, or null when there is no sample with that name.</summary>
    Task<Stream?> LoadSampleAsync(string sampleName, CancellationToken cancellationToken = default);
}
