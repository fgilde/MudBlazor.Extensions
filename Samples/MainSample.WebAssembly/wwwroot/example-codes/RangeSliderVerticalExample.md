```razor
@using MudBlazor.Extensions.Core
@using MudBlazor.Extensions.Helper
@using Nextended.Core.Contracts
@inherits ExampleBase

<MudText Typo="Typo.caption" Class="mt-2 mb-2">
    @L["Current period (vertical): {0} – {1}", _selectedRange.Start.ToShortDateString(), _selectedRange.End.ToShortDateString()]
</MudText>

<div style="height: 280px; display: flex; align-items: center;">
    <MudExRangeSlider T="DateTime"
                      @ref="ComponentRef"
                      ShowInputs="true"
                      Orientation="SliderOrientation.Vertical"
                      SizeRange="@(new MudExRange<DateTime>(new(2016, 1, 1), new(2024, 12, 31)))"
                      Size="Size.Large"
                      ThumbColor="MudExColor.Tertiary"
                      SelectionColor="MudExColor.Tertiary"
                      @bind-Value="_selectedRange"
                      AllowWholeRangeDrag="true">
    </MudExRangeSlider>
</div>

@code {
    private IRange<DateTime> _selectedRange = new MudExRange<DateTime>(new(2021, 1, 1), new(2021, 12, 31));

}

```
