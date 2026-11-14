```razor
@inherits ExampleBase
@using Doc = MudBlazor.Extensions.Office.Notebook.Notebook

<MudStack Row="true" AlignItems="AlignItems.Center" Spacing="4" Class="mb-3">
    <MudButton Variant="Variant.Outlined" Color="Color.Primary" OnClick="@(() => LoadAsync("sample.mxnb"))">@L["Load sample.mxnb"]</MudButton>
    <MudButton Variant="Variant.Outlined" Color="Color.Primary" OnClick="@NewNotebook">@L["New blank notebook"]</MudButton>
    <MudText Typo="Typo.body2">@_name</MudText>
</MudStack>

@if (_error != null)
{
    <MudAlert Severity="Severity.Error" Class="mb-3">@_error</MudAlert>
}

@if (_notebook != null)
{
    <div style="width: 100%; height: 600px;">
        <MudExNotebookEditor @ref="ComponentRef" @bind-Notebook="_notebook" />
    </div>
}

@code {
    private Doc? _notebook;
    private string _name = "";
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAsync("sample.mxnb");
    }

    private void NewNotebook()
    {
        _notebook = Doc.Create();
        _error = null;
        _name = "Notebook1.mxnb";
    }

    private async Task LoadAsync(string file)
    {
        try
        {
            var bytes = await Http.GetByteArrayAsync(NavigationManager.ToAbsoluteUri("sample-data/" + file));
            using var stream = new MemoryStream(bytes);
            _notebook = Doc.Load(stream);
            _error = null;
            _name = file;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidDataException)
        {
            _error = L["The sample file could not be loaded: {0}", ex.Message];
        }
    }
}

```
