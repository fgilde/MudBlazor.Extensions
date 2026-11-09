```razor
@using Microsoft.Extensions.Localization

@if (_shown)
{
    @ChildContent
}
else
{
    <MudButton Variant="Variant.Outlined" Color="Color.Primary" StartIcon="@Icons.Material.Filled.PlayArrow" OnClick="@(() => _shown = true)">@L["Show demo"]</MudButton>
}

@code {
    private bool _shown;

    [Inject] private IStringLocalizer<ExampleBase> L { get; set; } = null!;

    [Parameter] public RenderFragment? ChildContent { get; set; }
}

```
