```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Office.Presentation

<div style="width: 100%; height: 520px;">
    <MudExSlideEditor @ref="ComponentRef" Presentation="_presentation" ReadOnly="true" />
</div>

@code {
    private readonly Presentation _presentation = SlidesSamples.CreateDeck();
}

```
