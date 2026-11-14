```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Office.Presentation

<div style="width: 100%; height: 600px;">
    <MudExSlideEditor @ref="ComponentRef" @bind-Presentation="_presentation" />
</div>

@code {
    private Presentation _presentation = SlidesSamples.CreateTablesDeck();
}

```
