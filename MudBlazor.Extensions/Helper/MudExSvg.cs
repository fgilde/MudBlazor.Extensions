using MudBlazor.Extensions.Attribute;
using System.Reflection;
using System.Text;
using MudBlazor.Extensions.Core;
using MudBlazor.Utilities;
using MudBlazor.Extensions.Helper.Internal;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// Static Utility class for SVG
/// </summary>
[HasDocumentation("MudExSvg.md")]
public static class MudExSvg
{
    /// <summary>
    /// Combines SVG icons horizontally
    /// </summary>
    /// <param name="image">The first SVG image</param>
    /// <param name="other">An array of other SVG images to combine with the first one</param>
    /// <returns>A new SVG with the combined images aligned horizontally</returns>
    public static string CombineIconsHorizontal(string image, params string[] other) 
        => CombineIcons(14, 0, "0 0 24 24", image, other);

    /// <summary>
    /// Combines SVG icons vertically
    /// </summary>
    /// <param name="image">The first SVG image</param>
    /// <param name="other">An array of other SVG images to combine with the first one</param>
    /// <returns>A new SVG with the combined images aligned horizontally</returns>
    public static string CombineIconsVertical(string image, params string[] other) 
        => CombineIcons(0, 14, "0 0 24 24", image, other);

    /// <summary>
    /// Combines SVG icons centered overlapped
    /// </summary>
    /// <param name="image">The first SVG image</param>
    /// <param name="other">An array of other SVG images to combine with the first one</param>
    /// <returns>A new SVG with the combined images aligned horizontally</returns>
    public static string CombineIconsCentered(string image, params string[] other) 
        => CombineIcons(0, 0, null, image, other);


    /// <summary>
    /// Combines SVG icons.
    /// </summary>
    /// <param name="marginLeftRight">The margin applied to the left and right of the combined images.</param>
    /// <param name="marginTopBottom">The margin applied to the top and bottom of the combined images.</param>
    /// <param name="originalViewBox">The original viewBox of the SVG.</param>
    /// <param name="image">The first SVG image.</param>
    /// <param name="other">An array of other SVG images to combine with the first one.</param>
    /// <returns>A new SVG with the combined images.</returns>
    public static string CombineIcons(int marginLeftRight, int marginTopBottom, string originalViewBox, string image, params string[] other)
    {
        var svgImages = new[] { image }.Concat(other).ToArray();
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(originalViewBox))
        {
            var viewBoxValues = originalViewBox.Split(' ');
            var width = int.Parse(viewBoxValues[2]);
            var height = int.Parse(viewBoxValues[3]);
            width += (svgImages.Length - 1) * marginLeftRight;
            height += (svgImages.Length - 1) * marginTopBottom;
            var viewBox = $"{viewBoxValues[0]} {viewBoxValues[1]} {width} {height}";
            sb.AppendLine("<svg xmlns='http://www.w3.org/2000/svg' viewBox='" + viewBox + "'>");
        }
        else
        {
            sb.AppendLine("<svg xmlns='http://www.w3.org/2000/svg'>");
        }

        var index = 0;
        foreach (var svgImage in svgImages)
        {
            sb.AppendLine($"<g transform='translate({index * marginLeftRight}, {index * marginTopBottom})'>");
            sb.AppendLine(svgImage);
            sb.AppendLine("</g>");
            index++;
        }
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    /// <summary>
    /// Returns the fully-qualified name of the constant in <see cref="Icons"/> or whatever owner type that has the specified value.
    /// </summary>
    /// <param name="value">The value of the SVG constant for which to get a name.</param>
    /// <param name="ownerTypes">Types for search in</param>
    /// <returns>A string containing the fully-qualified name of the icon constant that matches the specified value.</returns>    
    public static string SvgPropertyNameForValue(string value, params Type[] ownerTypes)
    {
        return ownerTypes.Select(ownerType => ownerType.Assembly.GetTypes()
                .Where(t => t.Namespace == ownerType.Namespace)
                .ToList())
            .Select(types => types.Select(type => SearchTypeForValue(type, value))
                .FirstOrDefault(res => res != null))
            .FirstOrDefault(result => result != null);
    }
    
    /// <summary>
    /// Returns the fully-qualified name of the constant in <see cref="Icons"/> that has the specified value.
    /// </summary>
    /// <param name="value">The value of the SVG constant for which to get a name.</param>
    /// <returns>A string containing the fully-qualified name of the icon constant that matches the specified value.</returns>
    public static string SvgPropertyNameForValue(string value) => SvgPropertyNameForValue(value, typeof(Icons));

