```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Office.Spreadsheet

<MudText Typo="Typo.caption" Class="mb-2">@L["Selection: {0}", _selection]</MudText>

<div style="width: 100%; height: 560px;">
    <MudExSpreadsheetEditor @bind-Workbook="_workbook" SelectionChanged="@OnSelectionChanged" />
</div>

@code {
    private Workbook _workbook = SpreadsheetSamples.CreateBudget();
    private string _selection = "-";

    private void OnSelectionChanged(SpreadsheetSelection selection) => _selection = selection?.ToString() ?? "-";
}

```
