using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Nextended.Core;
using Nextended.Core.Attributes;

namespace MudBlazor.Extensions.Services;

/// <summary>
/// Service class to manage and apply appearances to components.
/// </summary>
[RegisterAs(typeof(MudExAppearanceService), ServiceLifetime = ServiceLifetime.Scoped)]
public class MudExAppearanceService
{
    private string GetClass(IMudExAppearance appearance) => appearance switch { IMudExClassAppearance classAppearance => classAppearance.Class, _ => string.Empty };
    private string GetStyle(IMudExAppearance appearance) => appearance switch { IMudExStyleAppearance styleAppearance => styleAppearance.Style, _ => string.Empty };
    private IJSRuntime GetRuntime() => JSRuntime ?? JsImportHelper.GetInitializedJsRuntime();

    private IJSRuntime JSRuntime { get; }

    /// <summary>
    /// Creates a new instance of the service.
    /// </summary>
    public MudExAppearanceService(IJSRuntime jsRuntime)
    {
        JSRuntime = jsRuntime;
    }

    internal MudExAppearanceService() : this(null)
    { }

    /// <summary>
    /// Applies the specified appearance to a MudBlazor component temporarily.
    /// </summary>
    /// <returns>The applied appearance.</returns>
    public async Task<TAppearance> ApplyTemporarilyToAsync<TAppearance>(TAppearance appearance, MudComponentBase component, Func<bool> expression, bool keepExisting = true, CancellationToken ct = default)
        where TAppearance : IMudExAppearance
    {

        await ApplyToAsync(appearance, component, keepExisting);
        await expression.WaitForTrueAsync(ct);
        await RemoveFromAsync(appearance, component);

        return appearance;
    }

    /// <summary>
    /// Applies the specified appearance to a ElementReference temporarily.
    /// </summary>
    /// <returns>The applied appearance.</returns>
    public async Task<TAppearance> ApplyTemporarilyToAsync<TAppearance>(TAppearance appearance, ElementReference elementRef, Func<bool> expression, bool keepExisting = true, CancellationToken ct = default)
        where TAppearance : IMudExAppearance
    {
        await ApplyToAsync(appearance, elementRef, keepExisting, ct);
        await expression.WaitForTrueAsync(ct);
        await RemoveFromAsync(appearance, elementRef, ct);

        return appearance;
    }

    /// <summary>
    /// Applies the specified appearance to a element that will searched by a selector temporarily.
    /// </summary>
    /// <returns>The applied appearance.</returns>
    public async Task<TAppearance> ApplyTemporarilyToAsync<TAppearance>(TAppearance appearance, string elementSelector, Func<bool> expression, bool keepExisting = true, CancellationToken ct = default)
        where TAppearance : IMudExAppearance
    {
        await ApplyToAsync(appearance, elementSelector, keepExisting, ct);
        await expression.WaitForTrueAsync(ct);
        await RemoveFromAsync(appearance, elementSelector, ct);

        return appearance;
    }

    /// <summary>
    /// Applies the specified appearance to a dialog reference temporarily.
    /// </summary>
    /// <returns>The applied appearance.</returns>
    public async Task<TAppearance> ApplyTemporarilyToAsync<TAppearance>(TAppearance appearance, IDialogReference dlg, Func<bool> expression, bool keepExisting = true, CancellationToken ct = default)
        where TAppearance : IMudExAppearance
    {
        await ApplyToAsync(appearance, dlg, keepExisting, ct);
        await expression.WaitForTrueAsync(ct);
        await RemoveFromAsync(appearance, dlg, ct);

        return appearance;
    }

    /// <summary>
    /// Applies the specified appearance to a MudBlazor component temporarily.
    /// </summary>
    /// <returns>The applied appearance.</returns>
    public async Task<TAppearance> ApplyTemporarilyToAsync<TAppearance>(TAppearance appearance, MudComponentBase component, TimeSpan? duration = null, bool keepExisting = true, CancellationToken ct = default)
        where TAppearance : IMudExAppearance
    {
        await ApplyToAsync(appearance, component, keepExisting);
        await Task.Delay(duration ?? TimeSpan.FromSeconds(1), ct);
        await RemoveFromAsync(appearance, component);

        return appearance;
    }

    /// <summary>
    /// Applies the specified appearance to a ElementReference temporarily.
    /// </summary>
    /// <returns>The applied appearance.</returns>
    public async Task<TAppearance> ApplyTemporarilyToAsync<TAppearance>(TAppearance appearance, ElementReference elementRef, TimeSpan? duration = null, bool keepExisting = true, CancellationToken ct = default)
        where TAppearance : IMudExAppearance
    {
        await ApplyToAsync(appearance, elementRef, keepExisting, ct);
        await Task.Delay(duration ?? TimeSpan.FromSeconds(1), ct);
        await RemoveFromAsync(appearance, elementRef, ct);

        return appearance;
    }

