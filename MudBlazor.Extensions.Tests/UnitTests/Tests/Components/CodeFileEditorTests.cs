using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;
using System.Text;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class CodeFileEditorTests : IAsyncLifetime
{
    private readonly List<TestContext> _contexts = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
            await context.DisposeAsync();
    }

    private sealed record Infos(string FileName, byte[] Bytes) : IMudExFileDisplayInfos
    {
        public string Url => null;
        public string ContentType => "text/plain";
        public Stream ContentStream { get; } = new MemoryStream(Bytes);
    }

    private IRenderedComponent<MudExFileDisplayCode> Create(byte[] bytes, bool readOnly = false)
    {
        var context = new TestContext();
        _contexts.Add(context);
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context.Render<MudExFileDisplayCode>(p => p
            .Add(c => c.FileDisplayInfos, new Infos("a.cs", bytes))
            .Add(c => c.ReadOnly, readOnly));
    }

    private static async Task<byte[]> Export(IRenderedComponent<MudExFileDisplayCode> cut)
    {
        await using var content = await cut.InvokeAsync(() => cut.Instance.ExportAsync());
        using var ms = new MemoryStream();
        await content.Stream.CopyToAsync(ms);
        return ms.ToArray();
    }

    [Fact]
    public void ReadOnly_RendersCodeView_Editable_RendersTextarea()
    {
        var readOnly = Create("a"u8.ToArray(), readOnly: true);
        Assert.Empty(readOnly.FindAll("textarea.mud-ex-code-editor"));
        Assert.NotEmpty(readOnly.FindComponents<MudExCodeView>());

        var editable = Create("a"u8.ToArray());
        Assert.Single(editable.FindAll("textarea.mud-ex-code-editor"));
    }

    [Fact]
    public void Typing_MarksDirty()
    {
        var cut = Create("a"u8.ToArray());
        cut.Find("textarea").Input("x");
        Assert.True(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task Export_KeepsCrLfLineEndings()
    {
        var cut = Create("a\r\nb"u8.ToArray());
        cut.Find("textarea").Input("a\nb\nc");
        Assert.Equal("a\r\nb\r\nc", Encoding.UTF8.GetString(await Export(cut)));
    }

    [Fact]
    public async Task Export_KeepsUtf16Bom()
    {
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("a\nb")).ToArray();
        var cut = Create(bytes);
        cut.Find("textarea").Input("a\nbc");
        var result = await Export(cut);
        Assert.Equal(new byte[] { 0xFF, 0xFE }, result.Take(2));
        Assert.Equal("a\nbc", Encoding.Unicode.GetString(result, 2, result.Length - 2));
    }

    [Fact]
    public async Task Export_Utf8WithoutBom_StaysWithoutBom()
    {
        var cut = Create("a"u8.ToArray());
        cut.Find("textarea").Input("ab");
        Assert.Equal("ab"u8.ToArray(), await Export(cut));
    }

    [Fact]
    public async Task Export_InvalidUtf8_RoundTripsBytesUnchanged()
    {
        byte[] bytes = [0x48, 0xE4, 0x0D, 0x0A, 0xFC];
        var cut = Create(bytes);
        Assert.Equal(bytes, await Export(cut));
    }

    [Fact]
    public async Task New_StartsEmptyTextFile()
    {
        var cut = Create("a"u8.ToArray());

        Assert.True(await cut.InvokeAsync(() => cut.Instance.NewAsync()));

        Assert.Equal("new.txt", cut.Instance.FileName);
        Assert.Equal("", cut.Find("textarea").GetAttribute("value") ?? "");
        Assert.Empty(await Export(cut));
        await using var content = await cut.InvokeAsync(() => cut.Instance.ExportAsync());
        Assert.Equal("new.txt", content.FileName);
        Assert.Equal("text/plain", content.ContentType);
    }

    private sealed class FailingStream : MemoryStream
    {
        public FailingStream() : base([1, 2, 3]) { }
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => throw new IOException("boom");
    }

    private sealed record FailingInfos : IMudExFileDisplayInfos
    {
        public string FileName => "a.cs";
        public string Url => null;
        public string ContentType => "text/plain";
        public Stream ContentStream { get; } = new FailingStream();
    }

    [Fact]
    public async Task LoadFailure_IsNotEditable()
    {
        var context = new TestContext();
        _contexts.Add(context);
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var host = context.Render<MudExFileDisplay>();
        var cut = context.Render<MudExFileDisplayCode>(p => p
            .AddCascadingValue(host.Instance)
            .Add(c => c.FileDisplayInfos, new FailingInfos()));
        Assert.Empty(cut.FindAll("textarea"));
        Assert.False(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));
        Assert.Contains("could not be loaded", host.Instance.ErrorMessage);
    }
}
