```razor
@using MudBlazor.Extensions.Helper
@using Nextended.Core.Contracts
@inherits ExampleBase

<MudText Typo="Typo.caption" Class="mt-2 mb-2">
    @L["Current period : {0} – {1}", _selectedRange.Start.ToLongTimeString(), _selectedRange.End.ToLongTimeString()]
</MudText>

<MudExRangeSlider T="TimeOnly"
                  @ref="ComponentRef"
                  ShowInputs="true"
                  SizeRange="@(new MudExRange<TimeOnly>(new TimeOnly(0, 0), new TimeOnly(23, 59)))"
                  StepResolver="@(StepResolvers.TimeOnly.Minutely())"
                  @bind-Value="_selectedRange"
                  AllowWholeRangeDrag="true" />

@code {
    private IRange<TimeOnly> _selectedRange = new MudExRange<TimeOnly>(new(1, 0), new(10, 00));
}

```
