using System.Security.Cryptography;

namespace Playzor.Server;

/// <summary>
/// Snippet storage on the local disk: one zip per snippet, plus a folder of sample archives.
/// Enough for a self hosted playground; swap it for blob storage when you need one.
/// </summary>
public class FilePlayzorSnippetStorage : IPlayzorSnippetStorage
{
    private readonly string _snippetDirectory;
    private readonly string _sampleDirectory;

    /// <summary>Creates the storage. Both directories are created on demand.</summary>
    /// <param name="snippetDirectory">Where saved snippets go.</param>
    /// <param name="sampleDirectory">Where the sample archives live, may be null.</param>
    public FilePlayzorSnippetStorage(string snippetDirectory, string? sampleDirectory = null)
    {
        _snippetDirectory = snippetDirectory ?? throw new ArgumentNullException(nameof(snippetDirectory));
        _sampleDirectory = sampleDirectory ?? Path.Combine(snippetDirectory, "samples");
    }

    /// <inheritdoc />
    public async Task<string> SaveAsync(Stream zipArchive, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_snippetDirectory);

        var id = NewId();
        await using var file = File.Create(Path.Combine(_snippetDirectory, id + ".zip"));
        await zipArchive.CopyToAsync(file, cancellationToken);
        return id;
    }

    /// <inheritdoc />
    public Task<Stream?> LoadAsync(string snippetId, CancellationToken cancellationToken = default)
        => Task.FromResult(Open(_snippetDirectory, snippetId));

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetSampleNamesAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_sampleDirectory))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        IReadOnlyList<string> names = Directory.GetFiles(_sampleDirectory, "*.zip")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => n != null)
            .Select(n => n!)
            .OrderBy(n => n)
            .ToArray();

        return Task.FromResult(names);
    }

    /// <inheritdoc />
    public Task<Stream?> LoadSampleAsync(string sampleName, CancellationToken cancellationToken = default)
        => Task.FromResult(Open(_sampleDirectory, sampleName));

    private static Stream? Open(string directory, string name)
    {
        // the name comes from a url: keep it inside the directory
        var safeName = Path.GetFileName(name ?? string.Empty);
        if (string.IsNullOrWhiteSpace(safeName))
            return null;

        var path = Path.Combine(directory, Path.ChangeExtension(safeName, ".zip"));
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    private static string NewId()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
}
