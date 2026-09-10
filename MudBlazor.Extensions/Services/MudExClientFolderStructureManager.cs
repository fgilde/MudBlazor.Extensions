using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core.FileManager;

namespace MudBlazor.Extensions.Services;

/// <summary>
/// Serves a folder from the user's own machine.
/// </summary>
/// <remarks>
/// Two acquisition modes behind one provider. <c>showDirectoryPicker()</c> enumerates lazily and can write,
/// but only Chromium has it. The fallback reads an <c>input webkitdirectory</c> selection: the whole tree at
/// once and strictly read-only. Which mode was used is visible in <see cref="Capabilities"/>.
/// The fallback needs JavaScript too, because Blazor's IBrowserFile does not expose webkitRelativePath and
/// without the relative paths there is no structure to build.
/// </remarks>
public class MudExClientFolderStructureManager : IMudExFileStructureManager, IMudExFileStructureWriter
{
    private const char SEP = (char)92;

    private readonly IJSRuntime _jsRuntime;

    // Set in the File System Access mode: the id of the picked root directory handle.
    private string _rootHandleId;

    // Set in the File System Access mode: whether the browser granted write access to the picked folder.
    // Kept for stage D, which restores it into Capabilities once the writer methods actually do something -
    // stage A caps Capabilities at Read|Download regardless of this, see PickFolderAsync.
    private bool _pickedWritable;

    // Bumped on every successful pick. A directory name like "docs" is common enough that a second pick's
    // JS-side handle map can end up reusing the very same id string an earlier pick already handed out to a
    // node the caller still holds (a breadcrumb entry, a cached tree node) - reusing it would then silently
    // resolve against the *new* folder's data instead of failing. Tagging every node with the generation it
    // was created under (see _nodeGeneration) and checking it before ever calling into JS turns that into a
    // hard "no" instead of "probably fine": a node from an earlier generation is refused locally, the same way
    // GetChildrenAsync already refuses a non-directory node, without needing JS to get anything right.
    private int _generation;

    // Tags each File System Access node with the pick (_generation) it was created under, without keeping the
    // node alive - a plain Dictionary<node, int> would pin every node ever created for the manager's whole
    // lifetime, which is its own leak. ConditionalWeakTable compares keys by reference regardless of whatever
    // equality Hierarchical{T} defines, which is exactly what "this exact node instance, not one that merely
    // looks the same" needs.
    private readonly ConditionalWeakTable<MudExFileStructureNode, GenerationTag> _nodeGeneration = new();

    private sealed class GenerationTag
    {
        public int Value;
    }

    // Set in the fallback mode: the flat structure, keyed by the path relative to the picked folder.
    private MudExInMemoryFileStructureManager _flat;

    // Fallback mode only: the browser file objects the content is read from, by relative path.
    private readonly Dictionary<string, IBrowserFile> _flatFiles = new(StringComparer.Ordinal);

    /// <summary>Creates the provider.</summary>
    public MudExClientFolderStructureManager(IJSRuntime jsRuntime)
        => _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    /// <inheritdoc />
    public MudExFileManagerCapabilities Capabilities { get; private set; } = MudExFileManagerCapabilities.None;

    /// <summary>Name of the picked folder, null while nothing is picked.</summary>
    public string RootName { get; private set; }

    /// <summary>True when this browser has the File System Access API.</summary>
    public Task<bool> IsFileSystemAccessSupportedAsync()
        => _jsRuntime.InvokeAsync<bool>("MudExFileSystemAccess.isSupported").AsTask();

    /// <summary>
    /// Opens the folder picker. Returns false when the browser cannot do it or the user cancelled, in which
    /// case the caller falls back to an input with webkitdirectory and <see cref="UseFlatFileListAsync"/>.
    /// </summary>
    public async Task<bool> PickFolderAsync(bool writable = true, CancellationToken ct = default)
    {
        var picked = await _jsRuntime.InvokeAsync<PickedDirectory>("MudExFileSystemAccess.pickDirectory", ct, writable);
        if (picked == null || string.IsNullOrEmpty(picked.Id))
            return false;

        _flat = null;
        _rootHandleId = picked.Id;
        _pickedWritable = picked.Writable;
        _generation++;
        RootName = picked.Name;
        // Only what the browser actually granted: a folder picked read-only has no writer method that can
        // succeed, so reporting them would let a consumer render controls that always fail.
        Capabilities = _pickedWritable
            ? MudExFileManagerCapabilities.All
            : MudExFileManagerCapabilities.Read | MudExFileManagerCapabilities.Download;
        return true;
    }

