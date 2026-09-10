using Bunit;
using MudBlazor;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// Covers the explorer behaviour of <see cref="MudExFileManager"/>: the structure panel, the file area and the
/// address bar are three views over one selection, so navigating in any of them has to move all of them - and
/// entering a lazily loaded folder has to wait for its contents before the level is rendered.
/// </summary>
/// <remarks>
/// These are the assertions that would have caught the panels being out of sync: they exercise the real
/// component, not the intent.
/// </remarks>
public class FileManagerNavigationTests
{
    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    /// <summary>A lazily loaded structure - the same shape the in-memory provider produces.</summary>
    private static MudExInMemoryFileStructureManager CreateStructure()
    {
        var manager = new MudExInMemoryFileStructureManager
        {
            Capabilities = MudExFileManagerCapabilities.Read | MudExFileManagerCapabilities.Download
        };

        manager.AddFile("documents/report.md", 12);
        manager.AddFile("documents/invoices/2026-01.csv", 34);
        manager.AddFile("source/Program.cs", 56);
        manager.AddFile("readme.md", 78);
        return manager;
    }

    private static IRenderedComponent<MudExFileManager> RenderManager(
        TestContext context, MudExInMemoryFileStructureManager structure, MudExFileManagerPanels panels = MudExFileManagerPanels.All)
        => context.Render<MudExFileManager>(parameters => parameters
            .Add(c => c.Manager, structure)
            .Add(c => c.Panels, panels)
            .Add(c => c.ShowBreadcrumb, true));

    [Fact]
    public async Task SelectingADirectoryMakesItTheCurrentDirectory()
    {
        await using var context = CreateContext();
        var cut = RenderManager(context, CreateStructure());

        var documents = cut.Instance.RootItems.First(n => n.Name == "documents");

        await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(documents));

        Assert.Same(documents, cut.Instance.CurrentDirectory);
        // Entering a folder clears the selection: what was selected belonged to the level just left.
        Assert.Empty(cut.Instance.SelectedNodes);
    }

    [Fact]
    public async Task EnteringALazyDirectoryLoadsItsChildrenBeforeReturning()
    {
        await using var context = CreateContext();
        var cut = RenderManager(context, CreateStructure());

        var documents = cut.Instance.RootItems.First(n => n.Name == "documents");

        await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(documents));

        // The whole point: without awaiting the load the file area would render an empty level and nothing
        // would bring it back.
        Assert.NotNull(documents.Children);
        Assert.Contains(documents.Children, c => c.Name == "report.md");
        Assert.Contains(documents.Children, c => c.Name == "invoices");
    }

    [Fact]
    public async Task SelectingAFileKeepsItsFolderAsTheCurrentDirectory()
    {
        await using var context = CreateContext();
        // No preview panel: selecting a file would mount the real viewer for it, and the viewers need
        // javascript this test context does not have. The assertion here is about where the manager is.
        var cut = RenderManager(context, CreateStructure(),
            MudExFileManagerPanels.Tree | MudExFileManagerPanels.Files);

        var documents = cut.Instance.RootItems.First(n => n.Name == "documents");
        await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(documents));
        var report = documents.Children.First(c => c.Name == "report.md");

        await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(report));

        Assert.Same(report, cut.Instance.SelectedNode);
        // An explorer does not leave the folder when a file in it is clicked.
        Assert.Same(documents, cut.Instance.CurrentDirectory);
    }

    [Fact]
    public async Task EveryPanelRendersTheSelectionOfTheOthers()
    {
        await using var context = CreateContext();
        var cut = RenderManager(context, CreateStructure());

        var documents = cut.Instance.RootItems.First(n => n.Name == "documents");
        await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(documents));

        // The structure panel and the address bar are tree views over the place, so both must report it. The
        // file area is a MudExFileGrid and holds the selection instead.
        var views = cut.FindComponents<MudExTreeView<MudExFileStructureNode>>();
        Assert.NotEmpty(views);

        foreach (var view in views)
            Assert.Same(documents, view.Instance.SelectedNode);
    }

    [Fact]
    public async Task GoingUpReturnsToTheParentDirectory()
    {
        await using var context = CreateContext();
        var cut = RenderManager(context, CreateStructure());

        var documents = cut.Instance.RootItems.First(n => n.Name == "documents");
        await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(documents));
        var invoices = documents.Children.First(c => c.Name == "invoices");
        await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(invoices));

        Assert.Same(invoices, cut.Instance.CurrentDirectory);

        await cut.InvokeAsync(() => cut.Instance.NavigateUpAsync());

        Assert.Same(documents, cut.Instance.CurrentDirectory);
    }

    [Fact]
    public async Task WithoutAFileAreaTheTreeStillDrivesTheSelection()
    {
        await using var context = CreateContext();
        var cut = RenderManager(context, CreateStructure(), MudExFileManagerPanels.Tree);

        var readme = cut.Instance.RootItems.First(n => n.Name == "readme.md");
        await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(readme));

        // Tree | Preview is a valid panel set, and then the structure panel is the only thing that selects.
        Assert.Same(readme, cut.Instance.SelectedNode);
        Assert.Null(cut.Instance.CurrentDirectory);
    }
}
