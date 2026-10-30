using System.IO.Compression;
using SharpCompress.Archives;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// Helper class for converting between different archive formats.
/// </summary>
public class ArchiveConverter
{
    /// <summary>
    /// Extracts the contents of the given archive to a dictionary of memory streams.
    /// </summary>
    public static Dictionary<string, MemoryStream> ExtractToMemoryStreams(IArchive archive)
    {
        var fileStreams = new Dictionary<string, MemoryStream>();
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
        return fileStreams;
    }
    
    /// <summary>
    /// Converts a dictionary of memory streams to a single zip archive memory stream.
    /// </summary>
    public static MemoryStream ConvertMemoryStreamsToZip(Dictionary<string, MemoryStream> fileStreams)
    {
        var zipStream = new MemoryStream();
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
        zipStream.Position = 0;
        return zipStream;
    }

    /// <summary>
    /// Converts the given archive to a zip archive memory stream.
    /// </summary>
    public static MemoryStream ConvertArchiveToZip(IArchive archive)
    {
        Dictionary<string, MemoryStream> fileStreams = null;
        try
        {
            fileStreams = ExtractToMemoryStreams(archive);
            return ConvertMemoryStreamsToZip(fileStreams);
        }
        finally
        {
            if (fileStreams != null)
            {
                foreach (var ms in fileStreams.Values)                
                    ms.Dispose();                
            }
        }
    }

    /// <summary>
    /// Converts the given stream to a zip archive stream using the SharpCompress library.
    /// </summary>
    public static Stream ConvertToSystemCompressionZip(Stream unknownArchiveStream)
    {
        using var archive = OpenArchive(unknownArchiveStream);
        return archive.Type == SharpCompress.Common.ArchiveType.Zip ? unknownArchiveStream : ConvertArchiveToZip(archive);
    }

    /// <summary>
    /// Opens an archive with SharpCompress. Gzip is unpacked first, because SharpCompress 0.50 no longer detects tar.gz (.tgz) streams.
    /// A gzip that does not hold an archive opens as gzip archive with its single file.
    /// </summary>
    public static IArchive OpenArchive(Stream stream)
    {
        stream.Position = 0;
        if (stream.ReadByte() == 0x1f && stream.ReadByte() == 0x8b)
        {
            stream.Position = 0;
            var unpacked = new MemoryStream();
            using (var gzip = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true))
                gzip.CopyTo(unpacked);
            unpacked.Position = 0;
            try
            {
                return ArchiveFactory.OpenArchive(unpacked);
            }
            catch
            {
                unpacked.Dispose(); // no archive inside, e.g. a plain file.txt.gz
            }
        }
        stream.Position = 0;
        return ArchiveFactory.OpenArchive(stream);
    }
}
