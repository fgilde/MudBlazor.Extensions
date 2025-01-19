using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Interop;
using Nextended.Core.Attributes;

namespace MudBlazor.Extensions.Services;

/// <summary>
/// Overriden DialogService to provide additional functionality
/// </summary>
[RegisterAs(typeof(IMudExDialogService), RegisterAsImplementation = true, ServiceLifetime = ServiceLifetime.Scoped)]
public class MudExDialogService : DialogService, IMudExDialogService
{
    private readonly IDialogService _innerDialogService;
    private readonly IDialogEventService _dialogEventService;

    [JSInvokable]
    public async Task<object> PublishEvent(string eventName, string dialogId, DotNetObjectReference<ComponentBase> dialog, BoundingClientRect rect)
    {        
        dynamic eventToPublish = eventName switch
        {
            "OnDragStart" => new DialogDragStartEvent { Dialog = dialog.Value, Rect = rect, DialogId = dialogId},
            "OnDragEnd" => new DialogDragEndEvent { Dialog = dialog.Value, Rect = rect, DialogId = dialogId },
            "OnDragging" => new DialogDraggingEvent { Dialog = dialog.Value, Rect = rect, DialogId = dialogId },
            "OnResizing" => new DialogResizingEvent { Dialog = dialog.Value, Rect = rect, DialogId = dialogId },
            "OnResized" => new DialogResizedEvent { Dialog = dialog.Value, Rect = rect, DialogId = dialogId },
            "OnDialogClosing" => new DialogClosingEvent { Dialog = dialog.Value, Rect = rect, DialogId = dialogId },
            "OnAnimated" => new DialogAfterAnimationEvent { Dialog = dialog.Value, Rect = rect, DialogId = dialogId },
            _ => null
        };

        if (eventToPublish != null)
        {
            return await PublishDynamic(eventToPublish);
        }

        return null;
    }

    private Task PublishDynamic<TEvent>(TEvent eventToPublish) where TEvent : IDialogEvent
    {
        return _dialogEventService.Publish(eventToPublish);
    }

    /// <summary>
    /// JsRuntime
    /// </summary>
    public IJSRuntime JSRuntime { get; set; }

    /// <summary>
    /// ServiceProvider
    /// </summary>
    public IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// AppearanceService
    /// </summary>
    public MudExAppearanceService AppearanceService { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public MudExDialogService(IJSRuntime jsRuntime, 
        IServiceProvider serviceProvider, MudExAppearanceService appearanceService, 
        IDialogEventService dialogEventService)
    {
        _dialogEventService = dialogEventService;
        JSRuntime = jsRuntime;
        ServiceProvider = serviceProvider;
        AppearanceService = appearanceService;
        DialogInstanceAddedAsync += DialogInstanceAddedAsyncHandler;
        OnDialogCloseRequested += OnDialogCloseRequestedHandler;
    }

    private void OnDialogCloseRequestedHandler(IDialogReference reference, DialogResult result)
    {
        var c = reference.Dialog as ComponentBase;
        _ = _dialogEventService.Publish(new DialogClosedEvent { DialogReference = reference, Dialog = c, Result = result, DialogId = reference.GetDialogId() });
    }

    private async Task DialogInstanceAddedAsyncHandler(IDialogReference obj)
    {
        var id = obj.GetDialogId();        
        await _dialogEventService.Publish(new DialogBeforeOpenEvent { DialogReference = obj, DialogId = id });        
        var result = await obj.GetDialogAsync<ComponentBase>();        
        await _dialogEventService.Publish(new DialogAfterOpenEvent { DialogReference = obj, Dialog = result, DialogId = id });
        var dlgResult = await obj.Result;
        OnDialogCloseRequestedHandler(obj, dlgResult);
    }

}