using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// Covers <see cref="MudExFileGrid"/>: ordering, the three selection gestures, the keyboard, inline rename
/// and what it reports for drag and drop.
/// </summary>
public class FileGridTests
{
    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    private static MudExFileStructureNode Dir(string name) => new() { Name = name, FullPath = name, IsDirectory = true };

    private static MudExFileStructureNode File(string name, long size = 10, string type = "text/plain")
        => new() { Name = name, FullPath = name, Size = size, ContentType = type };

    private static List<MudExFileStructureNode> Sample() => new()
    {
        File("beta.txt", 30),
        Dir("zeta"),
        File("alpha.txt", 10),
        Dir("alpha-dir"),
        File("gamma.log", 20, "text/x-log")
    };

    private static IRenderedComponent<MudExFileGrid> Render(TestContext context,
        IReadOnlyCollection<MudExFileStructureNode> items, Action<ComponentParameterCollectionBuilder<MudExFileGrid>> extra = null)
        => context.Render<MudExFileGrid>(parameters =>
        {
            parameters.Add(c => c.Items, items);
            extra?.Invoke(parameters);
        });

    [Fact]
    public async Task DirectoriesComeFirstAndNamesAreSortedNaturally()
    {
        await using var context = CreateContext();
        var cut = Render(context, Sample());

        Assert.Equal(
            new[] { "alpha-dir", "zeta", "alpha.txt", "beta.txt", "gamma.log" },
            cut.Instance.OrderedItems.Select(n => n.Name));
    }

    [Fact]
    public async Task SortingBySizeKeepsDirectoriesOnTop()
    {
        await using var context = CreateContext();
        var cut = Render(context, Sample(), p => p
            .Add(c => c.SortBy, MudExFileGridSort.Size)
            .Add(c => c.SortDescending, true));

        var names = cut.Instance.OrderedItems.Select(n => n.Name).ToList();

        Assert.Equal(new[] { "alpha-dir", "zeta" }, names.Take(2).OrderBy(n => n));
        // The files, largest first.
        Assert.Equal(new[] { "beta.txt", "gamma.log", "alpha.txt" }, names.Skip(2));
    }

    [Fact]
    public async Task DirectoriesFirstCanBeTurnedOff()
    {
        await using var context = CreateContext();
        var cut = Render(context, Sample(), p => p.Add(c => c.DirectoriesFirst, false));

        Assert.Equal("alpha-dir", cut.Instance.OrderedItems[0].Name);
        Assert.Equal("alpha.txt", cut.Instance.OrderedItems[1].Name);
    }

    [Fact]
    public async Task APlainClickReplacesTheSelection()
    {
        await using var context = CreateContext();
        var items = Sample();
        var cut = Render(context, items);

        cut.FindAll(".mud-ex-file-grid-item")[0].Click();
        Assert.Single(cut.Instance.SelectedItems);

        cut.FindAll(".mud-ex-file-grid-item")[1].Click();
        Assert.Single(cut.Instance.SelectedItems);
    }

    [Fact]
    public async Task CtrlClickTogglesAnEntry()
    {
        await using var context = CreateContext();
        var cut = Render(context, Sample());

        var tiles = cut.FindAll(".mud-ex-file-grid-item");
        tiles[0].Click();
        cut.FindAll(".mud-ex-file-grid-item")[1].Click(new MouseEventArgs { CtrlKey = true });

        Assert.Equal(2, cut.Instance.SelectedItems.Count);

        // The same entry again removes it.
        cut.FindAll(".mud-ex-file-grid-item")[1].Click(new MouseEventArgs { CtrlKey = true });
        Assert.Single(cut.Instance.SelectedItems);
    }

