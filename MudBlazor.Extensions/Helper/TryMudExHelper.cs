using Microsoft.JSInterop;
using System.IO.Compression;
using System.Text;
using MudBlazor.Extensions.Components;

namespace MudBlazor.Extensions.Helper;

public static class TryMudExHelper
{
    private const string TRY_URL = "https://try.mudex.org"; //  "https://localhost:44394";
                                                           
    public static Task EditCodeInTryMudexAsync(string code, IJSRuntime jsRuntime)
    {
        if (code == null) throw new ArgumentNullException(nameof(code));

        var files = new Dictionary<string, string>
        {
            { "__Main.razor", code }
        };

        return EditCodeInTryMudexAsync(files, jsRuntime);
    }

    public static Task EditCodeInTryMudexAsync(string[] codes, IJSRuntime jsRuntime)
    {
        if (codes == null) throw new ArgumentNullException(nameof(codes));
        if (codes.Length == 0) throw new ArgumentException("At least one code snippet is required.", nameof(codes));

        var files = BuildFilesFromCodeArray(codes);
        return EditCodeInTryMudexAsync(files, jsRuntime);
    }

    public static async Task EditCodeInTryMudexAsync(
        IDictionary<string, string> files,
        IJSRuntime jsRuntime)
    {
        if (files == null) throw new ArgumentNullException(nameof(files));
        if (files.Count == 0) throw new ArgumentException("At least one file is required.", nameof(files));
        if (jsRuntime == null) throw new ArgumentNullException(nameof(jsRuntime));

        await using var memoryStream = new MemoryStream();

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var kvp in files)
            {
                var fileName = kvp.Key;
                var code = kvp.Value ?? string.Empty;

                code = MudExCodeView.StripMarkdownCodeFences(code);

                var entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                var bytes = Encoding.UTF8.GetBytes(code);
                await entryStream.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        memoryStream.Position = 0;

        using var content = new StreamContent(memoryStream);
        var client = new HttpClient();
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var response = await client.PostAsync($"{TRY_URL}/api/snippets", content);
        response.EnsureSuccessStatusCode();

        var snippetId = await response.Content.ReadAsStringAsync();
        await jsRuntime.InvokeVoidAsync("window.open", $"{TRY_URL}/snippet/{snippetId}");
    }

    private static IDictionary<string, string> BuildFilesFromCodeArray(string[] codes)
    {
        var result = new Dictionary<string, string>();

        for (var i = 0; i < codes.Length; i++)
        {
            var code = codes[i] ?? string.Empty;

            string fileName;
            if (i == 0)
            {
                fileName = "__Main.razor";
            }
            else
            {
                var ext = GetExtensionFromCodeBlock(code);
                fileName = $"File{i}.{ext}";
            }

            result[fileName] = code;
        }

        return result;
    }

    private static string GetExtensionFromCodeBlock(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "razor";

        using var reader = new StringReader(code);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();

            if (line.StartsWith("```"))
            {
                var lang = line.Trim('`').Trim().ToLowerInvariant();

                if (lang.Contains("c#") || lang.Contains("csharp") || lang.EndsWith("cs"))
                    return "cs";

                if (lang.Contains("razor") || lang.Contains("cshtml"))
                    return "razor";

                break;
            }

            if (!string.IsNullOrWhiteSpace(line))
                break;
        }

        return "razor";
    }
}