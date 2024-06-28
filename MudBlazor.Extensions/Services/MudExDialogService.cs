using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Interop;

namespace MudBlazor.Extensions.Services;

/// <summary>
/// Overriden DialogService to provide additional functionality
/// </summary>
public class MudExDialogService : IMudExDialogService
{
    private readonly IDialogService _innerDialogService;
    private readonly IDialogEventService _dialogEventService;

    [JSInvokable]
    public async Task<object> PublishEvent(string eventName, string dialogId, DotNetObjectReference<ComponentBase> dialog, BoundingClientRect rect)
    {        
        dynamic eventToPublish = eventName switch
        {
            "OnDragStart" => new DialogDragStartEvent { Dialog = dialog.Value, Rect = rect, DialogId = dialogId },
            "OnDragEnd" => new DialogDragEndEvent { Dialog = dialog.Value, Rect = rect, DialogId = dialogId },
            "OnDragging" => new DialogDraggingEvent { Dialog = dialog.Value, Rect = rect, DialogId = dialogId },
            "OnResizing" => new DialogResizingEvent { Dialog = dialog.Value, Rect = rect, DialogId = dialogId },
            "OnResized" => new DialogResizedEvent { Dialog = dialog.Value, Rect = rect, DialogId = dialogId },
            "OnDialogClosing" => new DialogClosingEvent { Dialog = dialog.Value, Rect = rect, DialogId = dialogId },
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
    public MudExDialogService(IDialogService innerDialogService, IJSRuntime jsRuntime, 
        IServiceProvider serviceProvider, MudExAppearanceService appearanceService, 
        IDialogEventService dialogEventService)
    {
        _innerDialogService = innerDialogService;
        _dialogEventService = dialogEventService;
        JSRuntime = jsRuntime;
        ServiceProvider = serviceProvider;
        AppearanceService = appearanceService;
        _innerDialogService.OnDialogInstanceAdded += OnDialogInstanceAddedHandler;
        _innerDialogService.OnDialogCloseRequested += OnDialogCloseRequestedHandler;
    }

    private void OnDialogCloseRequestedHandler(IDialogReference reference, DialogResult result)
    {
        var c = reference.Dialog as ComponentBase;
        _ = _dialogEventService.Publish(new DialogClosedEvent { DialogReference = reference, Dialog = c, Result = result, DialogId = reference.GetDialogId() });
    }

    private async void OnDialogInstanceAddedHandler(IDialogReference obj)
    {
        var id = obj.GetDialogId();
        await _dialogEventService.Publish(new DialogBeforeOpenEvent { DialogReference = obj, DialogId = id });        
        var result = await obj.GetDialogAsync<ComponentBase>();
        await _dialogEventService.Publish(new DialogAfterOpenEvent { DialogReference = obj, Dialog = result, DialogId = id });
        var dlgResult = await obj.Result;
        OnDialogCloseRequestedHandler(obj, dlgResult);
    }

    #region Delegating Implementation

    /// <inheritdoc />
    public IDialogReference Show<TComponent>() where TComponent : IComponent => _innerDialogService.Show<TComponent>();

    /// <inheritdoc />
    public IDialogReference Show<TComponent>(string title) where TComponent : IComponent => _innerDialogService.Show<TComponent>(title);

    /// <inheritdoc />
    public IDialogReference Show<TComponent>(string title, DialogOptions options) where TComponent : IComponent => _innerDialogService.Show<TComponent>(title, options);

    /// <inheritdoc />
    public IDialogReference Show<TComponent>(string title, DialogParameters parameters) where TComponent : IComponent => _innerDialogService.Show<TComponent>(title, parameters);

    /// <inheritdoc />
    public IDialogReference Show<TComponent>(string title, DialogParameters parameters, DialogOptions options) where TComponent : IComponent => _innerDialogService.Show<TComponent>(title, parameters, options);

    /// <inheritdoc />
    public IDialogReference Show(Type component) => _innerDialogService.Show(component);

    /// <inheritdoc />
    public IDialogReference Show(Type component, string title) => _innerDialogService.Show(component, title);

    /// <inheritdoc />
    public IDialogReference Show(Type component, string title, DialogOptions options)
        => _innerDialogService.Show(component, title, options);

    /// <inheritdoc />
    public IDialogReference Show(Type component, string title, DialogParameters parameters)
        => _innerDialogService.Show(component, title, parameters);

    /// <inheritdoc />
    public IDialogReference Show(Type component, string title, DialogParameters parameters, DialogOptions options)
        => _innerDialogService.Show(component, title, parameters, options);

    /// <inheritdoc />
    public Task<IDialogReference> ShowAsync<TComponent>() where TComponent : IComponent
        => _innerDialogService.ShowAsync<TComponent>();

    /// <inheritdoc />
    public Task<IDialogReference> ShowAsync<TComponent>(string title) where TComponent : IComponent
        => _innerDialogService.ShowAsync<TComponent>(title);

    /// <inheritdoc />
    public Task<IDialogReference> ShowAsync<TComponent>(string title, DialogOptions options) where TComponent : IComponent
        => _innerDialogService.ShowAsync<TComponent>(title, options);

    /// <inheritdoc />
    public Task<IDialogReference> ShowAsync<TComponent>(string title, DialogParameters parameters) where TComponent : IComponent
        => _innerDialogService.ShowAsync<TComponent>(title, parameters);

    /// <inheritdoc />
    public Task<IDialogReference> ShowAsync<TComponent>(string title, DialogParameters parameters, DialogOptions options) where TComponent : IComponent
        => _innerDialogService.ShowAsync<TComponent>(title, parameters, options);

    /// <inheritdoc />
    public Task<IDialogReference> ShowAsync(Type component)
        => _innerDialogService.ShowAsync(component);

    /// <inheritdoc />
    public Task<IDialogReference> ShowAsync(Type component, string title)
        => _innerDialogService.ShowAsync(component, title);

    /// <inheritdoc />
    public Task<IDialogReference> ShowAsync(Type component, string title, DialogOptions options)
        => _innerDialogService.ShowAsync(component, title, options);

    /// <inheritdoc />
    public Task<IDialogReference> ShowAsync(Type component, string title, DialogParameters parameters)
        => _innerDialogService.ShowAsync(component, title, parameters);

    /// <inheritdoc />
    public Task<IDialogReference> ShowAsync(Type component, string title, DialogParameters parameters, DialogOptions options)
        => _innerDialogService.ShowAsync(component, title, parameters, options);

    /// <inheritdoc />
    public IDialogReference CreateReference()
        => _innerDialogService.CreateReference();

    /// <inheritdoc />
    public Task<bool?> ShowMessageBox(string title, string message, string yesText = "OK", string noText = null,
        string cancelText = null, DialogOptions options = null)
        => _innerDialogService.ShowMessageBox(title, message, yesText, noText, cancelText, options);

    /// <inheritdoc />
    public Task<bool?> ShowMessageBox(string title, MarkupString markupMessage, string yesText = "OK", string noText = null,
        string cancelText = null, DialogOptions options = null)
        => _innerDialogService.ShowMessageBox(title, markupMessage, yesText, noText, cancelText, options);

    /// <inheritdoc />
    public Task<bool?> ShowMessageBox(MessageBoxOptions messageBoxOptions, DialogOptions options = null)
        => _innerDialogService.ShowMessageBox(messageBoxOptions, options);

    /// <inheritdoc />
    public void Close(DialogReference dialog)
        => _innerDialogService.Close(dialog);

    /// <inheritdoc />
    public void Close(DialogReference dialog, DialogResult result)
        => _innerDialogService.Close(dialog, result);

    /// <inheritdoc />
    public event Action<IDialogReference> OnDialogInstanceAdded
    {
        add => _innerDialogService.OnDialogInstanceAdded += value;
        remove => _innerDialogService.OnDialogInstanceAdded -= value;
    }

    /// <inheritdoc />
    public event Action<IDialogReference, DialogResult> OnDialogCloseRequested
    {
        add => _innerDialogService.OnDialogCloseRequested += value;
        remove => _innerDialogService.OnDialogCloseRequested -= value;
    }

    #endregion
}