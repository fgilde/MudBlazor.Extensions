using MudBlazor.Extensions.Core;
using MudBlazor.Utilities;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.JSInterop;
using System.Collections.Concurrent;
using System.Globalization;
using MudBlazor.Extensions.Attribute;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// MudExStyleBuilder is useful to create style strings or convert any style to a class.
/// </summary>
[HasDocumentation("MudExStyleBuilder.md")]
public sealed class MudExStyleBuilder: IAsyncDisposable, IMudExStyleAppearance
{
    private static readonly string[] PropertiesToAddUnits = { "height", "width", "min-height", "min-width", "max-height", "max-width",
        "padding", "padding-top", "padding-right", "padding-bottom", "padding-left", "margin", "margin-top", "margin-right", "margin-bottom",
        "margin-left", "border-width", "border-top-width", "border-right-width", "border-bottom-width", "border-left-width", "font-size", "letter-spacing",
        "line-height", "word-spacing", "text-indent", "column-gap", "column-width", "top", "right", "bottom", "left", "transform", "translate", "translateX",
        "translateY", "translateZ", "translate3d", "rotate", "rotateX", "rotateY", "rotateZ", "scale", "scaleX", "scaleY", "scaleZ", "scale3d",
        "skew", "skewX", "skewY", "perspective"};

    private Dictionary<string, string> _additionalStyles = new();
    private List<string> _raws = new();
    
    private readonly ConcurrentDictionary<string, byte> _temporaryCssClasses = new();
    private Dictionary<string, MudExStyleBuilder> _pseudoElementsStyles = new();

    #region Static Methods

    public static MudExStyleBuilder Default => new();

    /// <summary>
    /// Static factory method to create a <see cref="MudExStyleBuilder"/>
    /// </summary>
    public static MudExStyleBuilder Empty() => new();


