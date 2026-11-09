```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Core

<MudStack Spacing="4">
    @foreach (var (name, color) in _colors)
    {
        <div>
            <MudText Typo="Typo.subtitle2" Class="mb-1">@name</MudText>
            <RibbonWordLikeExample Color="@color" Title="@($"{name}.docx")" ContextualColor="@color" PictureSelected="true" ShowModeSwitch="false" />
        </div>
    }
</MudStack>

@code {
    private static readonly (string Name, MudExColor Color)[] _colors =
    [
        ("Color.Success", MudBlazor.Color.Success),
        ("#217346 (Excel green)", "#217346"),
        ("#C43E1C (PowerPoint orange)", "#C43E1C"),
        ("#7719AA (OneNote purple)", "#7719AA")
    ];
}

```
