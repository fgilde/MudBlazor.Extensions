using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;
using System.Reflection.Metadata;

namespace MudBlazor.Extensions.Components.Base;

public class MudExBaseComponent<T> : ComponentBase, IMudExComponent
    where T : MudExBaseComponent<T>
{
    [Inject] protected IServiceProvider ServiceProvider { get; set; }
    [Parameter] public IStringLocalizer Localizer { get; set; }
    private IStringLocalizer<T> _fallbackLocalizer => Get<IStringLocalizer<T>>();
    protected IDialogService DialogService => Get<IDialogService>();
    public IJSRuntime JsRuntime => Get<IJSRuntime>();
    protected IStringLocalizer LocalizerToUse => Localizer ?? _fallbackLocalizer;

    protected TService Get<TService>() => ServiceProvider.GetService<TService>();
    protected IEnumerable<TService> GetServices<TService>() => ServiceProvider.GetServices<TService>();
    public string TryLocalize(string text, params object[] args) => LocalizerToUse.TryLocalize(text, args);
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

    public virtual async Task ImportModuleAndCreateJsAsync()
    {
        var references = await JsRuntime.ImportModuleAndCreateJsAsync<T>(GetJsArguments());
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