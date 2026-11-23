using System.Globalization;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

internal static class DocumentUnits
{
    private const double TwipsPerInch = 1440;
    private const double CmPerInch = 2.54;

    public static bool Metric(CultureInfo culture)
    {
        try
        {
            return new RegionInfo(culture.Name).IsMetric;
        }
        catch (Exception)
        {
            return true;
        }
    }

    public static double FromTwips(int twips, bool metric) => Math.Round(twips / TwipsPerInch * (metric ? CmPerInch : 1), 2);

    public static int ToTwips(double value, bool metric) =>
        (int)Math.Round(Math.Clamp(value / (metric ? CmPerInch : 1) * TwipsPerInch, -DocumentCss.MaxTwips, DocumentCss.MaxTwips));

    public static string Unit(bool metric) => metric ? "cm" : "in";
}
