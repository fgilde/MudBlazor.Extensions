using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Try.Core;
using Nextended.Core.Extensions;
using MudBlazor.Extensions.Services;


public class NugetReferenceService
{
    private readonly HttpClient _httpClient;
    private readonly MudExFileService _fileService;
    private static readonly ConcurrentDictionary<string, List<(string AssemblyName, byte[] AssemblyBytes)>> _packageCache = new();

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
        foreach (var package in packages)
        {
            await (updateStatusFunc?.Invoke($"Loading Nuget package {package.Id} {package.Version}") ?? Task.CompletedTask);

            if (CoreConstants.DefaultPackages.All(dp => dp.Id != package.Id))
            {
                var assemblyInfos = await EnsurePackageDownloadedAsync(package);
                if (assemblyInfos?.Any() == true)
                {
                    results.AddRange(assemblyInfos);
                }
            }
        }
        return results;
    }

    public async Task<IEnumerable<PortableExecutableReference>> GetMetadataReferencesAsync(IEnumerable<INugetPackageReference> packages, Func<string, Task> updateStatusFunc = null)
    {
        await (updateStatusFunc?.Invoke($"Loading packages...") ?? Task.CompletedTask);
        var assemblies = await GetAssemblyStreamsAsync(packages, updateStatusFunc);
        return assemblies.Select(info => MetadataReference.CreateFromStream(info.Stream));
    }

    private async Task<List<(string AssemblyName, MemoryStream Stream)>> EnsurePackageDownloadedAsync(INugetPackageReference package)
    {
        var cacheKey = $"{package.Id}.{package.Version}";
        if (!_packageCache.TryGetValue(cacheKey, out var assemblyInfo))
        {
            var assemblyInfos = await DownloadAndExtractPackageAsync(package);
            _packageCache.TryAdd(cacheKey, assemblyInfos.Select(info => (info.AssemblyName, info.Stream.ToByteArray())).ToList());
            return assemblyInfos;
        }
        
        List<(string AssemblyName, MemoryStream Stream)> results = assemblyInfo.Select(i => (i.AssemblyName, new MemoryStream(i.AssemblyBytes))).ToList();
        results.Select(r => r.Stream).Apply(s => s.Seek(0, SeekOrigin.Begin));
        return results;
    }

    private async Task<List<(string AssemblyName, MemoryStream Stream)>> DownloadAndExtractPackageAsync(INugetPackageReference package)
    {
        var packageId = package.Id;
        var version = package.Version;
        //var packageUrl = $"https://api.nuget.org/v3-flatcontainer/{packageId}/{version}/{packageId}.{version}.nupkg";
        //var packageUrl = $"https://www.nuget.org/api/v2/package/{packageId}/{version}";
        var packageUrl = $"api/nuget/package/{packageId}/{version}";

        using var response = await _httpClient.GetAsync(packageUrl);
        response.EnsureSuccessStatusCode();

        
        using var stream = await response.Content.ReadAsStreamAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        var entries = await _fileService.ReadArchiveAsync(memoryStream, package.Id, "application/zip");
        var dlls = entries.List.Where(entry => entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).DistinctBy(e => e.FullName).ToArray();
        var dllStreams = new List<(string AssemblyName, MemoryStream Stream)>();

        foreach (var dll in dlls)
        {
            var entryStream = new MemoryStream();
            using var entryOriginalStream = dll.OpenReadStream();
            await entryOriginalStream.CopyToAsync(entryStream);
            entryStream.Seek(0, SeekOrigin.Begin); // Rewind the stream for future reading
            dllStreams.Add((dll.Name, entryStream));
        }


        //using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

        //var dllStreams = new List<Stream>();
        //foreach (var entry in archive.Entries)
        //{
        //    // Check if the entry is a .dll file in a 'lib' subdirectory
        //    if (entry.FullName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) &&
        //        entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        //    {
        //        var entryStream = new MemoryStream();
        //        using var entryOriginalStream = entry.Open();
        //        await entryOriginalStream.CopyToAsync(entryStream);
        //        entryStream.Seek(0, SeekOrigin.Begin); // Rewind the stream for future reading
        //        dllStreams.Add(entryStream);
        //    }
        //}

        return dllStreams;
    }
}