    /// <summary>
    /// Takes the flat file list of an <c>input webkitdirectory</c> selection and builds the structure from the
    /// relative paths. Read-only, because the browser gives no write access on this path.
    /// </summary>
    public Task UseFlatFileListAsync(IReadOnlyCollection<FlatEntry> entries)
    {
        var list = (entries ?? Array.Empty<FlatEntry>())
            .Where(e => !string.IsNullOrWhiteSpace(e.RelativePath))
            .ToList();

        // Every path is prefixed with the picked folder. Keeping it would add one useless level.
        var rootPrefix = CommonRootFolder(list.Select(e => e.RelativePath));
        RootName = rootPrefix;

        _rootHandleId = null;
        _flatFiles.Clear();

        // The in-memory provider does the path to tree work; content comes from the browser file objects,
        // which is why it is wired up as a content provider instead of being handed bytes it does not have.
        _flat = new MudExInMemoryFileStructureManager
        {
            Capabilities = MudExFileManagerCapabilities.Read | MudExFileManagerCapabilities.Download,
            ContentProvider = OpenFlatFileAsync
        };

        foreach (var entry in list)
        {
            var path = Strip(entry.RelativePath, rootPrefix);
            _flat.AddFile(path, entry.Size, entry.LastModified);
            if (entry.File != null)
                _flatFiles[path] = entry.File;
        }

        Capabilities = _flat.Capabilities;
        return Task.CompletedTask;
    }

    // An entry whose FlatEntry carried no IBrowserFile is in the structure but has nothing to read. Handing
    // back an empty stream would make it indistinguishable from a genuinely empty file, which is exactly what
    // IMudExFileStructureManager.OpenReadAsync forbids.
    private Task<Stream> OpenFlatFileAsync(string path, CancellationToken ct)
        => _flatFiles.TryGetValue(path, out var file)
            ? Task.FromResult(file.OpenReadStream(long.MaxValue, ct))
            : throw new InvalidOperationException($"'{path}' cannot be read: the selection carried no file for it.");

    /// <inheritdoc />
    public async Task<HashSet<MudExFileStructureNode>> GetRootAsync(CancellationToken ct = default)
    {
        if (_flat != null)
            return await _flat.GetRootAsync(ct);
        if (_rootHandleId == null)
            return new HashSet<MudExFileStructureNode>();

        return await ListAsync(_rootHandleId, null, ct);
    }

    /// <inheritdoc />
    public async Task<HashSet<MudExFileStructureNode>> GetChildrenAsync(MudExFileStructureNode node, CancellationToken ct = default)
    {
        if (_flat != null)
            return IsFromAPick(node)
                ? new HashSet<MudExFileStructureNode>()
                : await _flat.GetChildrenAsync(node, ct);
        if (node == null || !node.IsDirectory || !TryGetCurrentHandleId(node, out var handleId))
            return new HashSet<MudExFileStructureNode>();

        return await ListAsync(handleId, node, ct);
    }

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(MudExFileStructureNode node, CancellationToken ct = default)
    {
        if (_flat != null)
        {
            if (IsFromAPick(node))
                throw new InvalidOperationException(
                    $"'{node.FullPath}' cannot be read: it belongs to a picked folder that a later flat selection replaced.");
            return await _flat.OpenReadAsync(node, ct);
        }
        if (!TryGetCurrentHandleId(node, out var handleId))
            throw new InvalidOperationException(
                $"'{node?.FullPath}' cannot be read: it does not belong to the folder that is currently picked.");

        // Handed over as a blob url so the bytes never travel through the interop boundary.
        var url = await _jsRuntime.InvokeAsync<string>("MudExFileSystemAccess.createFileUrl", ct, handleId);
        if (string.IsNullOrEmpty(url))
            throw new InvalidOperationException($"'{node.FullPath}' cannot be read: the browser provided no content for it.");

        try
        {
            using var client = new HttpClient();
            var bytes = await client.GetByteArrayAsync(url, ct);
            return new MemoryStream(bytes, writable: false);
        }
        finally
        {
            // CancellationToken.None on purpose: a cancelled read must still release the blob url, so the
            // cleanup call must not be skippable by the same token that just cancelled the read.
            await _jsRuntime.InvokeVoidAsync("MudExFileSystemAccess.revokeUrl", CancellationToken.None, url);
        }
    }

