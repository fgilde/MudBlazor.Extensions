using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// Two files of the same kind share one viewer instance, so switching from one geojson to the next - or from
/// one log to another - has to make the viewer load again.
/// </summary>
/// <remarks>
/// It cannot notice that by itself: <see cref="MudExFileDisplay"/> passes itself as the
/// <see cref="IMudExFileDisplayInfos"/>, so the viewer is handed the very same object every time and only the
/// values differ.
/// </remarks>
public class ViewerReloadTests
{
    private sealed class Infos : IMudExFileDisplayInfos
    {
        public string FileName { get; init; }
        public string Url { get; init; }
        public string ContentType { get; init; }
        public Stream ContentStream { get; init; }
    }

    private static Infos ForText(string fileName, string content) => new()
    {
        FileName = fileName,
        ContentType = "text/plain",
        ContentStream = new MemoryStream(Encoding.UTF8.GetBytes(content))
    };

    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    [Fact]
    public async Task ADifferentFileOfTheSameKindIsLoadedAgain()
    {
        await using var context = CreateContext();

        var first = ForText("first.log", "INFO first file marker");
        var cut = context.Render<MudExFileDisplayLog>(parameters => parameters
            .Add(c => c.FileDisplayInfos, first));

        Assert.Contains("first file marker", cut.Markup);

        // The same viewer instance, a different file.
        var second = ForText("second.log", "INFO second file marker");
        await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                [nameof(MudExFileDisplayLog.FileDisplayInfos)] = second
            })));

        Assert.Contains("second file marker", cut.Markup);
        Assert.DoesNotContain("first file marker", cut.Markup);
    }

    [Fact]
    public async Task TheSameFileKeepsItsParsedContent()
    {
        await using var context = CreateContext();

        var infos = ForText("only.log", "INFO the only marker");
        var cut = context.Render<MudExFileDisplayLog>(parameters => parameters
            .Add(c => c.FileDisplayInfos, infos));

        // Same values, same file. Whether it re-reads is covered by TheSameSourceIsReportedOnlyOnce; what
        // matters here is that being asked again does not lose what was parsed - the stream is consumed by now.
        await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                [nameof(MudExFileDisplayLog.FileDisplayInfos)] = infos
            })));

        Assert.Contains("the only marker", cut.Markup);
    }

    [Theory]
    [InlineData("a.geojson", "b.geojson")]
    [InlineData("a.geojson", "b.gpx")]
    public void ADifferentNameIsADifferentSource(string first, string second)
    {
        object loaded = null;

        // Same content, different file: the name alone has to count, because two geo formats share a viewer
        // and the viewer parses by name.
        var bytes = Encoding.UTF8.GetBytes("{}");
        Assert.True(new Infos { FileName = first, ContentStream = new MemoryStream(bytes) }.SourceChanged(ref loaded));
        Assert.True(new Infos { FileName = second, ContentStream = new MemoryStream(bytes) }.SourceChanged(ref loaded));
    }

    [Fact]
    public void TheSameSourceIsReportedOnlyOnce()
    {
        object loaded = null;
        var infos = ForText("same.log", "x");

        Assert.True(infos.SourceChanged(ref loaded));
        Assert.False(infos.SourceChanged(ref loaded));
    }

    [Fact]
    public void InfosWithoutAnythingToReadAreNoSource()
    {
        object loaded = null;

        Assert.False(new Infos { FileName = "empty.log" }.SourceChanged(ref loaded));
        Assert.False(new Infos { FileName = "empty.log", ContentStream = new MemoryStream() }.SourceChanged(ref loaded));
        Assert.False(((IMudExFileDisplayInfos)null).SourceChanged(ref loaded));
    }
}
