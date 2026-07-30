namespace TryMudEx.Server
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Try.Core;

    /// <summary>
    /// Serves index.html with its placeholders replaced: the asset version (cache buster for
    /// wwwroot scripts and styles) and the brand tokens of the requested domain, so title and
    /// social meta tags are correct before blazor even starts.
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
        private string _cachedVersion;

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

        public async Task<string> RenderAsync(Brand brand)
        {
            var version = GetAssetVersion();
            if (_cachedVersion != version)
            {
                _cache.Clear(); // assets changed — every rendered brand page is stale
                _cachedVersion = version;
            }
            else if (_cache.TryGetValue(brand.Key, out var cached))
            {
                return cached;
            }

            var file = _env.WebRootFileProvider.GetFileInfo("index.html");
            if (!file.Exists)
                throw new FileNotFoundException("index.html not found in web root.");

            string html;
            await using (var stream = file.CreateReadStream())
            using (var reader = new StreamReader(stream))
            {
                html = await reader.ReadToEndAsync();
            }

            html = html
                .Replace("{{ASSET_VERSION}}", version, StringComparison.Ordinal)
                .Replace("{{BRAND_KEY}}", brand.Key, StringComparison.Ordinal)
                .Replace("{{BRAND_NAME}}", brand.Name, StringComparison.Ordinal)
                .Replace("{{BRAND_TITLE}}", brand.Title, StringComparison.Ordinal)
                .Replace("{{BRAND_DESCRIPTION}}", brand.Description, StringComparison.Ordinal)
                .Replace("{{BRAND_CANONICAL}}", brand.CanonicalHost, StringComparison.Ordinal)
                .Replace("{{BRAND_LOGO}}", brand.LogoUrl, StringComparison.Ordinal)
                .Replace("{{BRAND_FAVICON}}", brand.FaviconUrl, StringComparison.Ordinal)
                .Replace("{{BRAND_SOCIAL}}", brand.SocialImageUrl, StringComparison.Ordinal)
                .Replace("{{BRAND_ACCENT}}", brand.AccentColor, StringComparison.Ordinal)
                .Replace("{{BRAND_CULTURE}}", brand.Culture, StringComparison.Ordinal);

            _cache[brand.Key] = html;
            return html;
        }

        public async Task WriteResponseAsync(HttpContext context)
        {
            var brandOverride = context.Request.Query.TryGetValue("brand", out var value) ? value.ToString() : null;
            var brand = Brand.FromHost(context.Request.Host.Value, brandOverride);

            var html = await RenderAsync(brand);
            context.Response.ContentType = "text/html; charset=utf-8";
            // the html itself must never be cached — it carries the asset version and the brand
            context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            await context.Response.WriteAsync(html);
        }
    }
}
