using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Extensions.Components;
using Nextended.Blazor.Models;

namespace MudBlazor.Extensions.Core;

public interface IMudExFileService : IAsyncDisposable
{
    /// <summary>
    /// Returns the content of a file as string
    /// </summary>
    string ReadAsStringFromStream(Stream stream);

    /// <summary>
    /// Returns the content of a file as string
    /// </summary>
    Task<string> ReadAsStringFromFileDisplayInfosAsync(IMudExFileDisplayInfos fileDisplayInfos);

    /// <summary>
    /// Reads a string from an url
    /// </summary>
    Task<string> ReadAsStringFromUrlAsync(string url);

    /// <summary>
    /// Reads filedisplay info as stream. this stream is a copy and needs to be disposed and closed.
    /// </summary>
    /// <param name="fileDisplayInfos"></param>
    /// <returns></returns>
    public Task<Stream> ReadStreamAsync(IMudExFileDisplayInfos fileDisplayInfos);

    /// <summary>
    /// Reads a stream from an url
    /// </summary>
    Task<Stream> ReadStreamAsync(string url);

    /// <summary>
    /// Reads a data url for a stream
    /// </summary>
    Task<string> ReadDataUrlForStreamAsync(Stream stream, string mimeType, bool useBlob);

    /// <summary>
    /// Creates a data url from bytes this can be a blob url or a data url
    /// </summary>
    Task<string> CreateDataUrlAsync(byte[] bytes, string mimeType, bool useBlob);

    /// <summary>
    /// Creates an url from a file this can be a blob url or a data url
    /// </summary>
    Task<string> CreateDataUrlAsync(IBrowserFile file, bool useBlob);

    /// <summary>
    /// Read bytes from a file
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    Task<byte[]> ReadBytesAsync(string url);

    /// <summary>
    /// Reads an archive with SharpCompress
    /// </summary>
    Task<(HashSet<MudExArchiveStructure> Structure, List<IArchivedBrowserFile> List)> ReadArchiveAsync(byte[] bytes, string rootFolderName, string contentType);

    /// <summary>
    /// Reads an archive with SharpCompress
    /// </summary>
    Task<(HashSet<MudExArchiveStructure> Structure, List<IArchivedBrowserFile> List)> ReadArchiveAsync(Stream stream, string rootFolderName, string contentType);

    /// <summary>
    /// Reads an archive with system compression
    /// </summary>
    Task<(HashSet<MudExArchiveStructure> Structure, List<IArchivedBrowserFile> List)> ReadArchiveWithSystemCompressionAsync(Stream stream, string rootFolderName, string contentType);

}