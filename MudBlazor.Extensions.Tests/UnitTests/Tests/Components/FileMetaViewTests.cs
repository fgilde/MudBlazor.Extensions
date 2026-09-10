using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// Covers <see cref="MudExFileMetaView"/>: the intrinsic properties render, extra <c>MetaInformation</c>
/// entries render, and null/empty/whitespace values are skipped unless <c>ShowEmptyValues</c> is set.
/// </summary>
public class FileMetaViewTests
{
    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    [Fact]
    public async Task RendersIntrinsicProperties()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExFileMetaView>(parameters => parameters
            .Add(c => c.FileName, "report.pdf")
            .Add(c => c.ContentType, "application/pdf")
            .Add(c => c.Size, 2048L)
            .Add(c => c.LastModified, new DateTimeOffset(2026, 9, 8, 10, 0, 0, TimeSpan.Zero))
            .Add(c => c.Path, "/documents/report.pdf"));

        Assert.Contains("report.pdf", cut.Markup);
        Assert.Contains("application/pdf", cut.Markup);
        Assert.Contains(Nextended.Blazor.Extensions.BrowserFileExtensions.GetReadableFileSize(2048L), cut.Markup);
        Assert.Contains("/documents/report.pdf", cut.Markup);
    }

    [Fact]
    public async Task RendersMetaInformationEntry()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExFileMetaView>(parameters => parameters
            .Add(c => c.FileName, "capture.har")
            .Add(c => c.MetaInformation, new Dictionary<string, object> { { "Requests", 42 } }));

        Assert.Contains("Requests", cut.Markup);
        Assert.Contains("42", cut.Markup);
    }

    [Fact]
    public async Task SkipsNullAndEmptyValuesByDefault()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExFileMetaView>(parameters => parameters
            .Add(c => c.FileName, "data.bin")
            .Add(c => c.Path, null)
            .Add(c => c.MetaInformation, new Dictionary<string, object>
            {
                { "Author", null },
                { "Notes", "   " },
                { "Requests", 3 }
            }));

        Assert.DoesNotContain("Author", cut.Markup);
        Assert.DoesNotContain("Notes", cut.Markup);
        Assert.DoesNotContain("Path", cut.Markup);
        Assert.Contains("Requests", cut.Markup);
    }

    [Fact]
    public async Task ShowsEmptyValuesWhenRequested()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExFileMetaView>(parameters => parameters
            .Add(c => c.FileName, "data.bin")
            .Add(c => c.ShowEmptyValues, true)
            .Add(c => c.MetaInformation, new Dictionary<string, object>
            {
                { "Author", null },
                { "Notes", "   " }
            }));

        Assert.Contains("Author", cut.Markup);
        Assert.Contains("Notes", cut.Markup);
    }
}