    [Fact]
    public async Task ShiftClickTakesTheRange()
    {
        await using var context = CreateContext();
        var cut = Render(context, Sample());

        cut.FindAll(".mud-ex-file-grid-item")[0].Click();
        cut.FindAll(".mud-ex-file-grid-item")[3].Click(new MouseEventArgs { ShiftKey = true });

        Assert.Equal(4, cut.Instance.SelectedItems.Count);
        Assert.Equal(
            cut.Instance.OrderedItems.Take(4).Select(n => n.Name).OrderBy(n => n),
            cut.Instance.SelectedItems.Select(n => n.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task WithoutMultiSelectCtrlAndShiftStillSelectOne()
    {
        await using var context = CreateContext();
        var cut = Render(context, Sample(), p => p.Add(c => c.AllowMultiSelect, false));

        cut.FindAll(".mud-ex-file-grid-item")[0].Click();
        cut.FindAll(".mud-ex-file-grid-item")[2].Click(new MouseEventArgs { CtrlKey = true });

        Assert.Single(cut.Instance.SelectedItems);
    }

    [Fact]
    public async Task SelectAllAndClearWork()
    {
        await using var context = CreateContext();
        var items = Sample();
        var cut = Render(context, items);

        await cut.InvokeAsync(() => cut.Instance.SelectAllAsync());
        Assert.Equal(items.Count, cut.Instance.SelectedItems.Count);

        await cut.InvokeAsync(() => cut.Instance.ClearSelectionAsync());
        Assert.Empty(cut.Instance.SelectedItems);
    }

    [Fact]
    public async Task ArrowKeysMoveTheSelectionAndEnterOpens()
    {
        await using var context = CreateContext();
        MudExFileStructureNode opened = null;
        var cut = Render(context, Sample(), p => p.Add(c => c.OnOpened, n => opened = n));

        var grid = cut.Find(".mud-ex-file-grid");
        grid.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Same(cut.Instance.OrderedItems[0], cut.Instance.SelectedItems.Single());

        grid.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Same(cut.Instance.OrderedItems[1], cut.Instance.SelectedItems.Single());

        grid.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Same(cut.Instance.OrderedItems[1], opened);
    }

    [Fact]
    public async Task DeleteAsksForTheWholeSelection()
    {
        await using var context = CreateContext();
        IReadOnlyCollection<MudExFileStructureNode> asked = null;
        var cut = Render(context, Sample(), p => p.Add(c => c.OnDeleteRequested, nodes => asked = nodes));

        await cut.InvokeAsync(() => cut.Instance.SelectAllAsync());
        cut.Find(".mud-ex-file-grid").KeyDown(new KeyboardEventArgs { Key = "Delete" });

        Assert.NotNull(asked);
        Assert.Equal(cut.Instance.OrderedItems.Count, asked.Count);
    }

    [Fact]
    public async Task EscapeClearsTheSelection()
    {
        await using var context = CreateContext();
        var cut = Render(context, Sample());

        await cut.InvokeAsync(() => cut.Instance.SelectAllAsync());
        cut.Find(".mud-ex-file-grid").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(cut.Instance.SelectedItems);
    }

    [Fact]
    public async Task F2StartsAnInlineRenameAndEnterReportsIt()
    {
        await using var context = CreateContext();
        MudExFileGridRenameRequest request = null;
        var cut = Render(context, Sample(), p => p.Add(c => c.OnRenamed, r => request = r));

        var first = cut.Instance.OrderedItems[0];
        await cut.InvokeAsync(() => cut.Instance.SelectAsync(new[] { first }));
        cut.Find(".mud-ex-file-grid").KeyDown(new KeyboardEventArgs { Key = "F2" });

        Assert.True(cut.Instance.IsRenaming);

        var input = cut.Find(".mud-ex-file-grid input");
        input.Input("renamed");
        input.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.NotNull(request);
        Assert.Same(first, request.Node);
        Assert.Equal("renamed", request.NewName);
        Assert.False(cut.Instance.IsRenaming);
    }

    [Fact]
    public async Task EscapeCancelsAnInlineRename()
    {
        await using var context = CreateContext();
        MudExFileGridRenameRequest request = null;
        var cut = Render(context, Sample(), p => p.Add(c => c.OnRenamed, r => request = r));

        var first = cut.Instance.OrderedItems[0];
        await cut.InvokeAsync(() => cut.Instance.StartRename(first));

        var input = cut.Find(".mud-ex-file-grid input");
        input.Input("nope");
        input.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(request);
        Assert.False(cut.Instance.IsRenaming);
    }

    [Fact]
    public async Task RenamingToTheSameNameReportsNothing()
    {
        await using var context = CreateContext();
        MudExFileGridRenameRequest request = null;
        var cut = Render(context, Sample(), p => p.Add(c => c.OnRenamed, r => request = r));

        var first = cut.Instance.OrderedItems[0];
        await cut.InvokeAsync(() => cut.Instance.StartRename(first));

        var input = cut.Find(".mud-ex-file-grid input");
        input.Input(first.Name);
        input.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Null(request);
    }

    [Fact]
    public async Task TheRubberBandSelectsWhatItTouched()
    {
        await using var context = CreateContext();
        var items = Sample();
        var cut = Render(context, items);

        var keys = new[] { items[0].FullPath, items[2].FullPath };
        await cut.InvokeAsync(() => cut.Instance.RubberBandSelected(keys, false));

        Assert.Equal(2, cut.Instance.SelectedItems.Count);
        Assert.Contains(items[0], cut.Instance.SelectedItems);
        Assert.Contains(items[2], cut.Instance.SelectedItems);
    }

    [Fact]
    public async Task TheRubberBandCanAddToTheSelection()
    {
        await using var context = CreateContext();
        var items = Sample();
        var cut = Render(context, items);

        await cut.InvokeAsync(() => cut.Instance.SelectAsync(new[] { items[1] }));
        await cut.InvokeAsync(() => cut.Instance.RubberBandSelected(new[] { items[0].FullPath }, true));

        Assert.Equal(2, cut.Instance.SelectedItems.Count);
    }

    [Fact]
    public async Task DetailsViewRendersTheColumns()
    {
        await using var context = CreateContext();
        var cut = Render(context, Sample(), p => p.Add(c => c.View, MudExFileGridView.Details));

        Assert.Contains("Name", cut.Markup);
        Assert.Contains("Size", cut.Markup);
        Assert.Contains("Modified", cut.Markup);
        Assert.Equal(Sample().Count, cut.FindAll(".mud-ex-file-grid-row").Count);
    }

    [Fact]
    public async Task AnEmptyLevelSaysSo()
    {
        await using var context = CreateContext();
        var cut = Render(context, Array.Empty<MudExFileStructureNode>());

        Assert.NotEmpty(cut.FindAll(".mud-ex-file-grid-empty"));
    }
}
