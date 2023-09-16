using Nextended.Blazor.Models;
using Nextended.Core;
using SharpCompress.Archives;

namespace MudBlazor.Extensions.Core;

/**
 * Represents a file entry inside of an archive as BrowserFile
 */
internal record MudExArchivedBrowserFile : IArchivedBrowserFile
{
    private readonly IArchiveEntry _entry;

    private readonly Lazy<byte[]> _lazyFileBytes;
    public byte[] FileBytes => _lazyFileBytes.Value;
    public MudExArchivedBrowserFile(string fileName, Stream contentStream)
    {
        Name = (fileName);
        Size = contentStream.Length;
        LastModified = DateTimeOffset.UtcNow; // We don't have the original timestamp here, so we're using the current time. You can change this if you have other means to get the timestamp.
        FullName = fileName;
        ContentType = MimeType.GetMimeType(fileName);

        if (string.IsNullOrWhiteSpace(FullName))
            FullName = Name;
        if (string.IsNullOrWhiteSpace(Name) && IsDirectory)
            Name = PathArray.Last(s => !string.IsNullOrEmpty(s));

        _lazyFileBytes = new Lazy<byte[]>(() =>
        {
            using var memoryStream = new MemoryStream();
            contentStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        });

        // Since this constructor doesn't use an IArchiveEntry, setting IsDirectory might need another approach if you wish to handle directories.
    }
    
    public MudExArchivedBrowserFile(IArchiveEntry entry)
    {
        _entry = entry;
        _lazyFileBytes = new Lazy<byte[]>(() =>
        {
            using var memoryStream = new MemoryStream();
            entry.WriteTo(memoryStream);
            return memoryStream.ToArray();
        });

        Name = _entry.Key.Split('/').LastOrDefault() ?? "";
        Size = _entry.Size;
        LastModified = _entry.LastModifiedTime ?? default;
        FullName = _entry.Key; // Assuming the Key is the full name/path within the archive.
        ContentType = MimeType.GetMimeType(entry.Key);

        if (string.IsNullOrWhiteSpace(FullName))
            FullName = Name;
        if (string.IsNullOrWhiteSpace(Name) && IsDirectory)
            Name = PathArray.Last(s => !string.IsNullOrEmpty(s));
    }

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        return new MemoryStream(FileBytes);
    }

    public string Name { get; init; }
    public DateTimeOffset LastModified { get; }
    public long Size { get; }
    public string ContentType { get; }
    public string FullName { get; }
    public string Path => FullName.TrimEnd(Name.ToCharArray());
    public bool IsDirectory => _entry.IsDirectory;
    public string[] PathArray => Path.Split('/').Where(s => !string.IsNullOrEmpty(s)).ToArray();
    public string ParentDirectoryName => PathArray?.Any() == true ? !IsDirectory ? PathArray.Last(s => !string.IsNullOrEmpty(s)) : PathArray[^2] : string.Empty;
}