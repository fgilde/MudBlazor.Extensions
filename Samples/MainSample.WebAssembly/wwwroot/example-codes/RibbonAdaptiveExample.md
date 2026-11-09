```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Core

<MudSlider T="int" Min="240" Max="1200" Step="10" @bind-Value="_width" Color="Color.Primary" Class="mb-3">@L["Container width: {0}px (shrink it to watch groups collapse by CollapsePriority)", _width]</MudSlider>

<RibbonWordLikeExample Width="@($"{_width}px")" Title="Adaptive.docx" />

@code {
    private int _width = 1000;
}

```
