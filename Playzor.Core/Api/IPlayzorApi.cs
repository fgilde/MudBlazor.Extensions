using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Playzor.Core.Api;

/// <summary>
/// Everything the editor cannot do from the browser alone.
/// <para>
/// Today that is exactly one thing: downloading a nuget package. nuget.org answers without CORS
/// headers, so a <c>.nupkg</c> can only be fetched through a server that forwards it. Searching
/// works from the browser directly, but sits on this interface too so a host can point both at
/// its own index.
/// </para>
/// <para>
/// Use <see cref="PlayzorApi"/> against your own server (see the Playzor.Server package and its
/// <c>MapPlayzorApi()</c>), against a public playground, or implement this yourself.
/// </para>
/// </summary>
public interface IPlayzorApi
{
    /// <summary>
    /// Downloads a nuget package. The stream is the raw <c>.nupkg</c> (a zip archive).
    /// </summary>
    /// <param name="packageId">Package id, for example <c>Humanizer</c>.</param>
    /// <param name="version">Exact version, for example <c>2.14.1</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Stream> GetPackageAsync(string packageId, string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the package index.
    /// </summary>
    /// <param name="searchString">Search term.</param>
    /// <param name="take">Maximum number of results.</param>
    /// <param name="skip">Results to skip, for paging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<NugetResponse> SearchPackagesAsync(string searchString, int take = 20, int skip = 0, CancellationToken cancellationToken = default);
}
