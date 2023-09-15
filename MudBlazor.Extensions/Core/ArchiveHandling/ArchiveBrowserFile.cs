using Nextended.Core;
using SharpCompress.Archives;

namespace MudBlazor.Extensions.Core.ArchiveHandling;
// TODO: Rename to ArchiveEntryBrowserFile
/**
 * Represents a Zip file entry compatiable as BrowserFile
 */
public record ArchiveBrowserFile : IArchiveBrowserFile<IArchiveEntry>
{
    public IArchiveEntry Entry { get; }
    public byte[] FileBytes { get; private set; }

    public ArchiveBrowserFile(IArchiveEntry entry, bool load = true)
    {
        Entry = entry;

        if (load && !entry.IsDirectory)
        {
            using var memoryStream = new MemoryStream();
            entry.WriteTo(memoryStream);
            FileBytes = memoryStream.ToArray();
        }

        Name = Entry.Key.Split('/').LastOrDefault() ?? "";
        Size = Entry.Size;
        LastModified = Entry.LastModifiedTime ?? default;
        FullName = Entry.Key; // Assuming the Key is the full name/path within the archive.
        ContentType = MimeType.GetMimeType(entry.Key);

        if (string.IsNullOrWhiteSpace(FullName))
            FullName = Name;
        if (string.IsNullOrWhiteSpace(Name) && IsDirectory)
            Name = PathArray.Last(s => !string.IsNullOrEmpty(s));
    }

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        if (!FileBytes?.Any() == true)
        {
            return Entry.OpenEntryStream();
            //using var memoryStream = new MemoryStream();
            //Entry.WriteTo(memoryStream);
            //FileBytes = memoryStream.ToArray();
        }
        return new MemoryStream(FileBytes);
    }

    public string Name { get; init; }
    public DateTimeOffset LastModified { get; }
    public long Size { get; }
    public string ContentType { get; }
    public string FullName { get; }
    public string Path => FullName.TrimEnd(Name.ToCharArray());
    public bool IsDirectory => Entry.IsDirectory;
    public string[] PathArray => Path.Split('/').Where(s => !string.IsNullOrEmpty(s)).ToArray();
    public string ParentDirectoryName => PathArray?.Any() == true ? !IsDirectory ? PathArray.Last(s => !string.IsNullOrEmpty(s)) : PathArray[^2] : string.Empty;
}