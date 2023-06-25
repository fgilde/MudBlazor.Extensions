using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components.Base;

/// <summary>
/// Base class for components that require a JS module to be imported and a JS object to be created.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class MudExJsRequiredBaseComponent<T> : MudExBaseComponent<T>, IAsyncDisposable, IJsMudExComponent<T>
    where T : MudExBaseComponent<T>
{
    /// <summary>
    /// The JS object reference.
    /// </summary>
    public IJSObjectReference JsReference { get; set; }

    /// <summary>
    /// The imported module reference
    /// </summary>
    public IJSObjectReference ModuleReference { get; set; }

    /// <summary>
    /// Reference to rendered element
    /// </summary>
    public ElementReference ElementReference { get; set; }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await ImportModuleAndCreateJsAsync();
        }
    }

    /// <summary>
    /// Returns an object that is passed forward to the js initialize and constructor method
    /// </summary>
    public virtual object[] GetJsArguments() => new object[] {ElementReference, CreateDotNetObjectReference()};

    /// <summary>
    /// The dotnet object reference for the js module
    /// </summary>
    public virtual DotNetObjectReference<MudExJsRequiredBaseComponent<T>> CreateDotNetObjectReference() => DotNetObjectReference.Create(this);

    /// <summary>
    /// Virtual base method to import the module
    /// </summary>
    public virtual async Task ImportModuleAndCreateJsAsync()
    {
        var references = await JsRuntime.ImportModuleAndCreateJsAsync<T>(GetJsArguments());
        JsReference = references.jsObjectReference;
        ModuleReference = references.moduleReference;
    }

    /// <inheritdoc/>
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