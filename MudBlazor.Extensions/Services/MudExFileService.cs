using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper.Internal;
using Nextended.Blazor.Models;
using Nextended.Core.Extensions;
using SharpCompress.Archives;
using System.IO.Compression;
using MudBlazor.Extensions.Helper;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Nextended.Blazor.Extensions;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Services;

public class MudExFileService : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private List<Stream> _streams = new();
    private List<string> _blobUris = new();

    public MudExFileService(IJSRuntime jsRuntime, IServiceProvider serviceProvider)
    {
        _httpClient = serviceProvider.GetService<HttpClient>() ?? new HttpClient();
        _jsRuntime = jsRuntime;
    }

    public string ReadAsStringFromStream(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();        
    }

    public async Task<string> ReadAsStringFromFileDisplayInfosAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        // Here we load the json string for given file
        if (fileDisplayInfos.ContentStream is { Length: > 0, CanRead: true })
        {
            var copy = await CopyStreamAsync(fileDisplayInfos.ContentStream);
            return ReadAsStringFromStream(copy); // If we have already a valid stream we can use it
        }
        if (DataUrl.TryParse(fileDisplayInfos.Url, out var data)) // If not but given url is a data url we can use the bytes from it
            return ReadAsStringFromStream(new MemoryStream(data.Bytes));
        if (!string.IsNullOrEmpty(fileDisplayInfos.Url)) // Otherwise we load the file        
            return await ReadAsStringFromUrlAsync(fileDisplayInfos.Url);
        
        return null;
    }

    public async Task<string> ReadAsStringFromUrlAsync(string url) => ReadAsStringFromStream(await ReadStreamAsync(url));

    public Task<Stream> ReadStreamAsync(string url) => (_httpClient ?? new HttpClient()).GetStreamAsync(url);

    public async Task<string> ReadDataUrlForStreamAsync(Stream stream, string mimeType, bool useBlob)
    {        
        var copy = await CopyStreamAsync(stream);
        return await CreateDataUrlAsync(copy.ToByteArray(), mimeType, useBlob);
    }

    public Task<string> CreateDataUrlAsync(byte[] bytes, string mimeType, bool useBlob)
    {        
        return useBlob 
            ? CreateBlobUrlAsync(bytes, mimeType)
            : DataUrl.GetDataUrlAsync(bytes, mimeType);
    }

    public async Task<string> CreateDataUrlAsync(IBrowserFile file, bool useBlob)
    {        
        return useBlob
               ? await CreateBlobUrlAsync(await file.GetBytesAsync(), file.ContentType)
               : await file.GetDataUrlAsync();
    }

    private async Task<string> CreateBlobUrlAsync(byte[] bytes, string mimeType)
    {        
        var blobUrl = await _jsRuntime.InvokeAsync<string>("MudExUriHelper.createBlobUrlFromByteArray", bytes, mimeType);
        _blobUris.Add(blobUrl);
        return blobUrl;
    }

    private async Task RevokeBlobUrlAsync(string blobUrl)
    {
        await _jsRuntime.InvokeVoidAsync("MudExUriHelper.revokeBlobUrl", blobUrl);
    }

    public async Task<(HashSet<MudExArchiveStructure> Structure, List<IArchivedBrowserFile> List )> ReadArchiveAsync(Stream stream, string rootFolderName, string contentType)
    {        
        var contentStream = await CopyStreamAsync(stream);
        var archive = ArchiveFactory.Open(contentStream);
        if (archive.Entries.Count() == 1 && archive.Entries.First().CompressionType == SharpCompress.Common.CompressionType.GZip)
        {
            using var memoryStream = new MemoryStream();
            archive.Entries.First().WriteTo(memoryStream);
            return await ReadArchiveAsync(memoryStream, rootFolderName, contentType);
        }

        var validEntries = archive.Entries.Select(entry => new MudExArchivedBrowserFile(entry) as IArchivedBrowserFile).ToList();
        var res = MudExArchiveStructure.CreateStructure(validEntries, rootFolderName);
        var structure = new[] { res }.ToHashSet();
        var list = structure.Recursive(z => z?.Children ?? Enumerable.Empty<MudExArchiveStructure>()).Where(c => c is { IsDirectory: false, BrowserFile: not null }).Select(c => c.BrowserFile).ToList();
        return (structure, list);
    }

    public async Task<(HashSet<MudExArchiveStructure> Structure, List<IArchivedBrowserFile> List)> ReadArchiveWithSystemCompressionAsync(Stream stream, string rootFolderName, string contentType)
    {
        var contentStream = await CopyStreamAsync(stream);
        contentStream = ArchiveConverter.ConvertToSystemCompressionZip(contentStream); // Converts archive to zip otherwise system compression cant read
        var entries = new ZipArchive(contentStream).Entries.Select(entry => new ZipBrowserFile(entry) as IArchivedBrowserFile).ToList();
        var res = MudExArchiveStructure.CreateStructure(entries, rootFolderName);
        var structure = new[] { res }.ToHashSet();
        var list = structure.Recursive(z => z?.Children ?? Enumerable.Empty<MudExArchiveStructure>()).Where(c => c is { IsDirectory: false, BrowserFile: not null }).Select(c => c.BrowserFile).ToList();
        return (structure, list);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var uri in _blobUris)
            await RevokeBlobUrlAsync(uri);
        foreach (var stream in _streams)
            await stream.DisposeAsync();
        _streams.Clear();
    }

    private async Task<Stream> CopyStreamAsync(Stream stream)
    {
        var res = await stream.CopyStreamAsync();
        _streams.Add(res);
        return res;
    }
}