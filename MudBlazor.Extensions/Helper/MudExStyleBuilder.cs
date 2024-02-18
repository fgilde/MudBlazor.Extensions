using MudBlazor.Extensions.Core;
using MudBlazor.Utilities;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.JSInterop;
using System.Collections.Concurrent;
using System.Globalization;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core.Css;
using Nextended.Core.Extensions;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// MudExStyleBuilder is useful to create style strings or convert any style to a class.
/// </summary>
[HasDocumentation("MudExStyleBuilder.md")]
public sealed class MudExStyleBuilder : IAsyncDisposable, IMudExStyleAppearance
{
    private static readonly string[] PropertiesToAddUnits = { "height", "width", "min-height", "min-width", "max-height", "max-width",
        "padding", "padding-top", "padding-right", "padding-bottom", "padding-left", "margin", "margin-top", "margin-right", "margin-bottom",
        "margin-left", "border-width", "border-top-width", "border-right-width", "border-bottom-width", "border-left-width", "font-size", "letter-spacing",
        "line-height", "word-spacing", "text-indent", "column-gap", "column-width", "top", "right", "bottom", "left", "transform", "translate", "translateX",
        "translateY", "translateZ", "translate3d", "rotate", "rotateX", "rotateY", "rotateZ", "scale", "scaleX", "scaleY", "scaleZ", "scale3d",
        "skew", "skewX", "skewY", "perspective"};

    private Dictionary<string, string> _additionalStyles = new();
    private readonly List<string> _rawStyles = new();

    private readonly ConcurrentDictionary<string, byte> _temporaryCssClasses = new();
    //private Dictionary<string, MudExStyleBuilder> _pseudoElementsStyles = new();

    private IJSRuntime JsRuntime { get; }

    /// <summary>
    /// Creates a new instance of <see cref="MudExStyleBuilder"/>
    /// </summary>
    /// <param name="jsRuntime">js runtime reference</param>
    public MudExStyleBuilder(IJSRuntime jsRuntime)
    {
        JsRuntime = jsRuntime;
    }

    internal MudExStyleBuilder() : this(null)
    { }


    #region Static Methods

    /// <summary>
    /// Static Property to access an instance <see cref="MudExStyleBuilder"/>
    /// </summary>
    public static MudExStyleBuilder Default => new();

    /// <summary>
    /// Static factory method to create a <see cref="MudExStyleBuilder"/>
    /// </summary>
    public static MudExStyleBuilder Empty() => new();


    /// <summary>
    /// Static factory method to create a <see cref="MudExStyleBuilder"/> from an object.
    /// </summary>
    public static MudExStyleBuilder FromObject(object obj, string existingCss = "", CssUnit cssUnit = CssUnit.Pixels) => FromStyle(GenerateStyleString(obj, cssUnit, existingCss));

    /// <summary>
    /// Static factory method to create a <see cref="MudExStyleBuilder"/> from an object.
    /// </summary>
    public static MudExStyleBuilder FromObject(object obj, CssUnit cssUnit) => FromStyle(GenerateStyleString(obj, cssUnit, string.Empty));

    /// <summary>
    /// Static factory method to create a <see cref="MudExStyleBuilder"/> from an existing style string.
    /// </summary>
    public static MudExStyleBuilder FromStyle(string style) => string.IsNullOrWhiteSpace(style) ? new MudExStyleBuilder() : new MudExStyleBuilder().WithStyle(style);

    /// <summary>
    /// Converts an object to a style string but only properties that are not passed in existingCss
    /// </summary>
    public static string GenerateStyleString(object obj, string existingCss = "") => GenerateStyleString(obj, CssUnit.Pixels, existingCss);

    /// <summary>
    /// Converts an object to a style string but only properties that are not passed in existingCss.
    /// </summary>
    public static string GenerateStyleString(object obj, CssUnit cssUnit, string existingCss = "")
    {
        string unit = cssUnit.GetDescription();
        var cssBuilder = new StringBuilder();
        var properties = obj.GetType().GetProperties();

        foreach (var property in properties)
        {
            var cssPropertyName = Regex.Replace(property.Name, "(?<!^)([A-Z])", "-$1").ToLower();
            object propertyValue = property.GetValue(obj, null);
            if (propertyValue != null)
            {
                if (propertyValue is Color color)
                    propertyValue = color.CssVarDeclaration();
                var formattedPropertyValue = propertyValue is string ? $"{propertyValue}" : propertyValue.ToString();
                if (int.TryParse(formattedPropertyValue, out _) && PropertiesToAddUnits.Contains(cssPropertyName))
                    formattedPropertyValue += unit;
                cssBuilder.Append(cssPropertyName + ": " + formattedPropertyValue + ";");
            }
        }

        return string.IsNullOrEmpty(existingCss) ? cssBuilder.ToString() : CombineStyleStrings(cssBuilder.ToString(), existingCss);
    }

    /// <summary>
    /// Combines two css style strings.
    /// </summary>
    /// <param name="cssString">css style string to be combined.</param>
    /// <param name="leadingCssString">css style string to be attached.</param>
    /// <returns>the combined css style strings.</returns>
    public static string CombineStyleStrings(string cssString, string leadingCssString)
    {
        var cssProperties = new Dictionary<string, string>();
        var cssRegex = new Regex(@"([\w-]+)\s*:\s*([^;]+)");
        var cssProperties1 = cssRegex.Matches(cssString);
        foreach (var property in cssProperties1.Cast<Match>())
            cssProperties.TryAdd(property.Groups[1].Value.Trim(), property.Groups[2].Value.Trim());


        var cssProperties2 = cssRegex.Matches(leadingCssString);
        foreach (var property in cssProperties2.Cast<Match>())
            cssProperties[property.Groups[1].Value.Trim()] = property.Groups[2].Value.Trim();

        return cssProperties.Aggregate("", (current, property) => current + (property.Key + ": " + property.Value + "; "));
    }

    /// <summary>
    /// Converts a CSS style string to an object
    /// </summary>
    /// <typeparam name="T">Type of object to create</typeparam>
    /// <param name="css">CSS style string</param>
    /// <returns>The created object</returns>
    public static T StyleStringToObject<T>(string css) where T : new()
    {
        var obj = new T();

        // Split the CSS string into individual properties
        var properties = css.Split(';');

        // Iterate through the properties and set the corresponding property values on the object
        foreach (var property in properties)
        {
            // Split the property into its name and value
            var propertyParts = property.Split(':');
            if (propertyParts.Length != 2)
                continue;

            var propertyName = propertyParts[0].Trim();
            var propertyValue = propertyParts[1].Trim();

            // Convert the property name to camelCase
            propertyName = Regex.Replace(propertyName, "-([a-z])", m => m.Groups[1].Value.ToUpperInvariant()).ToUpper(true);

            // Try to set the property value on the object
            try
            {
                obj.GetType().GetProperty(propertyName)?.SetValue(obj, propertyValue);
            }
            catch (Exception)
            {
                // Ignore any errors that occur while setting the property value
            }
        }

        return obj;
    }

    #endregion

    #region Fluent WithProperty Methods


