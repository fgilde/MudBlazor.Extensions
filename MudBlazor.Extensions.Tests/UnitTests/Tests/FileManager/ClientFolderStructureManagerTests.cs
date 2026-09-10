using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.FileManager;

/// <summary>
/// Covers everything about the local folder provider that does not need a real browser. The File System
/// Access path itself needs Chromium and a user gesture and is verified manually in the demo.
/// </summary>
public class ClientFolderStructureManagerTests
{
    private static MudExClientFolderStructureManager Create(out BunitJSInterop jsInterop)
    {
        var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        jsInterop = context.JSInterop;
        return new MudExClientFolderStructureManager(context.JSInterop.JSRuntime);
    }

    private static MudExClientFolderStructureManager.FlatEntry Entry(string relativePath, long size = 0, string content = null)
        => new()
        {
            RelativePath = relativePath,
            Size = size,
            File = content == null ? null : new StubBrowserFile(relativePath, content)
        };

    private sealed class StubBrowserFile : IBrowserFile
    {
        private readonly byte[] _content;

        public StubBrowserFile(string name, string content)
        {
            Name = name;
            _content = Encoding.UTF8.GetBytes(content);
        }

        public string Name { get; }
        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
        public long Size => _content.Length;
        public string ContentType => "text/plain";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
            => new MemoryStream(_content, writable: false);
    }

    [Fact]
    public async Task FlatFileList_BuildsATreeFromTheRelativePaths()
    {
        // This is the webkitdirectory fallback: one flat list, every directory implied by a path segment.
        var manager = Create(out _);
        await manager.UseFlatFileListAsync(new[]
        {
            Entry("root/docs/report.pdf", 10),
            Entry("root/docs/sub/deep.txt", 20),
            Entry("root/readme.md", 30)
        });

        var root = await manager.GetRootAsync();
        Assert.Equal(new[] { "docs", "readme.md" }, root.Select(n => n.Name).OrderBy(n => n));

        var docs = root.Single(n => n.Name == "docs");
        var children = await manager.GetChildrenAsync(docs);
        Assert.Equal(new[] { "report.pdf", "sub" }, children.Select(n => n.Name).OrderBy(n => n));
        Assert.Equal(10, children.Single(n => n.Name == "report.pdf").Size);
    }

