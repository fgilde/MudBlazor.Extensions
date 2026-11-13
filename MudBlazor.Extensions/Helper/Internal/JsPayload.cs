using System.Text.Json;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Helper.Internal;

internal static class JsPayload
{
    public const long MaxBytes = 64L * 1024 * 1024;
    public const int MaxItems = 100_000;

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static async Task<T> ReadAsync<T>(T value, IJSStreamReference stream)
    {
        if (stream == null)
            return value;
        await using (stream)
        {
            await using var data = await stream.OpenReadStreamAsync(MaxBytes);
            return await JsonSerializer.DeserializeAsync<T>(data, Options);
        }
    }

    public static async Task<T[]> ReadListAsync<T>(T[] value, IJSStreamReference stream)
    {
        var list = await ReadAsync(value, stream);
        return list is { Length: <= MaxItems } ? list : null;
    }
}
