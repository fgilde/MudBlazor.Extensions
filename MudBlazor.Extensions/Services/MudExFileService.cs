using MudBlazor.Extensions.Components;
using Nextended.Blazor.Models;

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
        if (fileDisplayInfos.ContentStream?.Length > 0)
            return ReadFromStream(fileDisplayInfos.ContentStream); // If we have already a valid stream we can use it
        if (DataUrl.TryParse(fileDisplayInfos.Url, out var data)) // If not but given url is a data url we can use the bytes from it
            return ReadFromStream(new MemoryStream(data.Bytes));
        if (!string.IsNullOrEmpty(fileDisplayInfos.Url)) // Otherwise we load the file        
            return await ReadFromUrlAsync(fileDisplayInfos.Url);
        
        return null;
    }

    public async Task<string> ReadFromUrlAsync(string url)
    {
        var stream = await (_httpClient ?? new HttpClient()).GetStreamAsync(url);
        return ReadFromStream(stream);
    }
}