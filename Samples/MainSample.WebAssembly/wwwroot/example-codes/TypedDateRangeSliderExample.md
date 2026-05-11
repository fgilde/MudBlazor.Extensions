```razor
@using Nextended.Core.Contracts
@using Nextended.Core.Types
@inherits ExampleBase

<MudText Typo="Typo.caption" Class="mt-2 mb-2">
    @L["Selected: {0} – {1}", _value.Start, _value.End]
</MudText>

<MudStack Row="true" Spacing="2" Class="mb-3" AlignItems="AlignItems.Center">
    <MudText>@L["Mode:"]</MudText>
    <MudExEnumSelect T="DateRangeSliderMode" @bind-Value="_mode" Style="min-width: 140px;" />
    <MudSwitch T="bool" @bind-Value="_zoom" Color="Color.Primary" Label="@L["Mouse wheel zoom"]" />
</MudStack>

<MudExDateRangeSlider @ref="_slider"
                      Mode="_mode"
                      EnableMouseWheelZoom="_zoom"
                      SizeRange="@_sizeRange"
                      @bind-Value="_value"
                      AllowWholeRangeDrag="true" />

@code {
    private MudExDateRangeSlider? _slider;
    private DateRangeSliderMode _mode = DateRangeSliderMode.Month;
    private bool _zoom = true;

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    private IRange<DateOnly> _sizeRange = new RangeOf<DateOnly>(Today.AddYears(-2), Today.AddYears(2), null);
    private IRange<DateOnly> _value = new RangeOf<DateOnly>(Today.AddMonths(-3), Today.AddMonths(3), null);
}

```
