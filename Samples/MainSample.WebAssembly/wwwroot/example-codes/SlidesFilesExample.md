```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Office.Presentation

<MudStack Row="true" AlignItems="AlignItems.Center" Spacing="4" Class="mb-3">
    <MudButton Variant="Variant.Outlined" Color="Color.Primary" OnClick="@(() => LoadAsync("sample.pptx"))">@L["Load sample.pptx"]</MudButton>
    <MudButton Variant="Variant.Outlined" Color="Color.Primary" OnClick="@NewDeck">@L["New blank deck"]</MudButton>
    <MudText Typo="Typo.body2">@_name</MudText>
</MudStack>

@if (_error != null)
{
    <MudAlert Severity="Severity.Error" Class="mb-3">@_error</MudAlert>
}

@if (_presentation != null)
{
    <div style="width: 100%; height: 600px;">
        <MudExSlideEditor @ref="ComponentRef" @bind-Presentation="_presentation" />
    </div>
}

@code {
    private Presentation? _presentation;
    private string _name = "";
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAsync("sample.pptx");
    }

    private void NewDeck()
    {
        _presentation = Presentation.Create();
        _error = null;
        _name = "Presentation1.pptx";
    }

    private async Task LoadAsync(string file)
    {
        try
        {
            var bytes = await Http.GetByteArrayAsync(NavigationManager.ToAbsoluteUri("sample-data/" + file));
            using var stream = new MemoryStream(bytes);
            _presentation = Presentation.Load(stream);
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
