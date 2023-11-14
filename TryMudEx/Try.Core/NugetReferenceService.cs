using System;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;

namespace Try.Core;

public class NugetReferenceService
{
    private readonly string _cacheDirectory;
    private readonly HttpClient _httpClient;

    public NugetReferenceService()
    {
        _cacheDirectory = "__nugetcache";
        _httpClient = new HttpClient();
    }

    public async Task<IEnumerable<MetadataReference>> GetMetadataReferencesAsync(IEnumerable<INugetPackageReference> packages)
    {
        var references = new List<MetadataReference>();

        foreach (var package in packages)
        {
            var packagePath = await EnsurePackageDownloadedAsync(package);
            var dllPaths = ExtractDlls(packagePath);
            references.AddRange(dllPaths.Select(dllPath => MetadataReference.CreateFromFile(dllPath)));
        }

        return references;
    }

    private async Task<string> EnsurePackageDownloadedAsync(INugetPackageReference package)
    {
        var packageCachePath = Path.Combine(_cacheDirectory, $"{package.Id}.{package.Version}");

        // Check if the package is already downloaded
        if (!Directory.Exists(packageCachePath))
        {
            // Download and extract the package
            // Here you need to implement the logic to download and extract NuGet packages
            // This is a placeholder for actual download and extraction logic
            await DownloadAndExtractPackageAsync(package, packageCachePath);
        }

        return packageCachePath;
    }

    private IEnumerable<string> ExtractDlls(string packagePath)
    {
        // Extract .dll files from the NuGet package
        // This is a placeholder for the actual extraction logic
        // You would typically find .dll files in the 'lib' folder of the package
        var dllPaths = Directory.GetFiles(packagePath, "*.dll", SearchOption.AllDirectories);
        return dllPaths;
    }

    private async Task DownloadAndExtractPackageAsync(INugetPackageReference package, string outputPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var packageId = package.Id;
            var version = package.Version;
            var packageUrl = $"https://api.nuget.org/v3-flatcontainer/{packageId}/{version}/{packageId}.{version}.nupkg";

            // Download the .nupkg file
            using var response = await _httpClient.GetAsync(packageUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);

            // Extract the .nupkg file
            memoryStream.Seek(0, SeekOrigin.Begin);
            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
            foreach (var entry in archive.Entries)
            {
                var fullPath = Path.Combine(outputPath, entry.FullName);
                if (string.IsNullOrEmpty(entry.Name)) // Entry is a directory
                {
                    Directory.CreateDirectory(fullPath);
                }
                else // Entry is a file
                {
                    entry.ExtractToFile(fullPath, overwrite: true);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}