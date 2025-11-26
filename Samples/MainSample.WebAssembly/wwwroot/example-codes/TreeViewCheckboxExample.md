```razor
@using MudBlazor.Extensions.Core.Enums
@inherits ExampleBase

<MudText Typo="Typo.body2" Class="mb-2">
    @L["Selected items"]: @(_selectedNodes.Count)
</MudText>
<MudToolBar>
    <MudSpacer />
    <MudExEnumSelect Variant="Variant.Outlined" Class="mb-2 mt-4" TEnum="TreeViewMode" @bind-Value="@_treeViewMode" Label="TreeViewMode" />
</MudToolBar>
<MudPaper Style=" width: 100%; min-height:300px; max-height: 450px; overflow: auto;" Square="true">
    <MudExTreeView @ref="ComponentRef"
                   Virtualize="false"
                   ViewMode="@_treeViewMode"
                   Items="@Entries"
                   AllowSelectionOfNonEmptyNodes="true">
        <ItemContentTemplate>
            <div style="display: flex; align-items: center; gap: 8px;">
                <MudCheckBox Value="@(context?.Value != null && _selectedNodes.Contains(context.Value))" T="bool" ValueChanged="@(value => OnNodeSelectionChanged(context, value))"></MudCheckBox>
                @* Render item content with default template *@
                @context.TreeView.RenderItemContent(context)
            </div>

        </ItemContentTemplate>
    </MudExTreeView>
</MudPaper>
<MudText Typo="Typo.caption" Class="mt-2">
    @L["Selected items"]: @string.Join(", ", _selectedNodes.Select(n => n.Name))
</MudText>

@code {
    private TreeViewMode _treeViewMode = TreeViewMode.Default;
    private HashSet<SampleTreeItem> _selectedNodes = new();
    public HashSet<SampleTreeItem> Entries { get; set; }

    protected override void OnInitialized()
    {
        Entries = SampleTreeStructure.GetItems();
    }

    private Task OnNodeSelectionChanged(TreeViewItemContext<SampleTreeItem> context, bool value)
    {
        if (value && context.Value != null)
        {
            _selectedNodes.Add(context.Value);
        }
        else if (context.Value != null && _selectedNodes.Contains(context.Value))
        {
            _selectedNodes.Remove(context.Value);
        }

        return Task.CompletedTask;
    }

}

```
