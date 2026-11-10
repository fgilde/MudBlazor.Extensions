```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Office.Spreadsheet

<MudStack Row="true" AlignItems="AlignItems.Center" Spacing="4" Class="mb-3">
    <MudButton Variant="Variant.Outlined" Color="Color.Primary" OnClick="@(() => LoadAsync("ExcelSheet.xlsx"))">@L["Load ExcelSheet.xlsx"]</MudButton>
    <MudButton Variant="Variant.Outlined" Color="Color.Primary" OnClick="@(() => LoadAsync("sample.csv"))">@L["Load sample.csv"]</MudButton>
    <MudText Typo="Typo.body2">@_name</MudText>
</MudStack>

@if (_error != null)
{
    <MudAlert Severity="Severity.Error" Class="mb-3">@_error</MudAlert>
}

@if (_workbook != null)
{
    <div style="width: 100%; height: 520px;">
        <MudExSpreadsheetEditor @ref="ComponentRef" @bind-Workbook="_workbook" />
    </div>
}

@code {
    private Workbook? _workbook;
    private string _name = "";
    private string? _error;

    protected override Task OnInitializedAsync() => LoadAsync("ExcelSheet.xlsx");

    private async Task LoadAsync(string file)
    {
        try
        {
            var bytes = await Http.GetByteArrayAsync(NavigationManager.ToAbsoluteUri("sample-data/" + file));
            using var stream = new MemoryStream(bytes);
            _workbook = file.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? CsvFormat.Read(stream) : XlsxReader.Read(stream);
            _error = null;
            _name = file;
        }
        catch (HttpRequestException ex)
        {
            _error = L["The sample file could not be loaded: {0}", ex.Message];
        }
    }
}

```
