using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using Moq;
using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.FileManager;

public class InMemoryFileStructureManagerTests
{
    private static MudExInMemoryFileStructureManager Manager() => new(
        "docs/report.pdf",
        "docs/notes.txt",
        "docs/sub/deep.txt",
        "readme.md");

    private static IBrowserFile FakeFile(string name, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var file = new Mock<IBrowserFile>();
        file.Setup(f => f.Name).Returns(name);
        file.Setup(f => f.Size).Returns(bytes.Length);
        file.Setup(f => f.LastModified).Returns(DateTimeOffset.UtcNow);
        file.Setup(f => f.OpenReadStream(It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(new MemoryStream(bytes));
        return file.Object;
    }

    [Fact]
    public async Task GetRootAsync_ReturnsTopLevelEntriesOnly()
    {
        var root = await Manager().GetRootAsync();

        Assert.Equal(new[] { "docs", "readme.md" }, root.Select(n => n.Name).OrderBy(n => n));
        Assert.True(root.Single(n => n.Name == "docs").IsDirectory);
        Assert.False(root.Single(n => n.Name == "readme.md").IsDirectory);
    }

    [Fact]
    public async Task DirectoryNode_LoadsItsChildrenThroughLoadChildrenFunc()
    {
        // This is the crux of the design: the tree view drives loading through the node, not through us.
        var root = await Manager().GetRootAsync();
        var docs = root.Single(n => n.Name == "docs");

        Assert.NotNull(docs.LoadChildrenFunc);

        await docs.LoadChildren();

        Assert.Equal(new[] { "notes.txt", "report.pdf", "sub" }, docs.Children.Select(n => n.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task LoadedChildren_KnowTheirParentSoThePathCanBeBuilt()
    {
        // GetPathString() walks Parent. The breadcrumb depends on it, so a manager that forgets to set
        // Parent breaks navigation without breaking anything else visibly.
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");
        await docs.LoadChildren();
        var sub = docs.Children.Single(n => n.Name == "sub");
        await sub.LoadChildren();
        var deep = sub.Children.Single();

        Assert.Same(sub, deep.Parent);
        Assert.Same(docs, sub.Parent);
        Assert.Equal("docs/sub/deep.txt", deep.FullPath);
    }

    [Fact]
    public async Task OpenReadAsync_ReturnsTheStoredContent()
    {
        var manager = Manager();
        manager.SetContent("readme.md", Encoding.UTF8.GetBytes("# hello"));
        var file = (await manager.GetRootAsync()).Single(n => n.Name == "readme.md");

        await using var stream = await manager.OpenReadAsync(file);
        using var reader = new StreamReader(stream);

        Assert.Equal("# hello", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task CreateDirectory_ShowsUpAsAChildOfItsParent()
    {
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");

        var created = await manager.CreateDirectoryAsync(docs, "new");

        Assert.True(created.IsDirectory);
        Assert.Equal("docs/new", created.FullPath);
        Assert.Contains("new", (await manager.GetChildrenAsync(docs)).Select(n => n.Name));
    }

    [Fact]
    public async Task Rename_MovesTheEntryAndItsDescendants()
    {
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");
        await docs.LoadChildren();
        var sub = docs.Children.Single(n => n.Name == "sub");

        await manager.RenameAsync(sub, "renamed");

        var children = await manager.GetChildrenAsync(docs);
        Assert.Contains("renamed", children.Select(n => n.Name));
        Assert.DoesNotContain("sub", children.Select(n => n.Name));

        var renamed = children.Single(n => n.Name == "renamed");
        Assert.Equal("docs/renamed/deep.txt", (await manager.GetChildrenAsync(renamed)).Single().FullPath);
    }

    [Fact]
    public async Task Rename_ReturnsANewNodeAndLeavesTheGivenOneUntouched()
    {
        // The writer contract: the returned node is the new one, the node passed in is stale and still
        // reports its old name and path. Mutating the input would only look right until a caller noticed its
        // already loaded descendants were never rewritten with it.
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");
        await docs.LoadChildren();
        var sub = docs.Children.Single(n => n.Name == "sub");

        var renamed = await manager.RenameAsync(sub, "renamed");

        Assert.NotSame(sub, renamed);
        Assert.Equal("sub", sub.Name);
        Assert.Equal("docs/sub", sub.FullPath);
        Assert.Equal("renamed", renamed.Name);
        Assert.Equal("docs/renamed", renamed.FullPath);
        Assert.True(renamed.IsDirectory);
        Assert.NotNull(renamed.LoadChildrenFunc);
        Assert.Same(docs, renamed.Parent);
    }

    [Fact]
    public async Task Rename_OfAFile_ReturnsANewFileNodeCarryingTheSizeAndTimestamp()
    {
        var stamp = new DateTimeOffset(2026, 9, 8, 10, 0, 0, TimeSpan.Zero);
        var manager = new MudExInMemoryFileStructureManager();
        manager.AddFile("docs/a.txt", 99, stamp);
        var docs = (await manager.GetRootAsync()).Single();
        var file = (await manager.GetChildrenAsync(docs)).Single();

        var renamed = await manager.RenameAsync(file, "b.txt");

        Assert.NotSame(file, renamed);
        Assert.Equal("docs/a.txt", file.FullPath);
        Assert.Equal("docs/b.txt", renamed.FullPath);
        Assert.False(renamed.IsDirectory);
        Assert.Equal(99, renamed.Size);
        Assert.Equal(stamp, renamed.LastModified);
        Assert.Null(renamed.LoadChildrenFunc);
    }

    // --- item 2: names a user can type into a "new folder" box ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateDirectory_WithAnEmptyOrWhitespaceName_IsRefusedAndAddsNothing(string? name)
    {
        // Clicking OK on an empty textbox is the commonest bad input a file manager gets. Without the guard
        // Combine composes "docs/", EnsureFree lets it through and a blank named phantom directory appears.
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.CreateDirectoryAsync(docs, name));

        Assert.Equal(new[] { "notes.txt", "report.pdf", "sub" }, (await manager.GetChildrenAsync(docs)).Select(n => n.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task CreateDirectory_WithNoParentAndNoName_DoesNotAddAPhantomAtTheRoot()
    {
        var manager = Manager();

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.CreateDirectoryAsync(null, null));

        Assert.Equal(new[] { "docs", "readme.md" }, (await manager.GetRootAsync()).Select(n => n.Name).OrderBy(n => n));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Rename_ToAnEmptyOrWhitespaceName_IsRefusedAndLeavesTheEntryAlone(string? name)
    {
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");
        await docs.LoadChildren();
        var sub = docs.Children.Single(n => n.Name == "sub");

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.RenameAsync(sub, name));

        Assert.Equal(new[] { "notes.txt", "report.pdf", "sub" }, (await manager.GetChildrenAsync(docs)).Select(n => n.Name).OrderBy(n => n));
        Assert.Equal("docs/sub/deep.txt", (await manager.GetChildrenAsync(sub)).Single().FullPath);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("nested/file.txt")]
    public async Task Upload_WithANameThatIsNotAPlainName_IsRefused(string name)
    {
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.UploadAsync(docs, FakeFile(name, "hi")));

        Assert.Equal(new[] { "notes.txt", "report.pdf", "sub" }, (await manager.GetChildrenAsync(docs)).Select(n => n.Name).OrderBy(n => n));
    }

    // --- item 9: an unresolvable node throws, it never resolves to an empty stream ---

    [Fact]
    public async Task OpenReadAsync_UnknownPath_ThrowsInsteadOfLookingLikeAnEmptyFile()
    {
        var manager = Manager();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.OpenReadAsync(new MudExFileStructureNode { Name = "ghost.txt", FullPath = "docs/ghost.txt" }));
    }

    [Fact]
    public async Task OpenReadAsync_DirectoryOrNull_Throws()
    {
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.OpenReadAsync(docs));
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.OpenReadAsync(null));
    }

    [Fact]
    public async Task OpenReadAsync_KnownFileWithNoStoredBytes_IsAGenuinelyEmptyFile()
    {
        // The other half of the contract: the structure resolved the node, there simply are no bytes. That is
        // an empty file and must not be turned into a failure.
        var manager = new MudExInMemoryFileStructureManager("a.txt");
        var file = (await manager.GetRootAsync()).Single();

        await using var stream = await manager.OpenReadAsync(file);

        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public async Task Move_RelocatesEveryGivenNodeInOneCall()
    {
        var manager = Manager();
        var root = await manager.GetRootAsync();
        var docs = root.Single(n => n.Name == "docs");
        var readme = root.Single(n => n.Name == "readme.md");

        await manager.MoveAsync(new[] { readme }, docs);

        Assert.Contains("readme.md", (await manager.GetChildrenAsync(docs)).Select(n => n.Name));
        Assert.DoesNotContain("readme.md", (await manager.GetRootAsync()).Select(n => n.Name));
    }

    [Fact]
    public async Task Move_IntoOwnDescendant_IsRefusedAndTreeStaysIntact()
    {
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");
        await docs.LoadChildren();
        var sub = docs.Children.Single(n => n.Name == "sub");

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.MoveAsync(new[] { docs }, sub));

        Assert.Equal(new[] { "docs", "readme.md" }, (await manager.GetRootAsync()).Select(n => n.Name).OrderBy(n => n));
        Assert.Equal(new[] { "notes.txt", "report.pdf", "sub" }, (await manager.GetChildrenAsync(docs)).Select(n => n.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task Move_FileOntoAnExistingDirectoryPath_IsRefused()
    {
        var manager = Manager();
        manager.AddFile("sub", 5); // a root level file literally named "sub", colliding with "docs/sub" once moved
        var root = await manager.GetRootAsync();
        var docs = root.Single(n => n.Name == "docs");
        var rootSubFile = root.Single(n => n.Name == "sub" && !n.IsDirectory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.MoveAsync(new[] { rootSubFile }, docs));

        var docsChildren = await manager.GetChildrenAsync(docs);
        Assert.Equal(new[] { "notes.txt", "report.pdf", "sub" }, docsChildren.Select(n => n.Name).OrderBy(n => n));
        Assert.True(docsChildren.Single(n => n.Name == "sub").IsDirectory);
    }

    [Fact]
    public async Task Move_DirectoryOntoAnExistingFilePath_IsRefused()
    {
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");
        var namesake = await manager.CreateDirectoryAsync(docs, "readme.md"); // docs/readme.md, a directory

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.MoveAsync(new[] { namesake }, null));

        Assert.True((await manager.GetChildrenAsync(docs)).Single(n => n.Name == "readme.md").IsDirectory);
        Assert.False((await manager.GetRootAsync()).Single(n => n.Name == "readme.md").IsDirectory);
    }

    [Fact]
    public async Task Rename_OntoExistingSiblingName_IsRefused()
    {
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");
        await docs.LoadChildren();
        var notes = docs.Children.Single(n => n.Name == "notes.txt");

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.RenameAsync(notes, "report.pdf"));

        Assert.Equal(new[] { "notes.txt", "report.pdf", "sub" }, (await manager.GetChildrenAsync(docs)).Select(n => n.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task CreateDirectory_WithNameThatAlreadyExists_IsRefused()
    {
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.CreateDirectoryAsync(docs, "sub"));

        Assert.Single((await manager.GetChildrenAsync(docs)), n => n.Name == "sub");
    }

    [Fact]
    public async Task Rename_NewNameContainingSeparator_IsRefused()
    {
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");
        await docs.LoadChildren();
        var sub = docs.Children.Single(n => n.Name == "sub");

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.RenameAsync(sub, "nested/sub"));

        Assert.Contains("sub", (await manager.GetChildrenAsync(docs)).Select(n => n.Name));
        Assert.Equal("docs/sub/deep.txt", (await manager.GetChildrenAsync(sub)).Single().FullPath);
    }

    [Fact]
    public async Task Upload_OntoExistingDirectoryPath_IsRefused()
    {
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");
        var file = FakeFile("sub", "hi"); // "sub" collides with the existing directory docs/sub

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.UploadAsync(docs, file));

        Assert.True((await manager.GetChildrenAsync(docs)).Single(n => n.Name == "sub").IsDirectory);
    }

    [Fact]
    public async Task Upload_OntoExistingFile_ReplacesItsContent()
    {
        // A repeat upload of the same file is a normal thing to want - it's treated as a replace, the same way
        // SetContent already replaces a file's stored bytes.
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");
        var file = FakeFile("notes.txt", "updated");

        var uploaded = await manager.UploadAsync(docs, file);

        Assert.False(uploaded.IsDirectory);
        Assert.Single((await manager.GetChildrenAsync(docs)), n => n.Name == "notes.txt");
        await using var stream = await manager.OpenReadAsync(uploaded);
        using var reader = new StreamReader(stream);
        Assert.Equal("updated", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task Delete_RemovesTheEntryAndItsDescendants()
    {
        var manager = Manager();
        var docs = (await manager.GetRootAsync()).Single(n => n.Name == "docs");

        await manager.DeleteAsync(new[] { docs });

        Assert.Equal(new[] { "readme.md" }, (await manager.GetRootAsync()).Select(n => n.Name));
    }

    [Fact]
    public void Capabilities_ReportEverything()
    {
        Assert.Equal(MudExFileManagerCapabilities.All, Manager().Capabilities);
    }

    [Fact]
    public async Task AddFile_KeepsSizeAndTimestampWithoutAnyContent()
    {
        // A provider may know the metadata but not the bytes - the local folder fallback is exactly that case.
        var stamp = new DateTimeOffset(2026, 9, 8, 10, 0, 0, TimeSpan.Zero);
        var manager = new MudExInMemoryFileStructureManager();
        manager.AddFile("docs/a.txt", 1234, stamp);

        var docs = (await manager.GetRootAsync()).Single();
        var file = (await manager.GetChildrenAsync(docs)).Single();

        Assert.Equal(1234, file.Size);
        Assert.Equal(stamp, file.LastModified);
    }

    [Fact]
    public async Task ContentProvider_ServesFilesThatHaveNoStoredBytes()
    {
        var manager = new MudExInMemoryFileStructureManager("a.txt")
        {
            ContentProvider = (path, _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes($"served {path}")))
        };
        var file = (await manager.GetRootAsync()).Single();

        await using var stream = await manager.OpenReadAsync(file);
        using var reader = new StreamReader(stream);

        Assert.Equal("served a.txt", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task StoredContent_WinsOverTheContentProvider()
    {
        var manager = new MudExInMemoryFileStructureManager("a.txt")
        {
            ContentProvider = (_, _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("from provider")))
        };
        manager.SetContent("a.txt", Encoding.UTF8.GetBytes("stored"));
        var file = (await manager.GetRootAsync()).Single();

        await using var stream = await manager.OpenReadAsync(file);
        using var reader = new StreamReader(stream);

        Assert.Equal("stored", await reader.ReadToEndAsync());
    }
}
