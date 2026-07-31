namespace Playzor.Server;

/// <summary>
/// Configuration of the endpoints <see cref="PlayzorEndpointRouteBuilderExtensions.MapPlayzorApi"/> adds.
/// </summary>
public class PlayzorServerOptions
{
    /// <summary>Prefix of every playzor route.</summary>
    public string RoutePrefix { get; set; } = "api/playzor";

    /// <summary>Where the packages are fetched from. <c>{id}</c> and <c>{version}</c> are replaced.</summary>
    public string PackageSourceUrl { get; set; } = "https://www.nuget.org/api/v2/package/{id}/{version}";

    /// <summary>
    /// How long a browser may cache a package. Id plus version is immutable on nuget.org, so a
    /// year is safe and saves both webassembly instances a download.
    /// </summary>
    public TimeSpan PackageCacheDuration { get; set; } = TimeSpan.FromDays(365);

    /// <summary>
    /// Origins allowed to call the package proxy from their own page, so a playground hosted
    /// elsewhere can use this server. Empty means same origin only, <c>*</c> means everyone.
    /// </summary>
    public IList<string> AllowedOrigins { get; } = new List<string>();

    /// <summary>Name of the cors policy the endpoints use.</summary>
    public string CorsPolicyName { get; set; } = "PlayzorApi";

    /// <summary>
    /// Adds the snippet endpoints. Needs an <c>IPlayzorSnippetStorage</c> in the container,
    /// otherwise they answer 501.
    /// </summary>
    public bool EnableSnippets { get; set; } = true;
}
