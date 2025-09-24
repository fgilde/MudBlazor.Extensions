using System.ComponentModel;

namespace MudBlazor.Extensions.Components;


/// <summary>
/// Dock mode for the layout
/// </summary>
public enum DockMode
{
    /// <summary>
    /// Dock mode with tabs and panels
    /// </summary>
    [Description(nameof(Dock))]
    Dock,
    /// <summary>
    /// Grid mode with panels only
    /// </summary>
    [Description(nameof(Grid))]
    Grid,

    /// <summary>
    /// Split mode
    /// </summary>
    [Description(nameof(Split))]
    Split,

    /// <summary>
    /// Pane mode
    /// </summary>
    [Description(nameof(Pane))]
    Pane
}


/// <summary>
/// Direction to dock a panel relative to another panel
/// </summary>
public enum DockDirection
{
    /// <summary>
    /// Dock to the left side of the target panel
    /// </summary>
    [Description(nameof(Left))]
    Left,

    /// <summary>
    /// Dock to the right side of the target panel
    /// </summary>
    [Description(nameof(Right))]
    Right,

    /// <summary>
    /// Dock above the target panel
    /// </summary>
    [Description(nameof(Above))]
    Above,

    /// <summary>
    /// Dock below the target panel
    /// </summary>
    [Description(nameof(Below))]
    Below,

    /// <summary>
    /// Dock within the target panel
    /// </summary>
    [Description(nameof(Within))]
    Within
}

public enum DockTheme
{
    [Description("dockview-theme-mud-ex")]
    MudBlazor,    
    
    [Description("dockview-theme-dark")]
    Dark,

    [Description("dockview-theme-vs")]
    VisualStudio,

    [Description("dockview-theme-light")]
    Light,    
    
    [Description("dockview-theme-light-spaced")]
    LightSpaced,

    [Description("dockview-theme-abyss")]
    Abyss,

    [Description("dockview-theme-abyss-spaced")]
    AbyssSpaced,

    [Description("dockview-theme-dracula")]
    Dracula,

    [Description("dockview-theme-replit")]
    Replit

}