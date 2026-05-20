using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Values for the CSS <c>flex-wrap</c> property.
/// </summary>
public enum FlexWrap
{
    /// <summary>All flex items are placed on one line.</summary>
    [Description("nowrap")] NoWrap,
    /// <summary>Items wrap onto multiple lines, top to bottom.</summary>
    [Description("wrap")] Wrap,
    /// <summary>Items wrap onto multiple lines, bottom to top.</summary>
    [Description("wrap-reverse")] WrapReverse
}
