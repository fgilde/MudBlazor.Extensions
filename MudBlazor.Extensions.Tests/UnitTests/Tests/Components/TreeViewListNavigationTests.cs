using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// Covers the back and home links of <see cref="MudExTreeViewList{T}"/>. They walk up two levels from the
/// selection, so a node without a parent - anything at root level - used to throw there.
/// </summary>
public class TreeViewListNavigationTests
{
    /// <summary>
    /// Exposes the back target, which is protected. Rendering does not reach it reliably: the navigation bar
    /// only appears for some level and selection combinations, and the crash needed the level to change while
    /// the bar was rendering, which a lazy load finishing in between is enough for.
    /// </summary>
    private sealed class ProbeList : MudExTreeViewList<MudExFileStructureNode>
    {
        public MudExFileStructureNode BackTarget() => GetBackNodeTarget().Context?.Value;
    }

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
            Capabilities = MudExFileManagerCapabilities.Read
        };
        manager.AddFile("documents/report.md", 12);
        manager.AddFile("documents/invoices/2026-01.csv", 34);
        manager.AddFile("source/Program.cs", 56);
        manager.AddFile("readme.md", 78);
        return manager;
    }

    // Rendered, not just constructed: a free instance has no render handle, and assigning the selection
    // already calls Update().
    private static ProbeList Probe(TestContext context, IReadOnlyCollection<MudExFileStructureNode> items,
        MudExFileStructureNode selected = null, Func<MudExFileStructureNode, bool> filter = null)
        => context.Render<ProbeList>(parameters => parameters
            .Add(c => c.Items, items)
            .Add(c => c.TextFunc, n => n.Name)
            .Add(c => c.AllowSelectionOfNonEmptyNodes, true)
            .Add(c => c.ItemFilter, filter)
            .Add(c => c.SelectedNode, selected)).Instance;

    [Theory]
    [InlineData("documents")]
    [InlineData("readme.md")]
    public async Task TheBackTargetOfARootLevelNodeIsNotAnError(string name)
    {
        await using var context = CreateContext();
        var root = await CreateStructure().GetRootAsync();

        Assert.Null(Probe(context, root, root.First(n => n.Name == name)).BackTarget());
    }

    [Fact]
    public async Task TheBackTargetWithoutASelectionIsNotAnError()
    {
        await using var context = CreateContext();
        var root = await CreateStructure().GetRootAsync();

        Assert.Null(Probe(context, root).BackTarget());
    }

    [Fact]
    public async Task TheBackTargetOfANestedFileIsItsGrandparent()
    {
        await using var context = CreateContext();
        var root = await CreateStructure().GetRootAsync();
        var documents = root.First(n => n.Name == "documents");
        await documents.LoadChildren();

        var invoices = documents.Children.First(c => c.Name == "invoices");
        await invoices.LoadChildren();

        // A file has no children, so the level shown is its parent's and back goes one further up.
        Assert.Same(documents, Probe(context, root, invoices.Children.First()).BackTarget());
    }

    [Fact]
    public async Task SwitchingBetweenRootFoldersDoesNotThrow()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExFileManager>(parameters => parameters
            .Add(c => c.Manager, CreateStructure())
            .Add(c => c.Panels, MudExFileManagerPanels.Tree | MudExFileManagerPanels.Files)
            .Add(c => c.TreePanelViewMode, TreeViewMode.List));

        foreach (var name in new[] { "documents", "source", "documents" })
        {
            var folder = cut.Instance.RootItems.First(n => n.Name == name);
            await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(folder));
            Assert.Same(folder, cut.Instance.CurrentDirectory);
        }

        // A file is selected, not entered - the folder it lives in stays open.
        var readme = cut.Instance.RootItems.First(n => n.Name == "readme.md");
        await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(readme));
        Assert.Same(readme, cut.Instance.SelectedNode);
    }

    [Fact]
    public async Task GoingDownAndBackUpToTheRootDoesNotThrow()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExFileManager>(parameters => parameters
            .Add(c => c.Manager, CreateStructure())
            .Add(c => c.Panels, MudExFileManagerPanels.Tree | MudExFileManagerPanels.Files));

        var documents = cut.Instance.RootItems.First(n => n.Name == "documents");
        await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(documents));
        var invoices = documents.Children.First(c => c.Name == "invoices");
        await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(invoices));

        await cut.InvokeAsync(() => cut.Instance.NavigateUpAsync());
        await cut.InvokeAsync(() => cut.Instance.NavigateUpAsync());

        Assert.Null(cut.Instance.CurrentDirectory);
    }

    [Fact]
    public async Task AFolderWhoseOnlyEntryIsFilteredOutDoesNotThrow()
    {
        await using var context = CreateContext();
        var structure = new MudExInMemoryFileStructureManager
        {
            Capabilities = MudExFileManagerCapabilities.Read
        };
        structure.AddFile("archive/.keep", 0);

        var root = await structure.GetRootAsync();
        var archive = root.First(n => n.Name == "archive");
        await archive.LoadChildren();

        // Folders only, so this level renders empty and the back target has nothing to walk up from.
        Assert.Null(Probe(context, root, archive, n => n.IsDirectory).BackTarget());
    }
}
