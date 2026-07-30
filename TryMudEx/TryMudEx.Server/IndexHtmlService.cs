namespace TryMudEx.Server
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Serves index.html with placeholders replaced. Currently only the asset version
    /// (cache buster for wwwroot scripts/styles); brand tokens follow in the branding phase.
    /// </summary>
    public class IndexHtmlService
    {
        // every file whose url carries {{ASSET_VERSION}} — the token must change when any of them does
        private static readonly string[] VersionedAssets =
        {
            "editor/main.js",
            "css/repl.css",
            "css/embed.css",
            "css/TryMudEx.min.css",
        };

        private readonly IWebHostEnvironment _env;
        private readonly ConcurrentDictionary<string, string> _cache = new();

        public IndexHtmlService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>Newest write time of the versioned assets, so a redeploy (or a rebuild) busts the browser cache.</summary>
        private string GetAssetVersion()
        {
            var ticks = VersionedAssets
                .Select(a => _env.WebRootFileProvider.GetFileInfo(a))
                .Where(f => f.Exists)
                .Select(f => f.LastModified.UtcTicks)
                .DefaultIfEmpty(0)
                .Max();

            return ticks.ToString("x");
        }

        public async Task<string> RenderAsync()
        {
            var version = GetAssetVersion();
            if (_cache.TryGetValue(version, out var cached))
                return cached;

            var file = _env.WebRootFileProvider.GetFileInfo("index.html");
            if (!file.Exists)
                throw new FileNotFoundException("index.html not found in web root.");

            await using var stream = file.CreateReadStream();
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();

            html = html.Replace("{{ASSET_VERSION}}", version, StringComparison.Ordinal);

            _cache.Clear(); // only the current version is worth keeping
            _cache[version] = html;
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
