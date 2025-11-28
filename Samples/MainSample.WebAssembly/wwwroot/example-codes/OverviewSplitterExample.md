```razor
@inherits ExampleBase

<MudExSplitter @ref="ComponentRef">
    <MudExSplitPanel>
        <MudPaper Class="pa-4" Elevation="2">
            <MudText Typo="Typo.body1">@L["Left Panel"]</MudText>
        </MudPaper>
    </MudExSplitPanel>
    <MudExSplitPanel>
        <MudPaper Class="pa-4" Elevation="2">
            <MudText Typo="Typo.body1">@L["Right Panel"]</MudText>
        </MudPaper>
    </MudExSplitPanel>
</MudExSplitter>

@code {
}

```
