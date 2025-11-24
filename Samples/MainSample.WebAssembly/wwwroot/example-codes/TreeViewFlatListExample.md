```razor
@inherits ExampleBase

<div style="width: 100%; max-height: 300px; overflow: auto">
    <p>@($"{L["Selected Node"]}: {_selectedNode?.Name}")</p>
    <MudExTreeViewFlatList @bind-SelectedNode="_selectedNode"
                           @ref="ComponentRef"
                           Items="@Entries">

    </MudExTreeViewFlatList>
</div>

@code {
    private SampleTreeItem _selectedNode;
    public HashSet<SampleTreeItem> Entries { get; set; }

    protected override void OnInitialized()
    {
        Entries = SampleTreeStructure.GetItems();
    }
}

```
