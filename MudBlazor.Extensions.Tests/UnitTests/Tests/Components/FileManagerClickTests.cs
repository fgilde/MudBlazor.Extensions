using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// Covers what happens when a user actually clicks, rather than what happens when the component's own
/// navigation method is called.
/// </summary>
/// <remarks>
/// This distinction is the whole point of this file. A click goes through
/// <see cref="MudExTreeViewBase{TItem}.NodeClick"/>, which only assigns the selection when
/// <see cref="MudExTreeViewBase{TItem}.IsAllowedToSelect"/> agrees - and that refuses a node with children
/// unless <c>AllowSelectionOfNonEmptyNodes</c> is set. A file manager whose folders are not selectable does
/// nothing at all when a folder is clicked, and no test that calls the navigation method directly can see it.
/// </remarks>
public class FileManagerClickTests
{
    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    private static MudExInMemoryFileStructureManager CreateStructure()
    {
        var manager = new MudExInMemoryFileStructureManager
        {
            Capabilities = MudExFileManagerCapabilities.Read | MudExFileManagerCapabilities.Download
        };

        manager.AddFile("documents/report.md", 12);
        manager.AddFile("documents/invoices/2026-01.csv", 34);
        manager.AddFile("readme.md", 78);
        return manager;
    }

    private static IRenderedComponent<MudExFileManager> RenderManager(TestContext context, MudExFileManagerPanels panels)
        => context.Render<MudExFileManager>(parameters => parameters
            .Add(c => c.Manager, CreateStructure())
            .Add(c => c.Panels, panels));

    /// <summary>Every tree view the panels rendered, inner instances included.</summary>
    private static IReadOnlyList<MudExTreeViewBase<MudExFileStructureNode>> Views(IRenderedComponent<MudExFileManager> cut)
        => cut.FindComponents<MudExTreeView<MudExFileStructureNode>>()
            .Select(v => (MudExTreeViewBase<MudExFileStructureNode>)v.Instance)
            .ToList();

    [Fact]
    public async Task ClickingAFolderInAnyPanelNavigatesIntoIt()
    {
        await using var context = CreateContext();
        var cut = RenderManager(context, MudExFileManagerPanels.Tree | MudExFileManagerPanels.Files);

        var documents = cut.Instance.RootItems.First(n => n.Name == "documents");
        var views = Views(cut);
        Assert.NotEmpty(views);

        foreach (var view in views)
        {
            // Reset, so each panel is proven on its own rather than riding on the previous one.
            await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(null));
            Assert.Null(cut.Instance.CurrentDirectory);

            await cut.InvokeAsync(() => view.NodeClick(documents));

            Assert.Same(documents, cut.Instance.CurrentDirectory);
        }
    }

    [Fact]
    public async Task ClickingAFolderMakesItsChildrenAvailable()
    {
        await using var context = CreateContext();
        var cut = RenderManager(context, MudExFileManagerPanels.Tree | MudExFileManagerPanels.Files);

        var documents = cut.Instance.RootItems.First(n => n.Name == "documents");
        var view = Views(cut).First();

        await cut.InvokeAsync(() => view.NodeClick(documents));

        Assert.NotNull(documents.Children);
        Assert.Contains(documents.Children, c => c.Name == "report.md");
        Assert.Contains(documents.Children, c => c.Name == "invoices");
    }

    [Fact]
    public async Task AFolderIsSelectableInEveryPanel()
    {
        await using var context = CreateContext();
        var cut = RenderManager(context, MudExFileManagerPanels.All);

        var documents = cut.Instance.RootItems.First(n => n.Name == "documents");

        foreach (var view in Views(cut))
            Assert.True(view.IsAllowedToSelect(documents),
                $"{view.GetType().Name} refuses to select a folder, so clicking one does nothing");
    }

    [Fact]
    public async Task ClickingThroughTwoLevelsKeepsEveryPanelOnTheSameNode()
    {
        await using var context = CreateContext();
        var cut = RenderManager(context, MudExFileManagerPanels.All);

        var documents = cut.Instance.RootItems.First(n => n.Name == "documents");
        var tree = Views(cut).First();

        await cut.InvokeAsync(() => tree.NodeClick(documents));
        var invoices = documents.Children.First(c => c.Name == "invoices");
        await cut.InvokeAsync(() => tree.NodeClick(invoices));

        Assert.Same(invoices, cut.Instance.CurrentDirectory);

        // The address bar is a separate instance; if it lags, it lags here.
        foreach (var view in Views(cut))
            Assert.Same(invoices, view.SelectedNode);
    }

    [Fact]
    public async Task TheAddressBarShowsThePathToTheCurrentDirectory()
    {
        await using var context = CreateContext();
        var cut = RenderManager(context, MudExFileManagerPanels.All);

        var documents = cut.Instance.RootItems.First(n => n.Name == "documents");
        await cut.InvokeAsync(() => Views(cut).First().NodeClick(documents));
        var invoices = documents.Children.First(c => c.Name == "invoices");
        await cut.InvokeAsync(() => Views(cut).First().NodeClick(invoices));

        var breadcrumb = cut.FindComponents<MudExTreeViewBreadcrumb<MudExFileStructureNode>>()
            .Select(c => c.Instance)
            .FirstOrDefault();
        Assert.NotNull(breadcrumb);

        // Both levels have to be crumbs, in order - that is what "in sync with the path" means.
        var path = breadcrumb.Path().ToList();
        Assert.Equal(new[] { "documents", "invoices" }, path.Select(n => n.Name));
    }

    [Fact]
    public async Task ClickingAFileSelectsItWithoutLeavingTheFolder()
    {
        await using var context = CreateContext();
        var cut = RenderManager(context, MudExFileManagerPanels.Tree | MudExFileManagerPanels.Files);

        var documents = cut.Instance.RootItems.First(n => n.Name == "documents");
        var view = Views(cut).First();
        await cut.InvokeAsync(() => view.NodeClick(documents));

        var report = documents.Children.First(c => c.Name == "report.md");
        await cut.InvokeAsync(() => view.NodeClick(report));

        Assert.Same(report, cut.Instance.SelectedNode);
        Assert.Same(documents, cut.Instance.CurrentDirectory);
    }

    [Fact]
    public async Task OpeningAFileRaisesOnFileOpened()
    {
        await using var context = CreateContext();
        MudExFileStructureNode opened = null;

        var cut = context.Render<MudExFileManager>(parameters => parameters
            .Add(c => c.Manager, CreateStructure())
            .Add(c => c.Panels, MudExFileManagerPanels.Tree | MudExFileManagerPanels.Files)
            .Add(c => c.OnFileOpened, node => opened = node));

        var readme = cut.Instance.RootItems.First(n => n.Name == "readme.md");
        await cut.InvokeAsync(() => cut.Instance.OpenAsync(readme));

        Assert.Same(readme, opened);
    }

    [Fact]
    public async Task OpeningAFolderEntersItWithoutRaisingOnFileOpened()
    {
        await using var context = CreateContext();
        var raised = false;

        var cut = context.Render<MudExFileManager>(parameters => parameters
            .Add(c => c.Manager, CreateStructure())
            .Add(c => c.Panels, MudExFileManagerPanels.Tree | MudExFileManagerPanels.Files)
            .Add(c => c.OnFileOpened, _ => raised = true));

        var documents = cut.Instance.RootItems.First(n => n.Name == "documents");
        await cut.InvokeAsync(() => cut.Instance.OpenAsync(documents));

        Assert.Same(documents, cut.Instance.CurrentDirectory);
        Assert.False(raised);
    }
}
