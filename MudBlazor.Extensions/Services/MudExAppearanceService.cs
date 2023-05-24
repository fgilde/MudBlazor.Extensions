using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Services;

public class MudExAppearanceService
{
    private string GetClass(IMudExAppearance appearance) => appearance switch { IMudExClassAppearance classAppearance => classAppearance.Class, _ => string.Empty };
    private string GetStyle(IMudExAppearance appearance) => appearance switch { IMudExStyleAppearance styleAppearance => styleAppearance.Style, _ => string.Empty };

    public async Task<TAppearance> ApplyAsClassOnlyToAsync<TAppearance, TComponent>(TAppearance appearance, TComponent component, Action<TComponent, string> updateFunc)
        where TAppearance : IMudExAppearance
    {
        if (appearance != null)
        {
            var cls = GetClass(appearance);
            var style = GetStyle(appearance);
            if (!string.IsNullOrEmpty(style))
            {
                var className = await MudExStyleBuilder.FromStyle(style).BuildAsClassRuleAsync();
                cls = $"{cls} {className}";
            }

            updateFunc(component, cls);
        }

        return appearance;
    }

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

    public async Task<TAppearance> ApplyToAsync<TAppearance>(TAppearance appearance, string elementSelector, bool keepExisting = true) where TAppearance : IMudExAppearance
    {
        if (string.IsNullOrEmpty(elementSelector))
            return appearance;

        await GetRuntime().InvokeVoidAsync("MudExCssHelper.setElementAppearance", elementSelector, GetClass(appearance), GetStyle(appearance), keepExisting);
        return appearance;
    }

    private IJSRuntime GetRuntime() => JsImportHelper.GetInitializedJsRuntime();

    public async Task<TAppearance> ApplyToAsync<TAppearance>(TAppearance appearance, ElementReference elementRef, bool keepExisting = true) where TAppearance : IMudExAppearance
    {
        await GetRuntime().InvokeVoidAsync("MudExCssHelper.setElementAppearanceOnElement", elementRef, GetClass(appearance), GetStyle(appearance), keepExisting);
        return appearance;
    }


    public async Task<TAppearance> ApplyToAsync<TAppearance>(TAppearance appearance, IDialogReference dialogReference, bool keepExisting = true) where TAppearance : IMudExAppearance
    {
        if (dialogReference.Dialog is MudComponentBase componentBase)
            return await ApplyToAsync(appearance, componentBase, keepExisting);

        await dialogReference.GetDialogAsync<ComponentBase>();
        var id = dialogReference?.GetDialogId();
        return await ApplyToAsync(appearance, $"#{id}", keepExisting);
    }

    public Task<TAppearance> RemoveFromAsync<TAppearance>(TAppearance appearance, MudComponentBase component) where TAppearance : IMudExAppearance
    {
        if (component != null)
        {
            var style = GetStyle(appearance);
            var cls = GetClass(appearance);
            if (!string.IsNullOrEmpty(cls))
                component.Class = component.Class.Replace(cls, string.Empty).Trim();
            if (!string.IsNullOrEmpty(style))
                component.Style = component.Style.Replace(style, string.Empty).Trim();
        }

        return Task.FromResult(appearance);
    }

    public async Task<TAppearance> RemoveFromAsync<TAppearance>(TAppearance appearance, string elementSelector) where TAppearance : IMudExAppearance
    {
        if (string.IsNullOrEmpty(elementSelector))
            return appearance;

        await GetRuntime().InvokeVoidAsync("MudExCssHelper.removeElementAppearance", elementSelector, GetClass(appearance), GetStyle(appearance));
        return appearance;
    }

    public async Task<TAppearance> RemoveFromAsync<TAppearance>(TAppearance appearance, ElementReference elementRef) where TAppearance : IMudExAppearance
    {
        await GetRuntime().InvokeVoidAsync("MudExCssHelper.removeElementAppearanceOnElement", elementRef, GetClass(appearance), GetStyle(appearance));
        return appearance;
    }

    public async Task<TAppearance> RemoveFromAsync<TAppearance>(TAppearance appearance, IDialogReference dialogReference) where TAppearance : IMudExAppearance
    {
        if (dialogReference.Dialog is MudComponentBase componentBase)
            return await RemoveFromAsync(appearance, componentBase);

        var id = dialogReference?.GetDialogId();
        return await RemoveFromAsync(appearance, $"#{id}");
    }

}