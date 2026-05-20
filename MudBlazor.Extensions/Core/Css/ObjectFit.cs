using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Values for the CSS <c>object-fit</c> property.
/// </summary>
public enum ObjectFit
{
    /// <summary>Content is sized to fill the element's box, stretched if necessary.</summary>
    [Description("fill")] Fill,
    /// <summary>Content is scaled to maintain aspect ratio while fitting inside the box.</summary>
    [Description("contain")] Contain,
    /// <summary>Content is scaled to maintain aspect ratio while filling the box, cropping if needed.</summary>
    [Description("cover")] Cover,
    /// <summary>Content is not resized.</summary>
    [Description("none")] None,
    /// <summary>Content is sized as if "none" or "contain" were specified, whichever is smaller.</summary>
    [Description("scale-down")] ScaleDown
}