    /// <summary>
    /// Applies the specified appearance to a element that will searched by a selector temporarily.
    /// </summary>
    /// <returns>The applied appearance.</returns>
    public async Task<TAppearance> ApplyTemporarilyToAsync<TAppearance>(TAppearance appearance, string elementSelector, TimeSpan? duration = null, bool keepExisting = true, CancellationToken ct = default)
        where TAppearance : IMudExAppearance
    {
        await ApplyToAsync(appearance, elementSelector, keepExisting, ct);
        await Task.Delay(duration ?? TimeSpan.FromSeconds(1), ct);
        await RemoveFromAsync(appearance, elementSelector, ct);

        return appearance;
    }

    /// <summary>
    /// Applies the specified appearance to a dialog reference temporarily.
    /// </summary>
    /// <returns>The applied appearance.</returns>
    public async Task<TAppearance> ApplyTemporarilyToAsync<TAppearance>(TAppearance appearance, IDialogReference dlg, TimeSpan? duration = null, bool keepExisting = true, CancellationToken ct = default)
        where TAppearance : IMudExAppearance
    {
        await ApplyToAsync(appearance, dlg, keepExisting, ct);
        await Task.Delay(duration ?? TimeSpan.FromSeconds(1), ct);
        await RemoveFromAsync(appearance, dlg, ct);

        return appearance;
    }


    /// <summary>
    /// Applies the specified appearance to a component, updating only its class attribute.
    /// </summary>
    /// <param name="appearance">The appearance to apply.</param>
    /// <param name="component">The component to which the appearance will be applied.</param>
    /// <param name="updateFunc">Function to update the class of the component.</param>
    /// <param name="ct">Token to cancel the JS-side class rule build.</param>
    /// <returns>The applied appearance.</returns>
    public async Task<TAppearance> ApplyAsClassOnlyToAsync<TAppearance, TComponent>(TAppearance appearance, TComponent component, Action<TComponent, string> updateFunc, CancellationToken ct = default)
        where TAppearance : IMudExAppearance
    {
        if (appearance != null)
        {
            var cls = GetClass(appearance);
            var style = GetStyle(appearance);
            if (!string.IsNullOrEmpty(style))
            {
                var className = await MudExStyleBuilder.FromStyle(style).BuildAsClassRuleAsync(null, GetRuntime());
                cls = $"{cls} {className}";
            }

            updateFunc(component, cls);
        }

        return appearance;
    }

    /// <summary>
    /// Applies the specified appearance to a MudBlazor component.
    /// </summary>
    /// <param name="appearance">The appearance to apply.</param>
    /// <param name="component">The component to which the appearance will be applied.</param>
    /// <param name="keepExisting">Flag indicating whether to keep existing class and style attributes.</param>
    /// <returns>The applied appearance.</returns>
    public Task<TAppearance> ApplyToAsync<TAppearance>(TAppearance appearance, MudComponentBase component, bool keepExisting = true)
        where TAppearance : IMudExAppearance
    {
        if (component != null)
        {
            var style = GetStyle(appearance);
            var cls = GetClass(appearance);
            if (!string.IsNullOrEmpty(cls))
                component.Class = keepExisting ? $"{component.Class} {cls}" : cls;
            if (!string.IsNullOrEmpty(style))
                component.Style = keepExisting ? $"{component.Style} {style}" : style;
        }

        return Task.FromResult(appearance);
    }

    /// <summary>
    /// Applies the specified appearance to an HTML element using its selector.
    /// </summary>
    /// <param name="appearance">The appearance to apply.</param>
    /// <param name="elementSelector">The selector of the element to which the appearance will be applied.</param>
    /// <param name="keepExisting">Flag indicating whether to keep existing class and style attributes.</param>
    /// <param name="ct">Token to cancel the JS call.</param>
    /// <returns>The applied appearance.</returns>
    public async Task<TAppearance> ApplyToAsync<TAppearance>(TAppearance appearance, string elementSelector, bool keepExisting = true, CancellationToken ct = default) where TAppearance : IMudExAppearance
    {
        if (string.IsNullOrEmpty(elementSelector))
            return appearance;

        await GetRuntime().InvokeVoidAsync("MudExCssHelper.setElementAppearance", ct, elementSelector, GetClass(appearance), GetStyle(appearance), keepExisting);
        return appearance;
    }

