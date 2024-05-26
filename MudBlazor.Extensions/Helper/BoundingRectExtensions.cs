using MudBlazor.Extensions.Core;
using MudBlazor.Interop;

namespace MudBlazor.Extensions.Helper;

public static class BoundingRectExtensions
{
    public static bool Contains(this BoundingClientRect rect, BoundingClientRect other)
        => rect.Left <= other.Left && rect.Right >= other.Right && rect.Top <= other.Top && rect.Bottom >= other.Bottom;

    public static bool Contains(this BoundingClientRect rect, double x, double y)
        => rect.Left <= x && rect.Right >= x && rect.Top <= y && rect.Bottom >= y;

    public static bool Intersects(this BoundingClientRect rect, BoundingClientRect other)
        => rect.Left < other.Right && rect.Right > other.Left && rect.Top < other.Bottom && rect.Bottom > other.Top;

    
    public static MudExDimension ToDimension(this BoundingClientRect rect, CssUnit cssUnit)
    {
        var widthSize = CreateMudExSize(rect.Width, rect.WindowWidth, cssUnit);
        var heightSize = CreateMudExSize(rect.Height, rect.WindowHeight, cssUnit);

        return new MudExDimension(widthSize, heightSize);
    }

    public static MudExPosition ToPosition(this BoundingClientRect rect, CssUnit cssUnit)
    {
        var leftSize = CreateMudExSize(rect.Left, rect.WindowWidth, cssUnit);
        var topSize = CreateMudExSize(rect.Top, rect.WindowHeight, cssUnit);

        return new MudExPosition(leftSize, topSize);
    }

    private static MudExSize<double> CreateMudExSize(double dimension, double referenceDimension, CssUnit cssUnit)
    {
        double conversionFactor = GetConversionFactor(dimension, referenceDimension, cssUnit);
        return new MudExSize<double>(dimension * conversionFactor, cssUnit);
    }

    private static double GetConversionFactor(double dimension, double referenceDimension, CssUnit cssUnit)
    {
        return cssUnit switch
        {
            CssUnit.Percentage =>
                100.0 / referenceDimension,
            CssUnit.ViewportWidth =>
                100.0 / referenceDimension,
            CssUnit.ViewportHeight =>
                100.0 / referenceDimension,
            CssUnit.ViewportMinimum =>
                100.0 / Math.Min(referenceDimension, dimension),
            CssUnit.ViewportMaximum =>
                100.0 / Math.Max(referenceDimension, dimension),
            _ => 1  // No conversion for absolute units
        };
    }

}