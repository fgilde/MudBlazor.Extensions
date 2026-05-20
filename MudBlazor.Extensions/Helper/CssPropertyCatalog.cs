using MudBlazor.Extensions.Core.Css;
using System.ComponentModel;
using System.Reflection;
using MudBlazor.Extensions.Helper.Internal;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// One entry of the CSS property catalog. Drives autocomplete, categorization and
/// the smart value editor selection in <see cref="MudBlazor.Extensions.Components.MudExStyleEditor"/>.
/// </summary>
public sealed class CssPropertyDescriptor
{
    /// <summary>Property name in kebab-case.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Display category bucket.</summary>
    public CssPropertyCategory Category { get; init; } = CssPropertyCategory.Other;
    /// <summary>Editor kind hint.</summary>
    public CssEditorPropertyKind Kind { get; init; } = CssEditorPropertyKind.Text;
    /// <summary>
    /// Optional enum type. When set, the editor automatically renders a Select picker
    /// whose options come from <see cref="EnumExtensions.ToDescriptionString(Enum)"/> of each enum value.
    /// </summary>
    public Type? EnumType { get; init; }
    /// <summary>Suggested values (for Select / Shorthand / autocomplete). Used as fallback when <see cref="EnumType"/> is not set.</summary>
    public string[] Suggestions { get; init; } = Array.Empty<string>();
    /// <summary>Optional inclusive minimum for slider/number kinds.</summary>
    public double? Min { get; init; }
    /// <summary>Optional inclusive maximum for slider/number kinds.</summary>
    public double? Max { get; init; }
    /// <summary>Optional step for slider/number kinds.</summary>
    public double? Step { get; init; }
    /// <summary>Short description shown as tooltip.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Returns the suggestion list, deriving it from <see cref="EnumType"/> when set, otherwise <see cref="Suggestions"/>.
    /// </summary>
    public string[] EffectiveSuggestions()
    {
        if (EnumType is { IsEnum: true })
        {
            return Enum.GetValues(EnumType)
                .Cast<Enum>()
                .Select(v => v.GetDescription())
                .ToArray();
        }
        return Suggestions ?? Array.Empty<string>();
    }

}

/// <summary>
/// Static catalog of CSS properties this library knows how to edit. Not exhaustive but covers
/// the common cases that benefit from a typed editor. Where possible, the descriptor points at a
/// strongly-typed enum (<see cref="CssPropertyDescriptor.EnumType"/>) so the editor and the
/// <see cref="MudExStyleBuilder"/> stay in sync.
/// </summary>
public static class CssPropertyCatalog
{
    private static readonly string[] FontWeightValues = { "normal", "bold", "lighter", "bolder", "100", "200", "300", "400", "500", "600", "700", "800", "900" };

    /// <summary>
    /// All known descriptors, keyed by property name.
    /// </summary>
    public static IReadOnlyDictionary<string, CssPropertyDescriptor> All { get; } = Build();

