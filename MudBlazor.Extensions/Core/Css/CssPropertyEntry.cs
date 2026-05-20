namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// One declaration in a parsed style string. Mutable working model used by
/// <see cref="MudBlazor.Extensions.Components.MudExStyleEditor"/>.
/// </summary>
public sealed class CssPropertyEntry
{
    /// <summary>
    /// CSS property name in kebab-case (e.g. "background-color").
    /// </summary>
    public string Property { get; set; } = string.Empty;

    /// <summary>
    /// Raw value of the declaration (without trailing semicolon).
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// When false, the declaration is preserved in the style string as a CSS comment.
    /// Lets the user temporarily disable a rule, matching the DevTools toggle behavior.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Stable id used as @key in the Razor render loop while editing.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();
}
