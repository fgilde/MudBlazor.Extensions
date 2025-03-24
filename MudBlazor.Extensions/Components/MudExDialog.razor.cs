using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// MudExDialog is the component to use when you want to show a dialog inlined in your page with all DialogExtensions.
/// </summary>
public partial class MudExDialog : IMudExComponent, IAsyncDisposable 
{
    private string _dialogId;
    private DialogOptionsEx _options;
    private IDialogReference _reference;
    
    [Inject] private IMudExDialogService DialogService { get; set; }
    [Inject] private IJSRuntime Js { get; set; }
    [Inject] private MudExAppearanceService AppearanceService { get; set; }
    [Inject] private IDialogEventService DialogEventService { get; set; }

    [Parameter] public EventCallback<DialogClosedEvent> OnClosed { get; set; }
    [Parameter] public EventCallback<DialogDragStartEvent> OnDragStart { get; set; }
    [Parameter] public EventCallback<DialogDraggingEvent> OnDragging { get; set; }
    [Parameter] public EventCallback<DialogDragEndEvent> OnDragEnd { get; set; }
    [Parameter] public EventCallback<DialogResizedEvent> OnDialogResized { get; set; }
    [Parameter] public EventCallback<DialogResizingEvent> OnDialogResize { get; set; }
    [Parameter] public EventCallback<DialogBeforeOpenEvent> OnBeforeOpened { get; set; }
    [Parameter] public EventCallback<DialogBeforeOpenEvent> OnAfterOpened { get; set; }

    
    /// <summary>
    /// Render base component
    /// </summary>
    private RenderFragment Inherited() => builder => base.BuildRenderTree(builder);

    protected override void OnInitialized()
    {
        DialogEventService.Subscribe<DialogBeforeOpenEvent>(HandleBeforeOpen);
        DialogEventService.Subscribe<DialogAfterOpenEvent>(HandleAfterOpen);
        DialogEventService.Subscribe<DialogDragStartEvent>(HandleOnDragStart);
        DialogEventService.Subscribe<DialogDragEndEvent>(HandleOnDragEnd);
        DialogEventService.Subscribe<DialogDraggingEvent>(HandleOnDragging);
        DialogEventService.Subscribe<DialogClosedEvent>(HandleOnDialogClosed);
        DialogEventService.Subscribe<DialogResizedEvent>(HandleOnDialogResized);
        DialogEventService.Subscribe<DialogResizingEvent>(HandleOnDialogResize);
        base.OnInitialized();
    }

    public ValueTask DisposeAsync()
    {
        DialogEventService.Unsubscribe<DialogBeforeOpenEvent>(HandleBeforeOpen);
        DialogEventService.Unsubscribe<DialogAfterOpenEvent>(HandleAfterOpen);
        DialogEventService.Unsubscribe<DialogDragStartEvent>(HandleOnDragStart);
        DialogEventService.Unsubscribe<DialogDragEndEvent>(HandleOnDragEnd);
        DialogEventService.Unsubscribe<DialogDraggingEvent>(HandleOnDragging);
        DialogEventService.Unsubscribe<DialogClosedEvent>(HandleOnDialogClosed);
        DialogEventService.Unsubscribe<DialogResizedEvent>(HandleOnDialogResized);
        DialogEventService.Unsubscribe<DialogResizingEvent>(HandleOnDialogResize);
        return ValueTask.CompletedTask;
    }


    private async Task HandleBeforeOpen(DialogBeforeOpenEvent arg)
    {
        if(!Visible)
            return;
        if (_dialogId == null)
        {
            _dialogId = arg.DialogId;
            _reference = arg.DialogReference;
        }

        if (EventIsForThisDialog(arg))
        {
            await arg.DialogReference.InjectOptionsAsync(DialogService, OptionsEx);
        }
    }

    private Task AfterOpened(DialogAfterOpenEvent arg)
    {
        return OnAfterOpened.InvokeAsync(arg);
    }
    
