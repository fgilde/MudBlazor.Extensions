```razor
@using MudBlazor.Extensions.Core
@using MudBlazor.Extensions.Core.Enums
@inherits ExampleBase

<MudExEnumSelect TEnum="TreeViewMode" @bind-Value="@_treeViewMode" Label="TreeViewMode" />
<MudCheckBox @bind-Value="_reverseExpand">@L["Switch expand button position"]</MudCheckBox>

<p>@($"{L["Selected Node"]}: {_selectedNode}")</p>
<MudExDivider Color="MudExColor.Primary" />

<div style="height: 60vh">
    <MudExTreeView @bind-SelectedNode="_selectedNode"
                   @ref="ComponentRef"
                   Parameters="@(new Dictionary<string, object> { { nameof(MudExCardList<object>.HoverMode), MudExCardHoverMode.Simple } })"
                   ViewMode="_treeViewMode" ExpandButtonDirection="@((_reverseExpand ? LeftOrRight.Right : LeftOrRight.Left ))" Items="@Entries">
        <ItemContentTemplate>
            <p style="text-decoration: underline">@context.Value.ToString()</p>
        </ItemContentTemplate>
    </MudExTreeView>
</div>


@code {

    private bool _reverseExpand;
    private NavigationEntry _selectedNode;
    private TreeViewMode _treeViewMode = TreeViewMode.Default;
    public HashSet<NavigationEntry> Entries { get; set; }

    protected override void OnInitialized()
    {
        Entries = Navigations.Default(NavigationManager);
    }
}

```
