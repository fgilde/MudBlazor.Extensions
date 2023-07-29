using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Services;

public class MudExDialogService : IMudExDialogService
{
    private readonly IDialogService _innerDialogService;
    public IJSRuntime JSRuntime { get; set; }
    public IServiceProvider ServiceProvider { get; set; }
    public MudExAppearanceService AppearanceService { get; set; }

    public MudExDialogService(IDialogService innerDialogService, IJSRuntime jsRuntime, IServiceProvider serviceProvider, MudExAppearanceService appearanceService)
    {
        _innerDialogService = innerDialogService;
        JSRuntime = jsRuntime;
        ServiceProvider = serviceProvider;
        AppearanceService = appearanceService;
    }
    
    #region Delegating Implementation

    public IDialogReference Show<TComponent>() where TComponent : ComponentBase => _innerDialogService.Show<TComponent>();

    public IDialogReference Show<TComponent>(string title) where TComponent : ComponentBase => _innerDialogService.Show<TComponent>(title);

    public IDialogReference Show<TComponent>(string title, DialogOptions options) where TComponent : ComponentBase => _innerDialogService.Show<TComponent>(title, options);

    public IDialogReference Show<TComponent>(string title, DialogParameters parameters) where TComponent : ComponentBase => _innerDialogService.Show<TComponent>(title, parameters);

    public IDialogReference Show<TComponent>(string title, DialogParameters parameters, DialogOptions options) where TComponent : ComponentBase => _innerDialogService.Show<TComponent>(title, parameters, options);

    public IDialogReference Show(Type component) => _innerDialogService.Show(component);

    public IDialogReference Show(Type component, string title) => _innerDialogService.Show(component, title);

    public IDialogReference Show(Type component, string title, DialogOptions options)
        => _innerDialogService.Show(component, title, options);

    public IDialogReference Show(Type component, string title, DialogParameters parameters)
        => _innerDialogService.Show(component, title, parameters);

    public IDialogReference Show(Type component, string title, DialogParameters parameters, DialogOptions options)
        => _innerDialogService.Show(component, title, parameters, options);

    public Task<IDialogReference> ShowAsync<TComponent>() where TComponent : ComponentBase
        => _innerDialogService.ShowAsync<TComponent>();

    public Task<IDialogReference> ShowAsync<TComponent>(string title) where TComponent : ComponentBase
        => _innerDialogService.ShowAsync<TComponent>(title);

    public Task<IDialogReference> ShowAsync<TComponent>(string title, DialogOptions options) where TComponent : ComponentBase
        => _innerDialogService.ShowAsync<TComponent>(title, options);

    public Task<IDialogReference> ShowAsync<TComponent>(string title, DialogParameters parameters) where TComponent : ComponentBase
        => _innerDialogService.ShowAsync<TComponent>(title, parameters);

    public Task<IDialogReference> ShowAsync<TComponent>(string title, DialogParameters parameters, DialogOptions options) where TComponent : ComponentBase
        => _innerDialogService.ShowAsync<TComponent>(title, parameters, options);

    public Task<IDialogReference> ShowAsync(Type component)
        => _innerDialogService.ShowAsync(component);

    public Task<IDialogReference> ShowAsync(Type component, string title)
        => _innerDialogService.ShowAsync(component, title);

    public Task<IDialogReference> ShowAsync(Type component, string title, DialogOptions options)
        => _innerDialogService.ShowAsync(component, title, options);

    public Task<IDialogReference> ShowAsync(Type component, string title, DialogParameters parameters)
        => _innerDialogService.ShowAsync(component, title, parameters);

    public Task<IDialogReference> ShowAsync(Type component, string title, DialogParameters parameters, DialogOptions options)
        => _innerDialogService.ShowAsync(component, title, parameters, options);

    public IDialogReference CreateReference()
        => _innerDialogService.CreateReference();

    public Task<bool?> ShowMessageBox(string title, string message, string yesText = "OK", string noText = null,
        string cancelText = null, DialogOptions options = null)
        => _innerDialogService.ShowMessageBox(title, message, yesText, noText, cancelText, options);

    public Task<bool?> ShowMessageBox(string title, MarkupString markupMessage, string yesText = "OK", string noText = null,
        string cancelText = null, DialogOptions options = null)
        => _innerDialogService.ShowMessageBox(title, markupMessage, yesText, noText, cancelText, options);

    public Task<bool?> ShowMessageBox(MessageBoxOptions messageBoxOptions, DialogOptions options = null)
        => _innerDialogService.ShowMessageBox(messageBoxOptions, options);

    public void Close(DialogReference dialog)
        => _innerDialogService.Close(dialog);

    public void Close(DialogReference dialog, DialogResult result)
        => _innerDialogService.Close(dialog, result);

    public event Action<IDialogReference> OnDialogInstanceAdded
    {
        add => _innerDialogService.OnDialogInstanceAdded += value;
        remove => _innerDialogService.OnDialogInstanceAdded -= value;
    }

    public event Action<IDialogReference, DialogResult> OnDialogCloseRequested
    {
        add => _innerDialogService.OnDialogCloseRequested += value;
        remove => _innerDialogService.OnDialogCloseRequested -= value;
    }

    #endregion
}