    private static string SearchTypeForValue(Type type, string value) 
        => (from field in type.GetFields(BindingFlags.Public | BindingFlags.Static) where (field.IsLiteral || field.IsStatic) && field.FieldType == typeof(string) let fieldValue = field.GetValue(null) as string where fieldValue == value select $"{type.FullName.Replace('+', '.')}.{field.Name}").FirstOrDefault();


    /// <summary>
    /// Returns the value of the constant in <see cref="Icons"/> that has the specified name.
    /// </summary>
    /// <param name="fullName">Name like MudBlazor.Icons.Outlined.Search</param>
    /// <returns>The value</returns>
    public static string SvgPropertyValueForName(string fullName) => SvgPropertyValueForName(fullName, typeof(Icons));

    /// <summary>
    /// Returns the value of the constant in <see cref="Icons"/> that has the specified name.
    /// </summary>
    /// <param name="fullName">Name like MudBlazor.Icons.Outlined.Search</param>
    /// <param name="ownerTypes">Types where to search in</param>
    /// <returns>The value</returns>
    public static string SvgPropertyValueForName(string fullName, params Type[] ownerTypes)
    {
        if (fullName.StartsWith("@"))
            fullName = fullName[1..];

        // Split the fullName into namespace + type and field parts
        var lastDotIndex = fullName.LastIndexOf('.');
        var typeFullName = fullName[..lastDotIndex];
        var fieldName = fullName[(lastDotIndex + 1)..];

        return (from ownerType in ownerTypes
                from type in ownerType.Assembly
            .GetTypes().Where(t => t.FullName != null && t.FullName.Replace('+', '.') == typeFullName)
                select type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static)
            into field
                where field != null && (field.IsLiteral || field.IsStatic) && field.FieldType == typeof(string)
                select field.GetValue(null)).OfType<string>()
            .FirstOrDefault();
    }


    public static IDictionary<string, string> GetAllSvgProperties(params Type[] ownerTypes)
    {
        var result = new Dictionary<string, string>();

        foreach (var ownerType in ownerTypes)
        {
            // Create a queue to hold the types to process
            var typesToProcess = new Queue<Type>();
            typesToProcess.Enqueue(ownerType);

            // While there are still types to process
            while (typesToProcess.Count > 0)
            {
                var type = typesToProcess.Dequeue();

                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (type.FullName == null || (!field.IsLiteral && !field.IsStatic) || field.FieldType != typeof(string) || field.GetValue(null) is not string fieldValue) 
                        continue;
                    var propertyName = $"{type.FullName.Replace('+', '.')}.{field.Name}";
                    result[propertyName] = fieldValue;
                }

                // Enqueue any nested types
                foreach (var nestedType in type.GetNestedTypes())
                {
                    typesToProcess.Enqueue(nestedType);
                }
            }
        }

        return result;
    }






    /// <summary>
    /// Combines two SVGs sliced either horizontally, vertically, or diagonally.
    /// </summary>
    /// <param name="svg1">The first SVG to slice and combine.</param>
    /// <param name="svg2">The second SVG to slice and combine.</param>
    /// <param name="width">The width of the output SVG.</param>
    /// <param name="height">The height of the output SVG.</param>
    /// <param name="sliceDirection">The direction to slice the SVGs.</param>
    /// <returns>A new SVG that contains both original SVGs sliced either horizontally, vertically, or diagonally.</returns>
    public static string CombineSliced(string svg1, string svg2, double width, double height, SliceDirection sliceDirection)
    {
        string svg1Content = svg1.Substring(svg1.IndexOf(">") + 1).TrimEnd('<', '/', 's', 'v', 'g', '>');
        string svg2Content = svg2.Substring(svg2.IndexOf(">") + 1).TrimEnd('<', '/', 's', 'v', 'g', '>');

        string clipPath1, clipPath2;

        switch (sliceDirection)
        {
            case SliceDirection.Diagonal:
                clipPath1 = $"<polygon points='0,0 {width},0 0,{height}'/>";
                clipPath2 = $"<polygon points='{width},0 {width},{height} 0,{height}'/>";
                break;

            case SliceDirection.Vertical:
                double halfWidth = width / 2;
                clipPath1 = $"<rect width='{halfWidth}px' height='{height}px'></rect>";
                clipPath2 = $"<rect x='{halfWidth}px' width='{halfWidth}px' height='{height}px'></rect>";
                break;

            case SliceDirection.Horizontal:
                double halfHeight = height / 2;
                clipPath1 = $"<rect width='{width}px' height='{halfHeight}px'></rect>";
                clipPath2 = $"<rect y='{halfHeight}px' width='{width}px' height='{halfHeight}px'></rect>";
                break;

            default:
                throw new ArgumentException("Invalid slice direction");
        }

        return $@"
        <svg width='{width}px' height='{height}px' xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'>
            <defs>
                <clipPath id='clipPath1'>
                    {clipPath1}
                </clipPath>
                <clipPath id='clipPath2'>
                    {clipPath2}
                </clipPath>
            </defs>
            <g clip-path='url(#clipPath1)'>
                {svg1Content}
            </g>
            <g clip-path='url(#clipPath2)'>
                {svg2Content}
            </g>
        </svg>";
    }


    /// <summary>
    /// Returns a sliced application image as preview for given Theme, with the specified dimensions and slice direction.
    /// </summary>
    /// <param name="theme">The color scheme to use for the image.</param>
    /// <param name="size">The dimensions of the output image.</param>
    /// <param name="sliceDirection">The direction to slice the image.</param>
    /// <returns>A new SVG that displays an application image sliced either horizontally, vertically, or diagonally.</returns>

    public static string ApplicationImage(MudTheme theme, MudExDimension size, SliceDirection sliceDirection)
        => CombineSliced(ApplicationImage(theme, true, size), ApplicationImage(theme, false, size), size.Width, size.Height, sliceDirection);

    /// <summary>
    /// Returns a default sliced application image as preview for given Theme.
    /// </summary>
    /// <param name="theme">The color scheme to use for the image.</param>
    /// <param name="size">The dimensions of the output image.</param>
    /// <returns>A new SVG that displays an application image sliced diagonally.</returns>
    public static string ApplicationImage(MudTheme theme, MudExDimension size)
        => ApplicationImage(theme, size, SliceDirection.Diagonal);

    /// <summary>
    /// Returns a default application image as preview for given Theme in dark or light.
    /// </summary>
    /// <param name="theme">The color scheme to use for the image.</param>
    /// <param name="dark">Whether to use the dark color scheme for the image.</param>
    /// <param name="size">The dimensions of the output image.</param>
    /// <returns>A new SVG that displays an application image sliced either horizontally, vertically, or diagonally.</returns>
    public static string ApplicationImage(MudTheme theme, bool dark, MudExDimension size)
        => ApplicationImage(theme, dark, size.Width, size.Height);

    /// <summary>
    ///  Returns a default application image as preview for given Theme in dark or light.
    /// </summary>
    /// <param name="theme">The color scheme to use for the image.</param>
    /// <param name="dark">Whether to use the dark color scheme for the image.</param>
    /// <param name="width">The width of the output image.</param>
    /// <param name="height">The height of the output image.</param>
    /// <returns>A new SVG that displays an application image sliced diagonally.</returns>
    public static string ApplicationImage(MudTheme theme, bool dark, string width, string height)
        => ApplicationImage(dark ? theme.PaletteDark : theme.Palette, width, height);

    /// <summary>
    ///  Returns a default application image as with given color palette.
    /// </summary>    
    public static string ApplicationImage(Palette palette, MudExDimension size)
        => ApplicationImage(palette, size.Width, size.Height);
    
    /// <summary>
    ///  Returns a default application image as with given color palette.
    /// </summary>    
    public static string ApplicationImage(Palette palette, string width, string height)
        => ApplicationImage(palette.AppbarBackground, palette.DrawerBackground, palette.Surface, new[] { palette.TextPrimary, palette.TextSecondary, palette.Primary, palette.Secondary, palette.Info, palette.Warning, palette.Error }, width, height);

    /// <summary>
    ///  Returns a default application image as with given color palette.
    /// </summary>    
    public static string ApplicationImage(PaletteLight palette, MudExDimension size)
        => ApplicationImage(palette, size.Width, size.Height);

    /// <summary>
    ///  Returns a default application image as with given color palette.
    /// </summary>    
    public static string ApplicationImage(PaletteDark palette, MudExDimension size)
        => ApplicationImage(palette, size.Width, size.Height);


    /// <summary>
    ///  Returns a default application image with given colors.
    /// </summary>    
    public static string ApplicationImage(MudColor appBarColor, MudColor drawerColor, MudColor surfaceColor, MudColor textColor, string size)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, new[] { textColor }, size, size);

    /// <summary>
    ///  Returns a default application image with given colors.
    /// </summary>    
    public static string ApplicationImage(MudColor appBarColor, MudColor drawerColor, MudColor surfaceColor, MudColor[] textColors, string size)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, textColors, size, size);

    /// <summary>
    ///  Returns a default application image with given colors.
    /// </summary>        
    public static string ApplicationImage(MudColor appBarColor, MudColor drawerColor, MudColor surfaceColor, MudColor textColor, MudExDimension dimension)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, new[] { textColor }, dimension.Width.ToString(), dimension.Height.ToString());

    /// <summary>
    ///  Returns a default application image with given colors.
    /// </summary>        
    public static string ApplicationImage(MudColor appBarColor, MudColor drawerColor, MudColor surfaceColor, MudColor[] textColors, MudExDimension dimension)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, textColors, dimension.Width.ToString(), dimension.Height.ToString());

    /// <summary>
    ///  Returns a default application image with given colors.
    /// </summary>    
    public static string ApplicationImage(MudColor appBarColor, MudColor drawerColor, MudColor surfaceColor, MudColor[] textColors, string width, string height)
        => ApplicationImage(appBarColor.ToString(), drawerColor.ToString(), surfaceColor.ToString(), textColors.Select(c => c.ToString()).ToArray(), width, height);

    /// <summary>
    ///  Returns a default application image with given colors.
    /// </summary>
    public static string ApplicationImage(string appBarColor, string drawerColor, string surfaceColor, string textColor, string size)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, new[] { textColor }, size, size);

    /// <summary>
    ///  Returns a default application image with given colors.
    /// </summary>        
    public static string ApplicationImage(string appBarColor, string drawerColor, string surfaceColor, string[] textColors, string size)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, textColors, size, size);

    /// <summary>
    ///  Returns a default application image with given colors.
    /// </summary>    
    public static string ApplicationImage(string appBarColor, string drawerColor, string surfaceColor, string textColor, MudExDimension dimension)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, new[] { textColor }, dimension.Width.ToString(), dimension.Height.ToString());

    /// <summary>
    ///  Returns a default application image with given colors.
    /// </summary>    
    public static string ApplicationImage(string appBarColor, string drawerColor, string surfaceColor, string[] textColors, MudExDimension dimension)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, textColors, dimension.Width.ToString(), dimension.Height.ToString());

    /// <summary>
    ///  Returns a default application image with given colors.
    /// </summary>    
    public static string ApplicationImage(string appBarColor, string drawerColor, string surfaceColor, string[] textColors, string width, string height)
    {
        // If only one color provided, repeat it to ensure min 3 lines
        textColors = textColors.Length > 1 ? textColors : Enumerable.Repeat(textColors[0], 3).ToArray();

        // wave lines
        //var fakeText = string.Join(" ", textColors.Select((color, i) => $@"<path d='M20,{20 + i * 10} Q30,{10 + i * 10} 40,{20 + i * 10} T60,{20 + i * 10} T80,{20 + i * 10} T100,{20 + i * 10}' stroke='{color}' stroke-width='2' fill='none' />    "));

        // straight lines
        var fakeText = string.Join(" ", textColors.Select((color, i) => $"<line x1='25%' y1='{20 + i * 10}%' x2='75%' y2='{20 + i * 10}%' style='stroke:{color};stroke-width:2' />"));

        // random wave lines
        //var random = new Random();
        //var fakeText = string.Join(" ", textColors.Select((color, i) =>
        //{
        //    var yOffset = 20 + i * 10;
        //    var randomAmplitude = random.Next(1, 5);
        //    return $@"
        //    <path d='M20,{yOffset} Q30,{yOffset - randomAmplitude} 40,{yOffset} T60,{yOffset} T80,{yOffset} T100,{yOffset}' stroke='{color}' stroke-width='2' fill='none' />";
        //}));

        return $@"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}'>                
            <rect width='100%' height='25%' style='fill:{appBarColor};' />                
            <rect y='10%' width='20%' height='75%' style='fill:{drawerColor};' />                
            <rect x='20%' y='10%' width='80%' height='75%' style='fill:{surfaceColor};' />                
            {fakeText}
        </svg>";
    }

}

/// <summary>
/// Enum to specify svg slice direction
/// </summary>
public enum SliceDirection
{
    Diagonal,
    Vertical,
    Horizontal
}