using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components.Base;

public abstract class MudExJsRequiredBaseComponent<T> : MudExBaseComponent<T>, IAsyncDisposable, IJsMudExComponent<T>
    where T : MudExBaseComponent<T>
{
    public IJSObjectReference JsReference { get; set; }
    public IJSObjectReference ModuleReference { get; set; }
    public ElementReference ElementReference { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await ImportModuleAndCreateJsAsync();
        }
    }

    public virtual object[] GetJsArguments() => new object[] {ElementReference, CreateDotNetObjectReference()};

    public virtual DotNetObjectReference<MudExJsRequiredBaseComponent<T>> CreateDotNetObjectReference() => DotNetObjectReference.Create(this);

    public virtual async Task ImportModuleAndCreateJsAsync()
    {
        var references = await JsRuntime.ImportModuleAndCreateJsAsync<T>(GetJsArguments());
        JsReference = references.jsObjectReference;
        ModuleReference = references.moduleReference;
    }


    public virtual async ValueTask DisposeAsync()
    {
        if (JsReference != null)
        {
            try { await JsReference.InvokeVoidAsync("dispose"); }
            catch {}
            await JsReference.DisposeAsync();
        }

        if (ModuleReference != null)
            await ModuleReference.DisposeAsync();

    }
}