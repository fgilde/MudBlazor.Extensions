namespace MudBlazor.Extensions.Helper.Internal;

internal static class StreamHelper
{

    public static async Task ReadStreamInChunksAsync(this Stream stream, byte[] buffer, int chunkSize = 4096, Action<int> bytesReadCallback = null, Func<bool> breakCondition = null)
    {
        int bytesRead;
        int totalBytesRead = 0;

        do
        {
            if (breakCondition != null && breakCondition()) return;

            bytesRead = await stream.ReadAsync(buffer, totalBytesRead, Math.Min(chunkSize, buffer.Length - totalBytesRead));
            totalBytesRead += bytesRead;
            bytesReadCallback?.Invoke(totalBytesRead);
        }
        while (bytesRead > 0 && totalBytesRead < buffer.Length);
    }

    // TODO instead of copy we should read chunked as buffer byte[]
    public static async Task<Stream> CopyStreamAsync(this Stream input)
    {
        if (input == null)
            return null;
        // Ensure the input stream's position is at the beginning
        try
        {
            input.Position = 0;
        }
        catch (Exception e)
        {}

        MemoryStream memoryStream = new MemoryStream();
        await input.CopyToAsync(memoryStream);

        // Reset the memory stream's position to the beginning before returning
        memoryStream.Position = 0;

        return memoryStream;
    }
}