    [Fact]
    public async Task FlatFileList_DropsTheCommonRootFolderName()
    {
        // webkitdirectory prefixes every path with the picked folder. Keeping it would show one useless level.
        var manager = Create(out _);
        await manager.UseFlatFileListAsync(new[] { Entry("picked/a.txt"), Entry("picked/b/c.txt") });

        Assert.Equal(new[] { "a.txt", "b" }, (await manager.GetRootAsync()).Select(n => n.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task FlatFileList_SingleFile_DropsThePickedFolderName()
    {
        // A folder with exactly one file still gets prefixed by webkitdirectory - one entry is enough to
        // identify the common root, so it is honest to strip it the same as with many entries.
        var manager = Create(out _);
        await manager.UseFlatFileListAsync(new[] { Entry("picked/onlyfile.txt") });

        Assert.Equal(new[] { "onlyfile.txt" }, (await manager.GetRootAsync()).Select(n => n.Name));
    }

    [Fact]
    public async Task FlatFileList_TwoTopLevelFolders_KeepsBothInsteadOfGuessingARoot()
    {
        // Nothing webkitdirectory produces can disagree on the top segment - this only happens when a caller
        // hands UseFlatFileListAsync entries from two unrelated picks. There is no single folder to strip in
        // that case, so the honest behaviour is to leave both top level names in place rather than picking one.
        var manager = Create(out _);
        await manager.UseFlatFileListAsync(new[] { Entry("folderA/x.txt"), Entry("folderB/y.txt") });

        Assert.Equal(new[] { "folderA", "folderB" }, (await manager.GetRootAsync()).Select(n => n.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task FlatFileList_ReportsReadOnlyCapabilities()
    {
        var manager = Create(out _);
        await manager.UseFlatFileListAsync(new[] { Entry("picked/a.txt") });

        Assert.Equal(MudExFileManagerCapabilities.Read | MudExFileManagerCapabilities.Download, manager.Capabilities);
    }

    [Fact]
    public async Task FlatFileList_ChildrenKnowTheirParent()
    {
        var manager = Create(out _);
        await manager.UseFlatFileListAsync(new[] { Entry("picked/docs/a.txt") });

        var docs = (await manager.GetRootAsync()).Single();
        var file = (await manager.GetChildrenAsync(docs)).Single();

        Assert.Same(docs, file.Parent);
        Assert.Equal("docs/a.txt", file.FullPath);
    }

    [Fact]
    public async Task BeforeAnyFolderIsPicked_ThereIsNothingAndNothingIsAllowed()
    {
        var manager = Create(out _);

        Assert.Empty(await manager.GetRootAsync());
        Assert.Equal(MudExFileManagerCapabilities.None, manager.Capabilities);
    }

    [Fact]
    public async Task IsFileSystemAccessSupportedAsync_AsksTheBrowser()
    {
        var manager = Create(out var jsInterop);
        jsInterop.Setup<bool>("MudExFileSystemAccess.isSupported").SetResult(true);

        Assert.True(await manager.IsFileSystemAccessSupportedAsync());
    }

    [Fact]
    public async Task FlatFileList_ReadsContentFromTheBrowserFile()
    {
        // The whole point of carrying IBrowserFile on FlatEntry: without it the preview stays empty.
        var manager = Create(out _);
        await manager.UseFlatFileListAsync(new[] { Entry("picked/docs/a.txt", 5, "hello") });

        var docs = (await manager.GetRootAsync()).Single();
        var file = (await manager.GetChildrenAsync(docs)).Single();

        await using var stream = await manager.OpenReadAsync(file);
        using var reader = new StreamReader(stream);

        Assert.Equal("hello", await reader.ReadToEndAsync());
    }

    // Pins the "writable" argument PickFolderAsync passes to pickDirectory - a wildcard matcher would also
    // match a call the manager never made.
    private static bool RequestedWritable(JSRuntimeInvocation invocation, bool expected)
        => invocation.Arguments.Count == 1 && invocation.Arguments[0] is bool w && w == expected;

    // Pins the handleId argument listDirectory/createFileUrl receive - same reasoning as RequestedWritable.
    private static bool ForHandle(JSRuntimeInvocation invocation, string expectedHandleId)
        => invocation.Arguments.Count == 1 && invocation.Arguments[0] is string id && id == expectedHandleId;

    [Fact]
    public async Task PickFolderAsync_WritablePick_ReportsEveryCapability()
    {
        var manager = Create(out var jsInterop);
        jsInterop.Setup<MudExClientFolderStructureManager.PickedDirectory>(
                "MudExFileSystemAccess.pickDirectory", args => RequestedWritable(args, true))
            .SetResult(new MudExClientFolderStructureManager.PickedDirectory { Id = "/", Name = "myFolder", Writable = true });

        var picked = await manager.PickFolderAsync(writable: true);

        Assert.True(picked);
        Assert.Equal("myFolder", manager.RootName);
        Assert.Equal(MudExFileManagerCapabilities.All, manager.Capabilities);
    }

    [Fact]
    public async Task PickFolderAsync_ReadOnlyPick_ReportsNoWriteCapability()
    {
        var manager = Create(out var jsInterop);
        jsInterop.Setup<MudExClientFolderStructureManager.PickedDirectory>(
                "MudExFileSystemAccess.pickDirectory", args => RequestedWritable(args, true))
            .SetResult(new MudExClientFolderStructureManager.PickedDirectory { Id = "/", Name = "myFolder", Writable = false });

        var picked = await manager.PickFolderAsync(writable: true);

        Assert.True(picked);
        // The picker was asked for write access and the browser did not grant it. Reporting a write
        // capability here would let a consumer render controls whose every call throws.
        Assert.Equal(MudExFileManagerCapabilities.Read | MudExFileManagerCapabilities.Download, manager.Capabilities);
    }

    [Fact]
    public async Task WriterMethods_OnAReadOnlyPick_ThrowNotSupported()
    {
        var manager = Create(out var jsInterop);
        jsInterop.Setup<MudExClientFolderStructureManager.PickedDirectory>(
                "MudExFileSystemAccess.pickDirectory", args => RequestedWritable(args, false))
            .SetResult(new MudExClientFolderStructureManager.PickedDirectory { Id = "/", Name = "myFolder", Writable = false });

        await manager.PickFolderAsync(writable: false);

        await Assert.ThrowsAsync<NotSupportedException>(() => manager.CreateDirectoryAsync(null, "new"));
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.DeleteAsync(Array.Empty<MudExFileStructureNode>()));
    }

    [Fact]
    public async Task CreateDirectoryAsync_OnAnOccupiedName_IsRefused()
    {
        var manager = Create(out var jsInterop);
        jsInterop.Setup<MudExClientFolderStructureManager.PickedDirectory>(
                "MudExFileSystemAccess.pickDirectory", args => RequestedWritable(args, true))
            .SetResult(new MudExClientFolderStructureManager.PickedDirectory { Id = "/", Name = "myFolder", Writable = true });
        jsInterop.Setup<bool>("MudExFileSystemAccess.entryExists", _ => true).SetResult(true);

        await manager.PickFolderAsync(writable: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.CreateDirectoryAsync(null, "docs"));
    }

    [Fact]
    public async Task CreateDirectoryAsync_WithAPathInsteadOfAName_IsRefused()
    {
        var manager = Create(out var jsInterop);
        jsInterop.Setup<MudExClientFolderStructureManager.PickedDirectory>(
                "MudExFileSystemAccess.pickDirectory", args => RequestedWritable(args, true))
            .SetResult(new MudExClientFolderStructureManager.PickedDirectory { Id = "/", Name = "myFolder", Writable = true });

        await manager.PickFolderAsync(writable: true);

        // A rename or a create changes what an entry is called, never where it lives.
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.CreateDirectoryAsync(null, "docs/sub"));
    }

    [Fact]
    public async Task CreateDirectoryAsync_OnAFreeName_ReturnsTheNewNode()
    {
        var manager = Create(out var jsInterop);
        jsInterop.Setup<MudExClientFolderStructureManager.PickedDirectory>(
                "MudExFileSystemAccess.pickDirectory", args => RequestedWritable(args, true))
            .SetResult(new MudExClientFolderStructureManager.PickedDirectory { Id = "/", Name = "myFolder", Writable = true });
        jsInterop.Setup<bool>("MudExFileSystemAccess.entryExists", _ => true).SetResult(false);
        jsInterop.Setup<MudExClientFolderStructureManager.DirectoryEntry>("MudExFileSystemAccess.createDirectory", _ => true)
            .SetResult(new MudExClientFolderStructureManager.DirectoryEntry { Id = "/docs", Name = "docs", IsDirectory = true });

        await manager.PickFolderAsync(writable: true);
        var created = await manager.CreateDirectoryAsync(null, "docs");

        Assert.Equal("docs", created.Name);
        Assert.Equal("docs", created.FullPath);
        Assert.True(created.IsDirectory);
    }

    [Fact]
    public async Task PickFolderAsync_Cancelled_LeavesTheProviderEmpty()
    {
        var manager = Create(out var jsInterop);
        jsInterop.Setup<MudExClientFolderStructureManager.PickedDirectory>(
                "MudExFileSystemAccess.pickDirectory", args => RequestedWritable(args, true))
            .SetResult(null);

        var picked = await manager.PickFolderAsync(writable: true);

        Assert.False(picked);
        Assert.Empty(await manager.GetRootAsync());
        Assert.Equal(MudExFileManagerCapabilities.None, manager.Capabilities);
    }

    [Fact]
    public async Task PickFolderAsync_ListsRootWithHandleParentAndComposedFullPath()
    {
        var manager = Create(out var jsInterop);
        jsInterop.Setup<MudExClientFolderStructureManager.PickedDirectory>(
                "MudExFileSystemAccess.pickDirectory", args => RequestedWritable(args, true))
            .SetResult(new MudExClientFolderStructureManager.PickedDirectory { Id = "/", Name = "myFolder", Writable = false });
        await manager.PickFolderAsync(writable: true);

        jsInterop.Setup<MudExClientFolderStructureManager.DirectoryEntry[]>(
                "MudExFileSystemAccess.listDirectory", args => ForHandle(args, "/"))
            .SetResult(new[]
            {
                new MudExClientFolderStructureManager.DirectoryEntry { Id = "/docs", Name = "docs", IsDirectory = true },
                new MudExClientFolderStructureManager.DirectoryEntry { Id = "/a.txt", Name = "a.txt", IsDirectory = false, Size = 5 }
            });

        var root = await manager.GetRootAsync();
        var docs = root.Single(n => n.Name == "docs");
        var file = root.Single(n => n.Name == "a.txt");

        Assert.Equal("/docs", docs.Handle);
        Assert.Equal("/a.txt", file.Handle);
        Assert.Null(docs.Parent);
        Assert.Null(file.Parent);
        Assert.Equal("docs", docs.FullPath);
        Assert.Equal("a.txt", file.FullPath);
        Assert.Equal(5, file.Size);
        // LoadChildrenFunc drives lazy loading in the tree view - only a directory node gets one.
        Assert.NotNull(docs.LoadChildrenFunc);
        Assert.Null(file.LoadChildrenFunc);
    }

    [Fact]
    public async Task PickFolderAsync_GetChildrenAsync_ComposesFullPathAndWiresLoadChildrenOnlyOnDirectories()
    {
        // PickFolderAsync_ListsRootWithHandleParentAndComposedFullPath only ever calls GetRootAsync, where the
        // parent is null - it never exercises the composed side of
        // `string.IsNullOrEmpty(parent?.FullPath) ? entry.Name : $"{parent.FullPath}/{entry.Name}"`. This one
        // does, one level down, through GetChildrenAsync on a real File System Access directory node.
        var manager = Create(out var jsInterop);
        jsInterop.Setup<MudExClientFolderStructureManager.PickedDirectory>(
                "MudExFileSystemAccess.pickDirectory", args => RequestedWritable(args, true))
            .SetResult(new MudExClientFolderStructureManager.PickedDirectory { Id = "/", Name = "myFolder", Writable = false });
        await manager.PickFolderAsync(writable: true);

        jsInterop.Setup<MudExClientFolderStructureManager.DirectoryEntry[]>(
                "MudExFileSystemAccess.listDirectory", args => ForHandle(args, "/"))
            .SetResult(new[] { new MudExClientFolderStructureManager.DirectoryEntry { Id = "/docs", Name = "docs", IsDirectory = true } });
        var docs = (await manager.GetRootAsync()).Single();

        jsInterop.Setup<MudExClientFolderStructureManager.DirectoryEntry[]>(
                "MudExFileSystemAccess.listDirectory", args => ForHandle(args, "/docs"))
            .SetResult(new[]
            {
                new MudExClientFolderStructureManager.DirectoryEntry { Id = "/docs/sub", Name = "sub", IsDirectory = true },
                new MudExClientFolderStructureManager.DirectoryEntry { Id = "/docs/a.txt", Name = "a.txt", IsDirectory = false }
            });

        var children = await manager.GetChildrenAsync(docs);
        var sub = children.Single(n => n.Name == "sub");
        var file = children.Single(n => n.Name == "a.txt");

        Assert.Equal("docs/sub", sub.FullPath);
        Assert.Equal("docs/a.txt", file.FullPath);
        Assert.Same(docs, sub.Parent);
        Assert.Same(docs, file.Parent);
        Assert.NotNull(sub.LoadChildrenFunc);
        Assert.Null(file.LoadChildrenFunc);
    }

    [Fact]
    public async Task OpenReadAsync_FileSystemAccessMode_ThrowsWhenTheBrowserGivesNoBlobUrl()
    {
        // Verifies the guard around the blob-url flow without needing an actual fetch of a blob: url, which
        // only a real browser can resolve - see the report's "OpenReadAsync HttpClient" note. It throws
        // rather than yielding an empty stream: an empty stream would make "no content" indistinguishable
        // from "an empty file", which is what IMudExFileStructureManager.OpenReadAsync forbids.
        var manager = Create(out var jsInterop);
        jsInterop.Setup<MudExClientFolderStructureManager.PickedDirectory>(
                "MudExFileSystemAccess.pickDirectory", args => RequestedWritable(args, true))
            .SetResult(new MudExClientFolderStructureManager.PickedDirectory { Id = "/", Name = "myFolder", Writable = false });
        await manager.PickFolderAsync(writable: true);

        jsInterop.Setup<MudExClientFolderStructureManager.DirectoryEntry[]>(
                "MudExFileSystemAccess.listDirectory", args => ForHandle(args, "/"))
            .SetResult(new[] { new MudExClientFolderStructureManager.DirectoryEntry { Id = "/a.txt", Name = "a.txt", IsDirectory = false } });
        var file = (await manager.GetRootAsync()).Single();

        jsInterop.Setup<string>(
                "MudExFileSystemAccess.createFileUrl", args => ForHandle(args, "/a.txt"))
            .SetResult(null!);

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.OpenReadAsync(file));
    }

    [Fact]
    public async Task OpenReadAsync_StaleNodeFromAnEarlierPick_Throws()
    {
        // GetChildrenAsync answers a stale node with an empty set, but a read cannot: a zero byte stream is
        // a plausible looking answer, and a caller writing it back over the real file is silent data loss.
        var manager = Create(out var jsInterop);
        jsInterop.Setup<MudExClientFolderStructureManager.PickedDirectory>(
                "MudExFileSystemAccess.pickDirectory", args => RequestedWritable(args, true))
            .SetResult(new MudExClientFolderStructureManager.PickedDirectory { Id = "/", Name = "folder1", Writable = false });
        await manager.PickFolderAsync(writable: true);

        jsInterop.Setup<MudExClientFolderStructureManager.DirectoryEntry[]>(
                "MudExFileSystemAccess.listDirectory", args => ForHandle(args, "/"))
            .SetResult(new[] { new MudExClientFolderStructureManager.DirectoryEntry { Id = "/a.txt", Name = "a.txt", IsDirectory = false } });
        var staleFile = (await manager.GetRootAsync()).Single();

        jsInterop.Setup<MudExClientFolderStructureManager.PickedDirectory>(
                "MudExFileSystemAccess.pickDirectory", args => RequestedWritable(args, false))
            .SetResult(new MudExClientFolderStructureManager.PickedDirectory { Id = "/", Name = "folder2", Writable = false });
        await manager.PickFolderAsync(writable: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.OpenReadAsync(staleFile));
    }

    [Fact]
    public async Task OpenReadAsync_FlatEntryWithoutABrowserFile_Throws()
    {
        // FlatEntry.File is optional, so the structure builds without it - but then there is nothing to read
        // and saying so beats handing back an empty stream that looks like a real, empty file.
        var manager = Create(out _);
        await manager.UseFlatFileListAsync(new[] { Entry("picked/docs/a.txt", 10) });

        var docs = (await manager.GetRootAsync()).Single();
        var file = (await manager.GetChildrenAsync(docs)).Single();

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.OpenReadAsync(file));
    }

    [Fact]
    public async Task PickFolderAsync_SecondPick_StaleNodeFromTheFirstNeverResolvesToTheSecondFoldersData()
    {
        // Round 1 bounded the JS handle map by keying ids on path, but that alone makes ids *reusable* across
        // picks - a directory named "docs" is common enough that pick 2 can mint the very same id string "/docs"
        // pick 1 already handed to a node the caller still holds (a breadcrumb, a cached tree node). Without a
        // generation check, GetChildrenAsync on that stale node would forward "/docs" to JS and could come back
        // with pick 2's children - a silently wrong answer, not a failure. It must come back empty instead.
        var manager = Create(out var jsInterop);

        jsInterop.Setup<MudExClientFolderStructureManager.PickedDirectory>(
                "MudExFileSystemAccess.pickDirectory", args => RequestedWritable(args, true))
            .SetResult(new MudExClientFolderStructureManager.PickedDirectory { Id = "/", Name = "folder1", Writable = false });
        await manager.PickFolderAsync(writable: true);

        jsInterop.Setup<MudExClientFolderStructureManager.DirectoryEntry[]>(
                "MudExFileSystemAccess.listDirectory", args => ForHandle(args, "/"))
            .SetResult(new[] { new MudExClientFolderStructureManager.DirectoryEntry { Id = "/docs", Name = "docs", IsDirectory = true } });
        var staleDocs = (await manager.GetRootAsync()).Single();

        // Second pick, distinguished from the first only by the "writable" argument so the mock can tell the
        // two PickFolderAsync calls apart - the point of the scenario is that both picks still hand out the
        // exact same id string "/" for their root and "/docs" for a same-named subfolder.
        jsInterop.Setup<MudExClientFolderStructureManager.PickedDirectory>(
                "MudExFileSystemAccess.pickDirectory", args => RequestedWritable(args, false))
            .SetResult(new MudExClientFolderStructureManager.PickedDirectory { Id = "/", Name = "folder2", Writable = false });
        await manager.PickFolderAsync(writable: false);

        jsInterop.Setup<MudExClientFolderStructureManager.DirectoryEntry[]>(
                "MudExFileSystemAccess.listDirectory", args => ForHandle(args, "/docs"))
            .SetResult(new[] { new MudExClientFolderStructureManager.DirectoryEntry { Id = "/docs/from-folder-2.txt", Name = "from-folder-2.txt", IsDirectory = false } });

        Assert.Empty(await manager.GetChildrenAsync(staleDocs));
    }

    [Fact]
    public async Task NodeFromAPick_NeverResolvesAgainstALaterFlatSelection()
    {
        // The mode-switch half of the generation guard. Both modes mint plain relative paths, so a node from
        // a File System Access pick would otherwise be matched against the flat structure by its FullPath
        // string alone - here "docs" exists in both and the read would silently answer from the wrong tree.
        // Flat nodes never carry a Handle and File System Access nodes always do, which is the whole test.
        var manager = Create(out var jsInterop);
        jsInterop.Setup<MudExClientFolderStructureManager.PickedDirectory>(
                "MudExFileSystemAccess.pickDirectory", args => RequestedWritable(args, true))
            .SetResult(new MudExClientFolderStructureManager.PickedDirectory { Id = "/", Name = "picked", Writable = false });
        await manager.PickFolderAsync(writable: true);

        jsInterop.Setup<MudExClientFolderStructureManager.DirectoryEntry[]>(
                "MudExFileSystemAccess.listDirectory", args => ForHandle(args, "/"))
            .SetResult(new[]
            {
                new MudExClientFolderStructureManager.DirectoryEntry { Id = "/docs", Name = "docs", IsDirectory = true },
                new MudExClientFolderStructureManager.DirectoryEntry { Id = "/a.txt", Name = "a.txt", IsDirectory = false }
            });
        var root = await manager.GetRootAsync();
        var pickedDocs = root.Single(n => n.Name == "docs");
        var pickedFile = root.Single(n => n.Name == "a.txt");

        // Same names, different folder, arriving through the other acquisition mode.
        await manager.UseFlatFileListAsync(new[] { Entry("other/docs/from-flat.txt", 5, "flat"), Entry("other/a.txt", 4, "flat") });

        Assert.Empty(await manager.GetChildrenAsync(pickedDocs));
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.OpenReadAsync(pickedFile));

        // The flat structure itself is perfectly readable through its own nodes - only the leftovers are refused.
        var flatDocs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");
        Assert.Equal(new[] { "from-flat.txt" }, (await manager.GetChildrenAsync(flatDocs)).Select(n => n.Name));
    }

    [Fact]
    public async Task WriterMethods_AllThrowNotSupported()
    {
        // Stage A never implements writing for this provider (see IMudExFileStructureWriter's remarks on
        // NotSupportedException vs InvalidOperationException) - every method must refuse unconditionally,
        // independent of whatever was picked.
        var manager = Create(out _);
        var node = new MudExFileStructureNode { Name = "a.txt", FullPath = "a.txt" };

        await Assert.ThrowsAsync<NotSupportedException>(() => manager.CreateDirectoryAsync(null, "new"));
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.RenameAsync(node, "b.txt"));
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.MoveAsync(new[] { node }, null));
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.DeleteAsync(new[] { node }));
        await Assert.ThrowsAsync<NotSupportedException>(() => manager.UploadAsync(null, null!));

        // The other providers' rename post-condition assertion, in the only form it can take here: a refusal
        // must leave the node it was handed exactly as it was.
        Assert.Equal("a.txt", node.Name);
        Assert.Equal("a.txt", node.FullPath);
    }

    [Fact]
    public async Task WriterMethods_MessagesSayWhatCannotBeDoneRatherThanWhenItArrives()
    {
        // The message reaches an end user in an exception dialog, so it must not carry our delivery plan.
        var manager = Create(out _);

        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => manager.RenameAsync(new MudExFileStructureNode { Name = "a.txt" }, "b.txt"));

        Assert.DoesNotContain("stage", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cannot rename", exception.Message);
    }
}
