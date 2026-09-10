using System.Text;
using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// Guards the element chain from <c>.mud-ex-file-display-container</c> down to the viewer.
/// </summary>
/// <remarks>
/// The container is <c>height: calc(100% - 60px)</c>, so every element between it and the viewer needs a
/// height of its own. An extra wrapper without one silently collapses every viewer that fills its box - a
/// pdf, an image, a leaflet map - and that is not visible in any behavioural test, only in the markup.
/// </remarks>
public class FileDisplayLayoutTests
{
    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    private static IRenderedComponent<MudExFileDisplay> Render(TestContext context, bool showMeta,
        MudExFileMetaPlacement placement = MudExFileMetaPlacement.Below)
        => context.Render<MudExFileDisplay>(parameters => parameters
            .Add(c => c.FileName, "points.geojson")
            .Add(c => c.ContentType, "application/octet-stream")
            .Add(c => c.ContentStream, new MemoryStream(Encoding.UTF8.GetBytes(
                "{\"type\":\"FeatureCollection\",\"features\":[]}")))
            .Add(c => c.ShowFileMeta, showMeta)
            .Add(c => c.FileMetaPlacement, placement));

    [Fact]
    public async Task WithoutTheMetaViewNothingSitsBetweenTheContainerAndTheViewer()
    {
        await using var context = CreateContext();
        var cut = Render(context, showMeta: false);

        // A direct child, or the height chain is broken.
        Assert.NotEmpty(cut.FindAll(".mud-ex-file-display-container > .mud-ex-file-display"));
        Assert.Empty(cut.FindAll(".mud-ex-file-display-content-row"));
        Assert.Empty(cut.FindAll(".mud-ex-file-display-content-main"));
    }

    [Theory]
    [InlineData(MudExFileMetaPlacement.Below)]
    [InlineData(MudExFileMetaPlacement.Above)]
    public async Task StackedMetaPlacementsAddNoWrapperEither(MudExFileMetaPlacement placement)
    {
        await using var context = CreateContext();
        var cut = Render(context, showMeta: true, placement);

        Assert.NotEmpty(cut.FindAll(".mud-ex-file-display-container > .mud-ex-file-display"));
        Assert.Empty(cut.FindAll(".mud-ex-file-display-content-row"));
    }

    [Fact]
    public async Task OnlyTheRightPlacementWrapsTheViewerInARow()
    {
        await using var context = CreateContext();
        var cut = Render(context, showMeta: true, MudExFileMetaPlacement.Right);

        // Here the wrapper is what the layout needs, and it carries its own height in css.
        Assert.NotEmpty(cut.FindAll(".mud-ex-file-display-content-row > .mud-ex-file-display-content-main"));
        Assert.NotEmpty(cut.FindAll(".mud-ex-file-display-content-main .mud-ex-file-display"));
    }

    [Fact]
    public async Task TheViewerIsRenderedOnceWhateverThePlacement()
    {
        await using var context = CreateContext();

        foreach (var placement in new[]
                 {
                     MudExFileMetaPlacement.Below, MudExFileMetaPlacement.Above, MudExFileMetaPlacement.Right
                 })
        {
            var cut = Render(context, showMeta: true, placement);
            Assert.Single(cut.FindAll(".mud-ex-file-display"));
        }
    }
}
