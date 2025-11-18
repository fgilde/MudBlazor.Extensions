```razor
@using MudBlazor.Extensions.Helper
@using Nextended.Core.Contracts
@inherits ExampleBase

<MudText Typo="Typo.body2" Class="mb-4">
    @L["In this example you control a simple numeric range from 0 to 100. The step size is set to 5 to allow coarse intervals."]
</MudText>

<MudText Typo="Typo.caption" Class="mt-2 mb-2">
    @L["Current range: {0} – {1}", _nums.Start, _nums.End]
</MudText>

<MudExRangeSlider T="int"
                  @ref="ComponentRef"
                  SizeRange="@(new MudExRange<int>(0, 100))"
                  StepLength="@(5)"
                  @bind-Value="_nums" />

@code {
    private IRange<int> _nums = new MudExRange<int>(20, 60);
}

```
