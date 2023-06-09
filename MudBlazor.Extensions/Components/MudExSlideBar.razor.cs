using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A Component that can used to slide in a ChildContent from the bottom, top , left or right by mouseover.
/// </summary>
public partial class MudExSlideBar
{
    private bool _isOpen;
    
    [Parameter] public Position Position { get; set; } = Position.Bottom;
    [Parameter] public bool AutoCollapse { get; set; } = true;
    [Parameter] public double OpacityNotFocused { get; set; } = .2;
    [Parameter] public bool RelativeToParent { get; set; } = false;
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
    [Parameter] public MudExColor BackgroundColor { get; set; } = Color.Transparent;
    [Parameter] public MudExColor BorderColor { get; set; } = Color.Transparent;
    [Parameter] public MudExSize<double> BorderSize { get; set; } = new(2, CssUnit.Pixels);
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public virtual RenderFragment ChildContent { get; set; }
    [Parameter] public bool DisableOpacityChange { get; set; }
    [Parameter] public bool HideContentWhenCollapsed { get; set; } = true;
    
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
        return new MudExStyleBuilder()
            .With("opacity", _isOpen ? "1" : OpacityNotFocused.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), !DisableOpacityChange)
            .With("background-color", BackgroundColor.ToCssStringValue(), !BackgroundColor.Is(Color.Transparent))
            .With("z-Index", "calc(var(--mud-zindex-dialog) + 10)", !RelativeToParent && (IsOpen || !AutoCollapse))
            .With("z-Index", "calc(var(--mud-zindex-dialog) - 1)", !RelativeToParent && (!IsOpen && AutoCollapse))
            .With($"border-{BorderDirection}", $"{BorderSize} solid {BorderColor.ToCssStringValue()}", !BorderColor.Is(Color.Transparent))
            .Build();
    }

    private string BorderDirection => Position switch
    {
        Position.Bottom => "top",
        Position.Top => "bottom",
        Position.Left => "right",
        Position.Right => "left",
        _ => "top"
    };

    private string CssClass()
    {
        return MudExCssBuilder.Default
            .AddClass("mud-ex-slidebar")
            .AddClass($"{Position.ToDescriptionString()}")
            .AddClass($"open", _isOpen || !AutoCollapse)
            .AddClass($"relative-to-parent", RelativeToParent)
            .Build();
    }

}