using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Services;

namespace MainSample.WebAssembly;

public class SampleDataService
{

    private readonly MudExFileService _fileService;
    private readonly NavigationManager _navigationManager;

    public SampleDataService(MudExFileService fileService, NavigationManager navigationManager)
    {
        _fileService = fileService;
        _navigationManager = navigationManager;
    }

    public IEnumerable<(string url, string contentType, string name)> GetSampleFiles()
    {
        yield return (SampleFile("Archive.zip", "application/zip"));
        yield return (SampleFile("AnotherRar.rar", "application/x-rar-compressed"));
        yield return (SampleFile("npm-example-0.0.20.tgz", "application/tar+gzip"));
        yield return (SampleFile("RarArchive.rar", "application/x-rar-compressed"));
        yield return (SampleFile("TarArchive.tar", "application/x-tar"));
        yield return (SampleFile("SevenZipArchive.7z", "application/x-7z-compressed"));        
        yield return (SampleFile("LargeZipArchive.zip", "application/zip"));
        yield return (SampleFile("sample.pdf", "application/pdf"));
        yield return (SampleFile("weather.json", "text/plain"));
        yield return (SampleFile("logo.png", "image/png"));
        yield return (SampleFile("readme.md", "text/markdown"));
    }

    public async Task<IEnumerable<(Stream stream, string contentType, string name)>>GetSampleFilesWithStreamAsync()
    {
        return (await Task.WhenAll(GetSampleFiles().Select(Sample))).Where(s => s != null).Select(s => s.Value);
    }

    private async Task<(Stream stream, string contentType, string name)?> Sample((string url, string contentType, string name) sampleFile)
    {
        try
        {
            var stream = await _fileService.ReadStreamAsync(sampleFile.url);
            return (stream, sampleFile.contentType, sampleFile.name);
        }
        catch (Exception e)
        {
            return null;
        }
    }

    private (string url, string contentType, string name) SampleFile(string filename, string contentType)
    {
        var absoluteUri = _navigationManager.ToAbsoluteUri($"sample-data/{filename}").AbsoluteUri;
        return (absoluteUri, contentType, filename);
    }
}