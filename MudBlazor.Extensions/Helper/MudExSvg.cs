using MudBlazor.Extensions.Attribute;
using System.Reflection;
using System.Text;
using MudBlazor.Extensions.Core;
using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Helper;

[HasDocumentation("MudExSvg.md")]
public static class MudExSvg
{
    public static T GetRandomMember<T>(Type type = null)
    {
        type = type ?? typeof(Icons.Material.Filled);
        var random = new Random();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(T))
            .ToList();

        if (fields.Count == 0)
            return default;

        int index = random.Next(fields.Count);
        return (T)fields[index].GetValue(null);
    }
    
    public static string CombineIconsHorizontal(string image, params string[] other) 
        => CombineIcons(14, 0, "0 0 24 24", image, other);

    public static string CombineIconsVertical(string image, params string[] other) 
        => CombineIcons(0, 14, "0 0 24 24", image, other);

    public static string CombineIconsCentered(string image, params string[] other) 
        => CombineIcons(0, 0, null, image, other);

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

    public static string SvgPropertyName(string value)
    {
        var types = typeof(Icons).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(Icons).Namespace).ToList();

        return types.Select(type => SearchTypeForValue(type, value)).FirstOrDefault(result => result != null);
    }

    private static string SearchTypeForValue(Type type, string value) 
        => (from field in type.GetFields(BindingFlags.Public | BindingFlags.Static) where (field.IsLiteral || field.IsStatic) && field.FieldType == typeof(string) let fieldValue = field.GetValue(null) as string where fieldValue == value select $"{type.FullName.Replace('+', '.')}.{field.Name}").FirstOrDefault();


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

    

    public static string ApplicationImage(MudTheme theme, MudExDimension size, SliceDirection sliceDirection)
        => CombineSliced(ApplicationImage(theme, true, size), ApplicationImage(theme, false, size), size.Width, size.Height, sliceDirection);

    public static string ApplicationImage(MudTheme theme, MudExDimension size)
        => ApplicationImage(theme, size, SliceDirection.Diagonal);
    
    public static string ApplicationImage(MudTheme theme, bool dark, MudExDimension size)
        => ApplicationImage(theme, dark, size.Width, size.Height);
    
    public static string ApplicationImage(MudTheme theme, bool dark, string width, string height)
        => ApplicationImage(dark ? theme.PaletteDark : theme.Palette, width, height);

    public static string ApplicationImage(Palette palette, MudExDimension size)
        => ApplicationImage(palette, size.Width, size.Height);
    public static string ApplicationImage(Palette palette, string width, string height)
        => ApplicationImage(palette.AppbarBackground, palette.DrawerBackground, palette.Surface, new[] { palette.TextPrimary, palette.TextSecondary, palette.Primary, palette.Secondary, palette.Info, palette.Warning, palette.Error }, width, height);

    public static string ApplicationImage(PaletteLight palette, MudExDimension size)
        => ApplicationImage(palette, size.Width, size.Height);
    //public static string ApplicationImage(PaletteLight palette, string width, string height)
    //    => ApplicationImage(palette.AppbarBackground, palette.DrawerBackground, palette.Surface, new[] { palette.TextPrimary, palette.TextSecondary, palette.Primary, palette.Secondary, palette.Info, palette.Warning, palette.Error }, width, height);

    public static string ApplicationImage(PaletteDark palette, MudExDimension size)
        => ApplicationImage(palette, size.Width, size.Height);
    //public static string ApplicationImage(PaletteDark palette, string width, string height)
    //    => ApplicationImage(palette.AppbarBackground, palette.DrawerBackground, palette.Surface, new[] { palette.TextPrimary, palette.TextSecondary, palette.Primary, palette.Secondary, palette.Info, palette.Warning, palette.Error }, width, height);

    

    public static string ApplicationImage(MudColor appBarColor, MudColor drawerColor, MudColor surfaceColor, MudColor textColor, string size)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, new[] { textColor }, size, size);


    public static string ApplicationImage(MudColor appBarColor, MudColor drawerColor, MudColor surfaceColor, MudColor[] textColors, string size)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, textColors, size, size);

    public static string ApplicationImage(MudColor appBarColor, MudColor drawerColor, MudColor surfaceColor, MudColor textColor, MudExDimension dimension)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, new[] { textColor }, dimension.Width.ToString(), dimension.Height.ToString());

    public static string ApplicationImage(MudColor appBarColor, MudColor drawerColor, MudColor surfaceColor, MudColor[] textColors, MudExDimension dimension)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, textColors, dimension.Width.ToString(), dimension.Height.ToString());

    public static string ApplicationImage(MudColor appBarColor, MudColor drawerColor, MudColor surfaceColor, MudColor[] textColors, string width, string height)
        => ApplicationImage(appBarColor.ToString(), drawerColor.ToString(), surfaceColor.ToString(), textColors.Select(c => c.ToString()).ToArray(), width, height);
    
    
    public static string ApplicationImage(string appBarColor, string drawerColor, string surfaceColor, string textColor, string size)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, new[] { textColor }, size, size);

    public static string ApplicationImage(string appBarColor, string drawerColor, string surfaceColor, string[] textColors, string size)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, textColors, size, size);


    public static string ApplicationImage(string appBarColor, string drawerColor, string surfaceColor, string textColor, MudExDimension dimension)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, new[] { textColor }, dimension.Width.ToString(), dimension.Height.ToString());

    public static string ApplicationImage(string appBarColor, string drawerColor, string surfaceColor, string[] textColors, MudExDimension dimension)
        => ApplicationImage(appBarColor, drawerColor, surfaceColor, textColors, dimension.Width.ToString(), dimension.Height.ToString());


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


public enum SliceDirection
{
    Diagonal,
    Vertical,
    Horizontal
}