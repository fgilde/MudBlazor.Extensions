using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Try.Core
{
    public record NugetDependency(string Id, string Version);

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

        private static readonly string[] FrameworkPackagePrefixes =
        {
            // ponytail: prefix list, not a real runtime closure — good enough until a package legitimately ships one of these
            "System.", "Microsoft.NETCore.", "Microsoft.AspNetCore.", "Microsoft.Extensions.",
            "Microsoft.JSInterop", "Microsoft.CSharp", "Microsoft.Win32.", "NETStandard.Library",
            "runtime.",
        };

        public static bool IsFrameworkPackage(string packageId) =>
            FrameworkPackagePrefixes.Any(p => packageId.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        /// <summary>Lower bound of a nuget version range, null when the range has no usable minimum.</summary>
        public static string ParseMinVersion(string versionRange)
        {
            if (string.IsNullOrWhiteSpace(versionRange))
                return null;

            var cleaned = versionRange.Trim().TrimStart('[', '(').TrimEnd(']', ')');
            var min = cleaned.Split(',')[0].Trim();
            return string.IsNullOrEmpty(min) ? null : min;
        }

        /// <summary>
        /// Dependencies of the best-matching group in a nuspec.
        /// Group selection follows TfmPreference; a group without targetFramework and flat
        /// &lt;dependency&gt; elements act as fallback. Dependencies without resolvable min version are dropped.
        /// </summary>
        public static IReadOnlyList<NugetDependency> GetDependencies(string nuspecXml)
        {
            var doc = XDocument.Parse(nuspecXml);
            // nuspec files use varying schema namespaces — match by local name
            var dependenciesElement = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "dependencies");
            if (dependenciesElement == null)
                return Array.Empty<NugetDependency>();

            var groups = dependenciesElement.Elements()
                .Where(e => e.Name.LocalName == "group")
                .ToList();

            IEnumerable<XElement> dependencyElements;
            if (groups.Count == 0)
            {
                dependencyElements = dependenciesElement.Elements().Where(e => e.Name.LocalName == "dependency");
            }
            else
            {
                var byTfm = groups
                    .GroupBy(g => NormalizeTfm((string)g.Attribute("targetFramework")))
                    .ToDictionary(g => g.Key, g => g.First());

                XElement selected = null;
                foreach (var tfm in TfmPreference)
                {
                    if (byTfm.TryGetValue(tfm, out selected))
                        break;
                }
                selected ??= byTfm.GetValueOrDefault(string.Empty);

                if (selected == null)
                    return Array.Empty<NugetDependency>();

                dependencyElements = selected.Elements().Where(e => e.Name.LocalName == "dependency");
            }

            return dependencyElements
                .Select(d => new NugetDependency((string)d.Attribute("id"), ParseMinVersion((string)d.Attribute("version"))))
                .Where(d => !string.IsNullOrEmpty(d.Id) && d.Version != null)
                .ToList();
        }

        // ".NETStandard2.0" -> "netstandard2.0", ".NETCoreApp3.1" -> "netcoreapp3.1", "net6.0" -> "net6.0", null -> ""
        private static string NormalizeTfm(string nuspecTargetFramework)
        {
            if (string.IsNullOrWhiteSpace(nuspecTargetFramework))
                return string.Empty;

            var tfm = nuspecTargetFramework.Trim().ToLowerInvariant();
            if (tfm.StartsWith(".netstandard"))
                return "netstandard" + tfm[".netstandard".Length..];
            if (tfm.StartsWith(".netcoreapp"))
                return "netcoreapp" + tfm[".netcoreapp".Length..];
            return tfm.TrimStart('.');
        }
    }
}
