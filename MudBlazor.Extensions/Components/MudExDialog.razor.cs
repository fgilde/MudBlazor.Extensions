using System.Reflection;
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
    //private IDialogReference _reference;

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

    /// <summary>
    /// Render base component
    /// </summary>
    private RenderFragment Inherited() => builder => base.BuildRenderTree(builder);

    protected override void OnInitialized()
    {        
        DialogEventService.Subscribe<DialogDragStartEvent>(HandleOnDragStart);
        DialogEventService.Subscribe<DialogDragEndEvent>(HandleOnDragEnd);
        DialogEventService.Subscribe<DialogDraggingEvent>(HandleOnDragging);
        DialogEventService.Subscribe<DialogClosedEvent>(HandleOnDialogClosed);
        DialogEventService.Subscribe<DialogResizedEvent>(HandleOnDialogResized);
        DialogEventService.Subscribe<DialogResizingEvent>(HandleOnDialogResize);
        base.OnInitialized();
    }

    private Task HandleOnDragStart(DialogDragStartEvent arg) => EventIsForThisDialog(arg) ? OnDragStart.InvokeAsync(arg) : Task.CompletedTask;
    private Task HandleOnDragEnd(DialogDragEndEvent arg) => EventIsForThisDialog(arg) ? OnDragEnd.InvokeAsync(arg) : Task.CompletedTask;
    private Task HandleOnDragging(DialogDraggingEvent arg) => EventIsForThisDialog(arg) ? OnDragging.InvokeAsync(arg) : Task.CompletedTask;
    private Task HandleOnDialogClosed(DialogClosedEvent arg) => EventIsForThisDialog(arg) ? OnClosed.InvokeAsync(arg) : Task.CompletedTask;
    private Task HandleOnDialogResized(DialogResizedEvent arg) => EventIsForThisDialog(arg) ? OnDialogResized.InvokeAsync(arg) : Task.CompletedTask;
    private Task HandleOnDialogResize(DialogResizingEvent arg) => EventIsForThisDialog(arg) ? OnDialogResize.InvokeAsync(arg) : Task.CompletedTask;
    

    public ValueTask DisposeAsync()
    {
        DialogEventService.Unsubscribe<DialogDragStartEvent>(HandleOnDragStart);
        DialogEventService.Unsubscribe<DialogDragEndEvent>(HandleOnDragEnd);
        DialogEventService.Unsubscribe<DialogDraggingEvent>(HandleOnDragging);
        DialogEventService.Unsubscribe<DialogClosedEvent>(HandleOnDialogClosed);
        DialogEventService.Unsubscribe<DialogResizedEvent>(HandleOnDialogResized);
        DialogEventService.Unsubscribe<DialogResizingEvent>(HandleOnDialogResize);        
        return ValueTask.CompletedTask;
    }
    
    private bool EventIsForThisDialog(IDialogEvent dialogEvent)
    {                        
        return _dialogId == dialogEvent.DialogId;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        EnsureInitialClass();
        base.OnParametersSet();
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue<DialogOptions>(nameof(Options), out var options) && options is DialogOptionsEx ex)
            _options = ex;        
        bool oldVisible = Visible;
        await base.SetParametersAsync(parameters);
        if (oldVisible != Visible)
        {
            if (Visible)
                await Show();
            else
                await CloseAsync();
        }
    }


    /// <summary>Show this inlined dialog</summary>
    /// <param name="title"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public new async Task<IDialogReference> Show(string title = null, DialogOptions options = null)
    {
        void OnAdded(IDialogReference reference)
        {
            _dialogId = reference.GetDialogId();
            DialogService.OnDialogInstanceAdded -= OnAdded;
        }

        DialogService.OnDialogInstanceAdded += OnAdded;        
        
        OptionsEx.JsRuntime = Js;
        OptionsEx.AppearanceService = AppearanceService;
        await DialogServiceExt.PrepareOptionsBeforeShow(OptionsEx);
        Task<IDialogReference> x = ShowAsync(title, options);
        return await x.InjectOptionsAsync(DialogService, OptionsEx);
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

    private void EnsureInitialClass()
    {
        if (string.IsNullOrEmpty(Class))
            Class = "mud-ex-dialog-initial";
        if (!Class.Contains("mud-ex-dialog-initial"))
            Class += " mud-ex-dialog-initial";
    }

}