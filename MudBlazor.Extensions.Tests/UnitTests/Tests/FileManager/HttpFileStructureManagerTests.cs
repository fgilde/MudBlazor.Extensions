using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using Moq;
using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.FileManager;

public class HttpFileStructureManagerTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();
        public Func<HttpRequestMessage, HttpResponseMessage>? Respond { get; set; }

        /// <summary>
        /// The parts of the last multipart body, snapshotted during the call. UploadAsync owns its
        /// MultipartFormDataContent and disposes it when it returns, and MultipartContent.Dispose clears its
        /// part list - so a test can only look at the parts from in here, while the request is still alive.
        /// </summary>
        public List<(string? Name, string? FileName, string Value)> MultipartParts { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (request.Content is MultipartFormDataContent multipart)
            {
                MultipartParts.Clear();
                foreach (var part in multipart)
                    MultipartParts.Add((
                        part.Headers.ContentDisposition?.Name?.Trim('"'),
                        part.Headers.ContentDisposition?.FileName?.Trim('"'),
                        await part.ReadAsStringAsync(cancellationToken)));
            }

            return Respond?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            };
        }
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private static (MudExHttpFileStructureManager Manager, StubHandler Handler) Create()
    {
        var handler = new StubHandler();
        var manager = new MudExHttpFileStructureManager(new HttpClient(handler))
        {
            BaseUrl = "https://example.com/api/files",
            Capabilities = MudExFileManagerCapabilities.All
        };
        return (manager, handler);
    }

    [Fact]
    public async Task GetRootAsync_CallsTheListRouteWithAnEmptyPath()
    {
        var (manager, handler) = Create();
        handler.Respond = _ => Json("""
            [ { "name": "docs", "isDirectory": true },
              { "name": "readme.md", "isDirectory": false, "size": 42, "contentType": "text/markdown" } ]
            """);

        var root = await manager.GetRootAsync();

        Assert.Equal("https://example.com/api/files?path=", handler.Requests.Single().RequestUri!.ToString());
        Assert.Equal(HttpMethod.Get, handler.Requests.Single().Method);
        Assert.Equal(new[] { "docs", "readme.md" }, root.Select(n => n.Name).OrderBy(n => n));

        var readme = root.Single(n => n.Name == "readme.md");
        Assert.Equal(42, readme.Size);
        Assert.Equal("text/markdown", readme.ContentType);
    }

    [Fact]
    public async Task DirectoryNodes_GetALoadChildrenFuncAndAComposedPath()
    {
        var (manager, handler) = Create();
        handler.Respond = request => request.RequestUri!.Query.Contains("path=docs")
            ? Json("""[ { "name": "notes.txt", "isDirectory": false } ]""")
            : Json("""[ { "name": "docs", "isDirectory": true } ]""");

        var docs = (await manager.GetRootAsync()).Single();
        Assert.NotNull(docs.LoadChildrenFunc);

        await docs.LoadChildren();

        var child = docs.Children.Single();
        Assert.Equal("docs/notes.txt", child.FullPath);
        Assert.Same(docs, child.Parent);
    }

    [Fact]
    public async Task ServerSuppliedFullPath_IsUsedInsteadOfComposingIt()
    {
        // A server may expose ids rather than paths, so an explicit fullPath always wins.
        var (manager, handler) = Create();
        handler.Respond = _ => Json("""[ { "name": "notes.txt", "fullPath": "id:8f21", "isDirectory": false } ]""");

        Assert.Equal("id:8f21", (await manager.GetRootAsync()).Single().FullPath);
    }

    [Fact]
    public async Task OpenReadAsync_CallsTheContentRoute()
    {
        var (manager, handler) = Create();
        handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("payload"))
        };

        await using var stream = await manager.OpenReadAsync(new MudExFileStructureNode { FullPath = "docs/a.txt" });
        using var reader = new StreamReader(stream);

        Assert.Equal("payload", await reader.ReadToEndAsync());
        // Uri.EscapeDataString emits upper-case hex escapes (%2F), not the lower-case %2f the brief's draft assumed.
        Assert.Equal("https://example.com/api/files/content?path=docs%2Fa.txt", handler.Requests.Single().RequestUri!.ToString());
    }

    [Fact]
    public async Task MoveAsync_SendsEveryPathInOneRequest()
    {
        var (manager, handler) = Create();
        handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.NoContent);

        await manager.MoveAsync(
            new[] { new MudExFileStructureNode { FullPath = "a.txt" }, new MudExFileStructureNode { FullPath = "b.txt" } },
            new MudExFileStructureNode { FullPath = "docs", IsDirectory = true });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://example.com/api/files/move", request.RequestUri!.ToString());

        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
        Assert.Equal(new[] { "a.txt", "b.txt" }, body.RootElement.GetProperty("paths").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal("docs", body.RootElement.GetProperty("target").GetString());
    }

    [Fact]
    public async Task FailingResponse_ThrowsWithTheStatusInTheMessage()
    {
        var (manager, handler) = Create();
        handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.Forbidden);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => manager.GetRootAsync());
        Assert.Contains("403", exception.Message);
    }

    [Fact]
    public void RouteOverride_ReplacesTheDefault()
    {
        var (manager, _) = Create();
        manager.ListRoute = "/browse";

        Assert.Equal("https://example.com/api/files/browse", manager.ResolveRoute(manager.ListRoute));
    }

    [Fact]
    public async Task CreateDirectoryAsync_PostsToTheFolderRoute()
    {
        var (manager, handler) = Create();
        handler.Respond = _ => Json("""{ "name": "new", "isDirectory": true }""");

        var node = await manager.CreateDirectoryAsync(new MudExFileStructureNode { FullPath = "docs", IsDirectory = true }, "new");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://example.com/api/files/folder", request.RequestUri!.ToString());

        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
        Assert.Equal("docs", body.RootElement.GetProperty("path").GetString());
        Assert.Equal("new", body.RootElement.GetProperty("name").GetString());

        Assert.Equal("new", node.Name);
        Assert.True(node.IsDirectory);
    }

    [Fact]
    public async Task RenameAsync_SendsAPatchToTheRenameRoute()
    {
        var (manager, handler) = Create();
        handler.Respond = _ => Json("""{ "name": "b.txt", "isDirectory": false }""");

        var node = await manager.RenameAsync(new MudExFileStructureNode { FullPath = "a.txt" }, "b.txt");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("https://example.com/api/files/rename", request.RequestUri!.ToString());

        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
        Assert.Equal("a.txt", body.RootElement.GetProperty("path").GetString());
        Assert.Equal("b.txt", body.RootElement.GetProperty("newName").GetString());

        Assert.Equal("b.txt", node.Name);
    }

    [Fact]
    public async Task RenameAsync_ReturnsANewNodeAndLeavesTheGivenOneUntouched()
    {
        // Same post-condition as the in-memory provider: the returned node is the new one, the input is
        // stale. Asserting it on the input node is what stops the two providers drifting apart again.
        var (manager, handler) = Create();
        handler.Respond = _ => Json("""{ "name": "b.txt", "fullPath": "docs/b.txt", "isDirectory": false }""");
        var input = new MudExFileStructureNode { Name = "a.txt", FullPath = "docs/a.txt" };

        var renamed = await manager.RenameAsync(input, "b.txt");

        Assert.NotSame(input, renamed);
        Assert.Equal("a.txt", input.Name);
        Assert.Equal("docs/a.txt", input.FullPath);
        Assert.Equal("b.txt", renamed.Name);
        Assert.Equal("docs/b.txt", renamed.FullPath);
    }

    [Fact]
    public async Task OpenReadAsync_LengthComesFromContentLengthWithoutReadingTheBody()
    {
        // Forwarding Length to the response stream throws for a non-seekable body, which makes a consumer
        // copy the whole file just to learn its size - MudExFileDisplay does exactly that.
        var (manager, handler) = Create();
        handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("payload"))
        };

        await using var stream = await manager.OpenReadAsync(new MudExFileStructureNode { FullPath = "docs/a.txt" });

        Assert.Equal(7, stream.Length);
        // Knowing the length is not being able to seek to it.
        Assert.False(stream.CanSeek);
    }

    [Fact]
    public async Task OpenReadAsync_ServerSays404_Throws()
    {
        // The failure contract every provider now shares: an unresolvable node never yields an empty stream.
        var (manager, handler) = Create();
        handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => manager.OpenReadAsync(new MudExFileStructureNode { FullPath = "docs/gone.txt" }));
    }

    [Fact]
    public async Task DeleteAsync_SendsEveryPathOnADeleteRequest()
    {
        var (manager, handler) = Create();
        handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.NoContent);

        await manager.DeleteAsync(new[]
        {
            new MudExFileStructureNode { FullPath = "a.txt" },
            new MudExFileStructureNode { FullPath = "docs", IsDirectory = true }
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("https://example.com/api/files", request.RequestUri!.ToString());

        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
        Assert.Equal(new[] { "a.txt", "docs" }, body.RootElement.GetProperty("paths").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task UploadAsync_PostsMultipartContentToTheUploadRoute()
    {
        var (manager, handler) = Create();
        handler.Respond = _ => Json("""{ "name": "new.txt", "isDirectory": false, "size": 7 }""");

        var node = await manager.UploadAsync(new MudExFileStructureNode { FullPath = "docs", IsDirectory = true }, FakeFile("new.txt", "payload"));

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://example.com/api/files/upload", request.RequestUri!.ToString());
        Assert.IsType<MultipartFormDataContent>(request.Content);

        // The part names are the wire contract a server author has to match, so they are pinned here.
        Assert.Equal(new[] { "file", "path" }, handler.MultipartParts.Select(part => part.Name).OrderBy(name => name));

        var filePart = handler.MultipartParts.Single(part => part.Name == "file");
        Assert.Equal("new.txt", filePart.FileName);
        Assert.Equal("payload", filePart.Value);
        Assert.Equal("docs", handler.MultipartParts.Single(part => part.Name == "path").Value);

        Assert.Equal("new.txt", node.Name);
        Assert.Equal(7, node.Size);
    }

    [Fact]
    public async Task UploadAsync_ToTheRoot_SendsAnEmptyPathPart()
    {
        // target is null at the top level, so the part must still be there and simply be empty - a server
        // that reads a required "path" field must not have to special case a missing part.
        var (manager, handler) = Create();
        handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.NoContent);

        await manager.UploadAsync(null, FakeFile("new.txt", "payload"));

        Assert.Equal(string.Empty, handler.MultipartParts.Single(part => part.Name == "path").Value);
    }

    [Fact]
    public async Task WriteAnsweredWith204AndNoBody_StillYieldsAUsableNode()
    {
        var (manager, handler) = Create();
        handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.NoContent);

        var node = await manager.CreateDirectoryAsync(new MudExFileStructureNode { FullPath = "docs", IsDirectory = true }, "new");

        Assert.Equal("new", node.Name);
        Assert.True(node.IsDirectory);
    }

    [Fact]
    public async Task WriteAnsweredWithAMalformedNonEmptyBody_Throws()
    {
        var (manager, handler) = Create();
        handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not json", Encoding.UTF8, "application/json")
        };

        await Assert.ThrowsAsync<JsonException>(
            () => manager.CreateDirectoryAsync(new MudExFileStructureNode { FullPath = "docs", IsDirectory = true }, "new"));
    }

    private static IBrowserFile FakeFile(string name, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var file = new Mock<IBrowserFile>();
        file.Setup(f => f.Name).Returns(name);
        file.Setup(f => f.Size).Returns(bytes.Length);
        file.Setup(f => f.ContentType).Returns("text/plain");
        file.Setup(f => f.OpenReadStream(It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(new MemoryStream(bytes));
        return file.Object;
    }
}