    private static IReadOnlyDictionary<string, CssPropertyDescriptor> Build()
    {
        var list = new List<CssPropertyDescriptor>
        {
            // Layout
            new() { Name = "display", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Select, EnumType = typeof(Display) },
            new() { Name = "position", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Select, EnumType = typeof(Position) },
            new() { Name = "top", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Size },
            new() { Name = "right", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Size },
            new() { Name = "bottom", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Size },
            new() { Name = "left", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Size },
            new() { Name = "z-index", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Number, Step = 1 },
            new() { Name = "float", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Select, EnumType = typeof(Float) },
            new() { Name = "clear", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Select, EnumType = typeof(Clear) },
            new() { Name = "overflow", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Select, EnumType = typeof(Overflow) },
            new() { Name = "overflow-x", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Select, EnumType = typeof(Overflow) },
            new() { Name = "overflow-y", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Select, EnumType = typeof(Overflow) },
            new() { Name = "visibility", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Select, EnumType = typeof(Visibility) },
            new() { Name = "box-sizing", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Select, EnumType = typeof(BoxSizing) },
            new() { Name = "pointer-events", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Select, EnumType = typeof(PointerEvents) },
            new() { Name = "user-select", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Select, EnumType = typeof(UserSelect) },
            new() { Name = "cursor", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Select, EnumType = typeof(Cursor) },
            new() { Name = "direction", Category = CssPropertyCategory.Layout, Kind = CssEditorPropertyKind.Select, EnumType = typeof(Direction) },

            // Sizing
            new() { Name = "width", Category = CssPropertyCategory.Sizing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "height", Category = CssPropertyCategory.Sizing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "min-width", Category = CssPropertyCategory.Sizing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "min-height", Category = CssPropertyCategory.Sizing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "max-width", Category = CssPropertyCategory.Sizing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "max-height", Category = CssPropertyCategory.Sizing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "aspect-ratio", Category = CssPropertyCategory.Sizing, Kind = CssEditorPropertyKind.Text, Suggestions = new[] { "1 / 1", "16 / 9", "4 / 3", "auto" } },
            new() { Name = "object-fit", Category = CssPropertyCategory.Sizing, Kind = CssEditorPropertyKind.Select, EnumType = typeof(ObjectFit) },

            // Spacing
            new() { Name = "margin", Category = CssPropertyCategory.Spacing, Kind = CssEditorPropertyKind.Shorthand, Suggestions = new[] { "0", "auto", "8px", "16px" } },
            new() { Name = "margin-top", Category = CssPropertyCategory.Spacing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "margin-right", Category = CssPropertyCategory.Spacing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "margin-bottom", Category = CssPropertyCategory.Spacing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "margin-left", Category = CssPropertyCategory.Spacing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "padding", Category = CssPropertyCategory.Spacing, Kind = CssEditorPropertyKind.Shorthand, Suggestions = new[] { "0", "8px", "16px" } },
            new() { Name = "padding-top", Category = CssPropertyCategory.Spacing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "padding-right", Category = CssPropertyCategory.Spacing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "padding-bottom", Category = CssPropertyCategory.Spacing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "padding-left", Category = CssPropertyCategory.Spacing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "gap", Category = CssPropertyCategory.Spacing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "row-gap", Category = CssPropertyCategory.Spacing, Kind = CssEditorPropertyKind.Size },
            new() { Name = "column-gap", Category = CssPropertyCategory.Spacing, Kind = CssEditorPropertyKind.Size },

            // Typography
            new() { Name = "color", Category = CssPropertyCategory.Typography, Kind = CssEditorPropertyKind.Color },
            new() { Name = "font-family", Category = CssPropertyCategory.Typography, Kind = CssEditorPropertyKind.Text, Suggestions = new[] { "Roboto, sans-serif", "Arial, sans-serif", "'Segoe UI', sans-serif", "monospace", "serif" } },
            new() { Name = "font-size", Category = CssPropertyCategory.Typography, Kind = CssEditorPropertyKind.Size },
            new() { Name = "font-weight", Category = CssPropertyCategory.Typography, Kind = CssEditorPropertyKind.Select, Suggestions = FontWeightValues },
            new() { Name = "font-style", Category = CssPropertyCategory.Typography, Kind = CssEditorPropertyKind.Select, EnumType = typeof(FontStyle) },
            new() { Name = "font-variant", Category = CssPropertyCategory.Typography, Kind = CssEditorPropertyKind.Select, EnumType = typeof(FontVariant) },
            new() { Name = "line-height", Category = CssPropertyCategory.Typography, Kind = CssEditorPropertyKind.Text, Suggestions = new[] { "1", "1.2", "1.5", "1.75", "normal" } },
            new() { Name = "letter-spacing", Category = CssPropertyCategory.Typography, Kind = CssEditorPropertyKind.Size },
            new() { Name = "word-spacing", Category = CssPropertyCategory.Typography, Kind = CssEditorPropertyKind.Size },
            new() { Name = "text-align", Category = CssPropertyCategory.Typography, Kind = CssEditorPropertyKind.Select, EnumType = typeof(TextAlign) },
            new() { Name = "text-transform", Category = CssPropertyCategory.Typography, Kind = CssEditorPropertyKind.Select, EnumType = typeof(TextTransform) },
            new() { Name = "text-decoration", Category = CssPropertyCategory.Typography, Kind = CssEditorPropertyKind.Select, EnumType = typeof(TextDecoration) },
            new() { Name = "text-indent", Category = CssPropertyCategory.Typography, Kind = CssEditorPropertyKind.Size },
            new() { Name = "white-space", Category = CssPropertyCategory.Typography, Kind = CssEditorPropertyKind.Select, EnumType = typeof(WhiteSpace) },
            new() { Name = "word-break", Category = CssPropertyCategory.Typography, Kind = CssEditorPropertyKind.Select, EnumType = typeof(WordBreak) },

            // Background
            new() { Name = "background-color", Category = CssPropertyCategory.Background, Kind = CssEditorPropertyKind.Color },
            new() { Name = "background-image", Category = CssPropertyCategory.Background, Kind = CssEditorPropertyKind.Text, Suggestions = new[] { "none", "url('')", "linear-gradient(0deg, #000, #fff)" } },
            new() { Name = "background", Category = CssPropertyCategory.Background, Kind = CssEditorPropertyKind.Shorthand },
            new() { Name = "background-size", Category = CssPropertyCategory.Background, Kind = CssEditorPropertyKind.Select, Suggestions = new[] { "auto", "cover", "contain", "100%" } },
            new() { Name = "background-position", Category = CssPropertyCategory.Background, Kind = CssEditorPropertyKind.Select, Suggestions = new[] { "center", "top", "right", "bottom", "left", "center center" } },
            new() { Name = "background-repeat", Category = CssPropertyCategory.Background, Kind = CssEditorPropertyKind.Select, EnumType = typeof(BackgroundRepeat) },
            new() { Name = "background-attachment", Category = CssPropertyCategory.Background, Kind = CssEditorPropertyKind.Select, EnumType = typeof(BackgroundAttachment) },

            // Border
            new() { Name = "border", Category = CssPropertyCategory.Border, Kind = CssEditorPropertyKind.Shorthand, Suggestions = new[] { "1px solid #000", "1px solid currentColor", "none" } },
            new() { Name = "border-width", Category = CssPropertyCategory.Border, Kind = CssEditorPropertyKind.Size },
            new() { Name = "border-style", Category = CssPropertyCategory.Border, Kind = CssEditorPropertyKind.Select, EnumType = typeof(BorderStyle) },
            new() { Name = "border-color", Category = CssPropertyCategory.Border, Kind = CssEditorPropertyKind.Color },
            new() { Name = "border-top", Category = CssPropertyCategory.Border, Kind = CssEditorPropertyKind.Shorthand },
            new() { Name = "border-right", Category = CssPropertyCategory.Border, Kind = CssEditorPropertyKind.Shorthand },
            new() { Name = "border-bottom", Category = CssPropertyCategory.Border, Kind = CssEditorPropertyKind.Shorthand },
            new() { Name = "border-left", Category = CssPropertyCategory.Border, Kind = CssEditorPropertyKind.Shorthand },
            new() { Name = "border-radius", Category = CssPropertyCategory.Border, Kind = CssEditorPropertyKind.Size },
            new() { Name = "outline", Category = CssPropertyCategory.Border, Kind = CssEditorPropertyKind.Shorthand, Suggestions = new[] { "none", "1px solid #000" } },
            new() { Name = "outline-offset", Category = CssPropertyCategory.Border, Kind = CssEditorPropertyKind.Size },

            // Effects
            new() { Name = "opacity", Category = CssPropertyCategory.Effects, Kind = CssEditorPropertyKind.Slider, Min = 0, Max = 1, Step = 0.01 },
            new() { Name = "box-shadow", Category = CssPropertyCategory.Effects, Kind = CssEditorPropertyKind.Shorthand, Suggestions = new[] { "none", "0 1px 3px rgba(0,0,0,0.2)", "0 4px 8px rgba(0,0,0,0.3)", "0 8px 24px rgba(0,0,0,0.4)" } },
            new() { Name = "text-shadow", Category = CssPropertyCategory.Effects, Kind = CssEditorPropertyKind.Shorthand, Suggestions = new[] { "none", "1px 1px 2px rgba(0,0,0,0.4)" } },
            new() { Name = "filter", Category = CssPropertyCategory.Effects, Kind = CssEditorPropertyKind.Shorthand, Suggestions = new[] { "none", "blur(4px)", "brightness(1.2)", "grayscale(1)", "drop-shadow(0 2px 4px #000)" } },
            new() { Name = "backdrop-filter", Category = CssPropertyCategory.Effects, Kind = CssEditorPropertyKind.Shorthand, Suggestions = new[] { "none", "blur(8px)", "saturate(180%)" } },
            new() { Name = "mix-blend-mode", Category = CssPropertyCategory.Effects, Kind = CssEditorPropertyKind.Select, EnumType = typeof(MixBlendMode) },

            // Transform / animation
            new() { Name = "transform", Category = CssPropertyCategory.Transform, Kind = CssEditorPropertyKind.Shorthand, Suggestions = new[] { "none", "translate(0,0)", "rotate(45deg)", "scale(1.1)", "skew(10deg, 0)" } },
            new() { Name = "transform-origin", Category = CssPropertyCategory.Transform, Kind = CssEditorPropertyKind.Text, Suggestions = new[] { "center", "top left", "bottom right", "50% 50%" } },
            new() { Name = "transition", Category = CssPropertyCategory.Transform, Kind = CssEditorPropertyKind.Shorthand, Suggestions = new[] { "all 0.3s ease", "all 0.2s ease-in-out", "none" } },
            new() { Name = "animation", Category = CssPropertyCategory.Transform, Kind = CssEditorPropertyKind.Shorthand },
            new() { Name = "will-change", Category = CssPropertyCategory.Transform, Kind = CssEditorPropertyKind.Select, Suggestions = new[] { "auto", "transform", "opacity", "scroll-position", "contents" } },

            // Flex / Grid
            new() { Name = "flex-direction", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Select, EnumType = typeof(FlexDirection) },
            new() { Name = "flex-wrap", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Select, EnumType = typeof(FlexWrap) },
            new() { Name = "flex-flow", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Select, EnumType = typeof(FlexFlow) },
            new() { Name = "flex-grow", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Number, Min = 0, Step = 1 },
            new() { Name = "flex-shrink", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Number, Min = 0, Step = 1 },
            new() { Name = "flex-basis", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Size },
            new() { Name = "flex", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Shorthand, Suggestions = new[] { "1", "0 1 auto", "1 1 100%", "none" } },
            new() { Name = "justify-content", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Select, EnumType = typeof(JustifyContent) },
            new() { Name = "align-items", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Select, EnumType = typeof(AlignItems) },
            new() { Name = "align-content", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Select, EnumType = typeof(JustifyContent) },
            new() { Name = "align-self", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Select, Suggestions = new[] { "auto", "stretch", "flex-start", "flex-end", "center", "baseline", "start", "end" } },
            new() { Name = "order", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Number, Step = 1 },
            new() { Name = "grid-template-columns", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Shorthand, Suggestions = new[] { "1fr 1fr", "repeat(3, 1fr)", "auto 1fr auto" } },
            new() { Name = "grid-template-rows", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Shorthand, Suggestions = new[] { "auto", "1fr 1fr", "repeat(3, 1fr)" } },
            new() { Name = "grid-column", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Shorthand, Suggestions = new[] { "auto", "1 / -1", "span 2" } },
            new() { Name = "grid-row", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Shorthand, Suggestions = new[] { "auto", "1 / -1", "span 2" } },
            new() { Name = "grid-gap", Category = CssPropertyCategory.FlexGrid, Kind = CssEditorPropertyKind.Size },
        };

        return list.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the catalog descriptor for the property or a fallback Text descriptor in the Other category.
    /// </summary>
    public static CssPropertyDescriptor Get(string property)
    {
        if (string.IsNullOrEmpty(property))
            return new CssPropertyDescriptor();
        return All.TryGetValue(property, out var desc)
            ? desc
            : new CssPropertyDescriptor { Name = property, Category = CssPropertyCategory.Other, Kind = CssEditorPropertyKind.Text };
    }
}
