namespace MudBlazor.Extensions.Components;

/// <summary>
/// Mode how css and style will be applied.
/// </summary>
public enum CssAndStyleApplyMode
{
    /// <summary>
    /// The css and style will be applied to the container before the content is rendered.
    /// </summary>
    BeforeContentRendered,

    /// <summary>
    /// The css and style will be applied to the container after the content is rendered.
    /// </summary>
    AfterContentRendered,

}