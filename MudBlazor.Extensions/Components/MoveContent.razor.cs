using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

public partial class MoveContent
{
    [Inject] private IServiceProvider _serviceProvider { get; set; }
    private IJSRuntime JsRuntime => _serviceProvider.GetService<IJSRuntime>();

    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public RenderFragment Found { get; set; }
    [Parameter] public RenderFragment NotFound { get; set; }
    [Parameter] public string ElementSelector { get; set; }
    [Parameter] public MoveContentMode Mode { get; set; } = MoveContentMode.MoveToSelector;
    [Parameter] public MoveContentPosition Position { get; set; } = MoveContentPosition.AfterEnd;

    public bool ElementFound { get; private set; }


    private ElementReference elementReference;
    private IJSObjectReference _jsReference;
    private IJSObjectReference _module;
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        ElementFound = await JsRuntime.InvokeAsync<object>("document.querySelector", ElementSelector) != null;
    }


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
            await ImportModuleAndExecuteMoveAsync();
    }

    private async Task ImportModuleAndExecuteMoveAsync()
    {
        var references = await JsRuntime.ImportModuleAndCreateJsAsync<MoveContent>(elementReference, DotNetObjectReference.Create(this));
        _jsReference = references.jsObjectReference;
        _module = references.moduleReference;
        await _jsReference.InvokeVoidAsync("move", ElementSelector, Mode.ToString(), Position.ToString());
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsReference != null)
        {
            await _jsReference.InvokeVoidAsync("dispose");
            await _jsReference.DisposeAsync();
        }

        if (_module != null)
            await _module.DisposeAsync();

    }
}