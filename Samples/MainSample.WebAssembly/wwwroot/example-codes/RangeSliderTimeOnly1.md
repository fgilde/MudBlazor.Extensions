```razor
@using MudBlazor.Extensions.Helper
@using Nextended.Core.Contracts
@using Nextended.Core.Types
@inherits ExampleBase

<MudText Typo="Typo.caption" Class="mt-2 mb-2">
    @L["Current period : {0} – {1}", _selectedRange.Start.ToLongTimeString(), _selectedRange.End.ToLongTimeString()]
</MudText>

<MudDivider Vertical="false" />

<MudText Typo="Typo.h6" Class="mt-2 mb-2">
    @L["Default steps"]
</MudText>

<MudExRangeSlider T="TimeOnly"
                  ShowInputs="true"
                  SizeRange="@(new MudExRange<TimeOnly>(new TimeOnly(0, 0), new TimeOnly(23, 59, 59)))"
                  @bind-Value="_selectedRange"
                  AllowWholeRangeDrag="true" />

<MudDivider Vertical="false" />


<MudText Typo="Typo.h6" Class="mt-2 mb-2">
    @L["Minutely steps"]
</MudText>

<MudExRangeSlider T="TimeOnly"
                  ShowInputs="true"
                  SizeRange="@(new MudExRange<TimeOnly>(new TimeOnly(0, 0), new TimeOnly(23, 59, 59)))"
                  StepResolver="@(StepResolvers.TimeOnly.Minutely())"
                  @bind-Value="_selectedRange"
                  AllowWholeRangeDrag="true" />

<MudDivider Vertical="false"/>

<MudText Typo="Typo.h6" Class="mt-2 mb-2">
    @L["2 Hourly steps (using StepLength)"]
</MudText>
<MudText Typo="Typo.subtitle1" Class="mt-2 mb-2">
    @L["Because time values always having a fixed size you can also use step length instead of resolvers."]
</MudText>

<MudExRangeSlider T="TimeOnly"
                  StepLength="@(new RangeLength<TimeOnly>(TimeSpan.FromHours(2).Ticks))"
                  ShowInputs="true"
                  SizeRange="@(new MudExRange<TimeOnly>(new TimeOnly(0, 0), new TimeOnly(23, 59, 59)))"
                  @bind-Value="_selectedRange"
                  AllowWholeRangeDrag="true" />

<MudDivider Vertical="false" />


<MudText Typo="Typo.h6" Class="mt-2 mb-2">
    @L["2 Hourly steps (using StepResolver)"]
</MudText>
<MudText Typo="Typo.subtitle1" Class="mt-2 mb-2">
    @L["But for sure resolvers are working as well."]
</MudText>

<MudExRangeSlider T="TimeOnly"
                  ShowInputs="true"
                  SizeRange="@(new MudExRange<TimeOnly>(new TimeOnly(0, 0), new TimeOnly(23, 59)))"
                  StepResolver="@(StepResolvers.TimeOnly.Hourly(2))"
                  @bind-Value="_selectedRange"
                  AllowWholeRangeDrag="true" />

@code {
    private IRange<TimeOnly> _selectedRange = new MudExRange<TimeOnly>(new(1, 0), new(10, 00));
}

```