    private Task Closed(DialogClosedEvent arg)
    {
        _dialogId = null;
        _reference = null;
        return OnClosed.InvokeAsync(arg);
    }


    private Task HandleAfterOpen(DialogAfterOpenEvent arg) => EventIsForThisDialog(arg) ? AfterOpened(arg) : Task.CompletedTask;
    private Task HandleOnDragStart(DialogDragStartEvent arg) => EventIsForThisDialog(arg) ? OnDragStart.InvokeAsync(arg) : Task.CompletedTask;
    private Task HandleOnDragEnd(DialogDragEndEvent arg) => EventIsForThisDialog(arg) ? OnDragEnd.InvokeAsync(arg) : Task.CompletedTask;
    private Task HandleOnDragging(DialogDraggingEvent arg) => EventIsForThisDialog(arg) ? OnDragging.InvokeAsync(arg) : Task.CompletedTask;
    private Task HandleOnDialogClosed(DialogClosedEvent arg) => EventIsForThisDialog(arg) ? Closed(arg) : Task.CompletedTask;
    private Task HandleOnDialogResized(DialogResizedEvent arg) => EventIsForThisDialog(arg) ? OnDialogResized.InvokeAsync(arg) : Task.CompletedTask;
    private Task HandleOnDialogResize(DialogResizingEvent arg) => EventIsForThisDialog(arg) ? OnDialogResize.InvokeAsync(arg) : Task.CompletedTask;
    

    
    private bool EventIsForThisDialog(IDialogEvent dialogEvent)
    {                        
        return _dialogId == dialogEvent.DialogId;
    }

    /// <summary>
    /// DialogOptionsEx for this dialog
    /// </summary>
    [Parameter]
    public DialogOptionsEx OptionsEx
    {
        get => _options ??= DialogOptionsEx.DefaultDialogOptions;
        set
        {
            _options = value;
            Options = value;
        }
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue<DialogOptions>(nameof(Class), out var cls))
        {
        }

        if (parameters.TryGetValue<DialogOptions>(nameof(Options), out var options))
        {
            _options = options; 
        }

        var updateRequired = parameters.TryGetValue<bool>(nameof(Visible), out var visible) && Visible != visible;
        await base.SetParametersAsync(parameters);
        if (updateRequired)
        {
            if (Visible && _reference == null)
            {
                _reference = await ShowDialogAsync();
            }
            else if (!Visible && _reference != null)
            {
                _reference?.Close();
                _reference = null;
                _dialogId = null;
                await InvokeAsync(StateHasChanged);

            }
        }
    }

    private async Task<IDialogReference> ShowDialogAsync()
    {
        var cls = Class;
        OptionsEx.JsRuntime ??= Js;
        await DialogServiceExt.PrepareOptionsBeforeShow(OptionsEx);
        var appliedOptions = DialogServiceExt.PrepareOptionsAfterShow(OptionsEx);
        Class = $"{EnsureInitialClass()} {appliedOptions?.DialogAppearance?.Class}";
        var result = await ShowAsync();
        Class = cls;
        return result;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Don't call base.OnAfterRenderAsync to avoid double rendering
    }

    public new Task CloseAsync(DialogResult? result = null)
    {
        if (_reference is null)
        {
            Visible = false;
            return Task.CompletedTask;
        }
                
        _reference.CloseAnimatedIf(result);
        _reference = null;
        
        return Task.CompletedTask;
    }

    private string EnsureInitialClass()
    {
        if (string.IsNullOrEmpty(Class))
            return MudExCss.Classes.Dialog._Initial;
        if (!Class.Contains(MudExCss.Classes.Dialog._Initial))
            return Class + MudExCss.Classes.Dialog._Initial;
        return Class;
    }

    public Task InvokeStateHasChanged() => InvokeAsync(StateHasChanged);

}