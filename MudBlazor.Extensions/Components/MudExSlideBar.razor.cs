using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Css;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A Component that can used to slide in a ChildContent from the bottom, top , left or right by mouseover.
/// </summary>
public partial class MudExSlideBar
{
    private bool _isOpen;

    /// <summary>
    /// The position where the MudExSlideBar should start to slide from.
    /// </summary>
    [Parameter, SafeCategory("Behavior")] public Position Position { get; set; } = Position.Bottom;

    /// <summary>
    /// If true, the MudExSlideBar will collapse automatically when the mouse leaves its bounds.
    /// </summary>
    [Parameter, SafeCategory("Behavior")] public bool AutoCollapse { get; set; } = true;

    /// <summary>
    /// Determines the opacity of the MudExSlideBar when it is not focused.
    /// </summary>
    [Parameter, SafeCategory("Appearance")] public double OpacityNotFocused { get; set; } = .2;

    /// <summary>
    /// If true, the position of the MudExSlideBar will be relative to the parent element.
    /// </summary>
    [Parameter, SafeCategory("Behavior")] public bool RelativeToParent { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the MudExSlideBar is open.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool IsOpen
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

    /// <summary>
    /// The background color of the MudExSlideBar.
    /// </summary>
    [Parameter, SafeCategory("Appearance")] public MudExColor BackgroundColor { get; set; } = Color.Transparent;

    /// <summary>
    /// The border color of the MudExSlideBar.
    /// </summary>
    [Parameter, SafeCategory("Appearance")] public MudExColor BorderColor { get; set; } = Color.Transparent;

    /// <summary>
    /// The border size of the MudExSlideBar.
    /// </summary>
    [Parameter, SafeCategory("Appearance")] public MudExSize<double> BorderSize { get; set; } = new(2, CssUnit.Pixels);

    /// <summary>
    /// An event that is raised when the IsOpen property changes.
    /// </summary>
    [Parameter, SafeCategory("Misc")] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>
    /// The child content of the MudExSlideBar.
    /// </summary>
    [Parameter, SafeCategory("Content")] public virtual RenderFragment ChildContent { get; set; }

    /// <summary>
    /// If true, changes in opacity of the MudExSlideBar are ignored.
    /// </summary>
    [Parameter, SafeCategory("Behavior")] public bool DisableOpacityChange { get; set; }

    /// <summary>
    /// If true, the child content of the MudExSlideBar will be hidden when the MudExSlideBar is collapsed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")] public bool HideContentWhenCollapsed { get; set; } = true;

    [Parameter] public MudExSize<double>? Width { get; set; }


    /// <summary>
    /// Shows the MudExSlideBar.
    /// </summary>
    [JSInvokable]
    public void Show()
    {
        _isOpen = true;
        IsOpenChanged.InvokeAsync(IsOpen);
        StateHasChanged();
    }

    /// <summary>
    /// Hides the MudExSlideBar
    /// </summary>
    [JSInvokable]
    public void Hide()
    {
        _isOpen = false;
        IsOpenChanged.InvokeAsync(IsOpen);
        StateHasChanged();
    }

    /// <summary>
    /// Mouse enter event handling.
    /// </summary>
    private Task MouseEnter()
    {
        Show();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Mouse leave event handling.
    /// </summary>
    private Task MouseLeave()
    {
        Hide();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the inline styles of the MudExSlideBar.
    /// </summary>
    private string Style()
    {
        return new MudExStyleBuilder()
            .With("opacity", _isOpen ? "1" : OpacityNotFocused.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), !DisableOpacityChange)
            .With("background-color", BackgroundColor.ToCssStringValue(), !BackgroundColor.Is(Color.Transparent))
            .With("z-Index", "calc(var(--mud-zindex-dialog) + 10)", !RelativeToParent && (IsOpen || !AutoCollapse))
            .With("z-Index", "calc(var(--mud-zindex-dialog) - 1)", !RelativeToParent && (!IsOpen && AutoCollapse))
            .With($"border-{BorderDirection}", $"{BorderSize} solid {BorderColor.ToCssStringValue()}", !BorderColor.Is(Color.Transparent))
            .WithWidth(Width, Width.HasValue) // TODO: TESTING
            .Build();
    }

    /// <summary>
    /// The direction where the border of the MudExSlideBar should be drawn.
    /// </summary>
    private string BorderDirection => Position switch
    {
        Position.Bottom => "top",
        Position.Top => "bottom",
        Position.Left => "right",
        Position.Right => "left",
        _ => "top"
    };

    /// <summary>
    /// Returns the CSS classes of the MudExSlideBar.
    /// </summary>
    private string CssClass()
    {
        return MudExCssBuilder.Default
            .AddClass("mud-ex-slidebar")
            .AddClass($"{Position.GetDescription()}")
            .AddClass($"open", _isOpen || !AutoCollapse)
            .AddClass($"relative-to-parent", RelativeToParent)
            .Build();
    }

    private string ContentStyle()
    {
        return MudExStyleBuilder.Default
            .WithOpacity(IsOpen || !AutoCollapse || !HideContentWhenCollapsed ? 1 : 0)
            .WithSize(100, CssUnit.Percentage)
            .WithOverflow(Overflow.Auto)            
            .Build();        
    }
}