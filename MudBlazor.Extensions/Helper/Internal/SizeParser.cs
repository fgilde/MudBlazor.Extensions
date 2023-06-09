using System.ComponentModel;
using System.Text.RegularExpressions;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Helper.Internal;

internal static class SizeParser
{
    public static (T Value, CssUnit Unit) ParseMudExSize<T>(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (default, CssUnit.Pixels);

        var regex = new Regex(@"^(\d*\.?\d+)([a-zA-Z%]+)$");
        var match = regex.Match(value);

        if (!match.Success)
            throw new ArgumentException("Invalid value and unit format.", nameof(value));

        var size = match.Groups[1].Value;
        var unit = match.Groups[2].Value;

        T sizeValue = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(size);

        var matchingUnit = Enum.GetValues(typeof(CssUnit))
            .Cast<CssUnit>()
            .FirstOrDefault(u => u.ToDescriptionString().Equals(unit, StringComparison.OrdinalIgnoreCase));

        if (Enum.IsDefined(typeof(CssUnit), matchingUnit))
            return (sizeValue, matchingUnit);
        
        throw new ArgumentException("Invalid unit", nameof(value));
    }
}