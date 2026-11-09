```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Office.Spreadsheet

<MudStack Row="true" AlignItems="AlignItems.Center" Spacing="4" Class="mb-3">
    <MudButton Variant="Variant.Outlined" Color="Color.Primary" OnClick="@(() => LoadAsync("ExcelSheet.xlsx"))">@L["Load ExcelSheet.xlsx"]</MudButton>
    <MudButton Variant="Variant.Outlined" Color="Color.Primary" OnClick="@(() => LoadAsync("sample.csv"))">@L["Load sample.csv"]</MudButton>
    <MudText Typo="Typo.body2">@_name</MudText>
</MudStack>

@if (_workbook != null)
{
    <div style="width: 100%; height: 520px;">
        <MudExSpreadsheetEditor @bind-Workbook="_workbook" />
    </div>
}

@code {
    private Workbook? _workbook;
    private string _name = "";

    protected override Task OnInitializedAsync() => LoadAsync("ExcelSheet.xlsx");

    private async Task LoadAsync(string file)
    {
        var bytes = await Http.GetByteArrayAsync(NavigationManager.ToAbsoluteUri("sample-data/" + file));
        using var stream = new MemoryStream(bytes);
        _workbook = file.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? CsvFormat.Read(stream) : XlsxReader.Read(stream);
        _name = file;
    }
}

```
