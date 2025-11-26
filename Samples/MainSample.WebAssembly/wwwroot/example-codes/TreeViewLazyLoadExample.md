```razor
@using MudBlazor.Extensions.Core.Enums
@inherits ExampleBase

<MudText Typo="Typo.body2" Class="mb-3">
    @L["This example demonstrates async/lazy loading of child nodes. Expand the 'Async load sample' node to see the loading behavior."]
</MudText>
<MudToolBar>
    <MudSpacer/>
    <MudExEnumSelect Variant="Variant.Outlined" Class="mb-2 mt-4" TEnum="TreeViewMode" @bind-Value="@_treeViewMode" Label="TreeViewMode"/>
</MudToolBar>
<MudPaper Style=" width: 100%; min-height:300px; max-height: 450px; overflow: auto;" Square="true">
    <MudExTreeView @ref="ComponentRef"
                   Virtualize="false"
                   ViewMode="@_treeViewMode"
                   Items="@Entries">

    </MudExTreeView>
</MudPaper>
@code {
    private TreeViewMode _treeViewMode = TreeViewMode.Default;
    public HashSet<SampleTreeItem> Entries { get; set; }

    protected override void OnInitialized()
    {
        Entries = new HashSet<SampleTreeItem>
        {
            new SampleTreeItem("Static Items")
            {
                Children = new HashSet<SampleTreeItem>
                {
                    new SampleTreeItem("Item 1"),
                    new SampleTreeItem("Item 2"),
                    new SampleTreeItem("Item 3")
                }
            },
            new SampleTreeItem("Async load sample 1", LoadChildrenAsync),
            new SampleTreeItem("Async load sample 2", LoadChildrenAsync),
            new SampleTreeItem("Mixed Content")
            {
                Children = new HashSet<SampleTreeItem>
                {
                    new SampleTreeItem("Static child"),
                    new SampleTreeItem("Async child", LoadChildrenAsync)
                }
            }
        };
    }

    private async Task<HashSet<SampleTreeItem>> LoadChildrenAsync(SampleTreeItem parent, CancellationToken token)
    {
        await Task.Delay(1500, token);
        
        return new HashSet<SampleTreeItem>
        {
            new SampleTreeItem($"{parent.Name} - Child 1"),
            new SampleTreeItem($"{parent.Name} - Child 2"),
            new SampleTreeItem($"{parent.Name} - Child 3")
            {
                Children = new HashSet<SampleTreeItem>
                {
                    new SampleTreeItem($"{parent.Name} - Grandchild 1"),
                    new SampleTreeItem($"{parent.Name} - Grandchild 2")
                }
            }
        };
    }
}

```
