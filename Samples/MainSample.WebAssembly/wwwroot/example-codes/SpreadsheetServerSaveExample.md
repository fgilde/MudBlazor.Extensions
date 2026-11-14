```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Core
@using MudBlazor.Extensions.Services
@inject MudExFileService FileService
@inject ISnackbar Snackbar

<MudText Typo="Typo.caption" Class="mb-2">@L["Last save: {0}", _last]</MudText>

@if (_stream != null)
{
    <div style="width: 100%; height: 520px;">
        <MudExFileDisplay @ref="ComponentRef" FileName="ExcelSheet.xlsx"
                          ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                          ContentStream="@_stream"
                          AllowEdit="true"
                          AllowLocalSave="false"
                          SaveRequested="@OnSaveRequested"
                          Dense="true" />
    </div>
}

@code {
    private Stream? _stream;
    private string _last = "-";

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _stream = await FileService.ReadStreamAsync(NavigationManager.ToAbsoluteUri("sample-data/ExcelSheet.xlsx").AbsoluteUri);
    }

    private async Task OnSaveRequested(MudExFileSaveRequest request)
    {
        var copy = new MemoryStream();
        await request.Content.Stream.CopyToAsync(copy);
        request.Handled = true;
        _last = $"{request.Content.FileName}, {copy.Length:N0} bytes";
        Snackbar.Add(L["Saved {0} to the server (simulated)", _last], Severity.Success);
    }
}

```
