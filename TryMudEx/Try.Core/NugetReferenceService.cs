using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Try.Core;
using Nextended.Core.Extensions;

public class NugetReferenceService
{
    private readonly HttpClient _httpClient = new();
    private readonly ConcurrentDictionary<string, List<byte[]>> _packageCache = new();

    public async Task<IEnumerable<MetadataReference>> GetMetadataReferencesAsync(IEnumerable<INugetPackageReference> packages, Func<string, Task> updateStatusFunc)
    {
        var references = new List<MetadataReference>();

        foreach (var package in packages)
        {
            await (updateStatusFunc?.Invoke($"Loading Nuget package {package.Id} {package.Version}") ?? Task.CompletedTask);
            var streams = await EnsurePackageDownloadedAsync(package);
            if (streams?.Any() == true)
            {
                references.AddRange(streams.Select(stream => MetadataReference.CreateFromStream(stream)));
            }
        }

        return references;
    }

    private async Task<List<Stream>> EnsurePackageDownloadedAsync(INugetPackageReference package)
    {
        var cacheKey = $"{package.Id}.{package.Version}";
        if (!_packageCache.TryGetValue(cacheKey, out var bytes))
        {
            var streams = await DownloadAndExtractPackageAsync(package);
            _packageCache.TryAdd(cacheKey, streams.Select(s => s.ToByteArray()).ToList());
            return streams;
        }
        
        var memoryStreams = bytes.Select(b => new MemoryStream(b) as Stream).ToList();
        memoryStreams.Apply(s => s.Seek(0, SeekOrigin.Begin));
        return memoryStreams;
    }

    private async Task<List<Stream>> DownloadAndExtractPackageAsync(INugetPackageReference package)
    {
        var packageId = package.Id;
        var version = package.Version;
        var packageUrl = $"https://api.nuget.org/v3-flatcontainer/{packageId}/{version}/{packageId}.{version}.nupkg";

        using var response = await _httpClient.GetAsync(packageUrl);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        memoryStream.Seek(0, SeekOrigin.Begin);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

        var dllStreams = new List<Stream>();
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                var entryStream = new MemoryStream();
                using var entryOriginalStream = entry.Open();
                await entryOriginalStream.CopyToAsync(entryStream);
                entryStream.Seek(0, SeekOrigin.Begin); // Rewind the stream for future reading
                dllStreams.Add(entryStream);
            }
        }

        return dllStreams;
    }
}
