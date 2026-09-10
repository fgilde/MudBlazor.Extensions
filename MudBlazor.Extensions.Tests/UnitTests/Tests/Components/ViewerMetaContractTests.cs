using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// Pins the contract between a viewer's <see cref="IMudExFileDisplay.FileMetaInformationAsync"/> and what
/// finally ends up on screen: whatever a viewer returns is shown, and it is shown even though the viewer can
/// only answer once it has parsed the file.
/// </summary>
/// <remarks>
/// A viewer is used here rather than a real csv or pdf, because the contract is the dictionary - the parsing
/// behind it is the viewer's business. The second test is the one that matters: a viewer that has nothing to
/// say on the first render and real values afterwards is the normal case (an excel viewer counts rows, a pdf
/// counts pages), and reporting only once would drop exactly those values.
/// </remarks>
public class ViewerMetaContractTests
{
    /// <summary>A content type no shipped viewer claims, so this test's viewer is the one that gets picked.</summary>
    private const string TestContentType = "application/x-mudex-viewer-meta-test";

    /// <summary>
    /// Stands in for any viewer. Reports nothing until <see cref="Parsed"/> is set, which is what a real
    /// viewer does: it knows its row or page count only after reading the file.
    /// </summary>
    private sealed class ProbeViewer : ComponentBase, IMudExFileDisplay
    {
        public static bool Parsed;

        public string Name => nameof(ProbeViewer);

        [Parameter] public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

        public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
            => Task.FromResult(fileDisplayInfos?.ContentType == TestContentType);

        public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
            => Task.FromResult<IDictionary<string, object>>(Parsed
                ? new Dictionary<string, object> { { "Rows", 42 }, { "Columns", 7 } }
                : new Dictionary<string, object>());
    }

    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.Services.AddScoped<IMudExFileDisplay, ProbeViewer>();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    private static Stream Content() => new MemoryStream(Encoding.UTF8.GetBytes("a;b\n1;2\n"));

    [Fact]
    public async Task WhateverTheViewerReportsIsHandedOver()
    {
        ProbeViewer.Parsed = true;
        await using var context = CreateContext();
        IDictionary<string, object> reported = null;

        context.Render<MudExFileDisplay>(parameters => parameters
            .Add(c => c.FileName, "probe.dat")
            .Add(c => c.ContentType, TestContentType)
            .Add(c => c.ContentStream, Content())
            .Add(c => c.FileMetaInformationChanged, meta => reported = meta));

        Assert.NotNull(reported);
        Assert.Equal(42, Assert.Contains("Rows", reported));
        Assert.Equal(7, Assert.Contains("Columns", reported));
    }

    [Fact]
    public async Task ValuesThatOnlyExistAfterParsingStillArrive()
    {
        // Nothing to report on the first render - the file has not been read yet.
        ProbeViewer.Parsed = false;
        await using var context = CreateContext();
        IDictionary<string, object> reported = null;

        var cut = context.Render<MudExFileDisplay>(parameters => parameters
            .Add(c => c.FileName, "probe.dat")
            .Add(c => c.ContentType, TestContentType)
            .Add(c => c.ContentStream, Content())
            .Add(c => c.FileMetaInformationChanged, meta => reported = meta));

        Assert.DoesNotContain("Rows", reported ?? new Dictionary<string, object>());

        // The viewer finished parsing. A single report would have missed this - and that is exactly what made
        // a csv show no row or column count.
        ProbeViewer.Parsed = true;
        cut.Render();

        Assert.NotNull(reported);
        Assert.Equal(42, Assert.Contains("Rows", reported));
        Assert.Equal(7, Assert.Contains("Columns", reported));
    }

    [Fact]
    public async Task TheMetaViewRendersWhatTheViewerReported()
    {
        ProbeViewer.Parsed = true;
        await using var context = CreateContext();

        var cut = context.Render<MudExFileDisplay>(parameters => parameters
            .Add(c => c.FileName, "probe.dat")
            .Add(c => c.ContentType, TestContentType)
            .Add(c => c.ContentStream, Content())
            .Add(c => c.ShowFileMeta, true)
            .Add(c => c.FileMetaPlacement, MudExFileMetaPlacement.Right));

        // Rendered, not just handed over: the inline meta view is fed from the same data.
        Assert.Contains("Rows", cut.Markup);
        Assert.Contains("42", cut.Markup);
        Assert.Contains("Columns", cut.Markup);
        Assert.Contains("7", cut.Markup);
    }

    /// <summary>
    /// Reports nothing until it has been asked <see cref="AnswersFromCall"/> times - which stands for a viewer
    /// whose reference is not assigned yet, or that is still reading the file.
    /// </summary>
    private sealed class LateViewer : ComponentBase, IMudExFileDisplay
    {
        public const int AnswersFromCall = 3;
        public static int Calls;

        public string Name => nameof(LateViewer);

        [Parameter] public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

        public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
            => Task.FromResult(fileDisplayInfos?.ContentType == LateContentType);

        public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
            => Task.FromResult<IDictionary<string, object>>(++Calls >= AnswersFromCall
                ? new Dictionary<string, object> { { "BitRate", 320 } }
                : new Dictionary<string, object>());
    }

    private const string LateContentType = "application/x-mudex-late-meta-test";

    [Fact]
    public async Task MetadataThatArrivesLateStillShowsUpWithoutAnyFurtherInteraction()
    {
        LateViewer.Calls = 0;

        await using var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.Services.AddScoped<IMudExFileDisplay, LateViewer>();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        IDictionary<string, object> reported = null;
        var cut = context.Render<MudExFileDisplay>(parameters => parameters
            .Add(c => c.FileName, "song.mp3")
            .Add(c => c.ContentType, LateContentType)
            .Add(c => c.ContentStream, Content())
            .Add(c => c.ShowFileMeta, true)
            .Add(c => c.FileMetaInformationChanged, meta => reported = meta));

        // Nobody pressed anything: the component has to come back for the value on its own.
        Assert.NotNull(reported);
        Assert.Equal(320, Assert.Contains("BitRate", reported));

        // The meta view turns the key into a label, so the markup carries "Bit rate", not the key.
        Assert.Contains("Bit rate", cut.Markup);
        Assert.Contains("320", cut.Markup);
    }

    [Fact]
    public async Task TheFileManagerDetailsPanelGetsTheViewersValues()
    {
        ProbeViewer.Parsed = true;
        await using var context = CreateContext();

        var structure = new MudExInMemoryFileStructureManager
        {
            Capabilities = MudExFileManagerCapabilities.Read
        };
        structure.AddFile("probe.dat", 8);
        structure.SetContent("probe.dat", Encoding.UTF8.GetBytes("a;b\n1;2\n"));

        var cut = context.Render<MudExFileManager>(parameters => parameters
            .Add(c => c.Manager, structure)
            .Add(c => c.Panels, MudExFileManagerPanels.Files | MudExFileManagerPanels.Preview | MudExFileManagerPanels.Meta));

        var probe = cut.Instance.RootItems.First();
        // The provider reports no content type for this extension, so the viewer is selected by the one the
        // node carries - set it explicitly to the type this test's viewer claims.
        probe.ContentType = TestContentType;

        await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(probe));

        var meta = cut.Instance.SelectedFileMetaInformation;
        Assert.NotNull(meta);
        Assert.Equal(42, Assert.Contains("Rows", meta));
        Assert.Equal(7, Assert.Contains("Columns", meta));
    }
}
