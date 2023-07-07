using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'cursor' property values.
/// </summary>
public enum Cursor
{
    [Description("pointer")]
    Pointer,
    [Description("default")]
    Default,
    [Description("auto")]
    Auto,
    [Description("crosshair")]
    Crosshair,
    [Description("move")]
    Move,
    [Description("text")]
    Text
}