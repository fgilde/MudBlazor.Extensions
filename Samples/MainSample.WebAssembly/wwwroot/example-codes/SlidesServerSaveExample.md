```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Core
@using MudBlazor.Extensions.Office.Presentation
@inject ISnackbar Snackbar

<MudText Typo="Typo.caption" Class="mb-2">@L["Last save: {0}", _last]</MudText>

<div style="width: 100%; height: 600px;">
    <MudExSlideEditor @ref="ComponentRef"
                      Presentation="_presentation"
                      AllowLocalSave="false"
                      SaveRequested="@OnSaveRequested" />
</div>

@code {
    private readonly Presentation _presentation = SlidesSamples.CreateDeck();
    private string _last = "-";

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
