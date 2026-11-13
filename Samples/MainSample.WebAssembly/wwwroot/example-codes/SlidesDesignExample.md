```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Office.Presentation

<MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2" Class="mb-3" Wrap="Wrap.Wrap">
    <MudText Typo="Typo.body2">@L["Apply a design to every slide:"]</MudText>
    @foreach (var design in SlideDesigns.BuiltIn)
    {
        var current = design;
        <MudButton Variant="Variant.Outlined" Color="Color.Primary" Size="Size.Small" OnClick="@(() => _presentation.Execute(new ApplyDesignCommand(current)))">@current.Name</MudButton>
    }
</MudStack>
<div style="width: 100%; height: 600px;">
    <MudExSlideEditor @ref="ComponentRef" @bind-Presentation="_presentation" />
</div>

@code {
    private Presentation _presentation = SlidesSamples.CreateDeck(SlideDesigns.BuiltIn[2]);
}

```
