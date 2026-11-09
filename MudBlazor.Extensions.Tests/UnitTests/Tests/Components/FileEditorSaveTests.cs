using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class FileEditorSaveTests
{
    

    private static (MudExFileService Service, BunitJSInterop Js) Create()
    {
        var context = new TestContext();
        var service = new MudExFileService(context.JSInterop.JSRuntime, context.Services);
        return (service, context.JSInterop);
    }

    private static MudExFileContent Content() => new()
    {
        FileName = "a.txt",
        ContentType = "text/plain",
        Stream = new MemoryStream("hello"u8.ToArray())
    };

    [Fact]
    public async Task SaveLocalFile_PassesHandleAndReturnsResult()
    {
        var (service, js) = Create();
        var invocation = js.Setup<MudExLocalFileSaveDto>("MudExFileSystemAccess.saveFile",
            i => i.Arguments[0] == null && i.Arguments[2] as string == "a.txt" && i.Arguments[3] as string == "text/plain");
        invocation.SetResult(new MudExLocalFileSaveDto("h1", "a.txt", "Local"));

        await using var content = Content();
        var result = await service.SaveLocalFileAsync(content);

        Assert.Equal(new MudExLocalFileSaveResult("h1", "a.txt", MudExFileSaveTarget.Local), result);
    }

    [Fact]
    public async Task SaveLocalFile_ReturnsNull_WhenUserCancels()
    {
        var (service, js) = Create();
        js.Setup<MudExLocalFileSaveDto>("MudExFileSystemAccess.saveFile", _ => true).SetResult(null);

        await using var content = Content();
        var result = await service.SaveLocalFileAsync(content);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveLocalFile_ReusesGivenHandle()
    {
        var (service, js) = Create();
        var invocation = js.Setup<MudExLocalFileSaveDto>("MudExFileSystemAccess.saveFile", _ => true);
        invocation.SetResult(new MudExLocalFileSaveDto("h1", "a.txt", "Local"));

        await using var content = Content();
        await service.SaveLocalFileAsync(content, "h1");

        Assert.Equal("h1", js.Invocations["MudExFileSystemAccess.saveFile"].Single().Arguments[0]);
    }
}
