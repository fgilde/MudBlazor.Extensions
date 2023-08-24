using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// MudExBaseInput
/// </summary>
public abstract class MudExBaseInput<T> : MudBaseInput<T>
{
    private IStringLocalizer<T> _fallbackLocalizer => Get<IStringLocalizer<T>>();

    /// <summary>
    /// Localizer for localize texts
    /// </summary>
    [Parameter] public IStringLocalizer Localizer { get; set; }

    /// <summary>
    /// Injected service provider
    /// </summary>
    [Inject] protected IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// JsRuntime
    /// </summary>
    [Inject] public IJSRuntime JsRuntime { get; set; }

    /// <summary>
    /// MudExConfiguration
    /// </summary>
    [Inject] protected MudExConfiguration MudExConfiguration { get; set; }


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

    
    
    /// <summary>
    /// The Adornment if used. By default, it is set to None.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public RenderFragment AdornmentStart { get; set; }

    /// <summary>
    /// The Adornment if used. By default, it is set to None.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public RenderFragment AdornmentEnd { get; set; }

    /// <summary>
    /// ForceShrink
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public bool ForceShrink { get; set; }


    /// <summary>
    /// ChildContentStyle
    /// </summary>
    [Parameter]
    public string ChildContentStyle { get; set; }

    internal virtual InputType GetInputType() => InputType.Text;

    /// <summary>
    /// SkipUpdateProcessOnSetParameters
    /// </summary>
    protected virtual bool SkipUpdateProcessOnSetParameters { get; set; }


    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (SkipUpdateProcessOnSetParameters)        
            return;       
        await base.SetParametersAsync(parameters);

    }

}
