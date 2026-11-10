```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Office.Spreadsheet

<MudAlert Severity="Severity.Info" Dense="true" Class="mb-3">
    @L["Try it: change a quarter value and watch the chart, the color scale and the totals update, pick another region in H1, or hover A1 for the note."]
</MudAlert>

<div style="width: 100%; height: 560px;">
    <MudExSpreadsheetEditor @bind-Workbook="_workbook" />
</div>

@code {
    private Workbook _workbook = SpreadsheetFeatureSamples.CreateOverview();
}

```
