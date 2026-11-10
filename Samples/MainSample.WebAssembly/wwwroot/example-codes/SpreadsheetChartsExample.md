```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Office.Spreadsheet

<MudAlert Severity="Severity.Info" Dense="true" Class="mb-3">
    @L["Try it: change a value in B2:C9 and the charts follow, drag or resize a chart, or select a range and insert another chart from the Insert tab."]
</MudAlert>

<div style="width: 100%; height: 960px;">
    <MudExSpreadsheetEditor @ref="ComponentRef" @bind-Workbook="_workbook" />
</div>

@code {
    private Workbook _workbook = SpreadsheetFeatureSamples.CreateCharts();
}

```
