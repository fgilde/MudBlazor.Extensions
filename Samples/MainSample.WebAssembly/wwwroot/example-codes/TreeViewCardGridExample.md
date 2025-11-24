```razor
@inherits ExampleBase

<div style="width: 100%; max-height:100%;">
    <p>@($"{L["Selected Node"]}: {_selectedNode?.Name}")</p>
    <MudExTreeViewCardGrid @bind-SelectedNode="_selectedNode"
                           @ref="ComponentRef"
                           HoverMode="MudExCardHoverMode.CardEffect3d | MudExCardHoverMode.LightBulb"
                           Items="@Entries">

    </MudExTreeViewCardGrid>
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
