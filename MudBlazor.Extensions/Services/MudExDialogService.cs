using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Interop;
using Nextended.Core.Attributes;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Services;

/// <summary>
/// Overriden DialogService to provide additional functionality
/// </summary>
[RegisterAs(typeof(IMudExDialogService), RegisterAsImplementation = true, ServiceLifetime = ServiceLifetime.Scoped)]
public class MudExDialogService : DialogService, IMudExDialogService
{
    private readonly IDialogEventService _dialogEventService;

    private readonly ConcurrentDictionary<Guid, (IDialogReference DialogReference, DialogOptionsEx Options)> _dialogs =
        new();

    [JSInvokable]
    public async Task<object> PublishEvent(string eventName, string dialogId,
        DotNetObjectReference<ComponentBase> dialog, BoundingClientRect rect)
    {
        
        dynamic eventToPublish = eventName switch
        {
            "OnDragStart" => new DialogDragStartEvent { Dialog = dialog.Value, Rect = rect, DialogId = dialogId },
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
            //if (eventToPublish is DialogClosingEvent closingEvent && closingEvent.Cancel != true && !closingEvent.DialogOptions.Modal)
            //    WorkaroundClosingNonModalDialog(closingEvent.Dialog);
            
        }

        return null;
    }

    #region NoModel Close
 
    MudDialogProvider _provider;


    public override IDialogReference CreateReference()
    {
        var result = base.CreateReference();

        return result;
    }

    private void InitProvider(MudDialogProvider findMudDialogProvider)
    {
        _provider = findMudDialogProvider;
    }


    private void WorkaroundClosingNonModalDialog(ComponentBase dialogComponent)
    {
        var dlgInstance = dialogComponent.FindMudDialogInstance();
        WorkaroundClosingNonModalDialog(dlgInstance.GetDialogReference());
    }

    private void WorkaroundClosingNonModalDialog(IDialogReference dialogRef)
    {
        MudDialogProvider provider = dialogRef.FindMudDialogProvider();
        List<IDialogReference> dialogs = provider.ExposeField<List<IDialogReference>>("_dialogs");
        bool hasDismissed = dialogRef.Dismiss(DialogResult.Cancel());
        if (hasDismissed && dialogs.Contains(dialogRef))
        {
            dialogs.Remove(dialogRef);
        }
    }

    #endregion


    private Task PublishDynamic<TEvent>(TEvent eventToPublish) where TEvent : IDialogEvent
    {
        eventToPublish.DialogOptions = GetDialogUsedDialogOptions(eventToPublish.DialogGuid);
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

    /// <inheritdoc />
    public IDialogReference GetDialogReference(Guid dialogGuid) => _dialogs.TryGetValue(dialogGuid, out var dialog) ? dialog.DialogReference : null;

    /// <inheritdoc />
    public DialogOptionsEx GetDialogUsedDialogOptions(Guid dialogGuid) => _dialogs.TryGetValue(dialogGuid, out var dialog) ? dialog.Options : null;

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
        var options = GetDialogUsedDialogOptions(reference.Id);
        if (options is { Modal: false })
        {
            _ = JSRuntime.InvokeVoidAsync("MudExDialogNoModalHandler.closeNonModal", reference.GetDialogId());
        }

        Remove(reference.Id);
        var c = reference.Dialog as ComponentBase;
        _ = _dialogEventService.Publish(new DialogClosedEvent { DialogOptions = options, DialogReference = reference, Dialog = c, Result = result, DialogId = reference.GetDialogId() });
    }

    private async Task DialogInstanceAddedAsyncHandler(IDialogReference obj)
    {
        var id = obj.GetDialogId();
        
        await _dialogEventService.Publish(new DialogBeforeOpenEvent { DialogReference = obj, DialogId = id });
        var result = await obj.GetDialogAsync<ComponentBase>();
        if(_provider == null)
            InitProvider(result.FindMudDialogProvider());
        var options = GetDialogUsedDialogOptions(obj.Id);
        await _dialogEventService.Publish(new DialogAfterOpenEvent { DialogOptions = options, DialogReference = obj, Dialog = result, DialogId = id });
        var dlgResult = await obj.Result;
        OnDialogCloseRequestedHandler(obj, dlgResult);
    }



    internal void AddOrUpdate(Guid dialogReferenceId, IDialogReference dialogReference, DialogOptionsEx options)
    {
        var opt = (options ?? DialogOptionsEx.DefaultDialogOptions).CloneOptions();
        _dialogs.AddOrUpdate(dialogReferenceId, (dialogReference, opt),
            (key, oldValue) => (dialogReference, opt));
    }

    internal void Remove(Guid dialogReferenceId)
    {
        _dialogs.TryRemove(dialogReferenceId, out _);
    }
}