    /// <summary>
    /// Applies the specified appearance to an HTML element using its reference.
    /// </summary>
    /// <param name="appearance">The appearance to apply.</param>
    /// <param name="elementRef">Reference to element</param>
    /// <param name="keepExisting">Flag indicating whether to keep existing class and style attributes.</param>
    /// <param name="ct">Token to cancel the JS call.</param>
    /// <returns>The applied appearance.</returns>
    public async Task<TAppearance> ApplyToAsync<TAppearance>(TAppearance appearance, ElementReference elementRef, bool keepExisting = true, CancellationToken ct = default) where TAppearance : IMudExAppearance
    {
        await GetRuntime().InvokeVoidAsync("MudExCssHelper.setElementAppearanceOnElement", ct, elementRef, GetClass(appearance), GetStyle(appearance), keepExisting);
        return appearance;
    }


    /// <summary>
    ///  Applies the specified appearance to a dialog
    /// </summary>
    public async Task<TAppearance> ApplyToAsync<TAppearance>(TAppearance appearance, IDialogReference dialogReference, bool keepExisting = true, CancellationToken ct = default) where TAppearance : IMudExAppearance
    {
        //if (dialogReference.Dialog is MudComponentBase componentBase)
        //    return await ApplyToAsync(appearance, componentBase, keepExisting);

        var component = await dialogReference.GetDialogAsync<ComponentBase>();
        if (component is MudDialog dlg)
        {
            return await ApplyToAsync(appearance, dlg, keepExisting);
        }

        var id = dialogReference?.GetDialogId();
        return await ApplyToAsync(appearance, $"#{id}", keepExisting, ct);
    }

    /// <summary>
    /// Removes the specified appearance from a MudBlazor component.
    /// </summary>
    /// <param name="appearance">The appearance to remove.</param>
    /// <param name="component">The component from which the appearance will be removed.</param>
    /// <returns>The removed appearance.</returns>
    public Task<TAppearance> RemoveFromAsync<TAppearance>(TAppearance appearance, MudComponentBase component) where TAppearance : IMudExAppearance
    {
        if (component != null)
        {
            var style = GetStyle(appearance);
            var cls = GetClass(appearance);
            if (!string.IsNullOrEmpty(cls))
                component.Class = component.Class?.Replace(cls, string.Empty).Trim();
            if (!string.IsNullOrEmpty(style))
                component.Style = component.Style?.Replace(style, string.Empty).Trim();
        }

        return Task.FromResult(appearance);
    }

    /// <summary>
    /// Removes the specified appearance from an HTML element using its selector.
    /// </summary>
    /// <param name="appearance">The appearance to remove.</param>
    /// <param name="elementSelector">The selector of the element from which the appearance will be removed.</param>
    /// <param name="ct">Token to cancel the JS call.</param>
    /// <returns>The removed appearance.</returns>
    public async Task<TAppearance> RemoveFromAsync<TAppearance>(TAppearance appearance, string elementSelector, CancellationToken ct = default) where TAppearance : IMudExAppearance
    {
        if (string.IsNullOrEmpty(elementSelector))
            return appearance;

        await GetRuntime().InvokeVoidAsync("MudExCssHelper.removeElementAppearance", ct, elementSelector, GetClass(appearance), GetStyle(appearance));
        return appearance;
    }

    /// <summary>
    /// Removes the specified appearance from an HTML element using its ElementReference.
    /// </summary>
    /// <param name="appearance">The appearance to remove.</param>
    /// <param name="elementRef">The ElementReference of the HTML element from which the appearance will be removed.</param>
    /// <param name="ct">Token to cancel the JS call.</param>
    /// <returns>The removed appearance.</returns>
    public async Task<TAppearance> RemoveFromAsync<TAppearance>(TAppearance appearance, ElementReference elementRef, CancellationToken ct = default) where TAppearance : IMudExAppearance
    {
        await GetRuntime().InvokeVoidAsync("MudExCssHelper.removeElementAppearanceOnElement", ct, elementRef, GetClass(appearance), GetStyle(appearance));
        return appearance;
    }

    /// <summary>
    /// Removes the specified appearance from a dialog.
    /// </summary>
    /// <param name="appearance">The appearance to remove.</param>
    /// <param name="dialogReference">The reference to the dialog from which the appearance will be removed.</param>
    /// <param name="ct">Token to cancel the JS call.</param>
    /// <returns>The removed appearance.</returns>
    public async Task<TAppearance> RemoveFromAsync<TAppearance>(TAppearance appearance, IDialogReference dialogReference, CancellationToken ct = default) where TAppearance : IMudExAppearance
    {
        if (dialogReference.Dialog is MudComponentBase componentBase)
            return await RemoveFromAsync(appearance, componentBase);

        var id = dialogReference.GetDialogId();
        return await RemoveFromAsync(appearance, $"#{id}", ct);
    }

}
