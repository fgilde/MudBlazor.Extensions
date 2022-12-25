using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components.Base;

public class MudExJsRequiredBaseComponent<T> : MudExBaseComponent<T>, IAsyncDisposable
    where T : MudExBaseComponent<T>
{
    protected IJSObjectReference JsReference;
    protected IJSObjectReference ModuleReference;
    protected ElementReference ElementReference;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await ImportModuleAndCreateJsAsync();
        }
    }
    
    protected virtual object[] GetJsArguments()
    {
        return new object[] {ElementReference, CreateDotNetObjectReference()};
    }

    protected virtual DotNetObjectReference<MudExJsRequiredBaseComponent<T>> CreateDotNetObjectReference()
    {
        return DotNetObjectReference.Create(this);
    }

    protected virtual async Task ImportModuleAndCreateJsAsync()
    {
        var references = await JsRuntime.ImportModuleAndCreateJsAsync<T>(GetJsArguments());
        JsReference = references.jsObjectReference;
        ModuleReference = references.moduleReference;
    }


    public virtual async ValueTask DisposeAsync()
    {
        if (JsReference != null)
        {
            try
            {
                await JsReference.InvokeVoidAsync("dispose");
            }
            catch
            {}
            await JsReference.DisposeAsync();
        }

        if (ModuleReference != null)
            await ModuleReference.DisposeAsync();

    }
}