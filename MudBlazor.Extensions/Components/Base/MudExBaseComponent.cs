using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;


namespace MudBlazor.Extensions.Components.Base;

public abstract class MudExBaseComponent<T> : ComponentBase, IMudExComponent
    where T : MudExBaseComponent<T>
{
    private object _previousKey;
    private Timer _renderFinishTimer;
    
    [Parameter] public IStringLocalizer Localizer { get; set; }
    [Parameter] public object RenderKey { get; set; }
    [Inject] protected IServiceProvider ServiceProvider { get; set; }
    public IJSRuntime JsRuntime => Get<IJSRuntime>();

    public bool IsRendered { get; protected set; }
    public bool IsFullyRendered { get; protected set; }
    
    private IStringLocalizer<T> _fallbackLocalizer => Get<IStringLocalizer<T>>();
    protected IDialogService DialogService => Get<IDialogService>();
    protected IStringLocalizer LocalizerToUse => Localizer ?? _fallbackLocalizer;
    protected TService Get<TService>() => ServiceProvider.GetService<TService>();
    protected IEnumerable<TService> GetServices<TService>() => ServiceProvider.GetServices<TService>();
    public string TryLocalize(string text, params object[] args) => LocalizerToUse.TryLocalize(text, args);
    
    protected override bool ShouldRender()
    {
        if (base.ShouldRender() || !Equals(_previousKey, RenderKey))
        {
            _previousKey = RenderKey;
            return true;
        }

        return false;
    }

    protected virtual Task OnFinishedRenderAsync()
    {

        return Task.CompletedTask;
    }

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

    public virtual T Refresh()
    {
        RenderKey = Guid.NewGuid();
        StateHasChanged();
        return (T) this;
    }

}

public interface IMudExComponent
{
}

internal interface IJsMudExComponent<T>: IMudExComponent, IAsyncDisposable
{
    public IJSRuntime JsRuntime { get; }

    public IJSObjectReference JsReference { get; set; }
    public IJSObjectReference ModuleReference { get; set; }
    public ElementReference ElementReference { get; set; }

    public virtual object[] GetJsArguments()
    {
        return new object[] { ElementReference, CreateDotNetObjectReference() };
    }

    public virtual DotNetObjectReference<IJsMudExComponent<T>> CreateDotNetObjectReference()
    {
        return DotNetObjectReference.Create(this);
    }

    public virtual async Task ImportModuleAndCreateJsAsync(string name = null)
    {
        var references = await JsRuntime.ImportModuleAndCreateJsAsync<T>(name, GetJsArguments());
        JsReference = references.jsObjectReference;
        ModuleReference = references.moduleReference;
    }

    public virtual async ValueTask DisposeModulesAsync()
    {
        if (JsReference != null)
        {
            try
            {
                await JsReference.InvokeVoidAsync("dispose");
            }
            catch
            { }
            await JsReference.DisposeAsync();
        }

        if (ModuleReference != null)
            await ModuleReference.DisposeAsync();

    }

}