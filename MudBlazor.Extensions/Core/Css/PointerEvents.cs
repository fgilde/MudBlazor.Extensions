using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Values for the CSS <c>pointer-events</c> property.
/// </summary>
public enum PointerEvents
{
    /// <summary>Element behaves normally with respect to pointer events.</summary>
    [Description("auto")] Auto,
    /// <summary>Element is not a target of pointer events. Events pass through to elements below.</summary>
    [Description("none")] None
}
