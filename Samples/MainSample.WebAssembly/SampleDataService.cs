using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
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

    public IEnumerable<SampleFile> GetSampleFiles()
    {
        yield return (CreateSampleFile("Archive.zip", "application/zip"));
        yield return (CreateSampleFile("AnotherRar.rar", "application/x-rar-compressed"));
        yield return (CreateSampleFile("npm-example-0.0.20.tgz", "application/tar+gzip"));
        yield return (CreateSampleFile("RarArchive.rar", "application/x-rar-compressed"));
        yield return (CreateSampleFile("TarArchive.tar", "application/x-tar"));
        yield return (CreateSampleFile("SevenZipArchive.7z", "application/x-7z-compressed"));        
        yield return (CreateSampleFile("LargeZipArchive.zip", "application/zip"));
        yield return (CreateSampleFile("sample.pdf", "application/pdf"));
        yield return (CreateSampleFile("weather.json", "text/plain"));
        yield return (CreateSampleFile("logo.png", "image/png"));
        yield return (CreateSampleFile("readme.md", "text/markdown"));
    }

    public async Task<IEnumerable<SampleFileWithStream?>> GetSampleFilesWithStreamAsync()
    {
        return (await Task.WhenAll(GetSampleFiles().Select(SampleWithStream))).Where(s => s != null);
    }

    private async Task<SampleFileWithStream?> SampleWithStream(SampleFile sampleFile)
    {
        try
        {
            var stream = await _fileService.ReadStreamAsync(sampleFile.Url);
            return new (stream, sampleFile.ContentType, sampleFile.Name, sampleFile.Icon);
        }
        catch (Exception e)
        {
            return null;
        }
    }
    
    private SampleFile CreateSampleFile(string filename, string contentType)
    {
        var absoluteUri = _navigationManager.ToAbsoluteUri($"sample-data/{filename}").AbsoluteUri;
        return new (absoluteUri, contentType, filename, BrowserFileExt.IconForFile(contentType));
    }
}

public class BaseFile
{
    public BaseFile(string contentType, string name, string icon)
    {
        ContentType = contentType;
        Color = BrowserFileExt.GetPreferredColor(contentType);
        Name = name;
        Icon = icon;
    }
    public MudExColor Color { get; set; }
    public string ContentType { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
}

public class SampleFile : BaseFile
{
    public SampleFile(string url, string contentType, string name, string icon)
        : base(contentType, name, icon) {Url = url;}

    public string Url { get; set; }
}

public class SampleFileWithStream : BaseFile
{
    public SampleFileWithStream(Stream stream, string contentType, string name, string icon)
        : base(contentType, name, icon) { Stream = stream; }

    public long Size => Stream.Length;
    public string ReadableSize => Nextended.Blazor.Extensions.BrowserFileExtensions.GetReadableFileSize(Size);
    public Stream Stream { get; set; }
}