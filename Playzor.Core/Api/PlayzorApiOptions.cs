namespace Playzor.Core.Api;

/// <summary>
/// Where <see cref="PlayzorApi"/> sends its requests.
/// </summary>
public class PlayzorApiOptions
{
    /// <summary>The public playground, usable as <see cref="BaseAddress"/> without running a server.</summary>
    public const string PlayzorNet = "https://playzor.net/";

    /// <summary>
    /// Base address of the server that answers the routes below. Empty means the app's own origin,
    /// which is what a host using <c>MapPlayzorApi()</c> wants. Set it to
    /// <see cref="PlayzorNet"/> to borrow the public playground instead of hosting the proxy.
    /// </summary>
    public string BaseAddress { get; set; } = string.Empty;

    /// <summary>
    /// Route of the package proxy. <c>{id}</c> and <c>{version}</c> are replaced.
    /// </summary>
    public string PackageRoute { get; set; } = "api/playzor/nuget/package/{id}/{version}";

    /// <summary>
    /// Search index. Answers with CORS headers, so the browser can call it directly and no proxy
    /// is involved.
    /// </summary>
    public string SearchUrl { get; set; } = "https://azuresearch-usnc.nuget.org/query";

    /// <summary>Route of the snippet endpoints used by <see cref="PlayzorHttpSnippetStore"/>.</summary>
    public string SnippetRoute { get; set; } = "api/playzor/snippets";

    internal string PackageUrl(string packageId, string version)
        => Combine(PackageRoute.Replace("{id}", packageId).Replace("{version}", version));

    internal string SnippetUrl(string suffix = null)
        => Combine(string.IsNullOrEmpty(suffix) ? SnippetRoute : $"{SnippetRoute.TrimEnd('/')}/{suffix}");

    private string Combine(string route)
        => string.IsNullOrEmpty(BaseAddress) ? route : $"{BaseAddress.TrimEnd('/')}/{route.TrimStart('/')}";
}
