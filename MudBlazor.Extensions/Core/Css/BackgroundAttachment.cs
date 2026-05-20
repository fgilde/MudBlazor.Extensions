using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Values for the CSS <c>background-attachment</c> property.
/// </summary>
public enum BackgroundAttachment
{
    /// <summary>The background scrolls with the page (default).</summary>
    [Description("scroll")] Scroll,
    /// <summary>The background is fixed relative to the viewport.</summary>
    [Description("fixed")] Fixed,
    /// <summary>The background is fixed relative to the element's content.</summary>
    [Description("local")] Local
}
