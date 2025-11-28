```razor
@inherits ExampleBase

<MudGrid>
    <MudItem xs="12" md="6">
        <MudExEnumSelect @ref="ComponentRef" TEnum="Priority?" @bind-Value="@SelectedPriority" Label="@L["Priority (nullable)"]" />
    </MudItem>
    <MudItem xs="12" md="6">
        @if (SelectedPriority.HasValue)
        {
            <MudText Typo="Typo.body1" Class="mt-3">@L["Selected"]: <strong>@SelectedPriority</strong></MudText>
        }
        else
        {
            <MudText Typo="Typo.body1" Color="Color.Secondary" Class="mt-3">@L["No priority selected"]</MudText>
        }
    </MudItem>
</MudGrid>

@code {
    public Priority? SelectedPriority { get; set; }

    public enum Priority
    {
        Low,
        Normal,
        High,
        Critical
    }
}

```
