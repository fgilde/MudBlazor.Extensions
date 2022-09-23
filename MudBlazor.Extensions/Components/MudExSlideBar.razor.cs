using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlideBar
{
    [Inject] protected IServiceProvider ServiceProvider { get; set; }
    protected IJSRuntime Js => ServiceProvider.GetService<IJSRuntime>();
    protected IDialogService DialogService => ServiceProvider.GetService<IDialogService>();
    private bool _isOpen;
    [Parameter] public Position Position { get; set; } = Position.Bottom;
    [Parameter] public bool AutoCollapse { get; set; } = true;
    [Parameter] public double OpacityNotFocused { get; set; } = .2;
    [Parameter] public bool IsOpen
    {
        get => _isOpen;
        set
        {
            if (value)
                Show();
            else
                Hide();
        }
    }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public virtual RenderFragment ChildContent { get; set; }

    [JSInvokable]
    public void Show()
    {
        _isOpen = true;
        IsOpenChanged.InvokeAsync(IsOpen);
        StateHasChanged();
    }
    
    [JSInvokable]
    public void Hide()
    {
        _isOpen = false;
        IsOpenChanged.InvokeAsync(IsOpen);
        StateHasChanged();
    }

    private Task MouseEnter()
    {
        Show();
        return Task.CompletedTask;
    }
    private Task MouseLeave()
    {
        Hide();
        return Task.CompletedTask;
    }

    private string Style()
    {
        return $"opacity: {(_isOpen ? 1 : OpacityNotFocused.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))};";
    }
    
    private string CssClass()
    {
        return $"mud-ex-slidebar {Position.ToDescriptionString()} {(_isOpen || !AutoCollapse ? "open" : "")}";
    }
}