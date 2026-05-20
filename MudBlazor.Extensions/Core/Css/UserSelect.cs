using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Values for the CSS <c>user-select</c> property.
/// </summary>
public enum UserSelect
{
    /// <summary>Default: text in editable elements is selectable, otherwise behavior depends on user agent.</summary>
    [Description("auto")] Auto,
    /// <summary>Text cannot be selected.</summary>
    [Description("none")] None,
    /// <summary>The user can select text in the element.</summary>
    [Description("text")] Text,
    /// <summary>Selecting the element selects all of its contents as a single unit.</summary>
    [Description("all")] All,
    /// <summary>Selection is contained to the element, will not extend to ancestors.</summary>
    [Description("contain")] Contain
}
