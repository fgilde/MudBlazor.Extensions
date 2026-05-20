namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// One CSS class harvested from the document's stylesheets, used by
/// <see cref="MudBlazor.Extensions.Components.MudExClassEditor"/>.
/// </summary>
public sealed class CssClassInfo
{
    /// <summary>
    /// Bare class name without leading dot.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// All selectors the class was found in.
    /// </summary>
    public string[] Selectors { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Origin label (stylesheet filename or "inline").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Inline cssText of the first rule that declared the class. Empty if not scanned.
    /// </summary>
    public string Styles { get; set; } = string.Empty;
}

/// <summary>
/// Group container as returned by the JS scan: one entry per stylesheet source.
/// </summary>
public sealed class CssClassInfoGroup
{
    /// <summary>
    /// Source label (filename of the stylesheet or "inline").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Classes harvested from this source.
    /// </summary>
    public CssClassInfo[] Classes { get; set; } = Array.Empty<CssClassInfo>();
}
