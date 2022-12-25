using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components.Base;

public class MudExBaseComponent<T> : ComponentBase 
    where T : MudExBaseComponent<T>
{
    [Inject] protected IServiceProvider ServiceProvider { get; set; }
    [Inject] protected IJSRuntime JsRuntime { get; set; }
    [Parameter] public IStringLocalizer Localizer { get; set; }
    private IStringLocalizer<T> _fallbackLocalizer => Get<IStringLocalizer<T>>();
    protected IDialogService DialogService => Get<IDialogService>();
    protected IStringLocalizer LocalizerToUse => Localizer ?? _fallbackLocalizer;

    protected TService Get<TService>() => ServiceProvider.GetService<TService>();
    protected IEnumerable<TService> GetServices<TService>() => ServiceProvider.GetServices<TService>();
    protected string TryLocalize(string text, params object[] args) => LocalizerToUse.TryLocalize(text, args);
}
