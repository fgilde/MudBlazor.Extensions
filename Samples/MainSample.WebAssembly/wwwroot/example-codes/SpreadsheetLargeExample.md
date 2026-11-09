```razor
@inherits ExampleBase
@using System.Diagnostics
@using MudBlazor.Extensions.Office.Spreadsheet

<MudStack Row="true" AlignItems="AlignItems.Center" Spacing="4" Class="mb-3">
    <MudButton Variant="Variant.Filled" Color="Color.Primary" Disabled="_busy" OnClick="@Generate">@L["Generate 100,000 rows x 30 columns"]</MudButton>
    <MudText Typo="Typo.body2">@_info</MudText>
</MudStack>

@if (_workbook != null)
{
    <div style="width: 100%; height: 520px;">
        <MudExSpreadsheetEditor @bind-Workbook="_workbook" />
    </div>
}

@code {
    private Workbook? _workbook;
    private bool _busy;
    private string _info = "";

    private async Task Generate()
    {
        _busy = true;
        _workbook = null;
        _info = L["Generating..."];
        await Task.Yield();
        var watch = Stopwatch.StartNew();
        _workbook = SpreadsheetSamples.CreateLarge();
        watch.Stop();
        _info = L["Built in {0} ms. Only the visible cells are rendered.", watch.ElapsedMilliseconds];
        _busy = false;
    }
}

```