    /// <inheritdoc />
    public async Task<MudExFileStructureNode> CreateDirectoryAsync(MudExFileStructureNode parent, string name, CancellationToken ct = default)
    {
        EnsureWritable("create directories");
        EnsureValidName(name);

        var parentId = ResolveDirectoryId(parent);
        await EnsureFreeAsync(parentId, name, ct);

        var entry = await _jsRuntime.InvokeAsync<DirectoryEntry>("MudExFileSystemAccess.createDirectory", ct, parentId, name);
        return entry != null
            ? ToNode(entry, parent)
            : throw new InvalidOperationException($"The directory '{name}' could not be created.");
    }

    /// <inheritdoc />
    public async Task<MudExFileStructureNode> RenameAsync(MudExFileStructureNode node, string newName, CancellationToken ct = default)
    {
        EnsureWritable("rename entries");
        EnsureValidName(newName);

        if (!TryGetCurrentHandleId(node, out var handleId))
            throw new InvalidOperationException(
                $"'{node?.FullPath}' cannot be renamed: it does not belong to the folder that is currently picked.");

        // A rename stays in the same directory, so the parent is both the source and the target.
        var parentId = ResolveDirectoryId(node.Parent);
        if (!string.Equals(node.Name, newName, StringComparison.Ordinal))
            await EnsureFreeAsync(parentId, newName, ct);

        var entry = await _jsRuntime.InvokeAsync<DirectoryEntry>(
            "MudExFileSystemAccess.moveEntry", ct, handleId, parentId, newName);
        return entry != null
            ? ToNode(entry, node.Parent)
            : throw new InvalidOperationException($"'{node.FullPath}' could not be renamed to '{newName}'.");
    }

