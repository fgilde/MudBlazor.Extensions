using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Extensions.Core.FileManager;

namespace MudBlazor.Extensions.Services;

/// <summary>
/// A file structure that lives entirely in memory, described by a list of paths. Used by the demos and as the
/// reference implementation in tests.
/// </summary>
/// <remarks>
/// Deliberately keeps only a flat set of paths as its state and derives nodes from it on every call. That
/// keeps rename and move trivially correct for whole subtrees, which is exactly the part a provider gets wrong.
/// Not registered for dependency injection - it is constructed with the structure it should serve.
/// </remarks>
public class MudExInMemoryFileStructureManager : IMudExFileStructureManager, IMudExFileStructureWriter
{
    private readonly Dictionary<string, FileMeta> _files = new(StringComparer.Ordinal);
    private readonly HashSet<string> _directoryPaths = new(StringComparer.Ordinal);
    private readonly Dictionary<string, byte[]> _content = new(StringComparer.Ordinal);

    /// <summary>
    /// Creates the structure from file paths separated by "/". Every directory in a path is created implicitly.
    /// </summary>
    public MudExInMemoryFileStructureManager(params string[] paths)
    {
        foreach (var path in paths ?? Array.Empty<string>())
            AddFile(path);
    }

    /// <inheritdoc />
    public MudExFileManagerCapabilities Capabilities { get; set; } = MudExFileManagerCapabilities.All;

    /// <summary>
    /// Where <see cref="OpenReadAsync"/> gets its bytes when the content was not stored with
    /// <see cref="SetContent"/>. Lets another provider reuse this structure logic while serving content from
    /// somewhere else - the local folder provider does exactly that for its read-only fallback.
    /// </summary>
    public Func<string, CancellationToken, Task<Stream>> ContentProvider { get; set; }

    /// <summary>
    /// Adds a file and every directory above it. Size and timestamp are kept separately from the content, so a
    /// caller that knows the metadata but not the bytes still gets correct values.
    /// </summary>
    public void AddFile(string path, long size = 0, DateTimeOffset? lastModified = null)
    {
        path = Normalize(path);
        if (path.Length == 0)
            return;

        _files[path] = new FileMeta { Size = size, LastModified = lastModified };
        var parent = ParentOf(path);
        while (!string.IsNullOrEmpty(parent))
        {
            _directoryPaths.Add(parent);
            parent = ParentOf(parent);
        }
    }

    /// <summary>Sets the content a file returns from <see cref="OpenReadAsync"/> and its size with it.</summary>
    public void SetContent(string fullPath, byte[] content)
    {
        var path = Normalize(fullPath);
        _content[path] = content;
        if (_files.TryGetValue(path, out var meta))
            meta.Size = content?.Length ?? 0;
        else
            AddFile(path, content?.Length ?? 0);
    }

    /// <inheritdoc />
    public Task<HashSet<MudExFileStructureNode>> GetRootAsync(CancellationToken ct = default)
        => Task.FromResult(NodesIn(string.Empty, null));

    /// <inheritdoc />
    public Task<HashSet<MudExFileStructureNode>> GetChildrenAsync(MudExFileStructureNode node, CancellationToken ct = default)
        => Task.FromResult(node == null || !node.IsDirectory
            ? new HashSet<MudExFileStructureNode>()
            : NodesIn(node.FullPath, node));

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(MudExFileStructureNode node, CancellationToken ct = default)
    {
        // A node this structure does not hold as a file has no content to open, and answering that with an
        // empty stream would make "gone" look exactly like "an empty file" - see the contract on
        // IMudExFileStructureManager.OpenReadAsync.
        if (node == null || node.IsDirectory || !_files.ContainsKey(node.FullPath))
            throw new InvalidOperationException($"'{node?.FullPath}' cannot be read: this structure holds no file at that path.");

        if (_content.TryGetValue(node.FullPath, out var stored))
            return Task.FromResult<Stream>(new MemoryStream(stored ?? Array.Empty<byte>(), writable: false));
        if (ContentProvider != null)
            return ContentProvider(node.FullPath, ct);

        // The file resolved, only nobody ever stored bytes for it - that is a genuinely empty file.
        return Task.FromResult<Stream>(new MemoryStream(Array.Empty<byte>(), writable: false));
    }

