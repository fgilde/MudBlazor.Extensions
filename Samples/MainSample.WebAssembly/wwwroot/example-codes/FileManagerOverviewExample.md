```razor
@using MudBlazor.Extensions.Services
@inherits ExampleBase

@if (_structure != null)
{
    <MudExFileManager @ref="ComponentRef"
                      Manager="@_structure"
                      Height="@("70vh")"
                      OnFileOpened="@(node => _lastOpened = node.Name)" />
}

@if (!string.IsNullOrEmpty(_lastOpened))
{
    <MudAlert Severity="Severity.Info" Dense="true" Class="mt-2">@L["Opened: {0}", _lastOpened]</MudAlert>
}

@code {
    private MudExInMemoryFileStructureManager _structure;
    private string _lastOpened;

    protected override async Task OnInitializedAsync()
    {
        // base registers the example with the demo box - without it the box has nothing to show.
        await base.OnInitializedAsync();
        _structure = await DemoFileStructure.BuildAsync(Http, NavigationManager.BaseUri);
    }
}

```