    /// <inheritdoc />
    public async Task MoveAsync(IReadOnlyCollection<MudExFileStructureNode> nodes, MudExFileStructureNode target, CancellationToken ct = default)
    {
        EnsureWritable("move entries");

        var toMove = (nodes ?? Array.Empty<MudExFileStructureNode>()).Where(n => n != null).ToList();
        if (toMove.Count == 0)
            return;

        var targetId = ResolveDirectoryId(target);

        // Validate everything before touching anything: the contract says a refused move leaves all given
        // nodes where they were.
        var handles = new List<(MudExFileStructureNode Node, string HandleId)>();
        foreach (var node in toMove)
        {
            if (!TryGetCurrentHandleId(node, out var handleId))
                throw new InvalidOperationException(
                    $"'{node.FullPath}' cannot be moved: it does not belong to the folder that is currently picked.");

            EnsureNotOwnDescendant(node, target);
            await EnsureFreeAsync(targetId, node.Name, ct);
            handles.Add((node, handleId));
        }

        foreach (var (node, handleId) in handles)
        {
            var entry = await _jsRuntime.InvokeAsync<DirectoryEntry>(
                "MudExFileSystemAccess.moveEntry", ct, handleId, targetId, node.Name);
            if (entry == null)
                throw new InvalidOperationException($"'{node.FullPath}' could not be moved.");
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(IReadOnlyCollection<MudExFileStructureNode> nodes, CancellationToken ct = default)
    {
        EnsureWritable("delete entries");

        foreach (var node in (nodes ?? Array.Empty<MudExFileStructureNode>()).Where(n => n != null))
        {
            if (!TryGetCurrentHandleId(node, out var handleId))
                throw new InvalidOperationException(
                    $"'{node.FullPath}' cannot be deleted: it does not belong to the folder that is currently picked.");

            await _jsRuntime.InvokeAsync<bool>("MudExFileSystemAccess.remove", ct, handleId);
        }
    }

    /// <inheritdoc />
    public async Task<MudExFileStructureNode> UploadAsync(MudExFileStructureNode target, IBrowserFile file, CancellationToken ct = default)
    {
        EnsureWritable("add files");
        if (file == null)
            throw new ArgumentNullException(nameof(file));

        var targetId = ResolveDirectoryId(target);

        // A stream reference instead of bytes: the content is transferred as a binary stream, so a large file
        // does not get base64 encoded through the JSON interop channel.
        await using var content = file.OpenReadStream(long.MaxValue, ct);
        using var streamRef = new DotNetStreamReference(content, leaveOpen: true);

        var entry = await _jsRuntime.InvokeAsync<DirectoryEntry>(
            "MudExFileSystemAccess.writeFile", ct, targetId, file.Name, streamRef);
        return entry != null
            ? ToNode(entry, target)
            : throw new InvalidOperationException($"'{file.Name}' could not be written to the selected folder.");
    }

    private void EnsureWritable(string what)
    {
        if (_flat != null || _rootHandleId == null || !_pickedWritable)
            throw new NotSupportedException($"This provider cannot {what} in the selected folder.");
    }

    private static void EnsureValidName(string name)
    {
        var invalid = string.IsNullOrWhiteSpace(name)
                      || name.IndexOf('/') >= 0
                      || name.IndexOf(SEP) >= 0
                      || name == "."
                      || name == "..";
        if (invalid)
            throw new InvalidOperationException($"'{name}' is not a plain entry name.");
    }

    // The root has no node of its own, so a null directory means the picked folder itself.
    private string ResolveDirectoryId(MudExFileStructureNode directory)
    {
        if (directory == null)
            return _rootHandleId ?? throw new NotSupportedException("No folder is picked.");
        if (!directory.IsDirectory)
            throw new InvalidOperationException($"'{directory.FullPath}' is not a directory.");
        if (!TryGetCurrentHandleId(directory, out var handleId))
            throw new InvalidOperationException(
                $"'{directory.FullPath}' does not belong to the folder that is currently picked.");
        return handleId;
    }

    private async Task EnsureFreeAsync(string parentId, string name, CancellationToken ct)
    {
        if (await _jsRuntime.InvokeAsync<bool>("MudExFileSystemAccess.entryExists", ct, parentId, name))
            throw new InvalidOperationException($"'{name}' already exists in the target directory.");
    }

    // Moving a directory into its own subtree would cut the subtree out of the structure - and on this
    // provider it would also make the recursive copy the move is built on recurse forever.
    private static void EnsureNotOwnDescendant(MudExFileStructureNode node, MudExFileStructureNode target)
    {
        for (var current = target; current != null; current = current.Parent)
        {
            if (ReferenceEquals(current, node))
                throw new InvalidOperationException($"'{node.FullPath}' cannot be moved into itself.");
        }
    }

    private MudExFileStructureNode ToNode(DirectoryEntry entry, MudExFileStructureNode parent)
    {
        var node = new MudExFileStructureNode
        {
            Name = entry.Name,
            FullPath = string.IsNullOrEmpty(parent?.FullPath) ? entry.Name : $"{parent.FullPath}/{entry.Name}",
            IsDirectory = entry.IsDirectory,
            Size = entry.Size,
            LastModified = entry.LastModified,
            Parent = parent,
            Handle = entry.Id
        };
        if (!string.IsNullOrEmpty(entry.ContentType))
            node.ContentType = entry.ContentType;
        if (entry.IsDirectory)
            node.LoadChildrenFunc = (n, token) => GetChildrenAsync(n, token);

        _nodeGeneration.AddOrUpdate(node, new GenerationTag { Value = _generation });
        return node;
    }

    private async Task<HashSet<MudExFileStructureNode>> ListAsync(string handleId, MudExFileStructureNode parent, CancellationToken ct)
    {
        var entries = await _jsRuntime.InvokeAsync<DirectoryEntry[]>("MudExFileSystemAccess.listDirectory", ct, handleId)
                      ?? Array.Empty<DirectoryEntry>();

        var result = new HashSet<MudExFileStructureNode>();
        foreach (var entry in entries)
        {
            var node = new MudExFileStructureNode
            {
                Name = entry.Name,
                FullPath = string.IsNullOrEmpty(parent?.FullPath) ? entry.Name : $"{parent.FullPath}/{entry.Name}",
                IsDirectory = entry.IsDirectory,
                Size = entry.Size,
                LastModified = entry.LastModified,
                Parent = parent,
                Handle = entry.Id
            };
            if (!string.IsNullOrEmpty(entry.ContentType))
                node.ContentType = entry.ContentType;

            if (entry.IsDirectory)
                node.LoadChildrenFunc = (n, token) => GetChildrenAsync(n, token);
            // Tags the node with the pick it came from - see _generation - so a stale reference to it is
            // refused instead of silently resolved against whatever a later pick's handle map holds.
            // AddOrUpdate rather than Add: stage C shares one node instance between two panels, and a
            // re-list handing back an already tagged instance would make Add throw for a re-tag that is right.
            _nodeGeneration.AddOrUpdate(node, new GenerationTag { Value = _generation });
            result.Add(node);
        }
        return result;
    }

    // A flat (webkitdirectory) node never carries a Handle, a File System Access node always does. Both
    // modes mint plain relative paths, so without this a node left over from a pick would resolve against a
    // later UseFlatFileListAsync structure by its FullPath string alone - the same silently-wrong-answer the
    // _generation check closes between two picks, only across the mode switch. The reverse direction needs
    // nothing: TryGetCurrentHandleId already refuses a flat node, which carries no handle at all.
    private static bool IsFromAPick(MudExFileStructureNode node) => node?.Handle != null;

    // A node's Handle string alone isn't enough to resolve safely: the same string can be reused by a later
    // pick for an unrelated directory (see _generation). Only a node still tagged with the currently active
    // pick is allowed through - anything else is treated exactly like a node with no usable handle at all.
    private bool TryGetCurrentHandleId(MudExFileStructureNode node, out string handleId)
    {
        handleId = null;
        if (node?.Handle is not string id)
            return false;
        if (!_nodeGeneration.TryGetValue(node, out var tag) || tag.Value != _generation)
            return false;

        handleId = id;
        return true;
    }

    private static string CommonRootFolder(IEnumerable<string> paths)
    {
        var firstSegments = paths
            .Select(p => p.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
            .Where(segments => segments.Length > 1)
            .Select(segments => segments[0])
            .Distinct()
            .ToList();

        return firstSegments.Count == 1 ? firstSegments[0] : string.Empty;
    }

    private static string Strip(string path, string rootPrefix)
    {
        path = path.Replace('\\', '/').TrimStart('/');
        return string.IsNullOrEmpty(rootPrefix) || !path.StartsWith(rootPrefix + "/", StringComparison.Ordinal)
            ? path
            : path.Substring(rootPrefix.Length + 1);
    }

    /// <summary>One file of an <c>input webkitdirectory</c> selection.</summary>
    public class FlatEntry
    {
        /// <summary>Path relative to the picked folder, including the folder name itself.</summary>
        public string RelativePath { get; set; }
        /// <summary>Size in bytes.</summary>
        public long Size { get; set; }
        /// <summary>Last write time.</summary>
        public DateTimeOffset? LastModified { get; set; }
        /// <summary>Mime type the browser reported.</summary>
        public string ContentType { get; set; }
        /// <summary>
        /// The browser file this entry came from. Without it the structure still builds, but the entry has no
        /// content and reading it throws instead of yielding an empty stream - so the caller pairs the relative
        /// paths from JS with the files Blazor handed it.
        /// </summary>
        public IBrowserFile File { get; set; }
    }

    // Internal rather than private: the test suite mocks the JS calls that return these shapes directly
    // (see ClientFolderStructureManagerTests), which needs the exact type InvokeAsync&lt;T&gt; asks for.
    internal sealed class PickedDirectory
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Writable { get; set; }
    }

    internal sealed class DirectoryEntry
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTimeOffset? LastModified { get; set; }
        public string ContentType { get; set; }
    }
}
