```razor
@inherits ExampleBase

<div style="height: 300px;">
    <MudExSplitter>
        <MudExSplitPanel>
            <MudPaper Class="pa-4" Elevation="2" Style="height: 100%;">
                <MudText Typo="Typo.body1">@L["Panel 1"]</MudText>
            </MudPaper>
        </MudExSplitPanel>
        <MudExSplitPanel>
            <MudExSplitter>
                <MudExSplitPanel>
                    <MudPaper Class="pa-4" Elevation="2" Style="height: 100%;">
                        <MudText Typo="Typo.body1">@L["Panel 2"]</MudText>
                    </MudPaper>
                </MudExSplitPanel>
                <MudExSplitPanel>
                    <MudPaper Class="pa-4" Elevation="2" Style="height: 100%;">
                        <MudText Typo="Typo.body1">@L["Panel 3"]</MudText>
                    </MudPaper>
                </MudExSplitPanel>
            </MudExSplitter>
        </MudExSplitPanel>
    </MudExSplitter>
</div>

@code {
}

```
