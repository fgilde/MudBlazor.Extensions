using Microsoft.JSInterop;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Helper;

public static class TryMudExHelper
{
    private const string TRY_URL = "https://try.mudex.org"; //  "https://localhost:44394";
    //private const string TRY_URL = "https://localhost:44394";

    public static async Task EditCodeInTryMudexAsync(string code, IJSRuntime jsRuntime)
    {
        code = StripMarkdownCodeFences(code);
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

    private static readonly Regex CodeFenceRegex = new Regex(
        @"^\s*```[a-zA-Z0-9]*\s*\r?\n(?<code>[\s\S]*?)\r?\n```(?:\s*)$",
        RegexOptions.Compiled);

    public static string StripMarkdownCodeFences(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var match = CodeFenceRegex.Match(input);
        if (!match.Success)
            return input; // Kein Markdown-Fence -> unverändert zurück

        return match.Groups["code"].Value;
    }
}