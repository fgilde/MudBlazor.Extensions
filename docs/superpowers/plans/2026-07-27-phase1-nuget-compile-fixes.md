# Phase 1: NuGet-Fix + Compile-Perf Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** NuGet-Pakete mit transitiven Dependencies und korrektem TFM-Filter laden (Humanizer-Demo läuft wieder), Paket-Downloads browser-cachen, Framework-Referenzen nur einmal laden.

**Architecture:** Pure Parsing-/Auswahl-Logik kommt in eine neue statische Klasse `NugetPackageHelper` (testbar ohne Netz). `NugetReferenceService` orchestriert Download + Rekursion. `NugetController` bekommt Cache-Header. `CompilationService` cacht die Framework-Referenzen in einem `Task`-Feld.

**Tech Stack:** .NET 10, Blazor WASM, Roslyn (`Microsoft.CodeAnalysis`), NUnit (Try.Tests), `System.Xml.Linq` für nuspec.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-27-trymudex-codepen-redesign-design.md` (Abschnitt Phase 1)
- Committen ja, **niemals pushen**. Keine Co-Authored-By/Generated-with-Zeilen in Commits.
- Commit-Stil des Repos: kurz, lowercase, imperativ (z.B. `nuget tfm filter`).
- TFM-Präferenz überall identisch: `net10.0 > net9.0 > net8.0 > net7.0 > net6.0 > net5.0 > netcoreapp3.1 > netstandard2.1 > netstandard2.0`
- Bestehender `SnippetsServiceTests` braucht Azure und darf weiter fehlschlagen/geskippt sein — Tests immer mit Filter laufen lassen: `--filter FullyQualifiedName~NugetPackageHelper`
- Test-Kommando: `dotnet test TryMudEx/Try.Tests/Try.Tests.csproj --filter FullyQualifiedName~NugetPackageHelper`
- Try.Tests referenziert TryMudEx.Client (zieht Try.Core transitiv) — neue Try.Core-Typen sind direkt verwendbar.

---

### Task 1: `NugetPackageHelper` — TFM-Auswahl (`SelectBestLibFolder`)

**Files:**
- Create: `TryMudEx/Try.Core/NugetPackageHelper.cs`
- Create: `TryMudEx/Try.Tests/NugetPackageHelperTests.cs`

**Interfaces:**
- Produces: `static string NugetPackageHelper.SelectBestLibFolder(IEnumerable<string> entryPaths)` — nimmt alle Entry-Pfade eines nupkg (z.B. `lib/netstandard2.0/Humanizer.dll`), gibt den besten lib-Ordner-Präfix zurück (`"lib/netstandard2.0/"`), `"lib/"` für flache Alt-Pakete, `null` wenn kein kompatibles lib existiert.
- Produces: `static readonly string[] NugetPackageHelper.TfmPreference`

- [ ] **Step 1: Failing Tests schreiben**

`TryMudEx/Try.Tests/NugetPackageHelperTests.cs`:

```csharp
using System.Linq;
using NUnit.Framework;
using Playzor.Core;

namespace Try.Tests;

[TestFixture]
public class NugetPackageHelperTests
{
    [Test]
    public void SelectBestLibFolder_PrefersHighestTfm()
    {
        var entries = new[]
        {
            "lib/netstandard2.0/Foo.dll",
            "lib/net6.0/Foo.dll",
            "lib/net472/Foo.dll",
        };
        Assert.That(NugetPackageHelper.SelectBestLibFolder(entries), Is.EqualTo("lib/net6.0/"));
    }

    [Test]
    public void SelectBestLibFolder_FallsBackToNetstandard()
    {
        var entries = new[] { "lib/netstandard2.0/Foo.dll", "lib/net472/Foo.dll" };
        Assert.That(NugetPackageHelper.SelectBestLibFolder(entries), Is.EqualTo("lib/netstandard2.0/"));
    }

    [Test]
    public void SelectBestLibFolder_IgnoresPlatformSpecificTfms()
    {
        var entries = new[] { "lib/net6.0-windows/Foo.dll", "lib/netstandard2.0/Foo.dll" };
        Assert.That(NugetPackageHelper.SelectBestLibFolder(entries), Is.EqualTo("lib/netstandard2.0/"));
    }

