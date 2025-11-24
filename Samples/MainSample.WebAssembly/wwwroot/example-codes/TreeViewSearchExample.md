```razor
@using MudBlazor.Extensions.Core.Enums
@inherits ExampleBase

<MudTextField @bind-Value="_searchText" 
              Label="Search" 
              Variant="Variant.Outlined" 
              Adornment="Adornment.End" 
              AdornmentIcon="@Icons.Material.Filled.Search"
              Class="mb-3"
              Immediate="true"
              OnDebounceIntervalElapsed="FilterTree" 
              DebounceInterval="300" />

<MudExTreeView @bind-SelectedNode="_selectedNode"
               @ref="ComponentRef"
               Style="max-height: 50vh; overflow: auto"
               Virtualize="false"
               ViewMode="TreeViewMode.Default"
               Items="@_filteredEntries">

</MudExTreeView>

@code {
    private SampleTreeItem _selectedNode;
    private string _searchText = "";
    public HashSet<SampleTreeItem> Entries { get; set; }
    private HashSet<SampleTreeItem> _filteredEntries = new();

    protected override void OnInitialized()
    {
        Entries = SampleTreeStructure.GetItems();
        _filteredEntries = Entries;
    }

    private void FilterTree()
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            _filteredEntries = Entries;
        }
        else
        {
            _filteredEntries = FilterNodes(Entries, _searchText.ToLower()).ToHashSet();
        }
        StateHasChanged();
    }

    private IEnumerable<SampleTreeItem> FilterNodes(IEnumerable<SampleTreeItem> nodes, string searchTerm)
    {
        foreach (var node in nodes)
        {
            var matchesSearch = node.Name?.ToLower().Contains(searchTerm) ?? false;
            var filteredChildren = node.Children != null 
                ? FilterNodes(node.Children, searchTerm).ToList() 
                : new List<SampleTreeItem>();

            if (matchesSearch || filteredChildren.Any())
            {
                var newNode = new SampleTreeItem(node.Name)
                {
                    Children = filteredChildren.Any() ? filteredChildren.ToHashSet() : node.Children
                };
                yield return newNode;
            }
        }
    }
}

```
