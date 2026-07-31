using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Playzor.Core.Api;

/// <summary>
/// Default <see cref="IPlayzorApi"/>: talks http to the routes in <see cref="PlayzorApiOptions"/>.
/// <code>
/// // own server (Playzor.Server, MapPlayzorApi)
/// new PlayzorApi(httpClient)
///
/// // no own server: borrow the public playground
/// new PlayzorApi(httpClient, PlayzorApiOptions.PlayzorNet)
/// </code>
/// </summary>
public class PlayzorApi : IPlayzorApi
{
    private readonly HttpClient _httpClient;

    /// <summary>Creates the client for the given options.</summary>
    public PlayzorApi(HttpClient httpClient, PlayzorApiOptions options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Options = options ?? new PlayzorApiOptions();
    }

    /// <summary>Creates the client against another playground, for example <see cref="PlayzorApiOptions.PlayzorNet"/>.</summary>
    public PlayzorApi(HttpClient httpClient, string baseAddress)
        : this(httpClient, new PlayzorApiOptions { BaseAddress = baseAddress })
    { }

    /// <summary>The routes in use.</summary>
    public PlayzorApiOptions Options { get; }

    /// <inheritdoc />
    public async Task<Stream> GetPackageAsync(string packageId, string version, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageId)) throw new ArgumentException("Package id is required.", nameof(packageId));
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Package version is required.", nameof(version));

        using var response = await _httpClient.GetAsync(Options.PackageUrl(packageId, version), cancellationToken);
        response.EnsureSuccessStatusCode();

        // copied out: the caller keeps the stream after the response is disposed
        var buffer = new MemoryStream();
        await (await response.Content.ReadAsStreamAsync(cancellationToken)).CopyToAsync(buffer, cancellationToken);
        buffer.Seek(0, SeekOrigin.Begin);
        return buffer;
    }

    /// <inheritdoc />
    public async Task<NugetResponse> SearchPackagesAsync(string searchString, int take = 20, int skip = 0, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(searchString))
            throw new ArgumentException("Search string cannot be null or empty.", nameof(searchString));

        var url = $"{Options.SearchUrl}?q={Uri.EscapeDataString(searchString)}&take={take}&skip={skip}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<NugetResponse>(cancellationToken: cancellationToken);
    }
}
