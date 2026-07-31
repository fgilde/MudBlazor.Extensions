using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Playzor.Core.Api;

/// <summary>
/// Snippet store that talks to the endpoints of the Playzor.Server package
/// (<c>MapPlayzorApi()</c>). Snippets travel as zip archives, one entry per file.
/// </summary>
public class PlayzorHttpSnippetStore : IPlayzorSnippetStore
{
    private readonly HttpClient _httpClient;
    private readonly PlayzorApiOptions _options;

    /// <summary>Creates the store for the given routes.</summary>
    public PlayzorHttpSnippetStore(HttpClient httpClient, PlayzorApiOptions options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? new PlayzorApiOptions();
    }

    /// <inheritdoc />
    public async Task<string> SaveAsync(IEnumerable<CodeFile> files, CancellationToken cancellationToken = default)
    {
        using var content = new StreamContent(ToZip(files));
        using var response = await _httpClient.PostAsync(_options.SnippetUrl(), content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadAsStringAsync(cancellationToken)).Trim('"');
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CodeFile>> LoadAsync(string snippetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(snippetId)) throw new ArgumentException("Snippet id is required.", nameof(snippetId));

        using var response = await _httpClient.GetAsync(_options.SnippetUrl(snippetId), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await FromZipAsync(await response.Content.ReadAsStreamAsync(cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetSampleNamesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(_options.SnippetUrl("samples"), cancellationToken);
        if (!response.IsSuccessStatusCode)
            return Array.Empty<string>();

        var names = await response.Content.ReadFromJsonAsync<string[]>(cancellationToken: cancellationToken);
        return names?.Select(Path.GetFileNameWithoutExtension).ToArray() ?? Array.Empty<string>();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CodeFile>> LoadSampleAsync(string sampleName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sampleName)) throw new ArgumentException("Sample name is required.", nameof(sampleName));

        using var response = await _httpClient.GetAsync(_options.SnippetUrl($"samples/{sampleName}"), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await FromZipAsync(await response.Content.ReadAsStreamAsync(cancellationToken));
    }

    private static MemoryStream ToZip(IEnumerable<CodeFile> files)
    {
        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var file in files)
            {
                using var entryStream = archive.CreateEntry(file.Path).Open();
                entryStream.Write(Encoding.UTF8.GetBytes(file.Content ?? string.Empty));
            }
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }

    private static async Task<IEnumerable<CodeFile>> FromZipAsync(Stream zipStream)
    {
        var result = new List<CodeFile>();
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            using var reader = new StreamReader(entry.Open());
            result.Add(new CodeFile { Path = entry.FullName, Content = await reader.ReadToEndAsync() });
        }

        return result;
    }
}
