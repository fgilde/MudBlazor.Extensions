using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public sealed class MudExOrgChartTests : IAsyncDisposable
{
    private sealed record Node(int Id, int? Parent, string Name);

    private static readonly List<Node> People =
    [
        new(1, null, "Ceo"),
        new(2, 1, "Cto"),
        new(3, 2, "Dev"),
        new(4, 1, "Cfo"),
        new(5, 99, "Orphan"),
        new(6, 7, "LoopA"),
        new(7, 6, "LoopB")
    ];

    private readonly TestContext _context = new();
    private readonly List<OrgChartReparentArgs<Node>> _reparented = [];

    public MudExOrgChartTests()
    {
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public ValueTask DisposeAsync() => _context.DisposeAsync();

    private IRenderedComponent<MudExOrgChart<Node>> Open(int? expandedDepth = null, string search = null, Func<Node, Node, bool> canReparent = null) =>
        _context.Render<MudExOrgChart<Node>>(p => p
            .Add(c => c.Items, People)
            .Add(c => c.IdFunc, n => n.Id)
            .Add(c => c.ParentIdFunc, n => n.Parent)
            .Add(c => c.TextFunc, n => n.Name)
            .Add(c => c.InitiallyExpandedDepth, expandedDepth)
            .Add(c => c.SearchText, search)
            .Add(c => c.AllowReparent, true)
            .Add(c => c.CanReparent, canReparent)
            .Add(c => c.OnReparent, a => _reparented.Add(a)));

    private static List<string> Cards(IRenderedComponent<MudExOrgChart<Node>> chart) =>
        chart.FindAll(".mud-ex-org-chart-node .mud-ex-org-chart-text").Select(e => e.TextContent).ToList();

    private static AngleSharp.Dom.IElement Card(IRenderedComponent<MudExOrgChart<Node>> chart, string name) =>
        chart.FindAll(".mud-ex-org-chart-node").Single(e => e.QuerySelector(".mud-ex-org-chart-text")?.TextContent == name);

    [Fact]
    public void MissingParentsAndCyclesBecomeRootsAndEveryItemShowsOnce()
    {
        var chart = Open();

        Assert.Equal(["Ceo", "Cto", "Dev", "Cfo", "Orphan", "LoopA", "LoopB"], Cards(chart));
        Assert.Equal(3, chart.FindAll(".mud-ex-org-chart-tree > li").Count);
    }

    [Fact]
    public void ChildrenFuncBuildsTheTreeFromTheRoots()
    {
        var chart = _context.Render<MudExOrgChart<string>>(p => p
            .Add(c => c.Items, ["root"])
            .Add(c => c.ChildrenFunc, s => s.Length < 6 ? [s + "a", s + "b"] : []));

        Assert.Equal(7, chart.FindAll(".mud-ex-org-chart-node").Count);
    }

    [Fact]
    public async Task InitiallyExpandedDepthCollapsesDeeperNodesAndToggleOpensThem()
    {
        var chart = Open(expandedDepth: 1);

        Assert.DoesNotContain("Dev", Cards(chart));
        Card(chart, "Cto").QuerySelector(".mud-ex-org-chart-toggle")!.Click();
        Assert.Contains("Dev", Cards(chart));

        await chart.InvokeAsync(chart.Instance.CollapseAll);
        Assert.Equal(["Ceo", "Cto", "Cfo", "Orphan", "LoopA", "LoopB"], Cards(chart));
    }

    [Fact]
    public void SearchHighlightsMatchesAndOpensTheWayToTheFirst()
    {
        var chart = Open(expandedDepth: 0, search: "dev");

        Assert.Contains("mud-ex-org-chart-current", Card(chart, "Dev").ClassName);
        Assert.Single(chart.FindAll(".mud-ex-org-chart-match"));
    }

    [Fact]
    public async Task DropOntoAnotherNodeReparentsButNeverIntoItsOwnSubtree()
    {
        var chart = Open();

        await Card(chart, "Cto").TriggerEventAsync("ondragstart", new DragEventArgs());
        await Card(chart, "Dev").TriggerEventAsync("ondrop", new DragEventArgs());
        Assert.Empty(_reparented);

        await Card(chart, "Cto").TriggerEventAsync("ondragstart", new DragEventArgs());
        await Card(chart, "Cfo").TriggerEventAsync("ondrop", new DragEventArgs());
        var args = Assert.Single(_reparented);
        Assert.Equal(("Cto", "Ceo", "Cfo"), (args.Item.Name, args.OldParent.Name, args.NewParent.Name));
    }

    [Fact]
    public async Task CanReparentVetoesADrop()
    {
        var chart = Open(canReparent: (_, parent) => parent?.Name != "Cfo");

        await Card(chart, "Dev").TriggerEventAsync("ondragstart", new DragEventArgs());
        await Card(chart, "Cfo").TriggerEventAsync("ondrop", new DragEventArgs());

        Assert.Empty(_reparented);
    }

    [Fact]
    public async Task ClickSelectsTheNode()
    {
        Node selected = null;
        var chart = _context.Render<MudExOrgChart<Node>>(p => p
            .Add(c => c.Items, People)
            .Add(c => c.IdFunc, n => n.Id)
            .Add(c => c.ParentIdFunc, n => n.Parent)
            .Add(c => c.TextFunc, n => n.Name)
            .Add(c => c.SelectedItemChanged, n => selected = n));

        await Card(chart, "Cfo").ClickAsync(new MouseEventArgs());

        Assert.Equal("Cfo", selected?.Name);
        Assert.Contains("mud-ex-org-chart-selected", Card(chart, "Cfo").ClassName);
    }
}
