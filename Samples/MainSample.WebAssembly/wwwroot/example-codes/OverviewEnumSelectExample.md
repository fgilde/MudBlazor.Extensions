```razor
@inherits ExampleBase

<MudExEnumSelect @ref="ComponentRef" TEnum="DayOfWeek" @bind-Value="_day" Label="@L["Select a day"]" />

<MudText Typo="Typo.body2" Class="mt-3">
    @L["Selected"]: <strong>@_day</strong>
</MudText>

@code {
    private DayOfWeek _day = DayOfWeek.Monday;
}

```