    /// <summary>
    /// Static factory method to create a <see cref="MudExStyleBuilder"/> from an object.
    /// </summary>
    public static MudExStyleBuilder FromObject(object obj, string existingCss = "", CssUnit cssUnit = CssUnit.Pixels) => FromStyle(GenerateStyleString(obj, cssUnit, existingCss));
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
        string unit = cssUnit.ToDescriptionString();
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
                var formattedPropertyValue = propertyValue is string ? $"{propertyValue}" : propertyValue?.ToString();
                if (int.TryParse(formattedPropertyValue, out _) && PropertiesToAddUnits.Contains(cssPropertyName))
                    formattedPropertyValue += unit;
                cssBuilder.Append(cssPropertyName + ": " + formattedPropertyValue + ";");
            }
        }

        return string.IsNullOrEmpty(existingCss) ? cssBuilder.ToString() : CombineStyleStrings(cssBuilder.ToString(), existingCss);
    }

    /// <summary>
    /// Combines two css style strings
    /// </summary>
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
    /// Converts a css style string to an object
    /// </summary>
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
            propertyName = Regex.Replace(propertyName, "-([a-z])", m => m.Groups[1].Value.ToUpperInvariant());

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
    public MudExStyleBuilder With(object styleObj, CssUnit cssUnit = CssUnit.Pixels, bool when = true) => !when ? this : WithStyle(GenerateStyleString(styleObj, cssUnit));
    public MudExStyleBuilder WithColor(string color, bool when = true) => With("color", color, when);
    public MudExStyleBuilder WithColor(MudExColor color, bool when = true) => WithColor(color.ToCssStringValue(), when);
    public MudExStyleBuilder WithColor(MudColor color, bool when = true) => WithColor(color.ToString(), when);
    public MudExStyleBuilder WithColor(System.Drawing.Color color, bool when = true) => WithColor(color.ToMudColor(), when);
    public MudExStyleBuilder WithColor(Color color, bool when = true) => WithColor(color.CssVarDeclaration(), when);

    public MudExStyleBuilder WithFill(string color, bool when = true) => With("fill", color, when);
    public MudExStyleBuilder WithFill(MudExColor color, bool when = true) => WithFill(color.ToCssStringValue(), when);
    public MudExStyleBuilder WithFill(MudColor color, bool when = true) => WithFill(color.ToString(), when);
    public MudExStyleBuilder WithFill(System.Drawing.Color color, bool when = true) => WithFill(color.ToMudColor(), when);
    public MudExStyleBuilder WithFill(Color color, bool when = true) => WithFill(color.CssVarDeclaration(), when);

    public MudExStyleBuilder WithBorder(string border, bool when = true) => With("border", border, when);
    public MudExStyleBuilder WithBorderRadius(string borderRadius, bool when = true) => With("border-radius", borderRadius, when);
    public MudExStyleBuilder WithBorderRadius(double radius, CssUnit unit, bool when = true) => WithBorderRadius(new MudExSize<double>(radius, unit).ToString(), when);

    public MudExStyleBuilder WithBackground(MudExColor color, bool when = true) => WithBackground(color.ToCssStringValue(), when);
    public MudExStyleBuilder WithBackground(string background, bool when = true) => With("background", background, when);
    public MudExStyleBuilder WithBackground(MudColor color, bool when = true) => WithBackground(color.ToString(), when);
    public MudExStyleBuilder WithBackground(System.Drawing.Color color, bool when = true) => WithBackground(color.ToMudColor(), when);
    public MudExStyleBuilder WithBackground(Color color, bool when = true) => WithBackground(color.CssVarDeclaration(), when);

    public MudExStyleBuilder WithBackgroundColor(MudExColor color, bool when = true) => WithBackgroundColor(color.ToCssStringValue(), when);
    public MudExStyleBuilder WithBackgroundColor(string background, bool when = true) => With("background-color", background, when);
    public MudExStyleBuilder WithBackgroundColor(MudColor color, bool when = true) => WithBackgroundColor(color.ToString(), when);
    public MudExStyleBuilder WithBackgroundColor(System.Drawing.Color color, bool when = true) => WithBackgroundColor(color.ToMudColor(), when);
    public MudExStyleBuilder WithBackgroundColor(Color color, bool when = true) => WithBackgroundColor(color.CssVarDeclaration(), when);

    public MudExStyleBuilder WithBackgroundImage(string backgroundImage, bool when = true) => With("background-image", backgroundImage, when);

    public MudExStyleBuilder WithBackgroundSize(string backgroundSize, bool when = true) => With("background-size", backgroundSize, when);

    public MudExStyleBuilder WithDisplay(string display, bool when = true) => With("display", display, when);

    public MudExStyleBuilder WithFlex(string flex, bool when = true) => With("flex", flex, when);

    public MudExStyleBuilder WithJustifyContent(string justifyContent, bool when = true) => With("justify-content", justifyContent, when);

    public MudExStyleBuilder WithAlignItems(string alignItems, bool when = true) => With("align-items", alignItems, when);

    public MudExStyleBuilder WithPosition(string position, bool when = true) => With("position", position, when);

    public MudExStyleBuilder WithTop(string top, bool when = true) => With("top", top, when);

    public MudExStyleBuilder WithBottom(string bottom, bool when = true) => With("bottom", bottom, when);

    public MudExStyleBuilder WithLeft(string left, bool when = true) => With("left", left, when);

    public MudExStyleBuilder WithRight(string right, bool when = true) => With("right", right, when);

    public MudExStyleBuilder WithZIndex(int zIndex, bool when = true) => With("z-index", zIndex.ToString(), when);

    public MudExStyleBuilder WithFontSize(string fontSize, bool when = true) => With("font-size", fontSize, when);

    public MudExStyleBuilder WithFontWeight(FontWeight fontWeight, bool when = true) => WithFontWeight(Nextended.Core.Helper.EnumExtensions.ToDescriptionString(fontWeight), when);
    public MudExStyleBuilder WithFontWeight(string fontWeight, bool when = true) => With("font-weight", fontWeight, when);

    public MudExStyleBuilder WithTextAlign(string textAlign, bool when = true) => With("text-align", textAlign, when);

    public MudExStyleBuilder WithTextDecoration(TextDecoration textDecoration, bool when = true) => WithTextDecoration(Nextended.Core.Helper.EnumExtensions.ToDescriptionString(textDecoration), when);

    public MudExStyleBuilder WithTextDecoration(string textDecoration, bool when = true) => With("text-decoration", textDecoration, when);

    public MudExStyleBuilder WithLetterSpacing(string letterSpacing, bool when = true) => With("letter-spacing", letterSpacing, when);

    public MudExStyleBuilder WithLineHeight(string lineHeight, bool when = true) => With("line-height", lineHeight, when);

    public MudExStyleBuilder WithTextTransform(string textTransform, bool when = true) => With("text-transform", textTransform, when);

    public MudExStyleBuilder WithWhiteSpace(string whiteSpace, bool when = true) => With("white-space", whiteSpace, when);


    public MudExStyleBuilder WithWidth(string width, bool when = true) => With("width", width, when);

    public MudExStyleBuilder WithWidth(double width, CssUnit unit, bool when = true) => WithWidth(new MudExSize<double>(width, unit).ToString(), when);

    public MudExStyleBuilder WithWidth(MudExSize<double> size, bool when = true) => WithWidth(size.ToString(), when);

    public MudExStyleBuilder WithHeight(string height, bool when = true) => With("height", height, when);

    public MudExStyleBuilder WithHeight(double height, CssUnit unit, bool when = true) => WithHeight(new MudExSize<double>(height, unit).ToString(), when);

    public MudExStyleBuilder WithHeight(MudExSize<double> size, bool when = true) => WithHeight(size.ToString(), when);
    
    public MudExStyleBuilder WithOpacity(double opacity, bool when = true) => WithOpacity(DoubleToString(opacity), when);
    public MudExStyleBuilder WithOpacity(string opacity, bool when = true) => With("opacity", opacity, when);

    public MudExStyleBuilder WithMargin(string margin, bool when = true) => With("margin", margin, when);

    public MudExStyleBuilder WithMargin(double margin, CssUnit unit, bool when = true) => WithMargin(new MudExSize<double>(margin, unit).ToString(), when);

    public MudExStyleBuilder WithMargin(MudExSize<double> size, bool when = true) => WithMargin(size.ToString(), when);

    public MudExStyleBuilder WithPadding(string padding, bool when = true) => With("padding", padding, when);

    public MudExStyleBuilder WithPadding(double padding, CssUnit unit, bool when = true) => WithPadding(new MudExSize<double>(padding, unit).ToString(), when);

    public MudExStyleBuilder WithPadding(MudExSize<double> size, bool when = true) => WithPadding(size.ToString(), when);

    public MudExStyleBuilder WithOverflow(string overflow, bool when = true) => With("overflow", overflow, when);

    public MudExStyleBuilder WithCursor(string cursor, bool when = true) => With("cursor", cursor, when);

    public MudExStyleBuilder WithListStyleType(string listStyleType, bool when = true) => With("list-style-type", listStyleType, when);

    public MudExStyleBuilder WithTransition(string transition, bool when = true) => With("transition", transition, when);

    public MudExStyleBuilder WithTransform(string transform, bool when = true) => With("transform", transform, when);

    public MudExStyleBuilder WithFlexDirection(string flexDirection, bool when = true) => With("flex-direction", flexDirection, when);

    public MudExStyleBuilder WithFlexWrap(string flexWrap, bool when = true) => With("flex-wrap", flexWrap, when);

    public MudExStyleBuilder WithBoxShadow(string boxShadow, bool when = true) => With("box-shadow", boxShadow, when);

    public MudExStyleBuilder WithTextShadow(string textShadow, bool when = true) => With("text-shadow", textShadow, when);

    public MudExStyleBuilder WithWordBreak(string wordBreak, bool when = true) => With("word-break", wordBreak, when);

    public MudExStyleBuilder WithWordSpacing(string wordSpacing, bool when = true) => With("word-spacing", wordSpacing, when);

    public MudExStyleBuilder WithBackfaceVisibility(string backfaceVisibility, bool when = true) => With("backface-visibility", backfaceVisibility, when);

    public MudExStyleBuilder WithOutline(string outline, bool when = true) => With("outline", outline, when);

    public MudExStyleBuilder WithMaxWidth(string maxWidth, bool when = true) => With("max-width", maxWidth, when);

    public MudExStyleBuilder WithMinWidth(string minWidth, bool when = true) => With("min-width", minWidth, when);

    public MudExStyleBuilder WithMaxHeight(string maxHeight, bool when = true) => With("max-height", maxHeight, when);

    public MudExStyleBuilder WithMinHeight(string minHeight, bool when = true) => With("min-height", minHeight, when);

    public MudExStyleBuilder WithResize(string resize, bool when = true) => With("resize", resize, when);

    public MudExStyleBuilder WithVisibility(string visibility, bool when = true) => With("visibility", visibility, when);

    public MudExStyleBuilder WithFlexBasis(string flexBasis, bool when = true) => With("flex-basis", flexBasis, when);

    public MudExStyleBuilder WithFlexGrow(string flexGrow, bool when = true) => With("flex-grow", flexGrow, when);

    public MudExStyleBuilder WithFlexShrink(string flexShrink, bool when = true) => With("flex-shrink", flexShrink, when);

    public MudExStyleBuilder WithOrder(string order, bool when = true) => With("order", order, when);

    public MudExStyleBuilder WithGridGap(string gridGap, bool when = true) => With("grid-gap", gridGap, when);

    public MudExStyleBuilder WithGridColumn(string gridColumn, bool when = true) => With("grid-column", gridColumn, when);

    public MudExStyleBuilder WithGridRow(string gridRow, bool when = true) => With("grid-row", gridRow, when);

    public MudExStyleBuilder WithGridTemplateColumns(string gridTemplateColumns, bool when = true) => With("grid-template-columns", gridTemplateColumns, when);

    public MudExStyleBuilder WithGridTemplateRows(string gridTemplateRows, bool when = true) => With("grid-template-rows", gridTemplateRows, when);

    public MudExStyleBuilder WithObjectFit(string objectFit, bool when = true) => With("object-fit", objectFit, when);

    public MudExStyleBuilder WithObjectPosition(string objectPosition, bool when = true) => With("object-position", objectPosition, when);

    public MudExStyleBuilder WithTextOverflow(string textOverflow, bool when = true) => With("text-overflow", textOverflow, when);

    public MudExStyleBuilder WithVerticalAlign(string verticalAlign, bool when = true) => With("vertical-align", verticalAlign, when);


    public MudExStyleBuilder WithMinSize(string width, string height, bool when = true) => WithMinWidth(width, when).WithMinHeight(height, when);
    public MudExStyleBuilder WithMinSize(double width, double height, CssUnit unit, bool when = true) => WithMinWidth(width, unit, when).WithMinHeight(height, unit, when);
    public MudExStyleBuilder WithMinSize(double size, CssUnit unit, bool when = true) => WithMinWidth(size, unit, when).WithMinHeight(size, unit, when);
    public MudExStyleBuilder WithMinSize(MudExSize<double> width, MudExSize<double> height, bool when = true) => WithMinWidth(width, when).WithMinHeight(height, when);
    public MudExStyleBuilder WithMinSize(MudExDimension size, bool when = true) => WithMinSize(size.Width, size.Height, when);

    public MudExStyleBuilder WithMaxSize(string width, string height, bool when = true) => WithMaxWidth(width, when).WithMaxHeight(height, when);
    public MudExStyleBuilder WithMaxSize(double width, double height, CssUnit unit, bool when = true) => WithMaxWidth(width, unit, when).WithMaxHeight(height, unit, when);
    public MudExStyleBuilder WithMaxSize(double size, CssUnit unit, bool when = true) => WithMaxWidth(size, unit, when).WithMaxHeight(size, unit, when);
    public MudExStyleBuilder WithMaxSize(MudExSize<double> width, MudExSize<double> height, bool when = true) => WithMaxWidth(width, when).WithMaxHeight(height, when);
    public MudExStyleBuilder WithMaxSize(MudExDimension size, bool when = true) => WithMaxSize(size.Width, size.Height, when);

    public MudExStyleBuilder WithSize(string width, string height, bool when = true) => WithWidth(width, when).WithHeight(height, when);
    public MudExStyleBuilder WithSize(double size, CssUnit unit, bool when = true) => WithWidth(size, unit, when).WithHeight(size, unit, when);
    public MudExStyleBuilder WithSize(double width, double height, CssUnit unit, bool when = true) => WithWidth(width, unit, when).WithHeight(height, unit, when);
    public MudExStyleBuilder WithSize(MudExSize<double> width, MudExSize<double> height, bool when = true) => WithWidth(width, when).WithHeight(height, when);
    public MudExStyleBuilder WithSize(MudExDimension size, bool when = true) => WithWidth(size.Width, when).WithHeight(size.Height, when);
    public MudExStyleBuilder WithDimension(MudExDimension size, bool when = true) => WithWidth(size.Width, when).WithHeight(size.Height, when);


    public MudExStyleBuilder WithMinHeight(double minHeight, CssUnit unit, bool when = true) => WithMinHeight(new MudExSize<double>(minHeight, unit), when);

    public MudExStyleBuilder WithMinHeight(MudExSize<double> size, bool when = true) => WithMinHeight(size.ToString(), when);
    
    public MudExStyleBuilder WithMinWidth(double minWidth, CssUnit unit, bool when = true) => WithMinWidth(new MudExSize<double>(minWidth, unit).ToString(), when);

    public MudExStyleBuilder WithMinWidth(MudExSize<double> size, bool when = true) => WithMinWidth(size.ToString(), when);
    
    public MudExStyleBuilder WithMaxHeight(double maxHeight, CssUnit unit, bool when = true) => WithMaxHeight(new MudExSize<double>(maxHeight, unit).ToString(), when);

    public MudExStyleBuilder WithMaxHeight(MudExSize<double> size, bool when = true) => WithMaxHeight(size.ToString(), when);
    
    public MudExStyleBuilder WithMaxWidth(double maxWidth, CssUnit unit, bool when = true) => WithMaxWidth(new MudExSize<double>(maxWidth, unit).ToString(), when);

    public MudExStyleBuilder WithMaxWidth(MudExSize<double> size, bool when = true) => WithMaxWidth(size.ToString(), when);
    
    public MudExStyleBuilder WithPaddingTop(string paddingTop, bool when = true) => With("padding-top", paddingTop, when);

    public MudExStyleBuilder WithPaddingTop(double paddingTop, CssUnit unit, bool when = true) => WithPaddingTop(new MudExSize<double>(paddingTop, unit).ToString(), when);

    public MudExStyleBuilder WithPaddingTop(MudExSize<double> size, bool when = true) => WithPaddingTop(size.ToString(), when);

    public MudExStyleBuilder WithPaddingRight(string paddingRight, bool when = true) => With("padding-right", paddingRight, when);

    public MudExStyleBuilder WithPaddingRight(double paddingRight, CssUnit unit, bool when = true) => WithPaddingRight(new MudExSize<double>(paddingRight, unit).ToString(), when);

    public MudExStyleBuilder WithPaddingRight(MudExSize<double> size, bool when = true) => WithPaddingRight(size.ToString(), when);

    public MudExStyleBuilder WithPaddingBottom(string paddingBottom, bool when = true) => With("padding-bottom", paddingBottom, when);

    public MudExStyleBuilder WithPaddingBottom(double paddingBottom, CssUnit unit, bool when = true) => WithPaddingBottom(new MudExSize<double>(paddingBottom, unit).ToString(), when);
    
    public MudExStyleBuilder WithPaddingBottom(MudExSize<double> size, bool when = true) => WithPaddingBottom(size.ToString(), when);

    public MudExStyleBuilder WithPaddingLeft(string paddingLeft, bool when = true) => With("padding-left", paddingLeft, when);

    public MudExStyleBuilder WithPaddingLeft(double paddingLeft, CssUnit unit, bool when = true) => WithPaddingLeft(new MudExSize<double>(paddingLeft, unit).ToString(), when);

    public MudExStyleBuilder WithPaddingLeft(MudExSize<double> size, bool when = true) => WithPaddingLeft(size.ToString(), when);


    public MudExStyleBuilder WithMarginTop(string marginTop, bool when = true) => With("margin-top", marginTop, when);

    public MudExStyleBuilder WithMarginTop(double marginTop, CssUnit unit, bool when = true) => WithMarginTop(new MudExSize<double>(marginTop, unit).ToString(), when);

    public MudExStyleBuilder WithMarginTop(MudExSize<double> size, bool when = true) => WithMarginTop(size.ToString(), when);

    public MudExStyleBuilder WithMarginRight(string marginRight, bool when = true) => With("margin-right", marginRight, when);

    public MudExStyleBuilder WithMarginRight(double marginRight, CssUnit unit, bool when = true) => WithMarginRight(new MudExSize<double>(marginRight, unit).ToString(), when);

    public MudExStyleBuilder WithMarginRight(MudExSize<double> size, bool when = true) => WithMarginRight(size.ToString(), when);

    public MudExStyleBuilder WithMarginBottom(string marginBottom, bool when = true) => With("margin-bottom", marginBottom, when);

    public MudExStyleBuilder WithMarginBottom(double marginBottom, CssUnit unit, bool when = true) => WithMarginBottom(new MudExSize<double>(marginBottom, unit).ToString(), when);

    public MudExStyleBuilder WithMarginBottom(MudExSize<double> size, bool when = true) => WithMarginBottom(size.ToString(), when);

    public MudExStyleBuilder WithMarginLeft(string marginLeft, bool when = true) => With("margin-left", marginLeft, when);

    public MudExStyleBuilder WithMarginLeft(double marginLeft, CssUnit unit, bool when = true) => WithMarginLeft(new MudExSize<double>(marginLeft, unit).ToString(), when);

    public MudExStyleBuilder WithMarginLeft(MudExSize<double> size, bool when = true) => WithMarginLeft(size.ToString(), when);

    public MudExStyleBuilder WithBorderWidth(string borderWidth, bool when = true) => With("border-width", borderWidth, when);

    public MudExStyleBuilder WithBorderWidth(double borderWidth, CssUnit unit, bool when = true) => WithBorderWidth(new MudExSize<double>(borderWidth, unit).ToString(), when);

    public MudExStyleBuilder WithBorderWidth(MudExSize<double> size, bool when = true) => WithBorderWidth(size.ToString(), when);

    public MudExStyleBuilder WithBorderTopWidth(string borderTopWidth, bool when = true) => With("border-top-width", borderTopWidth, when);

    public MudExStyleBuilder WithBorderTopWidth(double borderTopWidth, CssUnit unit, bool when = true) => WithBorderTopWidth(new MudExSize<double>(borderTopWidth, unit).ToString(), when);

    public MudExStyleBuilder WithBorderTopWidth(MudExSize<double> size, bool when = true) => WithBorderTopWidth(size.ToString(), when);

    public MudExStyleBuilder WithBorderRightWidth(string borderRightWidth, bool when = true) => With("border-right-width", borderRightWidth, when);

    public MudExStyleBuilder WithBorderRightWidth(double borderRightWidth, CssUnit unit, bool when = true) => WithBorderRightWidth(new MudExSize<double>(borderRightWidth, unit).ToString(), when);

    public MudExStyleBuilder WithBorderRightWidth(MudExSize<double> size, bool when = true) => WithBorderRightWidth(size.ToString(), when);

    public MudExStyleBuilder WithBorderBottomWidth(string borderBottomWidth, bool when = true) => With("border-bottom-width", borderBottomWidth, when);

    public MudExStyleBuilder WithBorderBottomWidth(double borderBottomWidth, CssUnit unit, bool when = true) => WithBorderBottomWidth(new MudExSize<double>(borderBottomWidth, unit).ToString(), when);

    public MudExStyleBuilder WithBorderBottomWidth(MudExSize<double> size, bool when = true) => WithBorderBottomWidth(size.ToString(), when);

    public MudExStyleBuilder WithBorderLeftWidth(string borderLeftWidth, bool when = true) => With("border-left-width", borderLeftWidth, when);

    public MudExStyleBuilder WithBorderLeftWidth(double borderLeftWidth, CssUnit unit, bool when = true) => WithBorderLeftWidth(new MudExSize<double>(borderLeftWidth, unit).ToString(), when);

    public MudExStyleBuilder WithBorderLeftWidth(MudExSize<double> size, bool when = true) => WithBorderLeftWidth(size.ToString(), when);

    public MudExStyleBuilder WithFontSize(double fontSize, CssUnit unit, bool when = true) => WithFontSize(new MudExSize<double>(fontSize, unit).ToString(), when);

    public MudExStyleBuilder WithFontSize(MudExSize<double> size, bool when = true) => WithFontSize(size.ToString(), when);

    public MudExStyleBuilder WithLetterSpacing(double letterSpacing, CssUnit unit, bool when = true) => WithLetterSpacing(new MudExSize<double>(letterSpacing, unit).ToString(), when);

    public MudExStyleBuilder WithLetterSpacing(MudExSize<double> size, bool when = true) => WithLetterSpacing(size.ToString(), when);
    
    public MudExStyleBuilder WithLineHeight(double lineHeight, CssUnit unit, bool when = true) => WithLineHeight(new MudExSize<double>(lineHeight, unit).ToString(), when);

    public MudExStyleBuilder WithLineHeight(MudExSize<double> size, bool when = true) => WithLineHeight(size.ToString(), when);

    public MudExStyleBuilder WithWordSpacing(double wordSpacing, CssUnit unit, bool when = true) => WithWordSpacing(new MudExSize<double>(wordSpacing, unit).ToString(), when);

    public MudExStyleBuilder WithWordSpacing(MudExSize<double> size, bool when = true) => WithWordSpacing(size.ToString(), when);

    public MudExStyleBuilder WithTextIndent(string textIndent, bool when = true) => With("text-indent", textIndent, when);

    public MudExStyleBuilder WithTextIndent(double textIndent, CssUnit unit, bool when = true) => WithTextIndent(new MudExSize<double>(textIndent, unit).ToString(), when);

    public MudExStyleBuilder WithTextIndent(MudExSize<double> size, bool when = true) => WithTextIndent(size.ToString(), when);

    public MudExStyleBuilder WithColumnGap(string columnGap, bool when = true) => With("column-gap", columnGap, when);

    public MudExStyleBuilder WithColumnGap(double columnGap, CssUnit unit, bool when = true) => WithColumnGap(new MudExSize<double>(columnGap, unit).ToString(), when);

    public MudExStyleBuilder WithColumnGap(MudExSize<double> size, bool when = true) => WithColumnGap(size.ToString(), when);

    public MudExStyleBuilder WithColumnWidth(string columnWidth, bool when = true) => With("column-width", columnWidth, when);

    public MudExStyleBuilder WithColumnWidth(double columnWidth, CssUnit unit, bool when = true) => WithColumnWidth(new MudExSize<double>(columnWidth, unit).ToString(), when);

    public MudExStyleBuilder WithColumnWidth(MudExSize<double> size, bool when = true) => WithColumnWidth(size.ToString(), when);

    public MudExStyleBuilder WithTop(double top, CssUnit unit, bool when = true) => WithTop(new MudExSize<double>(top, unit).ToString(), when);

    public MudExStyleBuilder WithTop(MudExSize<double> size, bool when = true) => WithTop(size.ToString(), when);
    
    public MudExStyleBuilder WithRight(double right, CssUnit unit, bool when = true) => WithRight(new MudExSize<double>(right, unit).ToString(), when);

    public MudExStyleBuilder WithRight(MudExSize<double> size, bool when = true) => WithRight(size.ToString(), when);

    public MudExStyleBuilder WithBottom(double bottom, CssUnit unit, bool when = true) => WithBottom(new MudExSize<double>(bottom, unit).ToString(), when);

    public MudExStyleBuilder WithBottom(MudExSize<double> size, bool when = true) => WithBottom(size.ToString(), when);

    public MudExStyleBuilder WithLeft(double left, CssUnit unit, bool when = true) => WithLeft(new MudExSize<double>(left, unit).ToString(), when);

    public MudExStyleBuilder WithLeft(MudExSize<double> size, bool when = true) => WithLeft(size.ToString(), when);

    public MudExStyleBuilder WithTranslate(string translate, bool when = true) => With("translate", translate, when);

    public MudExStyleBuilder WithTranslate(double translate, CssUnit unit, bool when = true) => WithTranslate(new MudExSize<double>(translate, unit).ToString(), when);

    public MudExStyleBuilder WithTranslate(MudExSize<double> size, bool when = true) => WithTranslate(size.ToString(), when);

    public MudExStyleBuilder WithTranslateX(string translateX, bool when = true) => With("translateX", translateX, when);

    public MudExStyleBuilder WithTranslateX(double translateX, CssUnit unit, bool when = true) => WithTranslateX(new MudExSize<double>(translateX, unit).ToString(), when);

    public MudExStyleBuilder WithTranslateX(MudExSize<double> size, bool when = true) => WithTranslateX(size.ToString(), when);

    public MudExStyleBuilder WithTranslateY(string translateY, bool when = true) => With("translateY", translateY, when);

    public MudExStyleBuilder WithTranslateY(double translateY, CssUnit unit, bool when = true) => WithTranslateY(new MudExSize<double>(translateY, unit).ToString(), when);

    public MudExStyleBuilder WithTranslateY(MudExSize<double> size, bool when = true) => WithTranslateY(size.ToString(), when);

    public MudExStyleBuilder WithTranslateZ(string translateZ, bool when = true) => With("translateZ", translateZ, when);

    public MudExStyleBuilder WithTranslateZ(double translateZ, CssUnit unit, bool when = true) => WithTranslateZ(new MudExSize<double>(translateZ, unit).ToString(), when);

    public MudExStyleBuilder WithTranslateZ(MudExSize<double> size, bool when = true) => WithTranslateZ(size.ToString(), when);

    public MudExStyleBuilder WithTranslate3d(string translate3d, bool when = true) => With("translate3d", translate3d, when);

    public MudExStyleBuilder WithRotate(string rotate, bool when = true) => With("rotate", rotate, when);

    public MudExStyleBuilder WithRotate(double rotate, CssUnit unit, bool when = true) => WithRotate(new MudExSize<double>(rotate, unit).ToString(), when);

    public MudExStyleBuilder WithRotate(MudExSize<double> size, bool when = true) => WithRotate(size.ToString(), when);

    public MudExStyleBuilder WithRotateX(string rotateX, bool when = true) => With("rotateX", rotateX, when);

    public MudExStyleBuilder WithRotateX(double rotateX, CssUnit unit, bool when = true) => WithRotateX(new MudExSize<double>(rotateX, unit).ToString(), when);

    public MudExStyleBuilder WithRotateX(MudExSize<double> size, bool when = true) => WithRotateX(size.ToString(), when);

    public MudExStyleBuilder WithRotateY(string rotateY, bool when = true) => With("rotateY", rotateY, when);

    public MudExStyleBuilder WithRotateY(double rotateY, CssUnit unit, bool when = true) => WithRotateY(new MudExSize<double>(rotateY, unit).ToString(), when);

    public MudExStyleBuilder WithRotateY(MudExSize<double> size, bool when = true) => WithRotateY(size.ToString(), when);

    public MudExStyleBuilder WithRotateZ(string rotateZ, bool when = true) => With("rotateZ", rotateZ, when);

    public MudExStyleBuilder WithRotateZ(double rotateZ, CssUnit unit, bool when = true) => WithRotateZ(new MudExSize<double>(rotateZ, unit).ToString(), when);

    public MudExStyleBuilder WithRotateZ(MudExSize<double> size, bool when = true) => WithRotateZ(size.ToString(), when);

    public MudExStyleBuilder WithScale(string scale, bool when = true) => With("scale", scale, when);

    public MudExStyleBuilder WithScale(double scale, bool when = true) => WithScale(scale.ToString(), when);

    public MudExStyleBuilder WithScaleX(string scaleX, bool when = true) => With("scaleX", scaleX, when);

    public MudExStyleBuilder WithScaleX(double scaleX, bool when = true) => WithScaleX(scaleX.ToString(), when);

    public MudExStyleBuilder WithScaleY(string scaleY, bool when = true) => With("scaleY", scaleY, when);

    public MudExStyleBuilder WithScaleY(double scaleY, bool when = true) => WithScaleY(scaleY.ToString(), when);

    public MudExStyleBuilder WithScaleZ(string scaleZ, bool when = true) => With("scaleZ", scaleZ, when);

    public MudExStyleBuilder WithScaleZ(double scaleZ, bool when = true) => WithScaleZ(scaleZ.ToString(), when);

    public MudExStyleBuilder WithScale3d(string scale3d, bool when = true) => With("scale3d", scale3d, when);

    public MudExStyleBuilder WithScale3d(double scale3d, bool when = true) => WithScale3d(scale3d.ToString(), when);

    public MudExStyleBuilder WithSkew(string skew, bool when = true) => With("skew", skew, when);

    public MudExStyleBuilder WithSkew(double skew, CssUnit unit, bool when = true) => WithSkew(new MudExSize<double>(skew, unit).ToString(), when);

    public MudExStyleBuilder WithSkew(MudExSize<double> size, bool when = true) => WithSkew(size.ToString(), when);

    public MudExStyleBuilder WithSkewX(string skewX, bool when = true) => With("skewX", skewX, when);

    public MudExStyleBuilder WithSkewX(double skewX, CssUnit unit, bool when = true) => WithSkewX(new MudExSize<double>(skewX, unit).ToString(), when);

    public MudExStyleBuilder WithSkewX(MudExSize<double> size, bool when = true) => WithSkewX(size.ToString(), when);

    public MudExStyleBuilder WithSkewY(string skewY, bool when = true) => With("skewY", skewY, when);

    public MudExStyleBuilder WithSkewY(double skewY, CssUnit unit, bool when = true) => WithSkewY(new MudExSize<double>(skewY, unit).ToString(), when);

    public MudExStyleBuilder WithSkewY(MudExSize<double> size, bool when = true) => WithSkewY(size.ToString(), when);

    public MudExStyleBuilder WithPerspective(string perspective, bool when = true) => With("perspective", perspective, when);

    public MudExStyleBuilder WithPerspective(double perspective, CssUnit unit, bool when = true) => WithPerspective(new MudExSize<double>(perspective, unit).ToString(), when);

    public MudExStyleBuilder WithPerspective(MudExSize<double> size, bool when = true) => WithPerspective(size.ToString(), when);

    public MudExStyleBuilder WithFontFamily(string fontFamily, bool when = true) => With("font-family", fontFamily, when);
    
    public MudExStyleBuilder WithFontStyle(FontStyle fontStyle, bool when = true) => WithFontStyle(Nextended.Core.Helper.EnumExtensions.ToDescriptionString(fontStyle), when);
    public MudExStyleBuilder WithFontStyle(string fontStyle, bool when = true) => With("font-style", fontStyle, when);

    public MudExStyleBuilder WithTextJustify(string textJustify, bool when = true) => With("text-justify", textJustify, when);

    public MudExStyleBuilder WithBackgroundPosition(string backgroundPosition, bool when = true) => With("background-position", backgroundPosition, when);

    public MudExStyleBuilder WithBackgroundRepeat(string backgroundRepeat, bool when = true) => With("background-repeat", backgroundRepeat, when);

    public MudExStyleBuilder WithBorderColor(string borderColor, bool when = true) => With("border-color", borderColor, when);
    public MudExStyleBuilder WithBorderColor(MudExColor color, bool when = true) => WithBorderColor(color.ToCssStringValue(), when);
    public MudExStyleBuilder WithBorderColor(MudColor color, bool when = true) => WithBorderColor(color.ToString(), when);
    public MudExStyleBuilder WithBorderColor(System.Drawing.Color color, bool when = true) => WithBorderColor(color.ToMudColor(), when);
    public MudExStyleBuilder WithBorderColor(Color color, bool when = true) => WithBorderColor(color.CssVarDeclaration(), when);

    public MudExStyleBuilder WithBorderStyle(BorderStyle borderStyle, bool when = true) => WithBorderStyle(Nextended.Core.Helper.EnumExtensions.ToDescriptionString(borderStyle), when);

    public MudExStyleBuilder WithBorderStyle(string borderStyle, bool when = true) => With("border-style", borderStyle, when);

    public MudExStyleBuilder WithFloat(string floatOption, bool when = true) => With("float", floatOption, when);

    public MudExStyleBuilder WithClear(string clear, bool when = true) => With("clear", clear, when);

    public MudExStyleBuilder WithOverflowX(string overflowX, bool when = true) => With("overflow-x", overflowX, when);

    public MudExStyleBuilder WithOverflowY(string overflowY, bool when = true) => With("overflow-y", overflowY, when);

    public MudExStyleBuilder WithDirection(string direction, bool when = true) => With("direction", direction, when);

    public MudExStyleBuilder WithColumns(string columns, bool when = true) => With("columns", columns, when);

    public MudExStyleBuilder WithColumnCount(int columnCount, bool when = true) => With("column-count", columnCount.ToString(), when);

    public MudExStyleBuilder WithColumnFill(string columnFill, bool when = true) => With("column-fill", columnFill, when);

    public MudExStyleBuilder WithColumnRule(string columnRule, bool when = true) => With("column-rule", columnRule, when);

    public MudExStyleBuilder WithColumnSpan(string columnSpan, bool when = true) => With("column-span", columnSpan, when);

    public MudExStyleBuilder AsImportant()
    {
        if (!_additionalStyles.Any())
        {
            throw new InvalidOperationException("There are no styles to modify.");
        }

        var l = _additionalStyles.Last();
        var newValue = l.Value + " !important";

        _additionalStyles = _additionalStyles.Take(_additionalStyles.Count - 1).ToDictionary(p => p.Key, p=> p.Value);
        _additionalStyles.Add(l.Key, newValue);

        return this;
    }

    public MudExStyleBuilder WithGap(string gap, bool when = true) => With("gap", gap, when);

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
        if(when)
            _additionalStyles[key] = value;
        return this;
    }
    
    public MudExStyleBuilder AddRaw(string styleStr, bool when = true)
    {
        if (!string.IsNullOrEmpty(styleStr) && when)
            _raws.Add(styleStr);
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
        jSRuntime ??= await JsImportHelper.GetInitializedJsRuntimeAsync();
        var result = await jSRuntime.InvokeAsync<string>("MudExCssHelper.createTemporaryClass", Build(), className);
        _temporaryCssClasses.TryAdd(result, 0);
        return result;
    }

    /// <summary>
    /// Removes a class that is temporary created
    /// </summary>
    public async ValueTask<string> RemoveClassRuleAsync(string className, IJSRuntime jSRuntime = null)
    {
        jSRuntime ??= await JsImportHelper.GetInitializedJsRuntimeAsync();
        var result = await jSRuntime.InvokeAsync<string>("MudExCssHelper.deleteClassRule", className);
        _temporaryCssClasses.TryRemove(className, out _);
        return result;
    }

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
        if (_raws?.Any() == true)
            css.AddRange(_raws);
        
        //  css.AddRange(_pseudoElementsStyles.Select(pseudo => $":{pseudo.Key} {{ {pseudo.Value.Build()} }}"));

        return string.Join(" ", css);
    }

    /// <summary>
    /// Converts this style to an object
    /// </summary>
    public T ToObject<T>() where T : new() => StyleStringToObject<T>(ToString());
    
    public override string ToString() => Build();
    
    public static explicit operator string(MudExStyleBuilder builder) => builder.ToString();
    public static explicit operator MudExStyleBuilder(string styles) => FromStyle(styles);

    /// <summary>
    /// The css style string
    /// </summary>
    public string Style => Build();

    private string DoubleToString(double value) => value.ToString(CultureInfo.InvariantCulture);

}


