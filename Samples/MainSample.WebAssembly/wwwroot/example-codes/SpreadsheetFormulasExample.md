```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Office.Spreadsheet

<MudAlert Severity="Severity.Info" Dense="true" Class="mb-3">
    @L["Try it: change a value in B2:G7, switch the lookup category in B10, or type =SUM(B2;C2) with either separator."]
</MudAlert>

<div style="width: 100%; height: 560px;">
    <MudExSpreadsheetEditor @bind-Workbook="_workbook" RibbonMode="MudExRibbonMode.Simplified" />
</div>

@code {
    private Workbook _workbook = SpreadsheetSamples.CreateBudget();
}

```
