using MudBlazor.Extensions.Attribute;
using System.Reflection;
using System.Text;

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
}