using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper.Internal;
using Nextended.Blazor.Models;
using Nextended.Core.Extensions;
using SharpCompress.Archives;

namespace MudBlazor.Extensions.Services;

public class MudExFileService
{
    private readonly HttpClient _httpClient;

    public MudExFileService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string ReadAsStringFromStream(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();        
    }

    public async Task<string> ReadAsStringFromFileDisplayInfosAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        // Here we load the json string for given file
        if (fileDisplayInfos.ContentStream is {Length: > 0, CanRead: true})
            return ReadAsStringFromStream(await fileDisplayInfos.ContentStream.CopyStreamAsync()); // If we have already a valid stream we can use it
        if (DataUrl.TryParse(fileDisplayInfos.Url, out var data)) // If not but given url is a data url we can use the bytes from it
            return ReadAsStringFromStream(new MemoryStream(data.Bytes));
        if (!string.IsNullOrEmpty(fileDisplayInfos.Url)) // Otherwise we load the file        
            return await ReadAsStringFromUrlAsync(fileDisplayInfos.Url);
        
        return null;
    }

    public async Task<string> ReadAsStringFromUrlAsync(string url) => ReadAsStringFromStream(await ReadStreamAsync(url));

    public Task<Stream> ReadStreamAsync(string url) => (_httpClient ?? new HttpClient()).GetStreamAsync(url);

    public async Task<string> ReadDataUrlForStreamAsync(Stream stream, string mimeType = "application/octet-stream") 
        => await DataUrl.GetDataUrlAsync((await stream.CopyStreamAsync()).ToByteArray(), mimeType);
    
    public async Task<HashSet<ArchiveStructure>> ReadArchiveAsync(Stream stream, string rootFolderName, string contentType)
    {
        var contentStream = await stream.CopyStreamAsync();
        var archive = ArchiveFactory.Open(contentStream);
        if (archive.Entries.Count() == 1 && archive.Entries.First().CompressionType == SharpCompress.Common.CompressionType.GZip)
        {
            using var memoryStream = new MemoryStream();
            archive.Entries.First().WriteTo(memoryStream);
            return await ReadArchiveAsync(memoryStream, rootFolderName, contentType);
        }

        var validEntries = archive.Entries.Select(entry => new MudExArchivedBrowserFile(entry) as IArchivedBrowserFile).ToList();
        var res = ArchiveStructure.CreateStructure(validEntries, rootFolderName);
        return new[] { res }.ToHashSet();
    }

    //public async Task<HashSet<ArchiveStructure>> ReadArchiveWithSystemCompressionAsync(Stream stream, string rootFolderName, string contentType)
    //{
    //    var contentStream = await stream.CopyStreamAsync();
    //    contentStream = ArchiveConverter.ConvertToSystemCompressionZip(contentStream); // Converts archive to zip otherwise system compression cant read
    //    var entries = new ZipArchive(contentStream).Entries.Select(entry => new ZipBrowserFile(entry) as IArchivedBrowserFile).ToList();
    //    var res = ArchiveStructure.CreateStructure(entries, rootFolderName);
    //    return new[] { res }.ToHashSet();
    //}
}