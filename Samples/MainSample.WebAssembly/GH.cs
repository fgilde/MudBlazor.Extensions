using Nextended.Core.Extensions;
using static System.Net.WebRequestMethods;

namespace MainSample.WebAssembly;

internal static class GH
{
    public const string BaseAddress = "https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions";
    public static string BaseAddressExtensions = $"{BaseAddress}/main/MudBlazor.Extensions";
    public static string BaseAddressSampleApp = $"{BaseAddress}/main/Samples/MainSample.WebAssembly";
    
    public static string Path(string file) => $"{BaseAddressSampleApp}{file.EnsureStartsWith("/")}";

    public static async Task<string> LoadDocumentation(this HttpClient http, string doc)
    {
        if (!doc.Contains("Docs/"))
            doc = $"Docs/{doc}";
        var url = $"{BaseAddressExtensions}{doc.EnsureStartsWith("/")}";
        return await Load(http, url);
    }


    private static async Task<string> Load(HttpClient http, string url)
    {
        try
        {
            var res = await http.GetStringAsync(url);
            return res;
        }
        catch
        {
            return string.Empty;
        }
    }
}