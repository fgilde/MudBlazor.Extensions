```razor
@inherits ExampleBase

<MudGrid>
    <MudItem xs="12" md="6">
        <MudExEnumSelect @ref="ComponentRef" TEnum="OrderStatus" @bind-Value="@Status" Label="@L["Order Status"]" />
    </MudItem>
    <MudItem xs="12" md="6">
        <MudText Typo="Typo.body1" Class="mt-3">@L["Selected"]: <strong>@Status</strong></MudText>
    </MudItem>
</MudGrid>

@code {
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }
}

```
