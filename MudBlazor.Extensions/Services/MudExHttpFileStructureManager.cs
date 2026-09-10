using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Extensions.Core.FileManager;

namespace MudBlazor.Extensions.Services;

/// <summary>
/// Serves a file structure from http endpoints. One <see cref="BaseUrl"/> plus conventional routes is enough,
/// which is what makes the provider configurable from a single html attribute in the web component.
/// </summary>
/// <remarks>
/// Not registered for dependency injection: it needs a base url that only the consumer knows.
/// </remarks>
public class MudExHttpFileStructureManager : IMudExFileStructureManager, IMudExFileStructureWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;

    /// <summary>Creates the provider for the given client.</summary>
    public MudExHttpFileStructureManager(HttpClient httpClient)
        => _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <summary>Base url every route is resolved against, for example <c>/api/files</c>.</summary>
    public string BaseUrl { get; set; }

    /// <inheritdoc />
    public MudExFileManagerCapabilities Capabilities { get; set; } = MudExFileManagerCapabilities.Read | MudExFileManagerCapabilities.Download;

    /// <summary>Route of the directory listing. Receives <c>?path=</c>.</summary>
    public string ListRoute { get; set; } = string.Empty;
    /// <summary>Route of the file content. Receives <c>?path=</c>.</summary>
    public string ContentRoute { get; set; } = "/content";
    /// <summary>Route that creates a directory.</summary>
    public string CreateFolderRoute { get; set; } = "/folder";
    /// <summary>Route that renames an entry.</summary>
    public string RenameRoute { get; set; } = "/rename";
    /// <summary>Route that moves entries.</summary>
    public string MoveRoute { get; set; } = "/move";
    /// <summary>Route that deletes entries.</summary>
    public string DeleteRoute { get; set; } = string.Empty;
    /// <summary>Route that accepts a new file.</summary>
    public string UploadRoute { get; set; } = "/upload";

    /// <summary>Combines <see cref="BaseUrl"/> with a route. Public so tests and consumers can verify it.</summary>
    public string ResolveRoute(string route) => $"{(BaseUrl ?? string.Empty).TrimEnd('/')}{route}";

    /// <inheritdoc />
    public Task<HashSet<MudExFileStructureNode>> GetRootAsync(CancellationToken ct = default)
        => ListAsync(null, ct);

    /// <inheritdoc />
    public Task<HashSet<MudExFileStructureNode>> GetChildrenAsync(MudExFileStructureNode node, CancellationToken ct = default)
        => node == null || !node.IsDirectory
            ? Task.FromResult(new HashSet<MudExFileStructureNode>())
            : ListAsync(node, ct);

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(MudExFileStructureNode node, CancellationToken ct = default)
    {
        // ResponseHeadersRead avoids buffering the whole body up front - large files stay on the wire until
        // the caller actually reads them. The response is then owned by the returned stream (see
        // ResponseOwningStream) so "the caller owns and disposes the stream" keeps holding.
        var response = await _httpClient.GetAsync(WithPath(ContentRoute, node?.FullPath), HttpCompletionOption.ResponseHeadersRead, ct);
        try
        {
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStreamAsync(ct);
            return new ResponseOwningStream(response, content);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<MudExFileStructureNode> CreateDirectoryAsync(MudExFileStructureNode parent, string name, CancellationToken ct = default)
    {
        var entry = await PostAsync<FileEntry>(CreateFolderRoute, new { path = parent?.FullPath, name }, ct);
        return ToNode(entry ?? new FileEntry { Name = name, IsDirectory = true }, parent);
    }

    /// <inheritdoc />
    public async Task<MudExFileStructureNode> RenameAsync(MudExFileStructureNode node, string newName, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, ResolveRoute(RenameRoute))
        {
            Content = JsonContent.Create(new { path = node?.FullPath, newName }, options: JsonOptions)
        };
        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var entry = await ReadEntryOrNull(response, ct);
        return ToNode(entry ?? new FileEntry { Name = newName, IsDirectory = node?.IsDirectory ?? false }, node?.Parent);
    }

    /// <inheritdoc />
    public async Task MoveAsync(IReadOnlyCollection<MudExFileStructureNode> nodes, MudExFileStructureNode target, CancellationToken ct = default)
    {
        var paths = (nodes ?? Array.Empty<MudExFileStructureNode>()).Select(n => n.FullPath).ToArray();
        using var response = await _httpClient.PostAsJsonAsync(ResolveRoute(MoveRoute),
            new { paths, target = target?.FullPath }, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(IReadOnlyCollection<MudExFileStructureNode> nodes, CancellationToken ct = default)
    {
        var paths = (nodes ?? Array.Empty<MudExFileStructureNode>()).Select(n => n.FullPath).ToArray();
        var request = new HttpRequestMessage(HttpMethod.Delete, ResolveRoute(DeleteRoute))
        {
            Content = JsonContent.Create(new { paths }, options: JsonOptions)
        };
        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task<MudExFileStructureNode> UploadAsync(MudExFileStructureNode target, IBrowserFile file, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream(long.MaxValue, ct));
        content.Add(fileContent, "file", file.Name);
        content.Add(new StringContent(target?.FullPath ?? string.Empty), "path");

        using var response = await _httpClient.PostAsync(ResolveRoute(UploadRoute), content, ct);
        response.EnsureSuccessStatusCode();

        var entry = await ReadEntryOrNull(response, ct);
        return ToNode(entry ?? new FileEntry { Name = file.Name, Size = file.Size, ContentType = file.ContentType }, target);
    }

    private async Task<HashSet<MudExFileStructureNode>> ListAsync(MudExFileStructureNode parent, CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(WithPath(ListRoute, parent?.FullPath), ct);
        response.EnsureSuccessStatusCode();

        var entries = await response.Content.ReadFromJsonAsync<List<FileEntry>>(JsonOptions, ct) ?? new List<FileEntry>();
        return entries.Select(entry => ToNode(entry, parent)).ToHashSet();
    }

    private async Task<T> PostAsync<T>(string route, object payload, CancellationToken ct) where T : class
    {
        using var response = await _httpClient.PostAsJsonAsync(ResolveRoute(route), payload, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await ReadOrNull<T>(response, ct);
    }

    private static Task<FileEntry> ReadEntryOrNull(HttpResponseMessage response, CancellationToken ct)
        => ReadOrNull<FileEntry>(response, ct);

    private static async Task<T> ReadOrNull<T>(HttpResponseMessage response, CancellationToken ct) where T : class
    {
        // A server is allowed to answer a write with 204 and no body - that is the only case treated as
        // "no entry". A non-empty body that fails to parse is a contract violation and must surface, exactly
        // like ListAsync's uncaught ReadFromJsonAsync already does - it must not be swallowed into a
        // synthesized fallback node that looks like success.
        if (response.Content == null || response.Content.Headers.ContentLength is null or 0)
            return null;
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
    }

    private string WithPath(string route, string path)
        => $"{ResolveRoute(route)}?path={Uri.EscapeDataString(path ?? string.Empty)}";

    private MudExFileStructureNode ToNode(FileEntry entry, MudExFileStructureNode parent)
    {
        var node = new MudExFileStructureNode
        {
            Name = entry.Name,
            // An explicit fullPath wins so a server may expose ids instead of paths.
            FullPath = !string.IsNullOrEmpty(entry.FullPath)
                ? entry.FullPath
                : string.IsNullOrEmpty(parent?.FullPath) ? entry.Name : $"{parent.FullPath}/{entry.Name}",
            IsDirectory = entry.IsDirectory,
            Size = entry.Size,
            LastModified = entry.LastModified,
            Parent = parent
        };

        if (!string.IsNullOrEmpty(entry.ContentType))
            node.ContentType = entry.ContentType;

        if (entry.IsDirectory)
            node.LoadChildrenFunc = (n, ct) => GetChildrenAsync(n, ct);

        return node;
    }

    /// <summary>One entry as the list endpoint returns it.</summary>
    private sealed class FileEntry
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTimeOffset? LastModified { get; set; }
        public string ContentType { get; set; }
    }

    /// <summary>
    /// Wraps a content stream so disposing it also disposes the <see cref="HttpResponseMessage"/> it came
    /// from. Lets <see cref="OpenReadAsync"/> read the body lazily (<see cref="HttpCompletionOption.ResponseHeadersRead"/>)
    /// while still honouring "the caller owns and disposes the stream" - without this, the response would leak.
    /// </summary>
    private sealed class ResponseOwningStream : Stream
    {
        private readonly HttpResponseMessage _response;
        private readonly Stream _inner;

        public ResponseOwningStream(HttpResponseMessage response, Stream inner)
        {
            _response = response;
            _inner = inner;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        // Answered from Content-Length when the server sent one. Forwarding to the response stream instead
        // throws for a non-seekable body, and a consumer that only wants the size then copies the whole file
        // to find it out - MudExFileDisplay does exactly that. CanSeek stays false either way: knowing the
        // length is not the same as being able to seek to it.
        public override long Length => _response.Content.Headers.ContentLength ?? _inner.Length;
        public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }

        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _inner.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                _response.Dispose();
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync();
            _response.Dispose();
        }
    }
}
