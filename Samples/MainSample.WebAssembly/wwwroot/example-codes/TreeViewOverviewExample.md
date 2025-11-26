```razor
@using MudBlazor.Extensions.Core.Enums
@inherits ExampleBase

<MudExEnumSelect Variant="Variant.Outlined" Class="mb-2 mt-4" TEnum="TreeViewMode" @bind-Value="@_treeViewMode" Label="TreeViewMode" />
<p>@L["Selected"]: @_selectedNode?.Name</p>

<MudExDivider/>
<MudExTreeView @bind-SelectedNode="_selectedNode"
               @ref="ComponentRef"
               Style="max-height: 50vh; overflow: auto"
               Virtualize="false"
               Parameters="@(new Dictionary<string, object> { { nameof(MudExCardList<object>.HoverMode), MudExCardHoverMode.Simple } })"
               ViewMode="_treeViewMode"
               Items="@Entries">

</MudExTreeView>

@code {
    
    private SampleTreeItem _selectedNode;
    private TreeViewMode _treeViewMode = TreeViewMode.Default;
    public HashSet<SampleTreeItem> Entries { get; set; }

    protected override void OnInitialized()
    {
        Entries = SampleTreeStructure.GetItems();
    }
}

```
