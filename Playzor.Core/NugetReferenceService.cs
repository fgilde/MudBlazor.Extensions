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
using Playzor.Core.Api;


public class NugetReferenceService
{
    private readonly IPlayzorApi _api;
    private readonly MudExFileService _fileService;
    private static readonly ConcurrentDictionary<string, List<(string AssemblyName, byte[] AssemblyBytes)>> _packageCache = new();
    private static readonly ConcurrentDictionary<string, IReadOnlyList<NugetDependency>> _dependencyCache = new();
    private static readonly ConcurrentDictionary<string, PortableExecutableReference[]> _referenceCache = new();

    public NugetReferenceService(IPlayzorApi api, MudExFileService fileService)
    {
        _api = api;
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

        List<(string AssemblyName, MemoryStream Stream)> results = cachedAssemblies.Select(i => (i.AssemblyName, new MemoryStream(i.AssemblyBytes))).ToList();
        results.Select(r => r.Stream).Apply(s => s.Seek(0, SeekOrigin.Begin));
        return (results, cachedDependencies);
    }

    private async Task<(List<(string AssemblyName, MemoryStream Stream)> Assemblies, IReadOnlyList<NugetDependency> Dependencies)> DownloadAndExtractPackageAsync(string packageId, string version)
    {
        // nuget.org sends no CORS headers, so the download goes through the configured api
        using var stream = await _api.GetPackageAsync(packageId, version);
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
