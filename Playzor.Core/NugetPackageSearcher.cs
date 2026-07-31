using System;
using System.Threading.Tasks;
using Playzor.Core.Api;

namespace Playzor.Core;

/// <summary>
/// Searches the nuget index through the configured <see cref="IPlayzorApi"/>.
/// </summary>
public class NuGetPackageSearcher
{
    private readonly IPlayzorApi _api;

    /// <summary>Creates the searcher.</summary>
    public NuGetPackageSearcher(IPlayzorApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <summary>Searches for packages.</summary>
    /// <param name="searchString">Search term.</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <param name="skip">Results to skip, for paging.</param>
    public Task<NugetResponse> SearchForPackagesAsync(string searchString, int limit = 20, int skip = 0)
        => _api.SearchPackagesAsync(searchString, limit, skip);
}
