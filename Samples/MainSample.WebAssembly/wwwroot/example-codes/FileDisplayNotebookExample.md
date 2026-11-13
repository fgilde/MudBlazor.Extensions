```razor
@inherits ExampleBase
@using MudBlazor.Extensions.Core
@using MudBlazor.Extensions.Services
@inject MudExFileService FileService

@if (_stream != null)
{
    <div style="width: 100%; height: 600px;">
        <MudExFileDisplay @ref="ComponentRef"
                          FileName="sample.mxnb"
                          ContentType="application/vnd.mudex.notebook+zip"
                          ContentStream="@_stream"
                          AllowEdit="true"
                          Dense="true" />
    </div>
}

@code {
    private Stream? _stream;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _stream = await FileService.ReadStreamAsync(NavigationManager.ToAbsoluteUri("sample-data/sample.mxnb").AbsoluteUri);
    }
}

```
