using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'cursor' property values.
/// </summary>
public enum Cursor
{
    /// <summary>
    /// Pointer cursor.
    /// </summary>
    [Description("pointer")]
    Pointer,
    
    /// <summary>
    /// Default cursor.
    /// </summary>
    [Description("default")]
    Default,
    
    /// <summary>
    /// Auto cursor.
    /// </summary>
    [Description("auto")]
    Auto,
    
    /// <summary>
    /// Crosshair cursor.
    /// </summary>
    [Description("crosshair")]
    Crosshair,
    
    /// <summary>
    /// Move cursor.
    /// </summary>
    [Description("move")]
    Move,
    
    /// <summary>
    /// Text cursor.
    /// </summary>
    [Description("text")]
    Text
}