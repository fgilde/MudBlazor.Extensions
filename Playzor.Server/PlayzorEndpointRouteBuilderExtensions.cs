using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Playzor.Server;

/// <summary>
/// Adds the endpoints a Playzor editor needs from its host.
/// </summary>
public static class PlayzorEndpointRouteBuilderExtensions
{
    private const string HttpClientName = "playzor-nuget";

    /// <summary>
    /// Registers what <see cref="MapPlayzorApi"/> needs. Call it when you want to configure the
    /// options or add a snippet storage; <c>MapPlayzorApi</c> works without it too.
    /// </summary>
    public static IServiceCollection AddPlayzorServer(this IServiceCollection services, Action<PlayzorServerOptions>? configure = null)
    {
        var options = new PlayzorServerOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddHttpClient(HttpClientName);

        if (options.AllowedOrigins.Count > 0)
        {
            services.AddCors(cors => cors.AddPolicy(options.CorsPolicyName, policy =>
            {
                if (options.AllowedOrigins.Contains("*"))
                    policy.AllowAnyOrigin();
                else
                    policy.WithOrigins(options.AllowedOrigins.ToArray());

                policy.AllowAnyHeader().AllowAnyMethod();
            }));
        }

        return services;
    }

    /// <summary>
    /// Maps the playzor endpoints:
    /// <list type="bullet">
    /// <item><c>GET {prefix}/nuget/package/{id}/{version}</c> — the package proxy the browser needs,
    /// because nuget.org answers without cors headers.</item>
    /// <item><c>POST {prefix}/snippets</c>, <c>GET {prefix}/snippets/{id}</c>,
    /// <c>GET {prefix}/snippets/samples</c>, <c>GET {prefix}/snippets/samples/{name}</c> — only when
    /// an <see cref="IPlayzorSnippetStorage"/> is registered.</item>
    /// </list>
    /// </summary>
    public static IEndpointRouteBuilder MapPlayzorApi(this IEndpointRouteBuilder endpoints, Action<PlayzorServerOptions>? configure = null)
    {
        var options = endpoints.ServiceProvider.GetService<PlayzorServerOptions>() ?? new PlayzorServerOptions();
        configure?.Invoke(options);

        var prefix = options.RoutePrefix.Trim('/');
        var group = endpoints.MapGroup(prefix);
        if (options.AllowedOrigins.Count > 0)
            group = group.RequireCors(options.CorsPolicyName);

        group.MapGet("nuget/package/{packageId}/{version}", async (string packageId, string version, HttpContext context) =>
        {
            var factory = context.RequestServices.GetService<IHttpClientFactory>();
            var client = factory?.CreateClient(HttpClientName) ?? new HttpClient();

            var url = options.PackageSourceUrl.Replace("{id}", Uri.EscapeDataString(packageId)).Replace("{version}", Uri.EscapeDataString(version));
            using var response = await client.GetAsync(url, context.RequestAborted);
            if (!response.IsSuccessStatusCode)
                return Results.StatusCode((int)response.StatusCode);

            // buffered on purpose: the response is disposed when this handler returns, while the
            // result writes the body afterwards — streaming it through would read a closed connection
            var package = await response.Content.ReadAsByteArrayAsync(context.RequestAborted);

            // id plus version is immutable, so the browser may keep it
            context.Response.Headers.CacheControl = $"public, max-age={(int)options.PackageCacheDuration.TotalSeconds}, immutable";

            return Results.File(package, "application/octet-stream", $"{packageId}.{version}.nupkg");
        });

        if (options.EnableSnippets)
            MapSnippets(group);

        return endpoints;
    }

    private static void MapSnippets(RouteGroupBuilder group)
    {
        group.MapPost("snippets", async (HttpContext context) =>
        {
            var storage = context.RequestServices.GetService<IPlayzorSnippetStorage>();
            if (storage == null) return NoStorage();

            var id = await storage.SaveAsync(context.Request.Body, context.RequestAborted);
            return Results.Ok(id);
        });

        group.MapGet("snippets/samples", async (HttpContext context) =>
        {
            var storage = context.RequestServices.GetService<IPlayzorSnippetStorage>();
            if (storage == null) return NoStorage();

            return Results.Ok(await storage.GetSampleNamesAsync(context.RequestAborted));
        });

        group.MapGet("snippets/samples/{sampleName}", async (string sampleName, HttpContext context) =>
        {
            var storage = context.RequestServices.GetService<IPlayzorSnippetStorage>();
            if (storage == null) return NoStorage();

            await using var stream = await storage.LoadSampleAsync(sampleName, context.RequestAborted);
            return stream == null ? Results.NotFound() : Results.File(await ReadAllAsync(stream, context.RequestAborted), "application/zip", $"{sampleName}.zip");
        });

        group.MapGet("snippets/{snippetId}", async (string snippetId, HttpContext context) =>
        {
            var storage = context.RequestServices.GetService<IPlayzorSnippetStorage>();
            if (storage == null) return NoStorage();

            await using var stream = await storage.LoadAsync(snippetId, context.RequestAborted);
            return stream == null ? Results.NotFound() : Results.File(await ReadAllAsync(stream, context.RequestAborted), "application/zip", $"{snippetId}.zip");
        });
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private static IResult NoStorage()
        => Results.Problem("No IPlayzorSnippetStorage is registered, so this playground cannot store snippets.", statusCode: StatusCodes.Status501NotImplemented);
}
