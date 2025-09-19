using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper.Internal;
using Nextended.Blazor.Models;
using Nextended.Core.Extensions;
using SharpCompress.Archives;
using System.IO.Compression;
using MudBlazor.Extensions.Helper;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Nextended.Blazor.Extensions;
using Microsoft.JSInterop;
using Nextended.Core.Attributes;
using BlazorJS;
using ExcelDataReader;
using Nextended.Core;
using System.Data;
using System.Text;

namespace MudBlazor.Extensions.Services;

/// <summary>
/// Service to handle file operations
/// </summary>
[RegisterAs(typeof(IMudExFileService), RegisterAsImplementation = true, ServiceLifetime = ServiceLifetime.Transient)]
public class MudExFileService : IMudExFileService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly List<Stream> _streams = new();
    private readonly List<string> _blobUris = new();

    /// <summary>
    /// Creates a new instance of MudExFileService
    /// </summary>
    public MudExFileService(IJSRuntime jsRuntime, IServiceProvider serviceProvider)
    {
        _httpClient = serviceProvider.GetService<HttpClient>() ?? new HttpClient();
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Returns the content of a file as string
    /// </summary>
    public string ReadAsStringFromStream(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Reads filedisplay info as stream. this stream is a copy and needs to be disposed and closed.
    /// </summary>
    /// <param name="fileDisplayInfos"></param>
    /// <returns></returns>
    public async Task<Stream> ReadStreamAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        Stream ms = null;
        if (fileDisplayInfos.ContentStream is { Length: > 0, CanRead: true })
        {
            ms = new MemoryStream();
            await fileDisplayInfos.ContentStream.CopyToAsync(ms);
        }
        else if (DataUrl.TryParse(fileDisplayInfos.Url, out var data))
        {
            ms = new MemoryStream(data.Bytes);
        }
        else if (!string.IsNullOrEmpty(fileDisplayInfos.Url))
        {          
            ms = await ReadStreamAsync(fileDisplayInfos.Url);            
        }
        if (ms != null)
            ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// Returns the content of a file as string
    /// </summary>
    public async Task<string> ReadAsStringFromFileDisplayInfosAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        // Here we load the json string for given file
        if (fileDisplayInfos.ContentStream is { Length: > 0, CanRead: true })
        {
            var copy = await CopyStreamAsync(fileDisplayInfos.ContentStream);
            return ReadAsStringFromStream(copy); // If we have already a valid stream we can use it
        }
        if (DataUrl.TryParse(fileDisplayInfos.Url, out var data)) // If not but given url is a data url we can use the bytes from it
            return ReadAsStringFromStream(new MemoryStream(data.Bytes));
        if (!string.IsNullOrEmpty(fileDisplayInfos.Url)) // Otherwise we load the file        
        {
            if (MudExResource.IsServerSide && fileDisplayInfos.Url.StartsWith("blob:")) // If server side rendering we need to ensure client is reading the blob
            {
                var result = await _jsRuntime.InvokeAsync<string>("MudExUriHelper.readBlobAsText", fileDisplayInfos.Url);
                return result;
            }
            return await ReadAsStringFromUrlAsync(fileDisplayInfos.Url);
        }

        return null;
    }

    public async Task<string> ToAbsoluteUrlAsync(string url)
    {
        if(string.IsNullOrEmpty(url) || DataUrl.IsDataUrl(url) || url.StartsWith("blob:", StringComparison.InvariantCultureIgnoreCase))
            return url;
        if(url.StartsWith("http:", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("https:", StringComparison.InvariantCultureIgnoreCase))
            return url;
        return await _jsRuntime.DInvokeAsync<string>((w, href) =>
        {
            var a = w.document.createElement('a');
            a.href = href;
            return a.href;
        }, url);
    }

    /// <summary>
    /// Converts a url to a blob url
    /// </summary>
    /// <returns></returns>
    public async Task<string> ToBlobUrlAsync(string url, string mimeType = "application/octet-stream")
    {
        if(string.IsNullOrEmpty(url) || url.StartsWith("blob:", StringComparison.InvariantCultureIgnoreCase))
            return url;
        if (DataUrl.TryParse(url, out var data))
            return await CreateBlobUrlAsync(data.Bytes, data.MimeType ?? mimeType);
        var bytes = await ReadBytesAsync(url);
        return await CreateBlobUrlAsync(bytes, mimeType);
    }


    /// <summary>
    /// Converts a url to a data url
    /// </summary>    
    /// <returns></returns>
    public async Task<string> ToDataUrlAsync(string url, string mimeType = "application/octet-stream")
    {
        if(string.IsNullOrEmpty(url) || DataUrl.IsDataUrl(url))
            return url;
        var bytes = await ReadBytesAsync(url);                        
        return await DataUrl.GetDataUrlAsync(bytes, mimeType);
    }

    /// <summary>
    /// Reads a string from an url
    /// </summary>
    public async Task<string> ReadAsStringFromUrlAsync(string url) => ReadAsStringFromStream(await ReadStreamAsync(url));

    /// <summary>
    /// Reads bytes from an url
    /// </summary>
    public async Task<byte[]> ReadBytesAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        if (DataUrl.TryParse(url, out var data)) // If not but given url is a data url we can use the bytes from it
            return data.Bytes;
        if (MudExResource.IsServerSide && url.StartsWith("blob:", StringComparison.InvariantCultureIgnoreCase)) // If server side rendering we need to ensure client is reading the blob
            return await _jsRuntime.InvokeAsync<byte[]>("MudExUriHelper.readBlobAsByteArray", url);
        
        return await (_httpClient ?? new HttpClient()).GetByteArrayAsync(url);
    }

    /// <summary>
    /// Reads a stream from an url
    /// </summary>
    public async Task<Stream> ReadStreamAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        if (DataUrl.TryParse(url, out var data)) // If not but given url is a data url we can use the bytes from it
            return new MemoryStream(data.Bytes);
        if (MudExResource.IsServerSide && url.StartsWith("blob:", StringComparison.InvariantCultureIgnoreCase)) // If server side rendering we need to ensure client is reading the blob
        {
            var resultByteArray = await _jsRuntime.InvokeAsync<byte[]>("MudExUriHelper.readBlobAsByteArray", url);
            return new MemoryStream(resultByteArray);
        }

        return await (_httpClient ?? new HttpClient()).GetStreamAsync(url);
    }

    /// <summary>
    /// Reads a data url for a stream
    /// </summary>
    public async Task<string> ReadDataUrlForStreamAsync(Stream stream, string mimeType, bool useBlob)
    {
        var copy = await CopyStreamAsync(stream);
        return await CreateDataUrlAsync(copy.ToByteArray(), mimeType, useBlob);
    }

    /// <summary>
    /// Creates a data url from bytes this can be a blob url or a data url
    /// </summary>
    public Task<string> CreateDataUrlAsync(byte[] bytes, string mimeType, bool useBlob)
    {
        return useBlob
            ? CreateBlobUrlAsync(bytes, mimeType)
            : DataUrl.GetDataUrlAsync(bytes, mimeType);
    }

    /// <summary>
    /// Creates an url from a file this can be a blob url or a data url
    /// </summary>
    public async Task<string> CreateDataUrlAsync(IBrowserFile file, bool useBlob)
    {
        return useBlob
               ? await CreateBlobUrlAsync(await file.GetBytesAsync(), file.ContentType)
               : await file.GetDataUrlAsync();
    }

    private async Task<string> CreateBlobUrlAsync(byte[] bytes, string mimeType)
    {
        var blobUrl = await _jsRuntime.InvokeAsync<string>("MudExUriHelper.createBlobUrlFromByteArray", bytes, mimeType);
        _blobUris.Add(blobUrl);
        return blobUrl;
    }

    /// <summary>
    /// Revokes a blob url
    /// </summary>
    private async Task RevokeBlobUrlAsync(string blobUrl)
    {
        if (string.IsNullOrEmpty(blobUrl) || !blobUrl.StartsWith("blob", StringComparison.InvariantCultureIgnoreCase))
            return;
        await _jsRuntime.InvokeVoidAsync("MudExUriHelper.revokeBlobUrl", blobUrl);
    }

    /// <summary>
    /// Reads an archive with SharpCompress
    /// </summary>
    public async Task<(HashSet<MudExArchiveStructure> Structure, List<IArchivedBrowserFile> List)> ReadArchiveAsync(byte[] bytes, string rootFolderName, string contentType)
    {
        using var memoryStream = new MemoryStream(bytes);
        return await ReadArchiveAsync(memoryStream, rootFolderName, contentType);
    }

    /// <summary>
    /// Reads an archive with SharpCompress
    /// </summary>
    public async Task<(HashSet<MudExArchiveStructure> Structure, List<IArchivedBrowserFile> List)> ReadArchiveAsync(Stream stream, string rootFolderName, string contentType)
    {
        var contentStream = await CopyStreamAsync(stream);
        var archive = ArchiveFactory.Open(contentStream);
        if (archive.Entries.Count() == 1 && archive.Entries.First().CompressionType == SharpCompress.Common.CompressionType.GZip)
        {
            using var memoryStream = new MemoryStream();
            archive.Entries.First().WriteTo(memoryStream);
            return await ReadArchiveAsync(memoryStream, rootFolderName, contentType);
        }

        var validEntries = archive.Entries.Select(entry => new MudExArchivedBrowserFile(entry) as IArchivedBrowserFile).ToList();
        var res = MudExArchiveStructure.CreateStructure(validEntries, rootFolderName);
        var structure = new[] { res }.ToHashSet();
        var list = structure.Recursive(z => z?.Children ?? Enumerable.Empty<MudExArchiveStructure>()).Where(c => c is { IsDirectory: false, BrowserFile: not null }).Select(c => c.BrowserFile).ToList();
        return (structure, list);
    }

    /// <summary>
    /// Reads an archive with system compression
    /// </summary>
    public async Task<(HashSet<MudExArchiveStructure> Structure, List<IArchivedBrowserFile> List)> ReadArchiveWithSystemCompressionAsync(Stream stream, string rootFolderName, string contentType)
    {
        var contentStream = await CopyStreamAsync(stream);
        contentStream = ArchiveConverter.ConvertToSystemCompressionZip(contentStream); // Converts archive to zip otherwise system compression cant read
        var entries = new ZipArchive(contentStream).Entries.Select(entry => new ZipBrowserFile(entry) as IArchivedBrowserFile).ToList();
        var res = MudExArchiveStructure.CreateStructure(entries, rootFolderName);
        var structure = new[] { res }.ToHashSet();
        var list = structure.Recursive(z => z?.Children ?? Enumerable.Empty<MudExArchiveStructure>()).Where(c => c is { IsDirectory: false, BrowserFile: not null }).Select(c => c.BrowserFile).ToList();
        return (structure, list);
    }
    
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var uri in _blobUris)
            await RevokeBlobUrlAsync(uri);
        foreach (var stream in _streams)
            await stream.DisposeAsync();
        _streams.Clear();
    }

    /// <inheritdoc />
    public ValueTask DownloadContentAsync(string content, string mimeType, string fileName)
    {
        return DownloadContentAsync(Encoding.UTF8.GetBytes(content), mimeType, fileName);
    }
    
    /// <inheritdoc />
    public ValueTask DownloadContentAsync(Stream stream, string mimeType, string fileName)
    {
        return DownloadContentAsync(stream.ToByteArray(), mimeType, fileName);
    }
    
    /// <inheritdoc />
    public async ValueTask DownloadContentAsync(byte[] bytes, string mimeType, string fileName)
    {
        var url = await CreateBlobUrlAsync(bytes, mimeType);
        //var url = await Nextended.Core.Types.DataUrl.GetDataUrlAsync(bytes, mimeType)
        await DownloadAsync(url, mimeType, fileName);
    }

    /// <inheritdoc />
    public ValueTask DownloadAsync(string url, string mimeType, string fileName)
    {
        return _jsRuntime.InvokeVoidAsync("MudBlazorExtensions.downloadFile", new
        {
            Url = url,
            FileName = $"{fileName}",
            MimeType = mimeType,
        });
    }

    /// <summary>
    /// Reads a stream as excel file
    /// </summary>
    public ExcelFile ReadExcelFile(Stream stream, string contentType)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var excelFile = new ExcelFile();

        using var reader = CreateReader(stream, contentType);

        // Configuration to use the first row as column headers
        var conf = new ExcelDataSetConfiguration
        {
            ConfigureDataTable = (_) => new ExcelDataTableConfiguration
            {
                UseHeaderRow = true
            }
        };

        var result = reader.AsDataSet(conf);

        foreach (DataTable table in result.Tables)
        {
            var sheet = new ExcelSheet
            {
                Name = table.TableName
            };

            foreach (DataRow row in table.Rows)
            {
                var excelRow = new ExcelRow();

                foreach (DataColumn col in table.Columns)
                {
                    excelRow.Cells[col.ColumnName] = row[col];
                }

                sheet.Rows.Add(excelRow);
            }

            excelFile.Sheets.Add(sheet);
        }

        return excelFile;
    }

    private IExcelDataReader CreateReader(Stream stream, string contentType)
    {
        if (MimeType.Matches(contentType, MimeType.Csv))
            return ExcelReaderFactory.CreateCsvReader(stream);
        return ExcelReaderFactory.CreateReader(stream);
    }

    private async Task<Stream> CopyStreamAsync(Stream stream)
    {
        var res = await stream.CopyStreamAsync();
        _streams.Add(res);
        return res;
    }
}