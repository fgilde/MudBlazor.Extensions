using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Utilities;

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

    [Parameter] public Color BackgroundColor { get; set; } = Color.Transparent;
    [Parameter] public Color BorderColor { get; set; } = Color.Transparent;
    [Parameter] public MudExSize<int> BorderSize { get; set; } = new(2, CssUnit.Pixels);
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
        //z-index: calc(var(--mud-zindex-dialog) + 10);
        return new StyleBuilder()
            .AddStyle("opacity", _isOpen ? "1" : OpacityNotFocused.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), !DisableOpacityChange)
            .AddStyle("background-color", BackgroundColor.CssVarDeclaration(), BackgroundColor != Color.Transparent)
            .AddStyle("z-Index", "calc(var(--mud-zindex-dialog) + 10)", !RelativeToParent && (IsOpen || !AutoCollapse))
            .AddStyle("z-Index", "calc(var(--mud-zindex-dialog) - 1)", !RelativeToParent && (!IsOpen && AutoCollapse))
            .AddStyle($"border-{BorderDirection}", $"{BorderSize} solid {BorderColor.CssVarDeclaration()}", BorderColor != Color.Transparent)
            .Build();
        // return $"opacity: {(_isOpen ? 1 : OpacityNotFocused.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))};";
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
        return CssBuilder.Default("mud-ex-slidebar")
            .AddClass($"{Position.ToDescriptionString()}")
            .AddClass($"open", _isOpen || !AutoCollapse)
            .AddClass($"relative-to-parent", RelativeToParent)
            .Build();
        // return $"mud-ex-slidebar {Position.ToDescriptionString()} {(_isOpen || !AutoCollapse ? "open" : "")} {(RelativeToParent ? "relative-to-parent" : "")}";
    }

}