    [Test]
    public void SelectBestLibFolder_ReturnsNullWhenOnlyIncompatible()
    {
        var entries = new[] { "lib/net472/Foo.dll", "ref/net6.0/Foo.dll", "analyzers/dotnet/cs/Gen.dll" };
        Assert.That(NugetPackageHelper.SelectBestLibFolder(entries), Is.Null);
    }

    [Test]
    public void SelectBestLibFolder_ReturnsNullForMetaPackageWithoutLib()
    {
        var entries = new[] { "Humanizer.nuspec", "_rels/.rels" };
        Assert.That(NugetPackageHelper.SelectBestLibFolder(entries), Is.Null);
    }

    [Test]
    public void SelectBestLibFolder_FlatLibFolder()
    {
        var entries = new[] { "lib/Foo.dll" };
        Assert.That(NugetPackageHelper.SelectBestLibFolder(entries), Is.EqualTo("lib/"));
    }

    [Test]
    public void SelectBestLibFolder_IsCaseInsensitive()
    {
        var entries = new[] { "Lib/NetStandard2.0/Foo.dll" };
        Assert.That(NugetPackageHelper.SelectBestLibFolder(entries), Is.EqualTo("Lib/NetStandard2.0/"));
    }
}
```

- [ ] **Step 2: Tests laufen lassen — müssen fehlschlagen (Compile-Fehler: Klasse existiert nicht)**

Run: `dotnet test TryMudEx/Try.Tests/Try.Tests.csproj --filter FullyQualifiedName~NugetPackageHelper`
Expected: Build-FAIL mit `CS0103`/`CS0246` (NugetPackageHelper unbekannt)

- [ ] **Step 3: Implementierung**

`TryMudEx/Try.Core/NugetPackageHelper.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace Playzor.Core
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
```

- [ ] **Step 4: Tests laufen lassen — müssen grün sein**

Run: `dotnet test TryMudEx/Try.Tests/Try.Tests.csproj --filter FullyQualifiedName~NugetPackageHelper`
Expected: 7 PASS

- [ ] **Step 5: Commit**

```bash
git add TryMudEx/Try.Core/NugetPackageHelper.cs TryMudEx/Try.Tests/NugetPackageHelperTests.cs
git commit -m "nuget tfm selection helper"
```

---

### Task 2: `NugetPackageHelper` — nuspec-Dependencies + Versions-Range + Framework-Skip

**Files:**
- Modify: `TryMudEx/Try.Core/NugetPackageHelper.cs`
- Modify: `TryMudEx/Try.Tests/NugetPackageHelperTests.cs`

**Interfaces:**
- Consumes: `TfmPreference` aus Task 1.
- Produces:
  - `record NugetDependency(string Id, string Version)` (Namespace `Try.Core`)
  - `static IReadOnlyList<NugetDependency> NugetPackageHelper.GetDependencies(string nuspecXml)` — Dependencies der am besten passenden Group (gleiche TFM-Präferenz; Group ohne targetFramework und flache `<dependency>`-Elemente als Fallback). Dependencies ohne auflösbare Min-Version werden weggelassen.
  - `static string NugetPackageHelper.ParseMinVersion(string versionRange)` — `"2.14.1"`→`"2.14.1"`, `"[2.14.1, 3.0)"`→`"2.14.1"`, `"[1.0.0]"`→`"1.0.0"`, `"(, 5.0]"`→`null`, `null/""`→`null`
  - `static bool NugetPackageHelper.IsFrameworkPackage(string packageId)` — true für Runtime-Pakete, die in Blazor WASM schon vorhanden sind

- [ ] **Step 1: Failing Tests ergänzen**

In `TryMudEx/Try.Tests/NugetPackageHelperTests.cs` ergänzen:

```csharp
    private const string NuspecWithGroups = """
        <?xml version="1.0" encoding="utf-8"?>
        <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
          <metadata>
            <id>Humanizer</id>
            <version>2.14.1</version>
            <dependencies>
              <group targetFramework=".NETFramework4.8">
                <dependency id="OnlyForNetFx" version="1.0.0" />
              </group>
              <group targetFramework=".NETStandard2.0">
                <dependency id="Humanizer.Core" version="[2.14.1]" />
                <dependency id="Humanizer.Core.af" version="[2.14.1]" />
              </group>
            </dependencies>
          </metadata>
        </package>
        """;

    [Test]
    public void GetDependencies_PicksBestMatchingGroup()
    {
        var deps = NugetPackageHelper.GetDependencies(NuspecWithGroups);
        Assert.That(deps.Select(d => d.Id), Is.EquivalentTo(new[] { "Humanizer.Core", "Humanizer.Core.af" }));
        Assert.That(deps.All(d => d.Version == "2.14.1"), Is.True);
    }

    [Test]
    public void GetDependencies_GroupWithoutTargetFrameworkIsFallback()
    {
        const string nuspec = """
            <?xml version="1.0"?>
            <package xmlns="http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd">
              <metadata>
                <id>X</id><version>1.0.0</version>
                <dependencies>
                  <group>
                    <dependency id="Newtonsoft.Json" version="13.0.1" />
                  </group>
                </dependencies>
              </metadata>
            </package>
            """;
        var deps = NugetPackageHelper.GetDependencies(nuspec);
        Assert.That(deps.Single().Id, Is.EqualTo("Newtonsoft.Json"));
        Assert.That(deps.Single().Version, Is.EqualTo("13.0.1"));
    }

    [Test]
    public void GetDependencies_FlatDependenciesWithoutGroups()
    {
        const string nuspec = """
            <?xml version="1.0"?>
            <package>
              <metadata>
                <id>Old</id><version>1.0.0</version>
                <dependencies>
                  <dependency id="A" version="1.2.3" />
                </dependencies>
              </metadata>
            </package>
            """;
        var deps = NugetPackageHelper.GetDependencies(nuspec);
        Assert.That(deps.Single(), Is.EqualTo(new NugetDependency("A", "1.2.3")));
    }

    [Test]
    public void GetDependencies_NoDependencies_ReturnsEmpty()
    {
        const string nuspec = """
            <?xml version="1.0"?>
            <package><metadata><id>X</id><version>1.0.0</version></metadata></package>
            """;
        Assert.That(NugetPackageHelper.GetDependencies(nuspec), Is.Empty);
    }

    [TestCase("2.14.1", "2.14.1")]
    [TestCase("[2.14.1, 3.0)", "2.14.1")]
    [TestCase("[1.0.0]", "1.0.0")]
    [TestCase("(, 5.0]", null)]
    [TestCase("", null)]
    [TestCase(null, null)]
    public void ParseMinVersion_Cases(string range, string expected)
    {
        Assert.That(NugetPackageHelper.ParseMinVersion(range), Is.EqualTo(expected));
    }

    [TestCase("System.Text.Json", true)]
    [TestCase("Microsoft.Extensions.Logging", true)]
    [TestCase("Microsoft.AspNetCore.Components.Web", true)]
    [TestCase("Microsoft.NETCore.Platforms", true)]
    [TestCase("NETStandard.Library", true)]
    [TestCase("runtime.native.System", true)]
    [TestCase("Microsoft.CSharp", true)]
    [TestCase("Humanizer.Core", false)]
    [TestCase("Newtonsoft.Json", false)]
    [TestCase("Blazored.LocalStorage", false)]
    public void IsFrameworkPackage_Cases(string id, bool expected)
    {
        Assert.That(NugetPackageHelper.IsFrameworkPackage(id), Is.EqualTo(expected));
    }
