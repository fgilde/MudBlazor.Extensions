using SharpCompress.Archives.Rar;
using System.IO.Compression;
using SharpCompress.Archives;

namespace MudBlazor.Extensions.Helper;

public class ArchiveConverter
{
    /// <summary>
    /// Converts a rar to a zip
    /// </summary>    
    public static MemoryStream ConvertRarToZip(Stream rarStream)
    {
        var zipStream = new MemoryStream();
        var fileStreams = new Dictionary<string, MemoryStream>();

        try
        {
            // Extract RAR to MemoryStreams
            using (var archive = RarArchive.Open(rarStream))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        var ms = new MemoryStream();
                        entry.WriteTo(ms);
                        ms.Position = 0;
                        fileStreams.Add(entry.Key, ms);
                    }
                }
            }

            // Create a ZIP file from MemoryStreams
            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                foreach (var fileStreamPair in fileStreams)
                {
                    fileStreamPair.Value.Position = 0;
                    var entry = zip.CreateEntry(fileStreamPair.Key);
                    using var entryStream = entry.Open();
                    fileStreamPair.Value.CopyTo(entryStream);
                }
            }

            // Reset the position so the stream can be read from the beginning
            zipStream.Position = 0;

            return zipStream;
        }
        finally
        {
            // Dispose all MemoryStreams
            foreach (var ms in fileStreams.Values)
            {
                ms.Dispose();
            }
        }
    }

}