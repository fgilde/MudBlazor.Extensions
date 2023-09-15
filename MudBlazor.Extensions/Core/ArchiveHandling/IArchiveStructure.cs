namespace MudBlazor.Extensions.Core.ArchiveHandling;

public interface IArchiveStructure<T> where T : IArchiveBrowserFile
{
    string Name { get; set; }
    bool IsDirectory { get; }
    bool IsDownloading { get; set; }
    long Size { get; }
    IEnumerable<T> ContainingFiles { get; }
    T BrowserFile { get; set; }
    Task<MemoryStream> ToArchiveAsync();
    Task<byte[]> ToArchiveBytesAsync();    
}