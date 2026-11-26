```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Office.Document

<MudAlert Severity="Severity.Info" Dense="true" Class="mb-2">
    @L["Click into a table cell: the Table Design and Layout tabs appear. Insert > Picture adds an image, a click on it opens the Picture Format tab."]
</MudAlert>
<div style="width: 100%; height: 560px;">
    <MudExDocumentEditor @ref="ComponentRef" @bind-TextDocument="_document" />
</div>

@code {
    private TextDocument _document = DocumentSamples.Showcase();
}

```
