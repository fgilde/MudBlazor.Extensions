using System;
using System.Collections.Generic;
using System.Linq;

namespace Try.Core
{
    /// <summary>
    /// Pure helper logic for picking assemblies and dependencies out of nupkg content.
    /// No IO — fully unit-testable.
    /// </summary>
    public static class NugetPackageHelper
    {
        // Order matters: first match wins. WASM runtime is net10, netstandard2.0 is the floor.
        public static readonly string[] TfmPreference =
        {
            "net10.0", "net9.0", "net8.0", "net7.0", "net6.0", "net5.0",
            "netcoreapp3.1", "netstandard2.1", "netstandard2.0",
        };

        /// <summary>
        /// Picks the best lib/&lt;tfm&gt;/ folder from nupkg entry paths.
        /// Returns folder prefix with trailing slash (original casing), "lib/" for flat legacy packages,
        /// or null when the package has no compatible lib folder (e.g. meta packages).
        /// </summary>
        public static string SelectBestLibFolder(IEnumerable<string> entryPaths)
        {
            var libDlls = entryPaths
                .Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Replace('\\', '/'))
                .Where(p => p.StartsWith("lib/", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (libDlls.Count == 0)
                return null;

            // group by first folder below lib/ ("" for flat lib)
            var byTfm = libDlls
                .Select(p =>
                {
                    var parts = p.Split('/');
                    var tfm = parts.Length > 2 ? parts[1] : string.Empty;
                    var prefix = parts.Length > 2 ? $"{parts[0]}/{parts[1]}/" : $"{parts[0]}/";
                    return (Tfm: tfm.ToLowerInvariant(), Prefix: prefix);
                })
                .GroupBy(x => x.Tfm)
                .ToDictionary(g => g.Key, g => g.First().Prefix);

            foreach (var tfm in TfmPreference)
            {
                if (byTfm.TryGetValue(tfm, out var prefix))
                    return prefix;
            }

            // flat lib/ without tfm folders (legacy packages)
            if (byTfm.TryGetValue(string.Empty, out var flat))
                return flat;

            return null;
        }
    }
}
