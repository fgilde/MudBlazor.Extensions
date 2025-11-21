```razor
@using Nextended.Core.Contracts
@using Nextended.Core.Types
@inherits ExampleBase

<MudText Typo="Typo.caption" Class="mt-2 mb-2">
    @L["Current period: {0} – {1}", _selectedRange.Start.ToShortDateString(), _selectedRange.End.ToShortDateString()]
</MudText>

<MudExRangeSlider T="DateTime"
                  @ref="ComponentRef"
                  ShowInputs="true"
                  SizeRange="@(new RangeOf<DateTime>(new(2015, 1, 1), new(2022, 12, 31), null))"
                  StepLength="@(new RangeLength<DateTime>(TimeSpan.FromDays(1).Ticks))"
                  MinLength="@(new RangeLength<DateTime>(TimeSpan.FromDays(60).Ticks))"
                  MaxLength="@(new RangeLength<DateTime>(TimeSpan.FromDays(365 * 2).Ticks))"
                  @bind-Value="_selectedRange"
                  AllowWholeRangeDrag="true" />

@code {
    private IRange<DateTime> _selectedRange = new RangeOf<DateTime>(new(2021, 1, 1), new(2021, 12, 31), null);
}

```
