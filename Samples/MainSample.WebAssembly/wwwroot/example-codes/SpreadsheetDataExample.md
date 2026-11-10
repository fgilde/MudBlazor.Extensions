```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Office.Spreadsheet

<MudAlert Severity="Severity.Info" Dense="true" Class="mb-3">
    @L["Try it: use the filter buttons, sort a column from the Data tab, add a row below the table, or type a wrong value into the Validation sheet."]
</MudAlert>

<div style="width: 100%; height: 560px;">
    <MudExSpreadsheetEditor @ref="ComponentRef" @bind-Workbook="_workbook" />
</div>

@code {
    private Workbook _workbook = SpreadsheetFeatureSamples.CreateData();
}

```