    /// <inheritdoc />
    public Task<MudExFileStructureNode> CreateDirectoryAsync(MudExFileStructureNode parent, string name, CancellationToken ct = default)
    {
        EnsureValidName(name);
        var path = Combine(parent?.FullPath, name);
        EnsureFree(path);

        _directoryPaths.Add(path);
        return Task.FromResult(Directory(name, path, parent));
    }

    /// <inheritdoc />
    public Task<MudExFileStructureNode> RenameAsync(MudExFileStructureNode node, string newName, CancellationToken ct = default)
    {
        EnsureValidName(newName);

        var target = Combine(ParentOf(node.FullPath), newName);
        if (target != node.FullPath)
            EnsureFree(target);

        Relocate(node.FullPath, target);

        // The given node is deliberately left untouched: the writer contract says the returned node is the
        // new one and the input is stale, and mutating it here would not fix its already loaded descendants
        // anyway - Relocate only rewrites the flat path maps.
        return Task.FromResult(node.IsDirectory
            ? Directory(newName, target, node.Parent)
            : File(newName, target, node.Parent, node.Size, node.LastModified));
    }

    /// <inheritdoc />
    public Task MoveAsync(IReadOnlyCollection<MudExFileStructureNode> nodes, MudExFileStructureNode target, CancellationToken ct = default)
    {
        // Validate every move before applying any of them - a rejected call must leave the tree untouched.
        var moves = new List<(string From, string To)>();
        foreach (var node in nodes ?? Array.Empty<MudExFileStructureNode>())
        {
            var destination = Combine(target?.FullPath, node.Name);
            EnsureNotOwnDescendant(node, target);
            if (destination != node.FullPath)
                EnsureFree(destination);
            moves.Add((node.FullPath, destination));
        }

        foreach (var (from, to) in moves)
            Relocate(from, to);
        return Task.CompletedTask;
    }

    // _files and _directoryPaths are separate maps over the same key space - without this, relocating or
    // creating onto a path already taken by the other kind leaves a stale entry behind in one of them.
    private void EnsureFree(string path)
    {
        if (_files.ContainsKey(path) || _directoryPaths.Contains(path))
            throw new InvalidOperationException($"An entry already exists at '{path}'.");
    }

    // A directory can't become its own child - detects both moving onto itself and onto a descendant path.
    private static void EnsureNotOwnDescendant(MudExFileStructureNode node, MudExFileStructureNode target)
    {
        var targetPath = target?.FullPath ?? string.Empty;
        if (node.IsDirectory && (targetPath == node.FullPath || targetPath.StartsWith(node.FullPath + "/", StringComparison.Ordinal)))
            throw new InvalidOperationException($"Cannot move '{node.FullPath}' into itself or one of its own descendants.");
    }

