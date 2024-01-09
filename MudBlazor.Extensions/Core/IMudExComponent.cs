using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Core;


/// <summary>
/// Interface for all MudExComponents
/// </summary>
public interface IMudExComponent
{}

/// <summary>
/// Interface for components with js imports
/// </summary>
/// <typeparam name="T"></typeparam>
internal interface IJsMudExComponent<T> : IMudExComponent, IAsyncDisposable
{
    
    /// <summary>
    /// JsRuntime
    /// </summary>
    public IJSRuntime JsRuntime { get; }

    /// <summary>
    /// Reference to the js
    /// </summary>
    public IJSObjectReference JsReference { get; set; }

    /// <summary>
    /// Reference to imported module
    /// </summary>
    public IJSObjectReference ModuleReference { get; set; }

    /// <summary>
    /// Reference to rendered element
    /// </summary>
    public ElementReference ElementReference { get; set; }

    /// <summary>
    /// Returns the object that is passed to js
    /// </summary>
    public virtual object[] GetJsArguments()
    {
        return new object[] { ElementReference, CreateDotNetObjectReference() };
    }

    /// <summary>
    /// DotNetObjectReference for callbacks
    /// </summary>
    public virtual DotNetObjectReference<IJsMudExComponent<T>> CreateDotNetObjectReference()
    {
        return DotNetObjectReference.Create(this);
    }

    /// <summary>
    /// Imports the required module and calls the initialize method 
    /// </summary>
    public virtual async Task ImportModuleAndCreateJsAsync(string name = null)
    {
        await JsRuntime.InitializeMudBlazorExtensionsCoreAsync();
        var references = await JsRuntime.ImportModuleAndCreateJsAsync<T>(name, GetJsArguments());
        JsReference = references.jsObjectReference;
        ModuleReference = references.moduleReference;
    }

    /// <summary>
    /// Disposes all modules and references
    /// </summary>
    /// <returns></returns>
    public virtual async ValueTask DisposeModulesAsync()
    {
        if (JsReference != null)
        {
            try { await JsReference.InvokeVoidAsync("dispose"); } catch { }
            try { await JsReference.DisposeAsync(); } catch { }
        }

        if (ModuleReference != null)
            try { await ModuleReference.DisposeAsync(); } catch { }
    }

}