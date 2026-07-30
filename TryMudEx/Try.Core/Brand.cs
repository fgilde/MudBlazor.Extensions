namespace Try.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Everything that differs between the domains the playground is served on.
    /// Lives in Try.Core so the server (meta tags) and the client (ui) resolve brands identically.
    /// </summary>
    public record Brand
    {
        public string Key { get; init; } = "mudex";
        public string Name { get; init; } = "MudEx";
        public string Title { get; init; } = "TryMudEx - A playground for testing MudBlazor.Extensions and MudBlazor";
        public string Description { get; init; } = "Write, compile, execute and share Blazor components entirely in the browser.";
        public string CanonicalHost { get; init; } = "https://try.mudex.org";

        /// <summary>Square mark, used in the app loader and as the base for the favicon.</summary>
        public string LogoUrl { get; init; } = "images/Logo.png";

        /// <summary>Favicon (32px) — falls back to the logo when the brand has no dedicated icon.</summary>
        public string FaviconUrl { get; init; } = "images/logo.png";

        /// <summary>Wide image for social previews (og:image / twitter:image).</summary>
        public string SocialImageUrl { get; init; } = "sample-data/logo.png";

        public string AccentColor { get; init; } = "#ab68ff";
        public string Culture { get; init; } = "en";

        /// <summary>Packages every new snippet starts with.</summary>
        public string[] DefaultPackages { get; init; } = { "MudBlazor", "MudBlazor.Extensions" };

        /// <summary>Content of __Main.razor for a fresh snippet.</summary>
        public string DefaultSnippet { get; init; } = CoreConstants.MainComponentDefaultFileContent;

        public static readonly Brand MudEx = new();

        public static readonly Brand Playzor = new()
        {
            Key = "playzor",
            Name = "Playzor",
            Title = "Playzor - The Blazor playground",
            Description = "Write, compile and share Blazor components right in your browser. No setup, no install.",
            CanonicalHost = "https://playzor.net",
            LogoUrl = "images/playzor-icon-192.png",
            FaviconUrl = "images/playzor-icon-32.png",
            SocialImageUrl = "images/playzor-social.png",
            AccentColor = "#7c3aed",
            DefaultSnippet = PlayzorDefaultSnippet,
        };

        public static readonly Brand PlayzorDe = Playzor with
        {
            Key = "playzor-de",
            Title = "Playzor - Der Blazor-Playground",
            Description = "Blazor-Komponenten schreiben, kompilieren und teilen — direkt im Browser, ohne Installation.",
            CanonicalHost = "https://playzor.de",
            Culture = "de",
        };

        public static IReadOnlyList<Brand> All { get; } = new[] { MudEx, Playzor, PlayzorDe };

        /// <summary>
        /// Maps a request host to a brand. Unknown hosts (including localhost) fall back to MudEx;
        /// during development the brand can be forced with ?brand=playzor.
        /// </summary>
        public static Brand FromHost(string host, string brandOverride = null)
        {
            if (!string.IsNullOrWhiteSpace(brandOverride))
            {
                var forced = All.FirstOrDefault(b => string.Equals(b.Key, brandOverride, StringComparison.OrdinalIgnoreCase));
                if (forced != null) return forced;
            }

            if (string.IsNullOrWhiteSpace(host)) return MudEx;

            var name = host.Split(':')[0].ToLowerInvariant();
            if (name.StartsWith("www.")) name = name[4..];

            if (name == "playzor.de" || name.EndsWith(".playzor.de")) return PlayzorDe;
            if (name == "playzor.net" || name.EndsWith(".playzor.net")) return Playzor;

            return MudEx;
        }

        private const string PlayzorDefaultSnippet = """
                                                    @* A plain blazor component — add packages from the toolbar if you need more. *@

                                                    <h3>Hello Playzor</h3>
                                                    <p>You clicked @_count times.</p>
                                                    <button @onclick="() => _count++">Click me</button>

                                                    @code {
                                                        private int _count;
                                                    }
                                                    """;
    }
}
