using System.Text;
using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// Covers the handover of file metadata from <see cref="MudExFileDisplay"/> to whoever renders it.
/// </summary>
/// <remarks>
/// The metadata of a file is whatever the active viewer knows: an excel or csv viewer reports sheets, rows and
/// columns, a har reports request counts. Only the viewer can produce those, and only after it has parsed the
/// file - so a consumer with its own metadata panel has to receive them from the file display rather than
/// gather anything itself. These tests pin that path.
/// </remarks>
public class FileMetaHandoverTests
{
    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    [Fact]
    public async Task FileDisplayReportsItsMetadata()
    {
        await using var context = CreateContext();
        IDictionary<string, object> reported = null;

        var cut = context.Render<MudExFileDisplay>(parameters => parameters
            .Add(c => c.FileName, "data.csv")
            .Add(c => c.ContentType, "text/csv")
            .Add(c => c.ContentStream, new MemoryStream(Encoding.UTF8.GetBytes("a;b\n1;2\n")))
            .Add(c => c.FileMetaInformationChanged, meta => reported = meta));

        // The event has to fire without ShowFileMeta: a consumer rendering the view elsewhere never sets it.
        Assert.NotNull(reported);
        Assert.Equal("data.csv", Assert.Contains("File", reported));
        Assert.Equal("text/csv", Assert.Contains("ContentType", reported));
    }

    [Fact]
    public async Task FileDisplayDoesNotReportWithoutAListener()
    {
        await using var context = CreateContext();

        // Nothing to assert beyond "this does not throw and does not gather anything": the gathering is
        // skipped entirely when neither ShowFileMeta nor the callback asks for it.
        var cut = context.Render<MudExFileDisplay>(parameters => parameters
            .Add(c => c.FileName, "data.csv")
            .Add(c => c.ContentType, "text/csv")
            .Add(c => c.ContentStream, new MemoryStream(Encoding.UTF8.GetBytes("a;b\n1;2\n"))));

        Assert.False(cut.Instance.ShowFileMeta);
    }

    [Fact]
    public async Task TheDetailsPanelShowsWhatThePreviewReported()
    {
        await using var context = CreateContext();

        var structure = new MudExInMemoryFileStructureManager
        {
            Capabilities = MudExFileManagerCapabilities.Read
        };
        structure.AddFile("data.csv", 8);
        structure.SetContent("data.csv", Encoding.UTF8.GetBytes("a;b\n1;2\n"));

        var cut = context.Render<MudExFileManager>(parameters => parameters
            .Add(c => c.Manager, structure)
            .Add(c => c.Panels, MudExFileManagerPanels.Files | MudExFileManagerPanels.Preview | MudExFileManagerPanels.Meta));

        var csv = cut.Instance.RootItems.First();
        await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(csv));

        // Read from the component rather than from a rendered panel: which panels are laid out depends on
        // MudHidden, and that needs a breakpoint the test context cannot report.
        var meta = cut.Instance.SelectedFileMetaInformation;

        Assert.NotNull(meta);
        // Whatever the viewer put in there arrived - the file manager itself collects nothing.
        Assert.Contains("File", meta);
    }

    [Fact]
    public async Task IntrinsicKeysAreNotShownTwice()
    {
        await using var context = CreateContext();

        var cut = context.Render<MudExFileMetaView>(parameters => parameters
            .Add(c => c.FileName, "data.csv")
            .Add(c => c.ContentType, "text/csv")
            .Add(c => c.Size, 8L)
            .Add(c => c.MetaInformation, new Dictionary<string, object>
            {
                // Exactly what MudExFileDisplay reports: the file's own properties plus the viewer's extras.
                { "File", "data.csv" },
                { "ContentType", "text/csv" },
                { "Size", "8 bytes" },
                { "Rows", 1 },
                { "Columns", 2 }
            }));

        // The viewer's extras must be there ...
        Assert.Contains("Rows", cut.Markup);
        Assert.Contains("Columns", cut.Markup);

        // ... and the file's own properties exactly once, from the intrinsic parameters.
        Assert.Equal(1, CountOccurrences(cut.Markup, "data.csv"));
        Assert.Equal(1, CountOccurrences(cut.Markup, "text/csv"));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        for (var i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
            count++;
        return count;
    }
}
