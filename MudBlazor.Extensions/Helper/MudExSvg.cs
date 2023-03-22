using System.Text;

namespace MudBlazor.Extensions.Helper;

public static class MudExSvg
{
    public static string CombineIconsHorizontal(string image, params string[] other)
    {
        return CombineIcons(14, 0, "0 0 24 24", image, other);
    }

    public static string CombineIconsVertical(string image, params string[] other)
    {
        return CombineIcons(0, 14, "0 0 24 24", image, other);
    }

    public static string CombineIconsCentered(string image, params string[] other)
    {
        return CombineIcons(0, 0, null, image, other);
    }

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
}