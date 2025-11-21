```razor
@using MudBlazor.Extensions.Helper
@using Nextended.Core.Contracts
@inherits ExampleBase

<MudText Typo="Typo.caption" Class="mt-2 mb-2">
    @L["Current period: {0} – {1}", _selectedRange.Start.ToString("g"), _selectedRange.End.ToString("g")]
</MudText>

<MudExRangeSlider T="DateTime"
                  @ref="ComponentRef"
                  ShowInputs="true"
                  SizeRange="@(new MudExRange<DateTime>(new(2019, 1, 1, 2, 22, 12), new(2025, 12, 31, 6, 3, 12)))"
                  StepResolver="@(StepResolvers.DateTime.Quarterly(_ => 1))"
                  @bind-Value="_selectedRange"
                  AllowWholeRangeDrag="true" />

<MudText Typo="Typo.body2" Class="mt-3">
    @L["With StepResolvers you can easily configure the stepping behavior. For date ranges we provide several predefined resolvers. In this example StepResolvers.DateTime.Quarterly(_ => 1) makes the slider snap to quarters."]
</MudText>

@code {
    private IRange<DateTime> _selectedRange = new MudExRange<DateTime>(new(2021, 1, 1), new(2021, 12, 31));

}

```
