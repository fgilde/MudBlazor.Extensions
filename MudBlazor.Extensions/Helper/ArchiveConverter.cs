using System.IO.Compression;
using SharpCompress.Archives;

namespace MudBlazor.Extensions.Helper;

public class ArchiveConverter
{
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

    public static Stream ConvertToSystemCompressionZip(Stream unknownArchiveStream)
    {
        using var archive = ArchiveFactory.Open(unknownArchiveStream);
        return archive.Type == SharpCompress.Common.ArchiveType.Zip ? unknownArchiveStream : ConvertArchiveToZip(archive);
    }
    
}