```

- [ ] **Step 2: Tests laufen lassen — neue Tests müssen fehlschlagen (Compile-Fehler)**

Run: `dotnet test TryMudEx/Try.Tests/Try.Tests.csproj --filter FullyQualifiedName~NugetPackageHelper`
Expected: Build-FAIL (`GetDependencies`/`NugetDependency` unbekannt)

- [ ] **Step 3: Implementierung ergänzen**

In `TryMudEx/Try.Core/NugetPackageHelper.cs` ergänzen (using `System.Xml.Linq` oben dazu):

```csharp
    public record NugetDependency(string Id, string Version);
```

(direkt im Namespace `Try.Core`, oberhalb der Klasse) und in der Klasse:

```csharp
        private static readonly string[] FrameworkPackagePrefixes =
        {
            // ponytail: prefix list, not a real runtime closure — good enough until a package legitimately ships one of these
            "System.", "Microsoft.NETCore.", "Microsoft.AspNetCore.", "Microsoft.Extensions.",
            "Microsoft.JSInterop", "Microsoft.CSharp", "Microsoft.Win32.", "NETStandard.Library",
            "runtime.",
        };

        public static bool IsFrameworkPackage(string packageId) =>
            FrameworkPackagePrefixes.Any(p => packageId.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            || packageId.Equals("NETStandard.Library", StringComparison.OrdinalIgnoreCase);

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
```

- [ ] **Step 4: Tests laufen lassen — alle grün**

Run: `dotnet test TryMudEx/Try.Tests/Try.Tests.csproj --filter FullyQualifiedName~NugetPackageHelper`
Expected: alle PASS (Task 1 + Task 2 Tests)

- [ ] **Step 5: Commit**

```bash
git add TryMudEx/Try.Core/NugetPackageHelper.cs TryMudEx/Try.Tests/NugetPackageHelperTests.cs
git commit -m "nuspec dependency parsing and framework package skip list"
```

---

### Task 3: `NugetReferenceService` — TFM-Filter + transitive Dependencies + Referenz-Cache

**Files:**
- Modify: `TryMudEx/Try.Core/NugetReferenceService.cs`

**Interfaces:**
- Consumes: `NugetPackageHelper.SelectBestLibFolder`, `GetDependencies`, `IsFrameworkPackage`, `NugetDependency` (Tasks 1-2).
- Produces: öffentliche Signaturen bleiben UNVERÄNDERT (`GetAssemblyBytesAsync`, `GetAssemblyStreamsAsync`, `GetMetadataReferencesAsync`) — Aufrufer (`CompilationService.cs:235`, `Repl.razor.cs`, `App.razor:36-67`) brauchen keine Anpassung. Neu ist nur das Verhalten: transitive DLLs inklusive, nur bestes lib-TFM, dedupliziert nach Assembly-Dateiname.

**Hinweis:** Es gibt keinen automatisierten Test für diese Datei (Orchestrierung über `HttpClient` + `MudExFileService`; die Logik-Bausteine sind in Tasks 1-2 getestet). Verifikation: Build + E2E in Task 6.

- [ ] **Step 1: Umbau implementieren**

`TryMudEx/Try.Core/NugetReferenceService.cs` — kompletter neuer Inhalt:

```csharp
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Playzor.Core;
using Nextended.Core.Extensions;
using MudBlazor.Extensions.Services;


public class NugetReferenceService
{
    private readonly HttpClient _httpClient;
    private readonly MudExFileService _fileService;
    private static readonly ConcurrentDictionary<string, List<(string AssemblyName, byte[] AssemblyBytes)>> _packageCache = new();
    private static readonly ConcurrentDictionary<string, PortableExecutableReference[]> _referenceCache = new();

    public NugetReferenceService(HttpClient client, MudExFileService fileService)
    {
        _httpClient = client;
        _fileService = fileService;
    }

    public async Task<IEnumerable<(string AssemblyName, byte[] AssemblyBytes)>> GetAssemblyBytesAsync(IEnumerable<INugetPackageReference> packages, Func<string, Task> updateStatusFunc = null)
    {
        var assemblies = await GetAssemblyStreamsAsync(packages, updateStatusFunc);
        return assemblies.Select(info =>
        {
            info.Stream.Seek(0, SeekOrigin.Begin);
            return (info.AssemblyName, info.Stream.ToArray());
        });
    }

    public async Task<IEnumerable<(string AssemblyName, MemoryStream Stream)>> GetAssemblyStreamsAsync(IEnumerable<INugetPackageReference> packages, Func<string, Task> updateStatusFunc = null)
    {
        var results = new List<(string AssemblyName, MemoryStream Stream)>();
        // default packages are compiled in already; never re-download them (or their dependency trees)
        var visited = new HashSet<string>(CoreConstants.DefaultPackages.Select(dp => dp.Id), StringComparer.OrdinalIgnoreCase);

        foreach (var package in packages)
        {
            await LoadPackageRecursiveAsync(package.Id, package.Version, visited, results, updateStatusFunc);
        }
        return results;
    }

    public async Task<IEnumerable<PortableExecutableReference>> GetMetadataReferencesAsync(IEnumerable<INugetPackageReference> packages, Func<string, Task> updateStatusFunc = null)
    {
        await (updateStatusFunc?.Invoke($"Loading packages...") ?? Task.CompletedTask);
        var packageList = packages.Where(p => CoreConstants.DefaultPackages.All(dp => dp.Id != p.Id)).ToList();
        var cacheKey = string.Join("|", packageList.Select(p => $"{p.Id}.{p.Version}").OrderBy(x => x));

        if (_referenceCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var assemblies = await GetAssemblyStreamsAsync(packageList, updateStatusFunc);
        var references = assemblies.Select(info => MetadataReference.CreateFromStream(info.Stream)).ToArray();
        _referenceCache.TryAdd(cacheKey, references);
        return references;
    }

    private async Task LoadPackageRecursiveAsync(string packageId, string version, HashSet<string> visited, List<(string AssemblyName, MemoryStream Stream)> results, Func<string, Task> updateStatusFunc)
    {
        if (!visited.Add(packageId) || NugetPackageHelper.IsFrameworkPackage(packageId))
            return;

        await (updateStatusFunc?.Invoke($"Loading Nuget package {packageId} {version}") ?? Task.CompletedTask);

        var (assemblies, dependencies) = await EnsurePackageDownloadedAsync(packageId, version);

        foreach (var assembly in assemblies)
        {
            // first one wins — same assembly can arrive via several dependency paths
            if (results.Any(r => string.Equals(r.AssemblyName, assembly.AssemblyName, StringComparison.OrdinalIgnoreCase)))
                continue;
            results.Add(assembly);
        }

        foreach (var dependency in dependencies)
        {
            await LoadPackageRecursiveAsync(dependency.Id, dependency.Version, visited, results, updateStatusFunc);
        }
    }

    private async Task<(List<(string AssemblyName, MemoryStream Stream)> Assemblies, IReadOnlyList<NugetDependency> Dependencies)> EnsurePackageDownloadedAsync(string packageId, string version)
    {
        var cacheKey = $"{packageId}.{version}";
        if (!_packageCache.TryGetValue(cacheKey, out var cachedAssemblies) || !_dependencyCache.TryGetValue(cacheKey, out var cachedDependencies))
        {
            var (assemblies, dependencies) = await DownloadAndExtractPackageAsync(packageId, version);
            _packageCache.TryAdd(cacheKey, assemblies.Select(info => (info.AssemblyName, info.Stream.ToByteArray())).ToList());
            _dependencyCache.TryAdd(cacheKey, dependencies);
            return (assemblies, dependencies);
        }

        var results = cachedAssemblies.Select(i => (i.AssemblyName, new MemoryStream(i.AssemblyBytes))).ToList();
        results.Select(r => r.Stream).Apply(s => s.Seek(0, SeekOrigin.Begin));
        return (results, cachedDependencies);
    }

    private static readonly ConcurrentDictionary<string, IReadOnlyList<NugetDependency>> _dependencyCache = new();

    private async Task<(List<(string AssemblyName, MemoryStream Stream)> Assemblies, IReadOnlyList<NugetDependency> Dependencies)> DownloadAndExtractPackageAsync(string packageId, string version)
    {
        var packageUrl = $"api/nuget/package/{packageId}/{version}";

        using var response = await _httpClient.GetAsync(packageUrl);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        var entries = await _fileService.ReadArchiveAsync(memoryStream, packageId, "application/zip");
        var allEntries = entries.List.DistinctBy(e => e.FullName).ToArray();

        // dependencies from the embedded nuspec (top-level {id}.nuspec)
        IReadOnlyList<NugetDependency> dependencies = Array.Empty<NugetDependency>();
        var nuspecEntry = allEntries.FirstOrDefault(e =>
            e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase) && !e.FullName.Contains('/'));
        if (nuspecEntry != null)
        {
            using var nuspecStream = nuspecEntry.OpenReadStream();
            using var reader = new StreamReader(nuspecStream);
            dependencies = NugetPackageHelper.GetDependencies(await reader.ReadToEndAsync());
        }

        // only dlls from the best matching lib/<tfm>/ folder — a missing lib folder is fine (meta package)
        var bestLibFolder = NugetPackageHelper.SelectBestLibFolder(allEntries.Select(e => e.FullName));
        var dllStreams = new List<(string AssemblyName, MemoryStream Stream)>();
        if (bestLibFolder != null)
        {
            var dlls = allEntries.Where(e =>
                e.FullName.StartsWith(bestLibFolder, StringComparison.OrdinalIgnoreCase) &&
                e.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

            foreach (var dll in dlls)
            {
                var entryStream = new MemoryStream();
                using var entryOriginalStream = dll.OpenReadStream();
                await entryOriginalStream.CopyToAsync(entryStream);
                entryStream.Seek(0, SeekOrigin.Begin);
                dllStreams.Add((dll.Name, entryStream));
            }
        }

        return (dllStreams, dependencies);
    }
}
```

Wichtige Verhaltensdetails:
- `GetAssemblyStreamsAsync` filtert DefaultPackages jetzt über das `visited`-Set (vorher `if`-Check in der Schleife) — Verhalten identisch, aber auch transitiv wirksam.
- Meta-Paket ohne `lib/` (Humanizer!) liefert 0 eigene DLLs, aber seine Dependencies werden geladen — das ist der eigentliche Bugfix.
- `_referenceCache` verhindert wiederholtes `MetadataReference.CreateFromStream` pro Compile (Roslyn parsed sonst bei jedem Run alle PE-Images neu).
- `ReadArchiveAsync`-Pfadtrenner: nupkg-Entries nutzen `/`; `SelectBestLibFolder` normalisiert zusätzlich `\` → `/` (Task 1), Vergleiche hier laufen über `StartsWith`/`EndsWith` auf `FullName` — falls `MudExFileService` `\` liefert, matcht `bestLibFolder` trotzdem, weil beide aus denselben `FullName`-Werten stammen.

- [ ] **Step 2: Build**

Run: `dotnet build TryMudEx/TryMudEx.slnx`
Expected: 0 Fehler

- [ ] **Step 3: Bestehende Tests unverändert grün**

Run: `dotnet test TryMudEx/Try.Tests/Try.Tests.csproj --filter FullyQualifiedName~NugetPackageHelper`
Expected: alle PASS

- [ ] **Step 4: Commit**

```bash
git add TryMudEx/Try.Core/NugetReferenceService.cs
git commit -m "nuget transitive dependencies and tfm filter"
```

---

### Task 4: `NugetController` — Cache-Header + Statuscode durchreichen

**Files:**
- Modify: `TryMudEx/TryMudEx.Server/Controllers/NugetController.cs:27-44`

**Interfaces:**
- Consumes: nichts Neues.
- Produces: gleiche Route `api/nuget/package/{packageId}/{version}`; Response bekommt `Cache-Control: public, max-age=31536000, immutable` bei Erfolg; NuGet-Fehlerstatus (z.B. 404) wird durchgereicht statt pauschal 500.

- [ ] **Step 1: Implementierung**

In `TryMudEx/TryMudEx.Server/Controllers/NugetController.cs` die Methode `GetSamples` ersetzen:

```csharp
        [HttpGet("package/{packageId}/{version}")]
        public async Task<IActionResult> GetPackage(string packageId, string version)
        {
            var packageUrl = $"https://www.nuget.org/api/v2/package/{packageId}/{version}";
            var responseMessage = await _httpClient.GetAsync(packageUrl);

            if (!responseMessage.IsSuccessStatusCode)
            {
                return StatusCode((int)responseMessage.StatusCode);
            }

            var stream = await responseMessage.Content.ReadAsStreamAsync();
            var contentDisposition = responseMessage.Content.Headers.ContentDisposition?.ToString();

            // id+version is immutable on nuget.org — let the browser cache the nupkg (both wasm instances profit)
            Response.Headers.CacheControl = "public, max-age=31536000, immutable";

            return File(stream, "application/octet-stream", contentDisposition ?? "package.zip");
        }
```

(Methodenname `GetSamples` → `GetPackage` ist reine Umbenennung — Route kommt vom Attribut, keine Aufrufer betroffen.)

- [ ] **Step 2: Build**

Run: `dotnet build TryMudEx/TryMudEx.Server/TryMudEx.Server.csproj`
Expected: 0 Fehler

- [ ] **Step 3: Commit**

```bash
git add TryMudEx/TryMudEx.Server/Controllers/NugetController.cs
git commit -m "cache nuget proxy responses, pass through error status"
```

---

### Task 5: `CompilationService` — Framework-Referenzen einmalig laden

**Files:**
- Modify: `TryMudEx/Try.Core/CompilationService.cs:49-122` (Feld + `InitCompileAsync`)

**Interfaces:**
- Consumes: nichts Neues.
- Produces: `InitCompileAsync(PortableExecutableReference[])` behält Signatur; lädt die ~40 `/_framework/*.dll` nur noch beim ersten Aufruf (Task-Cache), baut `baseCompilation` pro Aufruf neu aus gecachten Referenzen (billig, nötig weil NuGet-Referenzen pro Compile variieren).

- [ ] **Step 1: Implementierung**

In `TryMudEx/Try.Core/CompilationService.cs`:

Feld ergänzen (unter `private CSharpParseOptions cSharpParseOptions;`, Zeile ~52):

```csharp
        private Task<List<PortableExecutableReference>> _frameworkReferencesTask;
```

`InitCompileAsync` (Zeile 60-122) ersetzen durch:

```csharp
        private async Task InitCompileAsync(PortableExecutableReference[] additionalReferences)
        {
            // framework references never change — load them exactly once per app lifetime
            var frameworkReferencesTask = _frameworkReferencesTask ??= LoadFrameworkReferencesAsync();
            List<PortableExecutableReference> frameworkReferences;
            try
            {
                frameworkReferences = await frameworkReferencesTask;
            }
            catch
            {
                // don't cache a failed load (e.g. transient network error) — retry next compile
                _frameworkReferencesTask = null;
                throw;
            }

            var references = new List<PortableExecutableReference>(frameworkReferences);
            if (additionalReferences?.Any() == true)
            {
                references.AddRange(additionalReferences);
            }

            baseCompilation = CSharpCompilation.Create(
                DefaultRootNamespace,
                Array.Empty<SyntaxTree>(),
                references,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    concurrentBuild: false,
                    //// Warnings CS1701 and CS1702 are disabled when compiling in VS too
                    specificDiagnosticOptions: new[]
                    {
                        new KeyValuePair<string, ReportDiagnostic>("CS1701", ReportDiagnostic.Suppress),
                        new KeyValuePair<string, ReportDiagnostic>("CS1702", ReportDiagnostic.Suppress),
                    }));

            cSharpParseOptions ??= new CSharpParseOptions(LanguageVersion.Preview);
        }

        private async Task<List<PortableExecutableReference>> LoadFrameworkReferencesAsync()
        {
            var basicReferenceAssemblyRoots = new[]
            {
                typeof(Console).Assembly, // System.Console
                typeof(Uri).Assembly, // System.Private.Uri
                typeof(AssemblyTargetedPatchBandAttribute).Assembly, // System.Private.CoreLib
                typeof(NavLink).Assembly, // Microsoft.AspNetCore.Components.Web
                typeof(IQueryable).Assembly, // System.Linq.Expressions
                typeof(HttpClientJsonExtensions).Assembly, // System.Net.Http.Json
                typeof(HttpClient).Assembly, // System.Net.Http
                typeof(IJSRuntime).Assembly, // Microsoft.JSInterop
                typeof(RequiredAttribute).Assembly, // System.ComponentModel.Annotations
                typeof(MudBlazor.MudButton).Assembly, // MudBlazor
                typeof(MudExIcon).Assembly, // MudBlazor
                typeof(JsonConvert).Assembly, // Newtonsoft
                typeof(WebAssemblyHostBuilder).Assembly, // Microsoft.AspNetCore.Components.WebAssembly
                typeof(FluentValidation.AbstractValidator<>).Assembly,
            };

            var assemblyNames = basicReferenceAssemblyRoots
                .SelectMany(assembly => assembly.GetReferencedAssemblies().Concat(new[] { assembly.GetName() }))
                .Select(x => x.Name)
                .Distinct()
                .ToList();

            var assemblyStreams = await GetStreamFromHttpAsync(_httpClient, assemblyNames);

            Dictionary<string, PortableExecutableReference> allReferenceAssemblies = assemblyStreams.ToDictionary(a => a.Key, a => MetadataReference.CreateFromStream(a.Value));

            return allReferenceAssemblies
                .Where(a => basicReferenceAssemblyRoots
                    .Select(x => x.GetName().Name)
                    .Union(basicReferenceAssemblyRoots.SelectMany(y => y.GetReferencedAssemblies().Select(z => z.Name)))
                    .Any(n => n == a.Key))
                .Select(a => a.Value)
                .ToList();
        }
```

(Der Filter-Block ist 1:1 der heutige Code — nur aus `InitCompileAsync` herausgezogen.)

- [ ] **Step 2: Build**

Run: `dotnet build TryMudEx/TryMudEx.slnx`
Expected: 0 Fehler

- [ ] **Step 3: Commit**

```bash
git add TryMudEx/Try.Core/CompilationService.cs
git commit -m "load framework references once per app lifetime"
```

---

### Task 6: E2E-Verifikation (manuell, kein Commit)

**Files:** keine Änderungen — reiner Verifikationstask.

- [ ] **Step 1: TryMudEx lokal starten**

Run: `dotnet run --project TryMudEx/TryMudEx.Server/TryMudEx.Server.csproj`
Expected: Server startet, App unter der ausgegebenen URL (typisch `https://localhost:7161` oder laut launchSettings) erreichbar.

- [ ] **Step 2: Humanizer-Szenario**

Im Browser:
1. Playground öffnen, NuGet-Manager öffnen (Paket-Icon in der Statusleiste unten)
2. `Humanizer` (Meta-Paket, aktuelle 2.x) installieren
3. In `__Main.razor` einfügen:

```razor
@using Humanizer;

<MudText>@("PascalCaseString".Humanize())</MudText>
<MudText>@(TimeSpan.FromDays(3).Humanize())</MudText>
```

4. Run (F5-Button/Ctrl+S)

Expected: kompiliert ohne Fehler, Preview zeigt `Pascal case string` und `3 days`. (Vorher: Compile-Fehler, weil Humanizer-DLLs fehlten.)

- [ ] **Step 3: Cache-Verifikation**

Browser-DevTools → Network → Seite neu laden → Paket erneut laden lassen (Run):
Expected: `api/nuget/package/...`-Requests kommen aus dem Disk/Memory-Cache (Status `200 (from cache)` bzw. kein erneuter Netz-Roundtrip); Response-Header enthält `Cache-Control: public, max-age=31536000, immutable`. `/_framework/*.dll`-Flut erscheint nur beim ersten Compile nach App-Start, nicht bei Folge-Compiles.

- [ ] **Step 4: Regressionscheck**

Ein bestehendes Sample laden (Samples-Menü) und ausführen.
Expected: kompiliert und rendert wie vorher.

---

## Self-Review (erledigt)

- Spec-Coverage Phase 1: 1.1 transitive+TFM (Tasks 1-3), 1.2 HTTP-Cache (Task 4), 1.3 Referenz-Cache (Task 5), Akzeptanzkriterien (Task 6). ✓
- Keine Platzhalter; jeder Code-Step enthält vollständigen Code. ✓
- Typkonsistenz: `NugetDependency(string Id, string Version)` einheitlich; `SelectBestLibFolder`/`GetDependencies`/`ParseMinVersion`/`IsFrameworkPackage` in Task 3 exakt wie in Tasks 1-2 definiert. `_dependencyCache`-Feld in Task 3 deklariert und verwendet. ✓