public enum FontStyle
{
    [Description("normal")]
    Normal,
    [Description("italic")]
    Italic,
    [Description("oblique")]
    Oblique,
    [Description("initial")]
    Initial,
    [Description("inherit")]
    Inherit
}

public enum FontWeight
{
    [Description("normal")]
    Normal,
    [Description("bold")]
    Bold,
    [Description("bolder")]
    Bolder,
    [Description("lighter")]
    Lighter,
    [Description("initial")]
    Initial,
    [Description("inherit")]
    Inherit
}

public enum BorderStyle
{
    [Description("none")]
    None,
    [Description("hidden")]
    Hidden,
    [Description("dotted")]
    Dotted,
    [Description("dashed")]
    Dashed,
    [Description("solid")]
    Solid,
    [Description("double")]
    Double,
    [Description("groove")]
    Groove,
    [Description("ridge")]
    Ridge,
    [Description("inset")]
    Inset,
    [Description("outset")]
    Outset,
    [Description("initial")]
    Initial,
    [Description("inherit")]
    Inherit,
    [Description("revert")]
    Revert,
    [Description("unset")]
    Unset
}

public enum FontVariant
{
    [Description("normal")]
    Normal,
    [Description("small-caps")]
    SmallCaps,
    [Description("initial")]
    Initial,
    [Description("inherit")]
    Inherit
}

public enum TextDecoration
{
    [Description("none")]
    None,
    [Description("underline")]
    Underline,
    [Description("overline")]
    Overline,
    [Description("line-through")]
    LineThrough,
    [Description("initial")]
    Initial,
    [Description("inherit")]
    Inherit
}