    // One guard for every string a user can type into a name box. Without it, an empty or whitespace name
    // composes a path ending in "/", which passes EnsureFree and adds a phantom entry whose name is blank; a
    // name containing a separator would relocate the entry instead of naming it, which is MoveAsync's job and
    // reproduces the self-descendant corruption MoveAsync guards against, just via a different method.
    private static void EnsureValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.IndexOf('/') >= 0 || name.IndexOf('\\') >= 0)
            throw new InvalidOperationException(
                $"'{name}' is not a valid name: a name must not be empty or whitespace and cannot contain a path separator.");
    }

    /// <inheritdoc />
    public Task DeleteAsync(IReadOnlyCollection<MudExFileStructureNode> nodes, CancellationToken ct = default)
    {
        foreach (var node in nodes ?? Array.Empty<MudExFileStructureNode>())
        {
            foreach (var path in Subtree(node.FullPath).ToList())
            {
                _files.Remove(path);
                _directoryPaths.Remove(path);
                _content.Remove(path);
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<MudExFileStructureNode> UploadAsync(MudExFileStructureNode target, IBrowserFile file, CancellationToken ct = default)
    {
        EnsureValidName(file.Name);
        var path = Combine(target?.FullPath, file.Name);
        // Re-uploading an existing file is a normal thing to want, and SetContent already treats a repeat
        // write as a plain replace - so only a collision with a directory is refused here.
        if (_directoryPaths.Contains(path))
            throw new InvalidOperationException($"Cannot upload to '{path}': a directory already exists there.");

        using var ms = new MemoryStream();
        await using (var source = file.OpenReadStream(long.MaxValue, ct))
            await source.CopyToAsync(ms, ct);

        AddFile(path, ms.Length, file.LastModified);
        _content[path] = ms.ToArray();
        return File(file.Name, path, target, ms.Length, file.LastModified);
    }

    // Moving a subtree is a prefix rewrite of every path below it - the reason the state is a flat path set.
    private void Relocate(string from, string to)
    {
        foreach (var path in Subtree(from).ToList())
        {
            var moved = to + path.Substring(from.Length);
            var isFile = _files.Remove(path, out var meta);
            _directoryPaths.Remove(path);

            if (isFile)
            {
                _files[moved] = meta;
                if (_content.Remove(path, out var bytes))
                    _content[moved] = bytes;
            }
            else
            {
                _directoryPaths.Add(moved);
            }
        }
    }

    private IEnumerable<string> Subtree(string path)
        => _files.Keys.Concat(_directoryPaths)
            .Where(p => p == path || p.StartsWith(path + "/", StringComparison.Ordinal));

    private HashSet<MudExFileStructureNode> NodesIn(string directory, MudExFileStructureNode parent)
    {
        var prefix = string.IsNullOrEmpty(directory) ? string.Empty : directory + "/";
        var result = new HashSet<MudExFileStructureNode>();

        foreach (var path in _directoryPaths.Where(p => IsDirectChild(p, prefix)))
            result.Add(Directory(NameOf(path), path, parent));
        foreach (var entry in _files.Where(e => IsDirectChild(e.Key, prefix)))
            result.Add(File(NameOf(entry.Key), entry.Key, parent, entry.Value.Size, entry.Value.LastModified));

        return result;
    }

    private static bool IsDirectChild(string path, string prefix)
        => path.StartsWith(prefix, StringComparison.Ordinal)
           && !path.Substring(prefix.Length).Contains('/');

    private MudExFileStructureNode Directory(string name, string path, MudExFileStructureNode parent)
    {
        var node = new MudExFileStructureNode
        {
            Name = name,
            FullPath = path,
            IsDirectory = true,
            Parent = parent
        };
        // The tree view loads on demand through this. Children stay empty until it does.
        node.LoadChildrenFunc = (n, ct) => GetChildrenAsync(n, ct);
        return node;
    }

    private static MudExFileStructureNode File(string name, string path, MudExFileStructureNode parent, long size, DateTimeOffset? modified)
        => new()
        {
            Name = name,
            FullPath = path,
            IsDirectory = false,
            Size = size,
            LastModified = modified,
            Parent = parent
        };

    private static string Normalize(string path) => (path ?? string.Empty).Replace('\\', '/').Trim('/');

    private static string Combine(string directory, string name)
        => string.IsNullOrEmpty(directory) ? Normalize(name) : Normalize(directory) + "/" + Normalize(name);

    private static string ParentOf(string path)
    {
        var index = path.LastIndexOf('/');
        return index < 0 ? string.Empty : path.Substring(0, index);
    }

    private static string NameOf(string path)
    {
        var index = path.LastIndexOf('/');
        return index < 0 ? path : path.Substring(index + 1);
    }

    private sealed class FileMeta
    {
        public long Size { get; set; }
        public DateTimeOffset? LastModified { get; set; }
    }
}
