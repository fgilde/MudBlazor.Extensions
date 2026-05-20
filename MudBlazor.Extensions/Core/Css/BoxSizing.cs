using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Values for the CSS <c>box-sizing</c> property.
/// </summary>
public enum BoxSizing
{
    /// <summary>The width and height only include the content. Padding and border are added outside the box.</summary>
    [Description("content-box")] ContentBox,
    /// <summary>The width and height include content, padding and border.</summary>
    [Description("border-box")] BorderBox
}
