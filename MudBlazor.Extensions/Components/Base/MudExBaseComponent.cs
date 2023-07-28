using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;


namespace MudBlazor.Extensions.Components.Base;

/// <summary>
/// Base component for the most of all MudExComponents
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class MudExBaseComponent<T> : ComponentBase, IMudExComponent
    where T : MudExBaseComponent<T>
{
    private object _previousKey;
    private Timer _renderFinishTimer;
    private IStringLocalizer<T> _fallbackLocalizer => Get<IStringLocalizer<T>>();

    /// <summary>
    /// Localizer for localize texts
    /// </summary>
    [Parameter] public IStringLocalizer Localizer { get; set; }

    /// <summary>
    /// Render key for refresh component
    /// </summary>
    [IgnoreOnObjectEdit]
    [Parameter] public object RenderKey { get; set; }

    /// <summary>
    /// Injected service provider
    /// </summary>
    [Inject] protected IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// JsRuntime
    /// </summary>
    [Inject] public IJSRuntime JsRuntime { get; set; }

    /// <summary>
    /// This is true if render has called already
    /// </summary>
    public bool IsRendered { get; protected set; }

    /// <summary>
    /// This is true if render has called already and finished all after render calls
    /// </summary>
    public bool IsFullyRendered { get; protected set; }
    
    /// <summary>
    /// DialogService
    /// </summary>
    protected IDialogService DialogService => Get<IDialogService>();

    /// <summary>
    /// The localizer to use
    /// </summary>
    protected IStringLocalizer LocalizerToUse => Localizer ?? _fallbackLocalizer;

    /// <summary>
    /// Returns the injected service for TService
    /// </summary>
    protected TService Get<TService>() => ServiceProvider.GetService<TService>();

    /// <summary>
    /// Returns the injected services for TService
    /// </summary>
    protected IEnumerable<TService> GetServices<TService>() => ServiceProvider.GetServices<TService>();

    /// <summary>
    /// Tries to localize given text if localizer and translation is available
    /// </summary>
    public string TryLocalize(string text, params object[] args) => LocalizerToUse.TryLocalize(text, args);

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        if (base.ShouldRender() || !Equals(_previousKey, RenderKey))
        {
            _previousKey = RenderKey;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Called when rendering is finished
    /// </summary>
    protected virtual Task OnFinishedRenderAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        IsRendered = true;
        _renderFinishTimer?.Dispose();
        _renderFinishTimer = new Timer(async _ =>
        {
            _renderFinishTimer?.Dispose();
            _renderFinishTimer = null;
            IsFullyRendered = true;
            await OnFinishedRenderAsync();
        }, null, 300, Timeout.Infinite); 
    }

    /// <summary>
    /// Refreshes this component and forces render
    /// </summary>
    /// <returns></returns>
    public virtual T Refresh()
    {
        RenderKey = Guid.NewGuid();
        StateHasChanged();
        return (T) this;
    }

}

/// <summary>
/// Interface for all MudExComponents
/// </summary>
public interface IMudExComponent
{
}

/// <summary>
/// Interface for components with js imports
/// </summary>
/// <typeparam name="T"></typeparam>
internal interface IJsMudExComponent<T>: IMudExComponent, IAsyncDisposable
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
        await JsRuntime.InitializeMudBlazorExtensionsAsync();
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
            try { await JsReference.InvokeVoidAsync("dispose"); }catch { }
            try { await JsReference.DisposeAsync(); } catch { }
        }

        if (ModuleReference != null)
            try { await ModuleReference.DisposeAsync(); } catch { }
    }

}