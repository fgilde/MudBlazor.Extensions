```razor
@inherits ExampleBase

<MudExPicker @ref="ComponentRef" Text="@PathText" AdornmentIcon="@Icons.Material.Filled.AccountTree">
    <MudExTreeViewDefault @ref="tree"
                          T="SampleTreeItem"
                          @bind-SelectedNode="_selectedNode"
                          AllowSelectionOfNonEmptyNodes="true"
                          AfterNodeSelected="@AfterNodeSelect"
                          Items="@Entries">
    </MudExTreeViewDefault>
</MudExPicker>

@code {
    MudExTreeViewDefault<SampleTreeItem> tree;
    private SampleTreeItem _selectedNode;
    public HashSet<SampleTreeItem> Entries { get; set; } = SampleTreeStructure.GetItems();
    private string PathText => _selectedNode != null ? _selectedNode.GetPathString(s => s.Name," -> ") : "Nothing selected";

    private Task AfterNodeSelect(SampleTreeItem item)
    {
        return ComponentRef is MudExPicker picker && picker.IsOpen ? picker.CloseAsync() : Task.CompletedTask;
    }
}

```
