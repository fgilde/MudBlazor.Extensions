using System.Collections;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components;
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
        yield return (SampleFile("ARar.rar", "application/x-rar-compressed"));
        yield return (SampleFile("sample.zip", "application/zip"));
        yield return (SampleFile("sample.pdf", "application/pdf"));
        yield return (SampleFile("weather.json", "text/plain"));
        yield return (SampleFile("logo.png", "image/png"));
        yield return (SampleFile("readme.md", "text/markdown"));
    }

    public async Task<IEnumerable<(Stream stream, string contentType, string name)>>GetSampleFilesWithStreamAsync()
    {
        var tasks = GetSampleFiles().Select(Sample);
        return await Task.WhenAll(tasks);
    }

    private Task<MudExFileDisplayContentErrorResult> HandleContentError(IMudExFileDisplayInfos arg)
    {
        if (arg.ContentType.Contains("word"))
        {
            return Task.FromResult(MudExFileDisplayContentErrorResult
                .RedirectTo("https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTiZiqnBKWS8NHcKbRH04UkYjrCgxUMz6sVNw&usqp=CAU", "image/png")
                .WithMessage("No word plugin found we display a sheep"));
        }
        return Task.FromResult(MudExFileDisplayContentErrorResult.Unhandled);
    }

    private async Task<(Stream stream, string contentType, string name)> Sample((string url, string contentType, string name) sampleFile)
    {
        var stream = await _fileService.ReadStreamAsync(sampleFile.url);
        return (stream, sampleFile.contentType, sampleFile.name);
    }

    private (string url, string contentType, string name) SampleFile(string filename, string contentType)
    {
        var absoluteUri = _navigationManager.ToAbsoluteUri($"sample-data/{filename}").AbsoluteUri;
        return (absoluteUri, contentType, filename);
    }
}