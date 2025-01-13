using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using BlazorJS.Attributes;
namespace MudBlazor.Extensions.Components.Base;

/// <summary>
/// Base class for components that require a JS module to be imported and a JS object to be created.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class MudExJsRequiredBaseComponent<T> : MudExBaseComponent<T>, IAsyncDisposable, IJsMudExComponent<T>
    where T : MudExBaseComponent<T>
{
    private readonly TaskCompletionSource<IJSObjectReference> _jsReferenceCreatedCompletionSource = new();

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

    internal IJsMudExComponent<T> AsJsComponent => this;

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
    /// Returns a task that completes when the reference is created
    /// </summary>
    public Task<IJSObjectReference> WaitReferenceCreatedAsync() => _jsReferenceCreatedCompletionSource.Task;

    /// <summary>
    /// Returns an object that is passed forward to the js initialize and constructor method
    /// </summary>
    public virtual object[] GetJsArguments() => new object[] { ElementReference, CreateDotNetObjectReference() };

    /// <summary>
    /// The dotnet object reference for the js module
    /// </summary>
    public virtual DotNetObjectReference<MudExJsRequiredBaseComponent<T>> CreateDotNetObjectReference() => DotNetObjectReference.Create(this);

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var hasJsChanges = parameters.AffectedForJs(this);
        await base.SetParametersAsync(parameters);
        if (hasJsChanges)
            await OnJsOptionsChanged();
    }

    /// <summary>
    /// Called when the js options are changed
    /// </summary>
    protected virtual Task OnJsOptionsChanged()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Virtual base method to import the module
    /// </summary>
    public virtual async Task ImportModuleAndCreateJsAsync()
    {
        await JsRuntime.InitializeMudBlazorExtensionsCoreAsync();
        var references = await JsRuntime.ImportModuleAndCreateJsAsync<T>(GetJsArguments());
        JsReference = references.jsObjectReference;
        ModuleReference = references.moduleReference;
        _jsReferenceCreatedCompletionSource.TrySetResult(JsReference);
    }

    /// <inheritdoc/>
    public override ValueTask DisposeAsync()
    {
        AsJsComponent?.DisposeModulesAsync();
        return base.DisposeAsync();
    }
}