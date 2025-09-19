using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Extensions.Components;
using Nextended.Blazor.Models;
using Nextended.Core.Extensions;
using System.Text;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// FileService 
/// </summary>
public interface IMudExFileService : IAsyncDisposable
{
    /// <summary>
    /// Reads a stream as excel file
    /// </summary>
    ExcelFile ReadExcelFile(Stream stream, string contentType);

    /// <summary>
    /// Converts a url to an absolute url
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    Task<string> ToAbsoluteUrlAsync(string url);

    /// <summary>
    /// Converts a url to a blob url
    /// </summary>
    /// <param name="url"></param>
    /// <param name="mimeType"></param>
    /// <returns></returns>
    Task<string> ToBlobUrlAsync(string url, string mimeType = "application/octet-stream");

    /// <summary>
    /// Converts a url to a data url
    /// </summary>
    /// <param name="url"></param>
    /// <param name="mimeType"></param>
    /// <returns></returns>
    Task<string> ToDataUrlAsync(string url, string mimeType = "application/octet-stream");

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

    
    /// <summary>
    /// Initiates the download of the specified content as a file with the given MIME type and file name.
    /// </summary>
    /// <param name="content">The content to be downloaded as a string.</param>
    /// <param name="mimeType">The MIME type of the file to be downloaded.</param>
    /// <param name="fileName">The name of the file to be downloaded.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public ValueTask DownloadContentAsync(string content, string mimeType, string fileName);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="mimeType"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public ValueTask DownloadContentAsync(Stream stream, string mimeType, string fileName);


    /// <summary>
    /// Initiates the download of content represented as a byte array.
    /// </summary>
    /// <param name="bytes">The content to be downloaded, represented as a byte array.</param>
    /// <param name="mimeType">The MIME type of the content, specifying its format.</param>
    /// <param name="fileName">The name of the file to be downloaded.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public ValueTask DownloadContentAsync(byte[] bytes, string mimeType, string fileName);


    /// <summary>
    /// Initiates the download of a file from the specified URL.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="mimeType">The MIME type of the file to be downloaded.</param>
    /// <param name="fileName">The name to assign to the downloaded file.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public ValueTask DownloadAsync(string url, string mimeType, string fileName);


}