namespace TryMudEx.Server
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Serves index.html with placeholders replaced. Currently only the asset version
    /// (cache buster for wwwroot scripts/styles); brand tokens follow in the branding phase.
    /// </summary>
    public class IndexHtmlService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ConcurrentDictionary<string, string> _cache = new();

        public IndexHtmlService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>Changes with every deployment, so browsers refetch scripts and styles.</summary>
        public static string AssetVersion { get; } =
            Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            + "-" + File.GetLastWriteTimeUtc(Assembly.GetExecutingAssembly().Location).Ticks.ToString("x");

        public async Task<string> RenderAsync()
        {
            const string cacheKey = "index";
            if (_cache.TryGetValue(cacheKey, out var cached))
                return cached;

            var file = _env.WebRootFileProvider.GetFileInfo("index.html");
            if (!file.Exists)
                throw new FileNotFoundException("index.html not found in web root.");

            await using var stream = file.CreateReadStream();
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();

            html = html.Replace("{{ASSET_VERSION}}", AssetVersion, StringComparison.Ordinal);

            _cache[cacheKey] = html;
            return html;
        }

        public async Task WriteResponseAsync(HttpContext context)
        {
            var html = await RenderAsync();
            context.Response.ContentType = "text/html; charset=utf-8";
            // the html itself must never be cached — it carries the asset version
            context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            await context.Response.WriteAsync(html);
        }
    }
}
