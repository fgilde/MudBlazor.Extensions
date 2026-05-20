namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// View mode for <see cref="MudBlazor.Extensions.Components.MudExStyleEditor"/>.
/// </summary>
public enum MudExStyleEditViewMode
{
    /// <summary>
    /// Inline editable list of "property: value;" rows like Chrome DevTools.
    /// </summary>
    DevTools,

    /// <summary>
    /// Grouped form with one panel per category (Layout, Spacing, Typography, ...).
    /// </summary>
    Categorized,

    /// <summary>
    /// Plain multiline text edit of the full style string.
    /// </summary>
    Raw
}

/// <summary>
/// View mode for <see cref="MudBlazor.Extensions.Components.MudExClassEditor"/>.
/// </summary>
public enum MudExClassEditViewMode
{
    /// <summary>
    /// Standard list grouped by source stylesheet.
    /// </summary>
    List,

    /// <summary>
    /// Dense chip cloud, ideal for small popovers.
    /// </summary>
    Compact,

    /// <summary>
    /// Card-like grid layout with style preview snippets.
    /// </summary>
    Grid
}

/// <summary>
/// Category bucket used to group CSS properties in the categorized view.
/// </summary>
public enum CssPropertyCategory
{
    /// <summary>Layout related (display, position, float, ...).</summary>
    Layout,
    /// <summary>Box model spacing (padding, margin).</summary>
    Spacing,
    /// <summary>Sizing (width, height, min/max).</summary>
    Sizing,
    /// <summary>Typography (font, text, line-height).</summary>
    Typography,
    /// <summary>Background and color.</summary>
    Background,
    /// <summary>Border and outline.</summary>
    Border,
    /// <summary>Visual effects (opacity, filter, shadow).</summary>
    Effects,
    /// <summary>Transform / animation.</summary>
    Transform,
    /// <summary>Flex / grid layout details.</summary>
    FlexGrid,
    /// <summary>Anything that does not fit a known category.</summary>
    Other
}

/// <summary>
/// Editor kind used to pick the right control for a given CSS property value.
/// </summary>
public enum CssEditorPropertyKind
{
    /// <summary>Free text input.</summary>
    Text,
    /// <summary>Numeric size with a unit (px, %, em, rem, ...).</summary>
    Size,
    /// <summary>Color picker.</summary>
    Color,
    /// <summary>Single numeric value (no unit).</summary>
    Number,
    /// <summary>Slider in a known range (e.g. opacity 0-1).</summary>
    Slider,
    /// <summary>Predefined value list (Select).</summary>
    Select,
    /// <summary>Multiple shorthand values (e.g. border, transform). Edited as text with suggestions.</summary>
    Shorthand
}
