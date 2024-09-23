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
        yield return (CreateSampleFile("Header.tiff", "image/tiff"));
        yield return (CreateSampleFile("SweetieBubbleGum-Regular.ttf", "font/ttf"));
        yield return (CreateSampleFile("4.Unified-Voice.mp3", "audio/mpeg3"));
        yield return (CreateSampleFile("WordDokument.doc", "application/msword"));
        yield return (CreateSampleFile("ExcelSheet.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
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
        catch
        {
            return null;
        }
    }
    
    private SampleFile CreateSampleFile(string filename, string contentType)
    {
        var absoluteUri = _navigationManager.ToAbsoluteUri($"sample-data/{filename}").AbsoluteUri;
        if (filename.EndsWith(".md"))
        {
            // Hack because not delivered by azure websites server
            absoluteUri = $"https://www.mudex.org/sample-data/{filename}";
        }
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

    public long Size
    {
        get
        {
            try
            {
                return Stream?.Length ?? 0;
            }
            catch
            {
                return 0;
            }
        }
    }

    public string ReadableSize => Nextended.Blazor.Extensions.BrowserFileExtensions.GetReadableFileSize(Size);
    public Stream Stream { get; set; }
}