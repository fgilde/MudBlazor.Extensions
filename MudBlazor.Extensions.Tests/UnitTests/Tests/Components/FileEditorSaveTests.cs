using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components;
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

    [Fact]
    public async Task SaveLocalFile_DownloadFallback_MapsTarget()
    {
        var (service, js) = Create();
        js.Setup<MudExLocalFileSaveDto>("MudExFileSystemAccess.saveFile", _ => true)
            .SetResult(new MudExLocalFileSaveDto(null, "a.txt", "Download"));

        await using var content = Content();
        var result = await service.SaveLocalFileAsync(content);

        Assert.Equal(MudExFileSaveTarget.Download, result.Target);
        Assert.Null(result.HandleId);
    }

    [Fact]
    public async Task ReleaseLocalFile_InvokesReleaseFile()
    {
        var (service, js) = Create();
        js.SetupVoid("MudExFileSystemAccess.releaseFile", _ => true).SetVoidResult();

        await service.ReleaseLocalFileAsync("h1");

        Assert.Equal("h1", js.Invocations["MudExFileSystemAccess.releaseFile"].Single().Arguments[0]);
    }
}

public class FileEditorBaseTests
{
    private sealed record Infos(string FileName, string Text) : IMudExFileDisplayInfos
    {
        public string Url => null;
        public string ContentType => "text/plain";
        public Stream ContentStream { get; } = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Text));
    }

    private static (IRenderedComponent<TestTextEditor> Cut, BunitJSInterop Js) Create(Action<ComponentParameterCollectionBuilder<TestTextEditor>> configure = null)
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = context.Render<TestTextEditor>(p =>
        {
            p.Add(c => c.FileDisplayInfos, new Infos("a.txt", "x"));
            configure?.Invoke(p);
        });
        return (cut, context.JSInterop);
    }

    private static void SetupLocalSave(BunitJSInterop js)
        => js.Setup<MudExLocalFileSaveDto>("MudExFileSystemAccess.saveFile", _ => true).SetResult(new MudExLocalFileSaveDto("h1", "a.txt", "Local"));

    private static IEnumerable<string> Identifiers(BunitJSInterop js) => js.Invocations.Select(i => i.Identifier);

    [Fact]
    public async Task Edit_MarksDirty_AndRaisesIsDirtyChangedOnce()
    {
        int dirtyChanged = 0, contentChanged = 0;
        var (cut, _) = Create(p => p
            .Add(c => c.IsDirtyChanged, EventCallback.Factory.Create<bool>(this, _ => dirtyChanged++))
            .Add(c => c.ContentChanged, EventCallback.Factory.Create(this, () => contentChanged++)));

        await cut.InvokeAsync(() => cut.Instance.Edit("a"));
        await cut.InvokeAsync(() => cut.Instance.Edit("b"));

        Assert.True(cut.Instance.IsDirty);
        Assert.Equal(1, dirtyChanged);
        Assert.Equal(2, contentChanged);
    }

    [Fact]
    public async Task Save_HandledByHost_DoesNotTouchLocalFileSystem()
    {
        MudExFileSavedEventArgs saved = null;
        var (cut, js) = Create(p => p
            .Add(c => c.SaveRequested, EventCallback.Factory.Create<MudExFileSaveRequest>(this, r => r.Handled = true))
            .Add(c => c.Saved, EventCallback.Factory.Create<MudExFileSavedEventArgs>(this, e => saved = e)));
        await cut.InvokeAsync(() => cut.Instance.Edit("a"));

        var result = await cut.InvokeAsync(() => cut.Instance.SaveAsync());

        Assert.True(result);
        Assert.False(cut.Instance.IsDirty);
        Assert.Equal(MudExFileSaveTarget.Host, saved.Target);
        Assert.DoesNotContain("MudExFileSystemAccess.saveFile", Identifiers(js));
    }

    [Fact]
    public async Task Save_Cancelled_KeepsDirty()
    {
        var savedCount = 0;
        var (cut, _) = Create(p => p
            .Add(c => c.SaveRequested, EventCallback.Factory.Create<MudExFileSaveRequest>(this, r => r.Cancel = true))
            .Add(c => c.Saved, EventCallback.Factory.Create<MudExFileSavedEventArgs>(this, _ => savedCount++)));
        await cut.InvokeAsync(() => cut.Instance.Edit("a"));

        Assert.False(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));
        Assert.True(cut.Instance.IsDirty);
        Assert.Equal(0, savedCount);
    }

    [Fact]
    public async Task Save_Local_SecondSaveReusesHandle()
    {
        var (cut, js) = Create();
        SetupLocalSave(js);
        await cut.InvokeAsync(() => cut.Instance.Edit("a"));

        Assert.True(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));
        await cut.InvokeAsync(() => cut.Instance.Edit("b"));
        Assert.True(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));

        var calls = js.Invocations["MudExFileSystemAccess.saveFile"].ToList();
        Assert.Null(calls[0].Arguments[0]);
        Assert.Equal("h1", calls[1].Arguments[0]);
    }

    [Fact]
    public async Task SaveAs_AlwaysAsksForNewTarget()
    {
        var (cut, js) = Create();
        SetupLocalSave(js);
        js.SetupVoid("MudExFileSystemAccess.releaseFile", _ => true).SetVoidResult();

        await cut.InvokeAsync(() => cut.Instance.SaveAsync());
        await cut.InvokeAsync(() => cut.Instance.SaveAsync(true));

        Assert.Null(js.Invocations["MudExFileSystemAccess.saveFile"].Last().Arguments[0]);
    }

    [Fact]
    public async Task Save_LocalNotAllowed_AndUnhandled_ReturnsFalse()
    {
        var (cut, js) = Create(p => p.Add(c => c.AllowLocalSave, false));
        await cut.InvokeAsync(() => cut.Instance.Edit("a"));

        Assert.False(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));
        Assert.True(cut.Instance.IsDirty);
        Assert.DoesNotContain("MudExFileSystemAccess.saveFile", Identifiers(js));
    }

    [Fact]
    public async Task Save_UserAbortsPicker_ReturnsFalse_NoSavedEvent()
    {
        var savedCount = 0;
        var (cut, js) = Create(p => p.Add(c => c.Saved, EventCallback.Factory.Create<MudExFileSavedEventArgs>(this, _ => savedCount++)));
        js.Setup<MudExLocalFileSaveDto>("MudExFileSystemAccess.saveFile", _ => true).SetResult(null);
        await cut.InvokeAsync(() => cut.Instance.Edit("a"));

        Assert.False(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));
        Assert.True(cut.Instance.IsDirty);
        Assert.Equal(0, savedCount);
    }

    [Fact]
    public async Task Save_HostHandlerThrows_KeepsDirty_AndRethrowsStandalone()
    {
        var (cut, _) = Create(p => p
            .Add(c => c.SaveRequested, EventCallback.Factory.Create<MudExFileSaveRequest>(this, _ => throw new InvalidOperationException("boom"))));
        await cut.InvokeAsync(() => cut.Instance.Edit("a"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.InvokeAsync(() => cut.Instance.SaveAsync()));

        Assert.Equal("boom", ex.Message);
        Assert.True(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task NewFileDisplayInfos_ResetsDirty_AndReleasesHandle()
    {
        var (cut, js) = Create();
        SetupLocalSave(js);
        js.SetupVoid("MudExFileSystemAccess.releaseFile", _ => true).SetVoidResult();
        await cut.InvokeAsync(() => cut.Instance.Edit("a"));
        await cut.InvokeAsync(() => cut.Instance.SaveAsync());
        await cut.InvokeAsync(() => cut.Instance.Edit("b"));

        cut.Render(p => p.Add(c => c.FileDisplayInfos, new Infos("b.txt", "new")));

        Assert.False(cut.Instance.IsDirty);
        Assert.Equal("new", cut.Instance.Text);
        Assert.Equal("h1", js.Invocations["MudExFileSystemAccess.releaseFile"].Single().Arguments[0]);
    }
}
