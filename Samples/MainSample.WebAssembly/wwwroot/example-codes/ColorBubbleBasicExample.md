```razor
@inherits ExampleBase
@using MudBlazor.Utilities

<MudExColorBubble @ref="ComponentRef" @bind-Color="@_color" />

<MudText Typo="Typo.body2" Class="mt-3">
    @L["Selected"]: @_color
</MudText>

@code {
    private MudColor _color = new("#1976d2");
}

```
