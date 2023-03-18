using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Components;

public partial class MoveContent
{
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public RenderFragment Found { get; set; }
    [Parameter] public RenderFragment NotFound { get; set; }
    [Parameter] public string ElementSelector { get; set; }
    [Parameter] public MoveContentMode Mode { get; set; } = MoveContentMode.MoveToSelector;
    [Parameter] public MoveContentPosition Position { get; set; } = MoveContentPosition.AfterEnd;

    public bool ElementFound { get; private set; }
    
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        ElementFound = await JsRuntime.InvokeAsync<object>("document.querySelector", ElementSelector) != null;
    }

    public override async Task ImportModuleAndCreateJsAsync()
    {
        await base.ImportModuleAndCreateJsAsync();
        await JsReference.InvokeVoidAsync("move", ElementSelector, Mode.ToString(), Position.ToString());
    }

}