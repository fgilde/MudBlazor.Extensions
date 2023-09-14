using System.IO.Compression;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;
using Nextended.Blazor.Models;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Services;

public class MudExFileService
{
    private readonly HttpClient _httpClient;

    public MudExFileService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string ReadFromStream(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();        
    }

    public async Task<string> ReadFromFileDisplayInfosAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        // Here we load the json string for given file
        if (fileDisplayInfos.ContentStream is {Length: > 0, CanRead: true})
            return ReadFromStream(await CopyStreamAsync(fileDisplayInfos.ContentStream)); // If we have already a valid stream we can use it
        if (DataUrl.TryParse(fileDisplayInfos.Url, out var data)) // If not but given url is a data url we can use the bytes from it
            return ReadFromStream(new MemoryStream(data.Bytes));
        if (!string.IsNullOrEmpty(fileDisplayInfos.Url)) // Otherwise we load the file        
            return await ReadFromUrlAsync(fileDisplayInfos.Url);
        
        return null;
    }

    public async Task<string> ReadFromUrlAsync(string url)
    {
        var stream = await ReadStreamAsync(url);
        return ReadFromStream(stream);
    }

    public Task<Stream> ReadStreamAsync(string url) => (_httpClient ?? new HttpClient()).GetStreamAsync(url);

    public async Task<string> ReadDataUrlForStreamAsync(Stream stream, string mimeType = "application/octet-stream") 
        => await DataUrl.GetDataUrlAsync((await CopyStreamAsync(stream)).ToByteArray(), mimeType);


    public async Task<(IList<ZipBrowserFile> Entries, HashSet<ZipStructure> Structure)?> ReadArchiveAsync(Stream stream, string rootFolderName, string contentType)
    {
        try
        {
            Stream contentStream = await CopyStreamAsync(stream);
            if (MimeType.IsRar(contentType))
                contentStream = ArchiveConverter.ConvertRarToZip(contentStream);

            var zipEntries = GetZipEntriesAsync(contentStream);
            var zipStructure = ZipStructure.CreateStructure(zipEntries, rootFolderName).ToHashSet();
            return (zipEntries, zipStructure);    
            
        }
        catch (Exception e)
        {
            Console.Write(e.Message);
        }
        return null;
    }

    private List<ZipBrowserFile> GetZipEntriesAsync(Stream stream)
    {
        //await using var ms = new MemoryStream();
        //await stream.CopyToAsync(ms);

        return new ZipArchive(stream).Entries.Select(entry => new ZipBrowserFile(entry)).ToList();
    }

    // TODO instead of copy we should read chunked as buffer byte[]
    private async Task<Stream> CopyStreamAsync(Stream input)
    {
        if (input == null)
            return null;
        // Ensure the input stream's position is at the beginning
        input.Position = 0;

        MemoryStream memoryStream = new MemoryStream();
        await input.CopyToAsync(memoryStream);

        // Reset the memory stream's position to the beginning before returning
        memoryStream.Position = 0;

        return memoryStream;
    }

}