    /// <summary>
    /// Adds all styles and values from given style string
    /// </summary>
    /// <param name="styleString">CSS style string to parse and add to the builder</param>
    /// <param name="when">If false, no properties will be added to the builder</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithStyle(string styleString, bool when = true)
    {
        if (!when)
            return this;
        styleString.Split(';')
            .Select(p => p.Split(':'))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim())
            .Apply(property => With(property.Key, property.Value));
        return this;
    }

    /// <summary>
    /// Converts given object to a CSS style string and adds all properties and values to this builder
    /// </summary>
    /// <param name="styleObj">Object to convert to a CSS style string</param>
    /// <param name="cssUnit">Unit to format CSS quantity-including properties. Default is CssUnit.Pixels.</param>
    /// <param name="when">If false, no properties will be added to the builder</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder With(object styleObj, CssUnit cssUnit = CssUnit.Pixels, bool when = true) => !when ? this : WithStyle(GenerateStyleString(styleObj, cssUnit));

    /// <summary>
    /// Adds a color property to the builder
    /// </summary>
    /// <param name="color">Value of the color property</param>
    /// <param name="when">If false, no property will be added to the builder</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithColor(string color, bool when = true) => With("color", color, when);

    /// <summary>
    /// Adds a color property to the builder.
    /// </summary>
    /// <param name="color">Value of the MudExColor property</param>
    /// <param name="when">If false, no property will be added to the builder</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithColor(MudExColor color, bool when = true) => WithColor(color.ToCssStringValue(), when);

    /// <summary>
    /// Adds a color property to the builder.
    /// </summary>
    /// <param name="color">Value of the MudColor property</param>
    /// <param name="when">If false, no property will be added to the builder</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithColor(MudColor color, bool when = true) => WithColor(color.ToString(), when);

    /// <summary>
    /// Adds a color property to the builder.
    /// </summary>
    /// <param name="color">Value of the System.Drawing.Color property</param>
    /// <param name="when">If false, no property will be added to the builder</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithColor(System.Drawing.Color color, bool when = true) => WithColor(color.ToMudColor(), when);

    /// <summary>
    /// Adds a color property with a value of `var(--{color})` to the builder.
    /// </summary>
    /// <param name="color">Color variable name, without "--" prefix</param>
    /// <param name="when">If false, no property will be added to the builder.</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithColor(Color color, bool when = true) => WithColor(color.CssVarDeclaration(), when);


    /// <summary>
    /// Adds a stroke property to the builder
    /// </summary>
    /// <param name="color">Value of the fill property</param>
    /// <param name="when">If false, no property will be added to the builder</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithStroke(string color, bool when = true) => With("stroke", color, when);

    /// <summary>
    /// Adds a stroke property to the builder
    /// </summary>
    /// <param name="color">Value of the MudExColor property</param>
    /// <param name="when">If false, no property will be added to the builder</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithStroke(MudExColor color, bool when = true) => WithStroke(color.ToCssStringValue(), when);

    /// <summary>
    /// Adds a stroke property to the builder
    /// </summary>
    /// <param name="color">Value of the MudColor property</param>
    /// <param name="when">If false, no property will be added to the builder</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithStroke(MudColor color, bool when = true) => WithStroke(color.ToString(), when);

    /// <summary>
    /// Adds a stroke property to the builder
    /// </summary>
    /// <param name="color">Value of the System.Drawing.Color property</param>
    /// <param name="when">If false, no property will be added to the builder</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithStroke(System.Drawing.Color color, bool when = true) => WithStroke(color.ToMudColor(), when);

    /// <summary>
    /// Adds a stroke property with a value of `var(--{color})` to the builder.
    /// </summary>
    /// <param name="color">Color variable name, without "--" prefix</param>
    /// <param name="when">If false, no property will be added to the builder.</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithStroke(Color color, bool when = true) => WithStroke(color.CssVarDeclaration(), when);


    /// <summary>
    /// Adds a fill property to the builder
    /// </summary>
    /// <param name="color">Value of the fill property</param>
    /// <param name="when">If false, no property will be added to the builder</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithFill(string color, bool when = true) => With("fill", color, when);

    /// <summary>
    /// Adds a fill property to the builder
    /// </summary>
    /// <param name="color">Value of the MudExColor property</param>
    /// <param name="when">If false, no property will be added to the builder</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithFill(MudExColor color, bool when = true) => WithFill(color.ToCssStringValue(), when);

    /// <summary>
    /// Adds a fill property to the builder
    /// </summary>
    /// <param name="color">Value of the MudColor property</param>
    /// <param name="when">If false, no property will be added to the builder</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithFill(MudColor color, bool when = true) => WithFill(color.ToString(), when);

    /// <summary>
    /// Adds a fill property to the builder
    /// </summary>
    /// <param name="color">Value of the System.Drawing.Color property</param>
    /// <param name="when">If false, no property will be added to the builder</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithFill(System.Drawing.Color color, bool when = true) => WithFill(color.ToMudColor(), when);

    /// <summary>
    /// Adds a fill property with a value of `var(--{color})` to the builder.
    /// </summary>
    /// <param name="color">Color variable name, without "--" prefix</param>
    /// <param name="when">If false, no property will be added to the builder.</param>
    /// <returns>This MudExStyleBuilder instance</returns>
    public MudExStyleBuilder WithFill(Color color, bool when = true) => WithFill(color.CssVarDeclaration(), when);

    /// <summary>
    /// Adds a background property to the builder. The background is a gradient of all entries in the color array.
    /// </summary>
    /// <param name="color">Array of the colors of the gradient stops. Multiple colors can be used for a smoother transition.</param>
    /// <param name="radius">degree of the gradient. Default is 0.</param>
    /// <param name="when">If false, no property will be added to the builder.</param>
    /// <returns>This MudExStyleBuilder instance.</returns>
    public MudExStyleBuilder WithBackground(string[] color, int radius, bool when = true)
        => With("background", $"linear-gradient({radius}deg, {string.Join(',', color.Distinct())})", when && color.Length > 1);

    /// <summary>
    /// Adds a background property to the builder. The background is a gradient of a preset palette's colors.
    /// </summary>
    /// <param name="palette">Name of the palette whose colors should be used.</param>
    /// <param name="when">If false, no property will be added to the builder.</param>
    /// <returns>This MudExStyleBuilder instance.</returns>
    public MudExStyleBuilder WithBackground(Palette palette, bool when = true) => WithBackground(palette.AllColors(), 0, when);

    /// <summary>
    /// Adds a background property to the builder. The background is a gradient of a dark preset palette's colors.
    /// </summary>
    /// <param name="palette">Name of the palette whose colors should beta used.</param>
    /// <param name="when">If false, no property will be added to the builder.</param>
    /// <returns>This MudExStyleBuilder instance.</returns>
    public MudExStyleBuilder WithBackground(PaletteDark palette, bool when = true) => WithBackground(palette.AllColors(), 0, when);

    /// <summary>
    /// Adds a background property to the builder. The background is a gradient of a light preset palette's colors.
    /// </summary>
    /// <param name="palette">Name of the palette whose colors should beta used.</param>
    /// <param name="when">If false, no property will be added to the builder.</param>
    /// <returns>This MudExStyleBuilder instance.</returns>
    public MudExStyleBuilder WithBackground(PaletteLight palette, bool when = true) => WithBackground(palette.AllColors(), 0, when);

    /// <summary>
    /// Adds a background property to the builder. The background is a gradient of a theme's palette or dark palette.
    /// </summary>
    /// <param name="theme">Theme to use.</param>
    /// <param name="dark">If true, the dark palette will be used.</param>
    /// <param name="when">If false, no property will be added to the builder.</param>
    /// <returns>This MudExStyleBuilder instance.</returns>
    public MudExStyleBuilder WithBackground(MudTheme theme, bool dark, bool when) => WithBackground(dark ? theme.PaletteDark : theme.Palette, when);

    /// <summary>
    /// Adds a background property to the builder. The background is a gradient of a concatenated list of two palettes, default and dark.
    /// </summary>
    /// <param name="theme">The theme to use.</param>
    /// <param name="when">If false, no property will be added to the builder.</param>
    /// <returns>This MudExStyleBuilder instance.</returns>
    public MudExStyleBuilder WithBackground(MudTheme theme, bool when = true) => WithBackground(theme.Palette.AllColors().Concat(theme.PaletteDark.AllColors()).ToArray(), when);

    /// <summary>
    /// Adds a background property to the builder. The background is a gradient of all entries in the color array.
    /// </summary>
    /// <param name="color">Array of the CSS values of the colors of the gradient stops. Multiple colors can be used for a smoother transition.</param>
    /// <param name="when">If false, no property will be added to the builder.</param>
    /// <returns>This MudExStyleBuilder instance.</returns>
    public MudExStyleBuilder WithBackground(string[] color, bool when = true) => WithBackground(color, 0, when);

    /// <summary>
    /// Adds a background property to the builder. The background is a gradient of all entries in the MudExColor array.
    /// </summary>
    /// <param name="color">Array of MudExColor objects representing the colors of the gradient stops. Multiple colors can be used for a smoother transition.</param>
    /// <param name="when">If false, no property will be added to the builder.</param>
    /// <returns>This MudExStyleBuilder instance.</returns>
    public MudExStyleBuilder WithBackground(MudExColor[] color, bool when = true) => WithBackground(color.Select(c => c.ToCssStringValue()).ToArray(), when);

    /// <summary>
    /// Adds a background property to the builder. The background is a gradient of all entries in the MudColor array.
    /// </summary>
    /// <param name="color">Array of MudColor objects representing the color of the gradient stops. Multiple colors can be used for a smoother transition.</param>
    /// <param name="when">If false, no property will be added to the builder.</param>
    /// <returns>This MudExStyleBuilder instance.</returns>
    public MudExStyleBuilder WithBackground(MudColor[] color, bool when = true) => WithBackground(color.Select(c => new MudExColor(c)).ToArray(), when);

    /// <summary>
    /// Adds a background property to the builder. The background is a gradient of all entries in the System.Drawing.Color array.
    /// </summary>
    /// <param name="color">Array of System.Drawing.Color objects representing the color of the gradient stops. Multiple colors can be used for a smoother transition.</param>
    /// <param name="when">If false, no property will be added to the builder.</param>
    /// <returns>This MudExStyleBuilder instance.</returns>
    public MudExStyleBuilder WithBackground(System.Drawing.Color[] color, bool when = true) => WithBackground(color.Select(c => new MudExColor(c)).ToArray(), when);

    /// <summary>
    /// Adds a background property to the builder. The background is a gradient of all entries in the Color array.
    /// </summary>
    public MudExStyleBuilder WithBackground(Color[] color, bool when = true) => WithBackground(color.Select(c => new MudExColor(c)).ToArray(), when);

    /// <summary>
    /// Adds a background property to the builder. The background is a gradient of all entries in the Color array.
    /// </summary>
    public MudExStyleBuilder WithBackground(MudExColor[] color, int radius, bool when = true) => WithBackground(color.Select(c => c.ToCssStringValue()).ToArray(), radius, when);

    /// <summary>
    /// Adds a background property to the builder. The background is a gradient of all entries in the Color array.
    /// </summary>
    public MudExStyleBuilder WithBackground(MudColor[] color, int radius, bool when = true) => WithBackground(color.Select(c => new MudExColor(c)).ToArray(), radius, when);

    /// <summary>
    /// Adds a background property to the builder. The background is a gradient of all entries in the Color array.
    /// </summary>
    public MudExStyleBuilder WithBackground(System.Drawing.Color[] color, int radius, bool when = true) => WithBackground(color.Select(c => new MudExColor(c)).ToArray(), radius, when);

    /// <summary>
    /// Adds a background property to the builder. The background is a gradient of all entries in the Color array.
    /// </summary>
    public MudExStyleBuilder WithBackground(Color[] color, int radius, bool when = true) => WithBackground(color.Select(c => new MudExColor(c)).ToArray(), radius, when);

    /// <summary>
    /// Adds a background property to the builder.
    /// </summary>

    /// <summary>
    /// Specifies a background color using a MudExColor object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBackground(MudExColor color, bool when = true) => WithBackground(color.ToCssStringValue(), when);

    /// <summary>
    /// Specifies a background color using a CSS color string, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBackground(string background, bool when = true) => With("background", background, when);

    /// <summary>
    /// Specifies a background color using a MudColor object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBackground(MudColor color, bool when = true) => WithBackground(color.ToString(), when);

    /// <summary>
    /// Specifies a background color using a System.Drawing.Color object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBackground(System.Drawing.Color color, bool when = true) => WithBackground(color.ToMudColor(), when);

    /// <summary>
    /// Specifies a background color using a Color object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBackground(Color color, bool when = true) => WithBackground(color.CssVarDeclaration(), when);

    /// <summary>
    /// Specifies a background color using a MudExColor object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBackgroundColor(MudExColor color, bool when = true) => WithBackgroundColor(color.ToCssStringValue(), when);

    /// <summary>
    /// Specifies a background color using a CSS color string, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBackgroundColor(string background, bool when = true) => With("background-color", background, when);

    /// <summary>
    /// Specifies a background color using a MudColor object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBackgroundColor(MudColor color, bool when = true) => WithBackgroundColor(color.ToString(), when);

    /// <summary>
    /// Specifies a background color using a System.Drawing.Color object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBackgroundColor(System.Drawing.Color color, bool when = true) => WithBackgroundColor(color.ToMudColor(), when);

    /// <summary>
    /// Specifies a background color using a Color object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBackgroundColor(Color color, bool when = true) => WithBackgroundColor(color.CssVarDeclaration(), when);

    /// <summary>
    /// Specifies a background image using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBackgroundImage(string backgroundImage, bool when = true) => With("background-image", backgroundImage, when);

    /// <summary>
    /// Specifies the background size using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBackgroundSize(string backgroundSize, bool when = true) => With("background-size", backgroundSize, when);

    /// <summary>
    /// Specifies the display property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithDisplay(string display, bool when = true) => With("display", display, when);

    /// <summary>
    /// Specifies the display property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithDisplay(Display display, bool when = true) => WithDisplay(display.GetDescription(), when);

    /// <summary>
    /// Specifies the flex property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithFlex(string flex, bool when = true) => With("flex", flex, when);

    /// <summary>
    /// Specifies the justify-content property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithJustifyContent(string justifyContent, bool when = true) => With("justify-content", justifyContent, when);

    /// <summary>
    /// Specifies the align-items property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithAlignItems(string alignItems, bool when = true) => With("align-items", alignItems, when);

    /// <summary>
    /// Specifies the align-items property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithAlignItems(MudBlazor.Extensions.Core.Css.AlignItems alignItems, bool when = true) => With("align-items", alignItems.GetDescription(), when);

    /// <summary>
    /// Specifies the position property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPosition(string position, bool when = true) => With("position", position, when);

    /// <summary>
    /// Specifies the position property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPosition(MudBlazor.Extensions.Core.Css.Position position, bool when = true) => WithPosition(position.GetDescription(), when);

    /// <summary>
    /// Specifies the top property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTop(string top, bool when = true) => With("top", top, when);

    /// <summary>
    /// Specifies the bottom property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBottom(string bottom, bool when = true) => With("bottom", bottom, when);

    /// <summary>
    /// Specifies the left property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithLeft(string left, bool when = true) => With("left", left, when);

    /// <summary>
    /// Specifies the right property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithRight(string right, bool when = true) => With("right", right, when);

    /// <summary>
    /// Specifies the z-index property using an integer value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithZIndex(int zIndex, bool when = true) => With("z-index", zIndex.ToString(), when);

    /// <summary>
    /// Specifies the font-size property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithFontSize(string fontSize, bool when = true) => With("font-size", fontSize, when);

    /// <summary>
    /// Specifies the font-weight property using a FontWeight enumeration value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithFontWeight(FontWeight fontWeight, bool when = true) => WithFontWeight(Nextended.Core.Helper.EnumExtensions.ToDescriptionString(fontWeight), when);

    /// <summary>
    /// Specifies the font-weight property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithFontWeight(string fontWeight, bool when = true) => With("font-weight", fontWeight, when);

    /// <summary>
    /// Specifies the text-align property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTextAlign(string textAlign, bool when = true) => With("text-align", textAlign, when);

    /// <summary>
    /// Specifies the text-decoration property using a TextDecoration enumeration value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTextDecoration(TextDecoration textDecoration, bool when = true) => WithTextDecoration(Nextended.Core.Helper.EnumExtensions.ToDescriptionString(textDecoration), when);

    /// <summary>
    /// Specifies the text-decoration property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTextDecoration(string textDecoration, bool when = true) => With("text-decoration", textDecoration, when);

    /// <summary>
    /// Specifies the letter-spacing property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithLetterSpacing(string letterSpacing, bool when = true) => With("letter-spacing", letterSpacing, when);

    /// <summary>
    /// Specifies the line-height property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithLineHeight(string lineHeight, bool when = true) => With("line-height", lineHeight, when);

    /// <summary>
    /// Specifies the text-transform property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTextTransform(string textTransform, bool when = true) => With("text-transform", textTransform, when);

    /// <summary>
    /// Specifies the text-transform property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTextTransform(TextTransform textTransform, bool when = true) => WithTextTransform(textTransform.GetDescription(), when);

    /// <summary>
    /// Specifies the white-space property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithWhiteSpace(string whiteSpace, bool when = true) => With("white-space", whiteSpace, when);

    /// <summary>
    /// Specifies the white-space property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithWhiteSpace(WhiteSpace whiteSpace, bool when = true) => With("white-space", whiteSpace.GetDescription(), when);

    /// <summary>
    /// Specifies the width property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithWidth(string width, bool when = true) => With("width", width, when);

    /// <summary>
    /// Specifies the width property using a numeric value and CSS unit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithWidth(double width, CssUnit unit, bool when = true) => WithWidth(new MudExSize<double>(width, unit).ToString(), when);

    /// <summary>
    /// Specifies the width property using a MudExSize value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithWidth(MudExSize<double> size, bool when = true) => WithWidth(size.ToString(), when);

    /// <summary>
    /// Specifies the height property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithHeight(string height, bool when = true) => With("height", height, when);

    /// <summary>
    /// Specifies the height property using a numeric value and CSS unit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithHeight(double height, CssUnit unit, bool when = true) => WithHeight(new MudExSize<double>(height, unit).ToString(), when);

    /// <summary>
    /// Specifies the height property using a MudExSize value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithHeight(MudExSize<double> size, bool when = true) => WithHeight(size.ToString(), when);

    /// <summary>
    /// Specifies the opacity property using a double value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithOpacity(double opacity, bool when = true) => WithOpacity(DoubleToString(opacity), when);

    /// <summary>
    /// Specifies the opacity property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithOpacity(string opacity, bool when = true) => With("opacity", opacity, when);

    /// <summary>
    /// Specifies the margin property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMargin(string margin, bool when = true) => With("margin", margin, when);

    /// <summary>
    /// Specifies the margin property using a numeric value and CSS unit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMargin(double margin, CssUnit unit, bool when = true) => WithMargin(new MudExSize<double>(margin, unit).ToString(), when);

    /// <summary>
    /// Specifies the margin property using a MudExSize value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMargin(MudExSize<double> size, bool when = true) => WithMargin(size.ToString(), when);

    /// <summary>
    /// Specifies the padding property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPadding(string padding, bool when = true) => With("padding", padding, when);

    /// <summary>
    /// Specifies the padding property using a numeric value and CSS unit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPadding(double padding, CssUnit unit, bool when = true) => WithPadding(new MudExSize<double>(padding, unit).ToString(), when);

    /// <summary>
    /// Specifies the padding property using a MudExSize value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPadding(MudExSize<double> size, bool when = true) => WithPadding(size.ToString(), when);

    /// <summary>
    /// Specifies the overflow property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithOverflow(string overflow, bool when = true) => With("overflow", overflow, when);


    /// <summary>
    /// Specifies the overflow property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithOverflow(Overflow overflow, bool when = true) => WithOverflow(overflow.GetDescription(), when);

    /// <summary>
    /// Specifies a hidden overflow if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithoutOverflow(bool when = true) => WithOverflow(Overflow.Hidden, when);

    /// <summary>
    /// Specifies the cursor property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithCursor(string cursor, bool when = true) => With("cursor", cursor, when);

    /// <summary>
    /// Specifies the cursor property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithCursor(Cursor cursor, bool when = true) => With("cursor", cursor.GetDescription(), when);

    /// <summary>
    /// Specifies the list-style-type property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithListStyleType(string listStyleType, bool when = true) => With("list-style-type", listStyleType, when);

    /// <summary>
    /// Specifies the transition property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTransition(string transition, bool when = true) => With("transition", transition, when);

    /// <summary>
    /// Specifies the transform property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTransform(string transform, bool when = true) => With("transform", transform, when);

    /// <summary>
    /// Specifies the flex-direction property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithFlexDirection(string flexDirection, bool when = true) => With("flex-direction", flexDirection, when);

    /// <summary>
    /// Specifies the flex-wrap property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithFlexWrap(string flexWrap, bool when = true) => With("flex-wrap", flexWrap, when);

    /// <summary>
    /// Specifies the box-shadow property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBoxShadow(string boxShadow, bool when = true) => With("box-shadow", boxShadow, when);

    /// <summary>
    /// Specifies the text-shadow property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTextShadow(string textShadow, bool when = true) => With("text-shadow", textShadow, when);

    /// <summary>
    /// Specifies the word-break property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithWordBreak(string wordBreak, bool when = true) => With("word-break", wordBreak, when);

    /// <summary>
    /// Specifies the word-spacing property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithWordSpacing(string wordSpacing, bool when = true) => With("word-spacing", wordSpacing, when);

    /// <summary>
    /// Specifies the back face visibility property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBackfaceVisibility(string backfaceVisibility, bool when = true) => With("backface-visibility", backfaceVisibility, when);

    /// <summary>
    /// Specifies the outline property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithOutline(string outline, bool when = true) => With("outline", outline, when);

    /// <summary>
    /// Specifies the max-width property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMaxWidth(string maxWidth, bool when = true) => With("max-width", maxWidth, when);

    /// <summary>
    /// Specifies the min-width property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMinWidth(string minWidth, bool when = true) => With("min-width", minWidth, when);

    /// <summary>
    /// Specifies the max-height property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMaxHeight(string maxHeight, bool when = true) => With("max-height", maxHeight, when);

    /// <summary>
    /// Specifies the min-height property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMinHeight(string minHeight, bool when = true) => With("min-height", minHeight, when);

    /// <summary>
    /// Specifies the resize property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithResize(string resize, bool when = true) => With("resize", resize, when);

    /// <summary>
    /// Specifies the visibility property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithVisibility(string visibility, bool when = true) => With("visibility", visibility, when);

    /// <summary>
    /// Specifies the visibility property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithVisibility(Visibility visibility, bool when = true) => WithVisibility(visibility.GetDescription(), when);

    /// <summary>
    /// Specifies the flex-basis property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithFlexBasis(string flexBasis, bool when = true) => With("flex-basis", flexBasis, when);

    /// <summary>
    /// Specifies the flex-grow property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithFlexGrow(string flexGrow, bool when = true) => With("flex-grow", flexGrow, when);

    /// <summary>
    /// Specifies the flex-shrink property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithFlexShrink(string flexShrink, bool when = true) => With("flex-shrink", flexShrink, when);

    /// <summary>
    /// Specifies the order property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithOrder(string order, bool when = true) => With("order", order, when);

    /// <summary>
    /// Specifies the grid-gap property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithGridGap(string gridGap, bool when = true) => With("grid-gap", gridGap, when);

    /// <summary>
    /// Specifies the grid-column property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithGridColumn(string gridColumn, bool when = true) => With("grid-column", gridColumn, when);

    /// <summary>
    /// Specifies the grid-row property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithGridRow(string gridRow, bool when = true) => With("grid-row", gridRow, when);

    /// <summary>
    /// Specifies the grid-template-columns property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithGridTemplateColumns(string gridTemplateColumns, bool when = true) => With("grid-template-columns", gridTemplateColumns, when);

    /// <summary>
    /// Specifies the grid-template-rows property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithGridTemplateRows(string gridTemplateRows, bool when = true) => With("grid-template-rows", gridTemplateRows, when);

    /// <summary>
    /// Specifies the object-fit property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithObjectFit(string objectFit, bool when = true) => With("object-fit", objectFit, when);

    /// <summary>
    /// Specifies the object-position property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithObjectPosition(string objectPosition, bool when = true) => With("object-position", objectPosition, when);

    /// <summary>
    /// Specifies the text-overflow property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTextOverflow(string textOverflow, bool when = true) => With("text-overflow", textOverflow, when);

    /// <summary>
    /// Specifies the vertical-align property using a CSS string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithVerticalAlign(string verticalAlign, bool when = true) => With("vertical-align", verticalAlign, when);

    /// <summary>
    /// Specifies both the min-width and min-height properties, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMinSize(string width, string height, bool when = true) => WithMinWidth(width, when).WithMinHeight(height, when);

    /// <summary>
    /// Specifies both the min-width and min-height properties using a numerical value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMinSize(double width, double height, CssUnit unit, bool when = true) => WithMinWidth(width, unit, when).WithMinHeight(height, unit, when);

    /// <summary>
    /// Specifies both the min-width and min-height properties using the same numerical value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMinSize(double size, CssUnit unit, bool when = true) => WithMinWidth(size, unit, when).WithMinHeight(size, unit, when);

    /// <summary>
    /// Specifies both the min-width and min-height properties using MudExSize objects, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMinSize(MudExSize<double> width, MudExSize<double> height, bool when = true) => WithMinWidth(width, when).WithMinHeight(height, when);

    /// <summary>
    /// Specifies both the min-width and min-height properties using a MudExDimension object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMinSize(MudExDimension size, bool when = true) => WithMinSize(size.Width, size.Height, when);

    /// <summary>
    /// Specifies both the max-width and max-height properties using string values, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMaxSize(string width, string height, bool when = true) => WithMaxWidth(width, when).WithMaxHeight(height, when);

    /// <summary>
    /// Specifies both the max-width and max-height properties using double values and a CssUnit for the values, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMaxSize(double width, double height, CssUnit unit, bool when = true) => WithMaxWidth(width, unit, when).WithMaxHeight(height, unit, when);

    /// <summary>
    /// Specifies both the max-width and max-height properties using the same double value and a CssUnit for the values, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMaxSize(double size, CssUnit unit, bool when = true) => WithMaxWidth(size, unit, when).WithMaxHeight(size, unit, when);

    /// <summary>
    /// Specifies both the max-width and max-height properties using MudExSize objects, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMaxSize(MudExSize<double> width, MudExSize<double> height, bool when = true) => WithMaxWidth(width, when).WithMaxHeight(height, when);

    /// <summary>
    /// Specifies both the max-width and max-height properties using a MudExDimension object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMaxSize(MudExDimension size, bool when = true) => WithMaxSize(size.Width, size.Height, when);

    /// <summary>
    /// Specifies both the width and height properties using string values, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithSize(string width, string height, bool when = true) => WithWidth(width, when).WithHeight(height, when);

    /// <summary>
    /// Specifies both the width and height properties using the same double value and a CssUnit for the values, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithSize(double size, CssUnit unit, bool when = true) => WithWidth(size, unit, when).WithHeight(size, unit, when);

    /// <summary>
    /// Specifies both the width and height properties using different double values and a CssUnit for the values, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithSize(double width, double height, CssUnit unit, bool when = true) => WithWidth(width, unit, when).WithHeight(height, unit, when);

    /// <summary>
    /// Specifies both the width and height properties using MudExSize objects, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithSize(MudExSize<double> width, MudExSize<double> height, bool when = true) => WithWidth(width, when).WithHeight(height, when);

    /// <summary>
    /// Specifies both the width and height properties using a MudExDimension object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithSize(MudExDimension size, bool when = true) => WithWidth(size.Width, when).WithHeight(size.Height, when);

    /// <summary>
    /// Specifies both the width and height properties using a MudExDimension object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithDimension(MudExDimension size, bool when = true) => WithWidth(size.Width, when).WithHeight(size.Height, when);

    /// <summary>
    /// Specifies the min-height property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMinHeight(double minHeight, CssUnit unit, bool when = true) => WithMinHeight(new MudExSize<double>(minHeight, unit), when);

    /// <summary>
    /// Specifies the min-height property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMinHeight(MudExSize<double> size, bool when = true) => WithMinHeight(size.ToString(), when);

    /// <summary>
    /// Specifies the min-width property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMinWidth(double minWidth, CssUnit unit, bool when = true) => WithMinWidth(new MudExSize<double>(minWidth, unit).ToString(), when);

    /// <summary>
    /// Specifies the min-width property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMinWidth(MudExSize<double> size, bool when = true) => WithMinWidth(size.ToString(), when);

    /// <summary>
    /// Specifies the max-height property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMaxHeight(double maxHeight, CssUnit unit, bool when = true) => WithMaxHeight(new MudExSize<double>(maxHeight, unit).ToString(), when);

    /// <summary>
    /// Specifies the max-height property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMaxHeight(MudExSize<double> size, bool when = true) => WithMaxHeight(size.ToString(), when);

    /// <summary>
    /// Specifies the max-width property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMaxWidth(double maxWidth, CssUnit unit, bool when = true) => WithMaxWidth(new MudExSize<double>(maxWidth, unit).ToString(), when);

    /// <summary>
    /// Specifies the max-width property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMaxWidth(MudExSize<double> size, bool when = true) => WithMaxWidth(size.ToString(), when);

    /// <summary>
    /// Specifies the padding-top property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPaddingTop(string paddingTop, bool when = true) => With("padding-top", paddingTop, when);

    /// <summary>
    /// Specifies the padding-top property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPaddingTop(double paddingTop, CssUnit unit, bool when = true) => WithPaddingTop(new MudExSize<double>(paddingTop, unit).ToString(), when);

    /// <summary>
    /// Specifies the padding-top property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPaddingTop(MudExSize<double> size, bool when = true) => WithPaddingTop(size.ToString(), when);

    /// <summary>
    /// Specifies the padding-right property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPaddingRight(string paddingRight, bool when = true) => With("padding-right", paddingRight, when);

    /// <summary>
    /// Specifies the padding-right property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPaddingRight(double paddingRight, CssUnit unit, bool when = true) => WithPaddingRight(new MudExSize<double>(paddingRight, unit).ToString(), when);

    /// <summary>
    /// Specifies the padding-right property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPaddingRight(MudExSize<double> size, bool when = true) => WithPaddingRight(size.ToString(), when);

    /// <summary>
    /// Specifies the padding-bottom property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPaddingBottom(string paddingBottom, bool when = true) => With("padding-bottom", paddingBottom, when);

    /// <summary>
    /// Specifies the padding-bottom property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPaddingBottom(double paddingBottom, CssUnit unit, bool when = true) => WithPaddingBottom(new MudExSize<double>(paddingBottom, unit).ToString(), when);

    /// <summary>
    /// Specifies the padding-bottom property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPaddingBottom(MudExSize<double> size, bool when = true) => WithPaddingBottom(size.ToString(), when);

    /// <summary>
    /// Specifies the padding-left property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPaddingLeft(string paddingLeft, bool when = true) => With("padding-left", paddingLeft, when);

    /// <summary>
    /// Specifies the padding-left property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPaddingLeft(double paddingLeft, CssUnit unit, bool when = true) => WithPaddingLeft(new MudExSize<double>(paddingLeft, unit).ToString(), when);

    /// <summary>
    /// Specifies the padding-left property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPaddingLeft(MudExSize<double> size, bool when = true) => WithPaddingLeft(size.ToString(), when);

    /// <summary>
    /// Specifies the margin-top property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMarginTop(string marginTop, bool when = true) => With("margin-top", marginTop, when);

    /// <summary>
    /// Specifies the margin-top property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMarginTop(double marginTop, CssUnit unit, bool when = true) => WithMarginTop(new MudExSize<double>(marginTop, unit).ToString(), when);

    /// <summary>
    /// Specifies the margin-top property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMarginTop(MudExSize<double> size, bool when = true) => WithMarginTop(size.ToString(), when);

    /// <summary>
    /// Specifies the margin-right property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMarginRight(string marginRight, bool when = true) => With("margin-right", marginRight, when);

    /// <summary>
    /// Specifies the margin-right property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMarginRight(double marginRight, CssUnit unit, bool when = true) => WithMarginRight(new MudExSize<double>(marginRight, unit).ToString(), when);

    /// <summary>
    /// Specifies the margin-right property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMarginRight(MudExSize<double> size, bool when = true) => WithMarginRight(size.ToString(), when);

    /// <summary>
    /// Specifies the margin-bottom property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMarginBottom(string marginBottom, bool when = true) => With("margin-bottom", marginBottom, when);

    /// <summary>
    /// Specifies the margin-bottom property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMarginBottom(double marginBottom, CssUnit unit, bool when = true) => WithMarginBottom(new MudExSize<double>(marginBottom, unit).ToString(), when);

    /// <summary>
    /// Specifies the margin-bottom property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMarginBottom(MudExSize<double> size, bool when = true) => WithMarginBottom(size.ToString(), when);

    /// <summary>
    /// Specifies the margin-left property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMarginLeft(string marginLeft, bool when = true) => With("margin-left", marginLeft, when);

    /// <summary>
    /// Specifies the margin-left property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMarginLeft(double marginLeft, CssUnit unit, bool when = true) => WithMarginLeft(new MudExSize<double>(marginLeft, unit).ToString(), when);

    /// <summary>
    /// Specifies the margin-left property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithMarginLeft(MudExSize<double> size, bool when = true) => WithMarginLeft(size.ToString(), when);


    /// <summary>
    /// Specifies the font-size property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithFontSize(double fontSize, CssUnit unit, bool when = true) => WithFontSize(new MudExSize<double>(fontSize, unit).ToString(), when);

    /// <summary>
    /// Specifies the font-size property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithFontSize(MudExSize<double> size, bool when = true) => WithFontSize(size.ToString(), when);

    /// <summary>
    /// Specifies the letter-spacing property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithLetterSpacing(double letterSpacing, CssUnit unit, bool when = true) => WithLetterSpacing(new MudExSize<double>(letterSpacing, unit).ToString(), when);

    /// <summary>
    /// Specifies the letter-spacing property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithLetterSpacing(MudExSize<double> size, bool when = true) => WithLetterSpacing(size.ToString(), when);

    /// <summary>
    /// Specifies the line-height property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithLineHeight(double lineHeight, CssUnit unit, bool when = true) => WithLineHeight(new MudExSize<double>(lineHeight, unit).ToString(), when);

    /// <summary>
    /// Specifies the line-height property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithLineHeight(MudExSize<double> size, bool when = true) => WithLineHeight(size.ToString(), when);

    /// <summary>
    /// Specifies the word-spacing property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithWordSpacing(double wordSpacing, CssUnit unit, bool when = true) => WithWordSpacing(new MudExSize<double>(wordSpacing, unit).ToString(), when);

    /// <summary>
    /// Specifies the word-spacing property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithWordSpacing(MudExSize<double> size, bool when = true) => WithWordSpacing(size.ToString(), when);

    /// <summary>
    /// Specifies the text-indent property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTextIndent(string textIndent, bool when = true) => With("text-indent", textIndent, when);

    /// <summary>
    /// Specifies the text-indent property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTextIndent(double textIndent, CssUnit unit, bool when = true) => WithTextIndent(new MudExSize<double>(textIndent, unit).ToString(), when);

    /// <summary>
    /// Specifies the text-indent property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTextIndent(MudExSize<double> size, bool when = true) => WithTextIndent(size.ToString(), when);

    /// <summary>
    /// Specifies the column-gap property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithColumnGap(string columnGap, bool when = true) => With("column-gap", columnGap, when);

    /// <summary>
    /// Specifies the column-gap property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithColumnGap(double columnGap, CssUnit unit, bool when = true) => WithColumnGap(new MudExSize<double>(columnGap, unit).ToString(), when);

    /// <summary>
    /// Specifies the column-gap property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithColumnGap(MudExSize<double> size, bool when = true) => WithColumnGap(size.ToString(), when);

    /// <summary>
    /// Specifies the column-width property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithColumnWidth(string columnWidth, bool when = true) => With("column-width", columnWidth, when);

    /// <summary>
    /// Specifies the column-width property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithColumnWidth(double columnWidth, CssUnit unit, bool when = true) => WithColumnWidth(new MudExSize<double>(columnWidth, unit).ToString(), when);

    /// <summary>
    /// Specifies the column-width property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithColumnWidth(MudExSize<double> size, bool when = true) => WithColumnWidth(size.ToString(), when);

    /// <summary>
    /// Specifies the top property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTop(double top, CssUnit unit, bool when = true) => WithTop(new MudExSize<double>(top, unit).ToString(), when);

    /// <summary>
    /// Specifies the top property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTop(MudExSize<double> size, bool when = true) => WithTop(size.ToString(), when);

    /// <summary>
    /// Specifies the right property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithRight(double right, CssUnit unit, bool when = true) => WithRight(new MudExSize<double>(right, unit).ToString(), when);

    /// <summary>
    /// Specifies the right property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithRight(MudExSize<double> size, bool when = true) => WithRight(size.ToString(), when);

    /// <summary>
    /// Specifies the bottom property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBottom(double bottom, CssUnit unit, bool when = true) => WithBottom(new MudExSize<double>(bottom, unit).ToString(), when);

    /// <summary>
    /// Specifies the bottom property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBottom(MudExSize<double> size, bool when = true) => WithBottom(size.ToString(), when);

    /// <summary>
    /// Specifies the left property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithLeft(double left, CssUnit unit, bool when = true) => WithLeft(new MudExSize<double>(left, unit).ToString(), when);

    /// <summary>
    /// Specifies the left property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithLeft(MudExSize<double> size, bool when = true) => WithLeft(size.ToString(), when);

    /// <summary>
    /// Specifies the translation property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTranslate(string translate, bool when = true) => With("translate", translate, when);

    /// <summary>
    /// Specifies the translation property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTranslate(double translate, CssUnit unit, bool when = true) => WithTranslate(new MudExSize<double>(translate, unit).ToString(), when);

    /// <summary>
    /// Specifies the translation property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTranslate(MudExSize<double> size, bool when = true) => WithTranslate(size.ToString(), when);

    /// <summary>
    /// Specifies the translateX property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTranslateX(string translateX, bool when = true) => With("translateX", translateX, when);

    /// <summary>
    /// Specifies the translateX property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTranslateX(double translateX, CssUnit unit, bool when = true) => WithTranslateX(new MudExSize<double>(translateX, unit).ToString(), when);

    /// <summary>
    /// Specifies the translateX property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTranslateX(MudExSize<double> size, bool when = true) => WithTranslateX(size.ToString(), when);

    /// <summary>
    /// Specifies the translateY property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTranslateY(string translateY, bool when = true) => With("translateY", translateY, when);

    /// <summary>
    /// Specifies the translateY property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTranslateY(double translateY, CssUnit unit, bool when = true) => WithTranslateY(new MudExSize<double>(translateY, unit).ToString(), when);

    /// <summary>
    /// Specifies the translateY property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTranslateY(MudExSize<double> size, bool when = true) => WithTranslateY(size.ToString(), when);

    /// <summary>
    /// Specifies the translateZ property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTranslateZ(string translateZ, bool when = true) => With("translateZ", translateZ, when);

    /// <summary>
    /// Specifies the translateZ property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTranslateZ(double translateZ, CssUnit unit, bool when = true) => WithTranslateZ(new MudExSize<double>(translateZ, unit).ToString(), when);

    /// <summary>
    /// Specifies the translateZ property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTranslateZ(MudExSize<double> size, bool when = true) => WithTranslateZ(size.ToString(), when);

    /// <summary>
    /// Specifies the 3d translation property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTranslate3d(string translate3d, bool when = true) => With("translate3d", translate3d, when);

    /// <summary>
    /// Specifies the rotate property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithRotate(string rotate, bool when = true) => With("rotate", rotate, when);

    /// <summary>
    /// Specifies the rotate property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithRotate(double rotate, CssUnit unit, bool when = true) => WithRotate(new MudExSize<double>(rotate, unit).ToString(), when);

    /// <summary>
    /// Specifies the rotate property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithRotate(MudExSize<double> size, bool when = true) => WithRotate(size.ToString(), when);

    /// <summary>
    /// Specifies the rotateX property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithRotateX(string rotateX, bool when = true) => With("rotateX", rotateX, when);

    /// <summary>
    /// Specifies the rotateX property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithRotateX(double rotateX, CssUnit unit, bool when = true) => WithRotateX(new MudExSize<double>(rotateX, unit).ToString(), when);

    /// <summary>
    /// Specifies the rotateX property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithRotateX(MudExSize<double> size, bool when = true) => WithRotateX(size.ToString(), when);

    /// <summary>
    /// Specifies the rotateY property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithRotateY(string rotateY, bool when = true) => With("rotateY", rotateY, when);

    /// <summary>
    /// Specifies the rotateY property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithRotateY(double rotateY, CssUnit unit, bool when = true) => WithRotateY(new MudExSize<double>(rotateY, unit).ToString(), when);

    /// <summary>
    /// Specifies the rotateY property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithRotateY(MudExSize<double> size, bool when = true) => WithRotateY(size.ToString(), when);

    /// <summary>
    /// Specifies the rotateZ property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithRotateZ(string rotateZ, bool when = true) => With("rotateZ", rotateZ, when);

    /// <summary>
    /// Specifies the rotateZ property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithRotateZ(double rotateZ, CssUnit unit, bool when = true) => WithRotateZ(new MudExSize<double>(rotateZ, unit).ToString(), when);

    /// <summary>
    /// Specifies the rotateZ property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithRotateZ(MudExSize<double> size, bool when = true) => WithRotateZ(size.ToString(), when);

    /// <summary>
    /// Specifies the scale property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithScale(string scale, bool when = true) => With("scale", scale, when);

    /// <summary>
    /// Specifies the scale property using a double value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithScale(double scale, bool when = true) => WithScale(DoubleToString(scale), when);

    /// <summary>
    /// Specifies the scaleX property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithScaleX(string scaleX, bool when = true) => With("scaleX", scaleX, when);

    /// <summary>
    /// Specifies the scaleX property using a double value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithScaleX(double scaleX, bool when = true) => WithScaleX(DoubleToString(scaleX), when);

    /// <summary>
    /// Specifies the scaleY property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithScaleY(string scaleY, bool when = true) => With("scaleY", scaleY, when);

    /// <summary>
    /// Specifies the scaleY property using a double value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithScaleY(double scaleY, bool when = true) => WithScaleY(DoubleToString(scaleY), when);

    /// <summary>
    /// Specifies the scaleZ property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithScaleZ(string scaleZ, bool when = true) => With("scaleZ", scaleZ, when);

    /// <summary>
    /// Specifies the scaleZ property using a double value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithScaleZ(double scaleZ, bool when = true) => WithScaleZ(DoubleToString(scaleZ), when);

    /// <summary>
    /// Specifies the scale 3d property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithScale3d(string scale3d, bool when = true) => With("scale3d", scale3d, when);

    /// <summary>
    /// Specifies the scale 3d property using a double value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithScale3d(double scale3d, bool when = true) => WithScale3d(DoubleToString(scale3d), when);

    /// <summary>
    /// Specifies the skew property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithSkew(string skew, bool when = true) => With("skew", skew, when);

    /// <summary>
    /// Specifies the skew property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithSkew(double skew, CssUnit unit, bool when = true) => WithSkew(new MudExSize<double>(skew, unit).ToString(), when);

    /// <summary>
    /// Specifies the skew property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithSkew(MudExSize<double> size, bool when = true) => WithSkew(size.ToString(), when);

    /// <summary>
    /// Specifies the skewX property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithSkewX(string skewX, bool when = true) => With("skewX", skewX, when);

    /// <summary>
    /// Specifies the skewX property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithSkewX(double skewX, CssUnit unit, bool when = true) => WithSkewX(new MudExSize<double>(skewX, unit).ToString(), when);

    /// <summary>
    /// Specifies the skewX property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithSkewX(MudExSize<double> size, bool when = true) => WithSkewX(size.ToString(), when);

    /// <summary>
    /// Specifies the skewY property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithSkewY(string skewY, bool when = true) => With("skewY", skewY, when);

    /// <summary>
    /// Specifies the skewY property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithSkewY(double skewY, CssUnit unit, bool when = true) => WithSkewY(new MudExSize<double>(skewY, unit).ToString(), when);

    /// <summary>
    /// Specifies the skewY property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithSkewY(MudExSize<double> size, bool when = true) => WithSkewY(size.ToString(), when);

    /// <summary>
    /// Specifies the perspective property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPerspective(string perspective, bool when = true) => With("perspective", perspective, when);

    /// <summary>
    /// Specifies the perspective property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPerspective(double perspective, CssUnit unit, bool when = true) => WithPerspective(new MudExSize<double>(perspective, unit).ToString(), when);

    /// <summary>
    /// Specifies the perspective property using a MudExSize object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithPerspective(MudExSize<double> size, bool when = true) => WithPerspective(size.ToString(), when);

    /// <summary>
    /// Specifies the font-family property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithFontFamily(string fontFamily, bool when = true) => With("font-family", fontFamily, when);

    /// <summary>
    /// Specifies the font-style property using a FontStyle enum value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithFontStyle(FontStyle fontStyle, bool when = true) => WithFontStyle(Nextended.Core.Helper.EnumExtensions.ToDescriptionString(fontStyle), when);

    /// <summary>
    /// Specifies the font-style property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithFontStyle(string fontStyle, bool when = true) => With("font-style", fontStyle, when);

    /// <summary>
    /// Specifies the text-justify property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithTextJustify(string textJustify, bool when = true) => With("text-justify", textJustify, when);

    /// <summary>
    /// Specifies the background-position property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBackgroundPosition(string backgroundPosition, bool when = true) => With("background-position", backgroundPosition, when);

    /// <summary>
    /// Specifies the background-repeat property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBackgroundRepeat(string backgroundRepeat, bool when = true) => With("background-repeat", backgroundRepeat, when);

    /// <summary>
    /// Specifies the border-width, border-style and border-color properties using a MudExSize object, BorderStyle enum value and MudExColor object, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorder(MudExSize<double> size, BorderStyle style, MudExColor color, bool when)
        => WithBorderWidth(size, when).WithBorderStyle(style, when).WithBorderColor(color, when);

    /// <summary>
    /// Specifies the border-width, border-style and border-color properties using a MudExSize object, BorderStyle enum value and MudExColor object.
    /// </summary>
    public MudExStyleBuilder WithBorder(MudExSize<double> size, BorderStyle style, MudExColor color)
       => WithBorderWidth(size).WithBorderStyle(style).WithBorderColor(color);

    /// <summary>
    /// Specifies the border property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorder(string border, bool when = true) => With("border", border, when);

    /// <summary>
    /// Specifies the border-radius property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderRadius(string borderRadius, bool when = true) => With("border-radius", borderRadius, when);

    /// <summary>
    /// Specifies the border-radius property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderRadius(double radius, CssUnit unit, bool when = true) => WithBorderRadius(new MudExSize<double>(radius, unit).ToString(), when);
    /// <summary>
    /// Specifies the border-width property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderWidth(string borderWidth, bool when = true) => With("border-width", borderWidth, when);

    /// <summary>
    /// Specifies the border-width property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderWidth(double borderWidth, CssUnit unit, bool when = true) => WithBorderWidth(new MudExSize<double>(borderWidth, unit).ToString(), when);

    /// <summary>
    /// Specifies the border-width property using a MudExSize value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderWidth(MudExSize<double> size, bool when = true) => WithBorderWidth(size.ToString(), when);

    /// <summary>
    /// Specifies the border-top-width property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderTopWidth(string borderTopWidth, bool when = true) => With("border-top-width", borderTopWidth, when);

    /// <summary>
    /// Specifies the border-top-width property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderTopWidth(double borderTopWidth, CssUnit unit, bool when = true) => WithBorderTopWidth(new MudExSize<double>(borderTopWidth, unit).ToString(), when);

    /// <summary>
    /// Specifies the border-top-width property using a MudExSize value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderTopWidth(MudExSize<double> size, bool when = true) => WithBorderTopWidth(size.ToString(), when);

    /// <summary>
    /// Specifies the border-right-width property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderRightWidth(string borderRightWidth, bool when = true) => With("border-right-width", borderRightWidth, when);

    /// <summary>
    /// Specifies the border-right-width property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderRightWidth(double borderRightWidth, CssUnit unit, bool when = true) => WithBorderRightWidth(new MudExSize<double>(borderRightWidth, unit).ToString(), when);

    /// <summary>
    /// Specifies the border-right-width property using a MudExSize value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderRightWidth(MudExSize<double> size, bool when = true) => WithBorderRightWidth(size.ToString(), when);

    /// <summary>
    /// Specifies the border-bottom-width property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderBottomWidth(string borderBottomWidth, bool when = true) => With("border-bottom-width", borderBottomWidth, when);

    /// <summary>
    /// Specifies the border-bottom-width property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderBottomWidth(double borderBottomWidth, CssUnit unit, bool when = true) => WithBorderBottomWidth(new MudExSize<double>(borderBottomWidth, unit).ToString(), when);

    /// <summary>
    /// Specifies the border-bottom-width property using a MudExSize value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderBottomWidth(MudExSize<double> size, bool when = true) => WithBorderBottomWidth(size.ToString(), when);

    /// <summary>
    /// Specifies the border-left-width property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderLeftWidth(string borderLeftWidth, bool when = true) => With("border-left-width", borderLeftWidth, when);

    /// <summary>
    /// Specifies the border-left-width property using a double value and a CssUnit, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderLeftWidth(double borderLeftWidth, CssUnit unit, bool when = true) => WithBorderLeftWidth(new MudExSize<double>(borderLeftWidth, unit).ToString(), when);

    /// <summary>
    /// Specifies the border-left-width property using a MudExSize value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderLeftWidth(MudExSize<double> size, bool when = true) => WithBorderLeftWidth(size.ToString(), when);

    /// <summary>
    /// Specifies the border-color property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderColor(string borderColor, bool when = true) => With("border-color", borderColor, when);

    /// <summary>
    /// Specifies the border-color property using a MudExColor value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderColor(MudExColor color, bool when = true) => WithBorderColor(color.ToCssStringValue(), when);

    /// <summary>
    /// Specifies the border-color property using a MudColor value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderColor(MudColor color, bool when = true) => WithBorderColor(color.ToString(), when);

    /// <summary>
    /// Specifies the border-color property using a System.Drawing.Color value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderColor(System.Drawing.Color color, bool when = true) => WithBorderColor(color.ToMudColor(), when);

    /// <summary>
    /// Specifies the border-color property using a Color value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderColor(Color color, bool when = true) => WithBorderColor(color.CssVarDeclaration(), when);

    /// <summary>
    /// Specifies the border-style property using a BorderStyle enum value, if the 'when' condition is true. Converts the enum to a string.
    /// </summary>
    public MudExStyleBuilder WithBorderStyle(BorderStyle borderStyle, bool when = true) => WithBorderStyle(Nextended.Core.Helper.EnumExtensions.ToDescriptionString(borderStyle), when);

    /// <summary>
    /// Specifies the border-style property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithBorderStyle(string borderStyle, bool when = true) => With("border-style", borderStyle, when);

    /// <summary>
    /// Specifies the outline property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithOutline(MudExSize<double> size, BorderStyle style, MudExColor color, bool when)
        => WithOutlineWidth(size, when).WithOutlineStyle(style, when).WithOutlineColor(color, when);

    /// <summary>
    /// Specifies the outline property using a string value, if the 'when' condition is true.
    /// </summary>
    public MudExStyleBuilder WithOutline(MudExSize<double> size, BorderStyle style, MudExColor color)
       => WithOutlineWidth(size).WithOutlineStyle(style).WithOutlineColor(color);

    /// <summary>
    /// Specifies the outline radius of an element.
    /// </summary>
    public MudExStyleBuilder WithOutlineRadius(string outlineRadius, bool when = true) => With("outline-radius", outlineRadius, when);

    /// <summary>
    /// Specifies the outline width of an element.
    /// </summary>
    public MudExStyleBuilder WithOutlineWidth(string outlineWidth, bool when = true) => With("outline-width", outlineWidth, when);

    /// <summary>
    /// Specifies the width of the top outline of an element.
    /// </summary>
    public MudExStyleBuilder WithOutlineTopWidth(string outlineTopWidth, bool when = true) => With("outline-top-width", outlineTopWidth, when);

    /// <summary>
    /// Specifies the width of the right outline of an element.
    /// </summary>
    public MudExStyleBuilder WithOutlineRightWidth(string outlineRightWidth, bool when = true) => With("outline-right-width", outlineRightWidth, when);

    /// <summary>
    /// Specifies the width of the bottom outline of an element.
    /// </summary>
    public MudExStyleBuilder WithOutlineBottomWidth(string outlineBottomWidth, bool when = true) => With("outline-bottom-width", outlineBottomWidth, when);

    /// <summary>
    /// Specifies the width of the left outline of an element.
    /// </summary>
    public MudExStyleBuilder WithOutlineLeftWidth(string outlineLeftWidth, bool when = true) => With("outline-left-width", outlineLeftWidth, when);

    /// <summary>
    /// Specifies the color of the outline of an element.
    /// </summary>
    public MudExStyleBuilder WithOutlineColor(string outlineColor, bool when = true) => With("outline-color", outlineColor, when);

    /// <summary>
    /// Specifies the style of the outline of an element.
    /// </summary>
    public MudExStyleBuilder WithOutlineStyle(string outlineStyle, bool when = true) => With("outline-style", outlineStyle, when);

    /// <summary>
    /// Specifies how an element should float.
    /// </summary>
    public MudExStyleBuilder WithFloat(string floatOption, bool when = true) => With("float", floatOption, when);

    /// <summary>
    /// Specifies how an element should float.
    /// </summary>
    public MudExStyleBuilder WithFloat(Float floatOption, bool when = true) => With("float", floatOption.GetDescription(), when);

    /// <summary>
    /// Specifies which sides of an element other floating elements are not allowed.
    /// </summary>
    public MudExStyleBuilder WithClear(string clear, bool when = true) => With("clear", clear, when);

    /// <summary>
    /// Specifies which sides of an element other floating elements are not allowed.
    /// </summary>
    public MudExStyleBuilder WithClear(Clear clear, bool when = true) => With("clear", clear.GetDescription(), when);

    /// <summary>
    /// Specifies whether to clip content, render scrollbars or just display overflow content of an element, for x-axis.
    /// </summary>
    public MudExStyleBuilder WithOverflowX(string overflowX, bool when = true) => With("overflow-x", overflowX, when);

    /// <summary>
    /// Specifies whether to clip content, render scrollbars or just display overflow content of an element, for x-axis.
    /// </summary>
    public MudExStyleBuilder WithOverflowX(Overflow overflowX, bool when = true) => With("overflow-x", overflowX.GetDescription(), when);

    /// <summary>
    /// Specifies whether to clip content, render scrollbars or just display overflow content of an element, for y-axis.
    /// </summary>
    public MudExStyleBuilder WithOverflowY(string overflowY, bool when = true) => With("overflow-y", overflowY, when);

    /// <summary>
    /// Specifies whether to clip content, render scrollbars or just display overflow content of an element, for y-axis.
    /// </summary>
    public MudExStyleBuilder WithOverflowY(Overflow overflowY, bool when = true) => With("overflow-y", overflowY.GetDescription(), when);

    /// <summary>
    /// Specifies the text direction/writing direction within a block-level element.
    /// </summary>
    public MudExStyleBuilder WithDirection(string direction, bool when = true) => With("direction", direction, when);

    /// <summary>
    /// Specifies the text direction/writing direction within a block-level element.
    /// </summary>
    public MudExStyleBuilder WithDirection(Direction direction, bool when = true) => With("direction", direction.GetDescription(), when);

    /// <summary>
    /// Specifies the column width and column count of an element.
    /// </summary>
    public MudExStyleBuilder WithColumns(string columns, bool when = true) => With("columns", columns, when);

    /// <summary>
    /// Specifies the number of columns an element should be divided into.
    /// </summary>
    public MudExStyleBuilder WithColumnCount(int columnCount, bool when = true) => With("column-count", columnCount.ToString(), when);

    /// <summary>
    /// Specifies how to fill columns.
    /// </summary>
    public MudExStyleBuilder WithColumnFill(string columnFill, bool when = true) => With("column-fill", columnFill, when);

    /// <summary>
    /// Specifies the width, style, and color of the rule between columns.
    /// </summary>
    public MudExStyleBuilder WithColumnRule(string columnRule, bool when = true) => With("column-rule", columnRule, when);

    /// <summary>
    /// Specifies how many columns an element should span across.
    /// </summary>
    public MudExStyleBuilder WithColumnSpan(string columnSpan, bool when = true) => With("column-span", columnSpan, when);

    /// <summary>
    /// Specifies the size of the gap between columns.
    /// </summary>
    public MudExStyleBuilder WithGap(string gap, bool when = true) => With("gap", gap, when);


    /// <summary>
    /// Specifies the 'outline-radius' CSS property with a double value and CSS unit.
    /// </summary>
    public MudExStyleBuilder WithOutlineRadius(double radius, CssUnit unit, bool when = true) => WithOutlineRadius(new MudExSize<double>(radius, unit).ToString(), when);

    /// <summary>
    /// Specifies the 'outline-width' CSS property with a double value and CSS unit.
    /// </summary>
    public MudExStyleBuilder WithOutlineWidth(double outlineWidth, CssUnit unit, bool when = true) => WithOutlineWidth(new MudExSize<double>(outlineWidth, unit).ToString(), when);

    /// <summary>
    /// Specifies the 'outline-width' CSS property with a size.
    /// </summary>
    public MudExStyleBuilder WithOutlineWidth(MudExSize<double> size, bool when = true) => WithOutlineWidth(size.ToString(), when);


    /// <summary>
    /// Specifies the 'outline-style' CSS property using a BorderStyle enum.
    /// </summary>
    public MudExStyleBuilder WithOutlineStyle(BorderStyle outlineStyle, bool when = true) => WithOutlineStyle(Nextended.Core.Helper.EnumExtensions.ToDescriptionString(outlineStyle), when);


    /// <summary>
    /// Specifies the 'outline-top-width' CSS property with a double value and CSS unit.
    /// </summary>
    public MudExStyleBuilder WithOutlineTopWidth(double outlineTopWidth, CssUnit unit, bool when = true) => WithOutlineTopWidth(new MudExSize<double>(outlineTopWidth, unit).ToString(), when);

    /// <summary>
    /// Specifies the 'outline-top-width' CSS property with a size.
    /// </summary>
    public MudExStyleBuilder WithOutlineTopWidth(MudExSize<double> size, bool when = true) => WithOutlineTopWidth(size.ToString(), when);

    // Similar comments would be there for WithOutlineRightWidth, WithOutlineBottomWidth, and WithOutlineLeftWidth methods...

    /// <summary>
    /// Specifies the 'outline-color' CSS property with MudExColor.
    /// </summary>
    public MudExStyleBuilder WithOutlineColor(MudExColor color, bool when = true) => WithOutlineColor(color.ToCssStringValue(), when);

    /// <summary>
    /// Specifies the 'outline-color' CSS property with MudColor.
    /// </summary>
    public MudExStyleBuilder WithOutlineColor(MudColor color, bool when = true) => WithOutlineColor(color.ToString(), when);

    /// <summary>
    /// Specifies the 'outline-color' CSS property with System.Drawing.Color.
    /// </summary>
    public MudExStyleBuilder WithOutlineColor(System.Drawing.Color color, bool when = true) => WithOutlineColor(color.ToMudColor(), when);

    /// <summary>
    /// Specifies the 'outline-color' CSS property with Color.
    /// </summary>
    public MudExStyleBuilder WithOutlineColor(Color color, bool when = true) => WithOutlineColor(color.CssVarDeclaration(), when);

    /// <summary>
    /// Specifies the 'outline-right-width' CSS property with a double value and CSS unit.
    /// </summary>
    public MudExStyleBuilder WithOutlineRightWidth(double outlineRightWidth, CssUnit unit, bool when = true) => WithOutlineRightWidth(new MudExSize<double>(outlineRightWidth, unit).ToString(), when);

    /// <summary>
    /// Specifies the 'outline-right-width' CSS property with a size.
    /// </summary>
    public MudExStyleBuilder WithOutlineRightWidth(MudExSize<double> size, bool when = true) => WithOutlineRightWidth(size.ToString(), when);

    /// <summary>
    /// Specifies the 'outline-bottom-width' CSS property with a double value and CSS unit.
    /// </summary>
    public MudExStyleBuilder WithOutlineBottomWidth(double outlineBottomWidth, CssUnit unit, bool when = true) => WithOutlineBottomWidth(new MudExSize<double>(outlineBottomWidth, unit).ToString(), when);

    /// <summary>
    /// Specifies the 'outline-bottom-width' CSS property with a size.
    /// </summary>
    public MudExStyleBuilder WithOutlineBottomWidth(MudExSize<double> size, bool when = true) => WithOutlineBottomWidth(size.ToString(), when);

    /// <summary>
    /// Specifies the 'outline-left-width' CSS property with a double value and CSS unit.
    /// </summary>
    public MudExStyleBuilder WithOutlineLeftWidth(double outlineLeftWidth, CssUnit unit, bool when = true) => WithOutlineLeftWidth(new MudExSize<double>(outlineLeftWidth, unit).ToString(), when);

    /// <summary>
    /// Specifies the 'outline-left-width' CSS property with a size.
    /// </summary>
    public MudExStyleBuilder WithOutlineLeftWidth(MudExSize<double> size, bool when = true) => WithOutlineLeftWidth(size.ToString(), when);

    /// <summary>
    /// Sets color or background color based on variant
    /// </summary>
    public MudExStyleBuilder WithColorForVariant(Variant variant, MudExColor color, bool when = true) =>
        WithBackgroundColor(color, variant == Variant.Filled && when)
            .WithColor(color, (variant == Variant.Text || variant == Variant.Outlined) && when)
            .WithBorderColor(color, variant == Variant.Outlined && when);


    /// <summary>
    /// Sets the animation style using a custom string.
    /// </summary>
    /// <param name="animationStyle">The custom animation style as a string.</param>
    /// <param name="when">Condition for applying the animation.</param>
    /// <returns>The modified MudExStyleBuilder object.</returns>
    public MudExStyleBuilder WithAnimation(string animationStyle, bool when = true) => With("animation", animationStyle, when);

    /// <summary>
    /// Sets the animation style using multiple optional parameters.
    /// </summary>
    /// <param name="type">The type of the animation.</param>
    /// <param name="duration">The duration of the animation.</param>
    /// <param name="direction">The direction of the animation.</param>
    /// <param name="animationTimingFunction">The timing function for the animation.</param>
    /// <param name="targetPosition">The target position for the dialog.</param>
    /// <param name="when">Condition for applying the animation.</param>
    /// <returns>The modified MudExStyleBuilder object.</returns>
    public MudExStyleBuilder WithAnimation(AnimationType type, TimeSpan? duration, AnimationDirection? direction, AnimationTimingFunction animationTimingFunction, DialogPosition? targetPosition, bool when = true)
        => WithAnimation(type.GetAnimationCssStyle(duration, direction, animationTimingFunction, targetPosition), when);

    /// <summary>
    /// Sets the animation style using just the type of the animation.
    /// </summary>
    /// <param name="type">The type of the animation.</param>
    /// <param name="when">Condition for applying the animation.</param>
    /// <returns>The modified MudExStyleBuilder object.</returns>
    public MudExStyleBuilder WithAnimation(AnimationType type, bool when = true)
        => WithAnimation(type, null, null, null, null, when);

    /// <summary>
    /// Sets the animation style using the type and duration of the animation.
    /// </summary>
    /// <param name="type">The type of the animation.</param>
    /// <param name="duration">The duration of the animation.</param>
    /// <param name="when">Condition for applying the animation.</param>
    /// <returns>The modified MudExStyleBuilder object.</returns>
    public MudExStyleBuilder WithAnimation(AnimationType type, TimeSpan duration, bool when = true)
        => WithAnimation(type, duration, null, null, null, when);

    /// <summary>
    /// Sets the animation style using the type, duration, and direction of the animation.
    /// </summary>
    /// <param name="type">The type of the animation.</param>
    /// <param name="duration">The duration of the animation.</param>
    /// <param name="direction">The direction of the animation.</param>
    /// <param name="when">Condition for applying the animation.</param>
    /// <returns>The modified MudExStyleBuilder object.</returns>
    public MudExStyleBuilder WithAnimation(AnimationType type, TimeSpan duration, AnimationDirection direction, bool when = true)
        => WithAnimation(type, duration, direction, null, null, when);

    /// <summary>
    /// Sets the animation style using the type, duration, direction, and timing function of the animation.
    /// </summary>
    /// <param name="type">The type of the animation.</param>
    /// <param name="duration">The duration of the animation.</param>
    /// <param name="direction">The direction of the animation.</param>
    /// <param name="animationTimingFunction">The timing function for the animation.</param>
    /// <param name="when">Condition for applying the animation.</param>
    /// <returns>The modified MudExStyleBuilder object.</returns>
    public MudExStyleBuilder WithAnimation(AnimationType type, TimeSpan duration, AnimationDirection direction, AnimationTimingFunction animationTimingFunction, bool when = true)
        => WithAnimation(type, duration, direction, animationTimingFunction, null, when);


    /// <summary>
    /// Sets the animation style with a gradient background using the type, duration, direction, timing function, and target position of the animation.
    /// </summary>
    public MudExStyleBuilder WithAnimatedGradientBackground(MudExColor[] colors, bool when = true) => WithAnimatedConicGradientBorderedBackground(0, colors, new[] { MudExColor.Transparent }, when);
   
    /// <summary>
    /// With animated gradient border
    /// </summary>
    public MudExStyleBuilder WithAnimatedGradientBorder(MudExSize<double> borderSize, MudExColor backgroundColor, MudExColor[] borderColors, bool when = true) => WithAnimatedConicGradientBorderedBackground(borderSize, new[] { backgroundColor }, borderColors, when);

    /// <summary>
    /// With animated gradient background
    /// </summary>
    public MudExStyleBuilder WithAnimatedGradientBackground(MudTheme theme, bool dark, bool when) => WithAnimatedGradientBackground(dark ? theme.PaletteDark : theme.Palette, when);

    /// <summary>
    /// With animated gradient background
    /// </summary>
    public MudExStyleBuilder WithAnimatedGradientBackground(MudTheme theme, bool when = true) => WithAnimatedGradientBackground(theme, false, when);

    /// <summary>
    /// With animated gradient background
    /// </summary>
    public MudExStyleBuilder WithAnimatedGradientBackground(Palette palette, bool when = true) => WithAnimatedConicGradientBorderedBackground(0, GetColorsFromPalette(palette), new[] { MudExColor.Transparent }, when);

    
    /// <summary>
    /// With animated gradient border based on palette
    /// </summary>
    public MudExStyleBuilder WithAnimatedGradientBorder(MudExSize<double> borderSize, Palette palette, bool when = true) => WithAnimatedGradientBorder(borderSize, palette.Surface, GetColorsFromPalette(palette), when);

    /// <summary>
    /// With animated gradient border based on theme
    /// </summary>
    public MudExStyleBuilder WithAnimatedGradientBorder(MudExSize<double> borderSize, MudTheme theme, bool dark, bool when = true) => WithAnimatedGradientBorder(borderSize, dark ? theme.PaletteDark : theme.Palette, when);
    
    /// <summary>
    /// With animated gradient border that looks like a skeleton loading wave
    /// </summary>
    public MudExStyleBuilder WithSkeletonLoadingBorder(MudExSize<double> borderSize, bool when = true) => WithAnimatedGradientBorder(borderSize, MudExColor.Surface, new []{ MudExColor.Dark, "rgba(0,0,0,.11)", MudExColor.Dark, "rgba(0,0,0,.11)" }, when);
    
    
    /// <summary>
    /// Returns an array of colors from a palette
    /// </summary>
    private MudExColor[] GetColorsFromPalette(Palette palette) => new MudExColor[] { palette.Primary, palette.Secondary, palette.Info, palette.Error, palette.Warning };

    /// <summary>
    /// Add a background image with a conic gradient and a border with a conic gradient
    /// </summary>
    public MudExStyleBuilder WithAnimatedConicGradientBorderedBackground(MudExSize<double> borderSize, MudExColor[] backgroundColors, MudExColor[] borderColors, bool when = true)
    {
        while (backgroundColors.Length < 3)
            backgroundColors = backgroundColors.Append(backgroundColors.Last()).ToArray();
        while (borderColors.Length < 2)
            borderColors = borderColors.Append(borderColors.Last()).ToArray();
        return With("--border-size", $"{borderSize}", when)
            .With("--border-angle", "0turn", when)
            .With("background-image", $"conic-gradient(from var(--border-angle), {string.Join(',', backgroundColors.Take(2).Select(c => c.ToCssStringValue()))} 50%, {string.Join(',', backgroundColors.Skip(2).Select(c => c.ToCssStringValue()))}), conic-gradient(from var(--border-angle), {borderColors.FirstOrDefault().ToCssStringValue()} 20%,{string.Join(',', borderColors.Skip(1).Select(c => c.ToCssStringValue()))})", when)
            .WithBackgroundSize("calc(100% - (var(--border-size) * 2)) calc(100% - (var(--border-size) * 2)), cover", when)
            .WithBackgroundPosition("center center", when)
            .WithBackgroundRepeat("no-repeat", when)
            .WithAnimation("mud-ex-bg-spin 3s linear infinite", when);
    }


    /// <summary>
    /// Adds an !important to last added style
    /// </summary>
    public MudExStyleBuilder AsImportant(bool when = true)
    {
        if (!when)
            return this;

        if (!_additionalStyles.Any())
        {
            throw new InvalidOperationException("There are no styles to modify.");
        }

        var l = _additionalStyles.Last();
        var newValue = l.Value + " !important";

        _additionalStyles = _additionalStyles.Take(_additionalStyles.Count - 1).ToDictionary(p => p.Key, p => p.Value);
        _additionalStyles.Add(l.Key, newValue);

        return this;
    }

    #endregion

    #region Fluent With Pseudo Methods

    //public MudExStyleBuilder WithPseudo(string pseudo, MudExStyleBuilder builder)
    //{
    //    _pseudoElementsStyles[pseudo] = builder;
    //    return this;
    //}

    //public MudExStyleBuilder WithHoverColor(string color, bool when = true)
    //{
    //    return WithPseudo("hover", new MudExStyleBuilder().With("color", color, when));
    //}

    #endregion

    /// <summary>
    /// Adds a style to the builder if the condition is true.
    /// </summary>
    public MudExStyleBuilder With(string key, string value, Func<bool> when) => With(key, value, when());

    /// <summary>
    /// Adds a style to the builder if the condition is true.
    /// </summary>
    public MudExStyleBuilder With(string key, string value, bool when = true)
    {
        if (when)
            _additionalStyles[key] = value;
        return this;
    }

    /// <summary>
    /// Adds a ra style string is condition is true
    /// </summary>
    public MudExStyleBuilder AddRaw(string styleStr, bool when = true)
    {
        if (!string.IsNullOrEmpty(styleStr) && when)
            _rawStyles.Add(styleStr);
        return this;
    }

    /// <summary>
    /// Creates a class for this style and returns a MudExCssBuilder with this class added
    /// </summary>
    public Task<MudExCssBuilder> AsCssBuilderAsync() => new MudExCssBuilder().AddClassFromStyleAsync(this);

    /// <summary>
    /// Creates a class for this style and returns the name of the created class
    /// </summary>
    public async ValueTask<string> BuildAsClassRuleAsync(string className = null, IJSRuntime jSRuntime = null)
    {
        jSRuntime ??= JsRuntime ?? JsImportHelper.GetInitializedJsRuntime();
        var result = await jSRuntime.InvokeAsync<string>("MudExCssHelper.createTemporaryClass", Build(), className);
        _temporaryCssClasses.TryAdd(result, 0);
        return result;
    }

    /// <summary>
    /// Removes a class that is temporary created
    /// </summary>
    public async ValueTask<string> RemoveClassRuleAsync(string className, IJSRuntime jSRuntime = null)
    {
        jSRuntime ??= JsRuntime ?? JsImportHelper.GetInitializedJsRuntime();
        var result = await jSRuntime.InvokeAsync<string>("MudExCssHelper.deleteClassRule", className);
        _temporaryCssClasses.TryRemove(className, out _);
        return result;
    }

    /// <summary>
    /// Disposes this instance
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var className in _temporaryCssClasses.Keys)
            await RemoveClassRuleAsync(className);
    }

    /// <summary>
    /// Builds the css style string
    /// </summary>
    public string Build()
    {
        var css = _additionalStyles.Select(style => $"{style.Key}: {style.Value};").ToList();
        if (_rawStyles?.Any() == true)
            css.AddRange(_rawStyles);

        //  css.AddRange(_pseudoElementsStyles.Select(pseudo => $":{pseudo.Key} {{ {pseudo.Value.Build()} }}"));

        return string.Join(" ", css);
    }

    /// <summary>
    /// Removes all styles
    /// </summary>    
    public MudExStyleBuilder Clear()
    {
        _additionalStyles?.Clear();
        _rawStyles?.Clear();
        return this;
    }

    /// <summary>
    /// Converts this style to an object
    /// </summary>
    public T ToObject<T>() where T : new() => StyleStringToObject<T>(ToString());

    /// <summary>
    /// Returns the css style string
    /// </summary>
    /// <returns></returns>
    public override string ToString() => Build();

    /// <summary>
    /// Explicit conversion to string
    /// </summary>
    /// <param name="builder"></param>
    public static explicit operator string(MudExStyleBuilder builder) => builder.ToString();

    /// <summary>
    /// Explicit conversion from string
    /// </summary>
    /// <param name="styles"></param>
    public static explicit operator MudExStyleBuilder(string styles) => FromStyle(styles);

    /// <summary>
    /// The css style string
    /// </summary>
    public string Style => Build();

    private string DoubleToString(double value) => value.ToString(CultureInfo.InvariantCulture);

}