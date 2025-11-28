```razor
@inherits ExampleBase

<div style="height: 300px;">
    <MudExSplitPanel @ref="ComponentRef" ColumnLayout="true">
        <Left>
            <MudExSplitPanelItem Style="height: 50%; min-height: 50px; background-color: var(--mud-palette-primary);">
                <MudText Typo="Typo.body1" Class="pa-4" Style="color: white;">@L["Top Panel"]</MudText>
            </MudExSplitPanelItem>
        </Left>
        <Right>
            <MudExSplitPanelItem Style="height: 50%; min-height: 50px; background-color: var(--mud-palette-secondary);">
                <MudText Typo="Typo.body1" Class="pa-4" Style="color: white;">@L["Bottom Panel"]</MudText>
            </MudExSplitPanelItem>
        </Right>
    </MudExSplitPanel>
</div>

@code {
}

```
