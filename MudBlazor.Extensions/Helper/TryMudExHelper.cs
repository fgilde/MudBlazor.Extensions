using Microsoft.JSInterop;
using System.IO.Compression;
using System.Text;
using MudBlazor.Extensions.Components;

namespace MudBlazor.Extensions.Helper;

public static class TryMudExHelper
{
    private const string TRY_URL = "https://try.mudex.org"; //  "https://localhost:44394";
    //private const string TRY_URL = "https://localhost:44394";

    public static async Task EditCodeInTryMudexAsync(string code, IJSRuntime jsRuntime)
    {
        code = MudExCodeView.StripMarkdownCodeFences(code);
        var baseUrl = TRY_URL;
        var client = new HttpClient();

        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var byteArray = Encoding.UTF8.GetBytes(code);
            var codeEntry = archive.CreateEntry("__Main.razor");
            await using var entryStream = codeEntry.Open();
            entryStream.Write(byteArray);
        }
        memoryStream.Position = 0;

        var inputData = new StreamContent(memoryStream);
        var response = await client.PostAsync($"{baseUrl}/api/snippets", inputData);
        var snippetId = await response.Content.ReadAsStringAsync();
        _ = jsRuntime.InvokeVoidAsync("window.open", $"{baseUrl}/snippet/{snippetId}");
    }
}