namespace MudBlazor.Extensions.Helper.Internal;

internal static class StreamHelper
{
    // TODO instead of copy we should read chunked as buffer byte[]
    public static async Task<Stream> CopyStreamAsync(this Stream input)
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