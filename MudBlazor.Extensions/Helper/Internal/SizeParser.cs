using System.ComponentModel;
using System.Text.RegularExpressions;
using MudBlazor.Extensions.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Helper.Internal;

internal static class SizeParser
{
    public static (T Value, CssUnit Unit) ParseMudExSize<T>(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (default, CssUnit.Pixels);
        if (double.TryParse(value, out var doubleValue))
            return (doubleValue.MapTo<T>(), CssUnit.Pixels); // TODO: use static T Parse with net7

        var regex = new Regex(@"^(\d*\.?\d+)([a-zA-Z%]+)$");
        var match = regex.Match(value);

        if (!match.Success)
            throw new ArgumentException("Invalid value and unit format.", nameof(value));

        var size = match.Groups[1].Value;
        var unit = match.Groups[2].Value;
        var sizeValue = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(size);

        var matchingUnit = Enum.GetValues(typeof(CssUnit))
            .Cast<CssUnit>()
            .FirstOrDefault(u => u.ToDescriptionString().Equals(unit, StringComparison.OrdinalIgnoreCase));

        return Enum.IsDefined(typeof(CssUnit), matchingUnit) 
            ? (sizeValue, matchingUnit) 
            : (sizeValue, CssUnit.Pixels);
    }

    //public static T Parse<T>(this ReadOnlySpan<char> input, IFormatProvider? formatProvider = null)
    //    where T: ISpanParsable<T>
    //{
    //    return T.Parse(input, formatProvider);
    //}
    
    //public static T Parse<T>(this string input, IFormatProvider? formatProvider = null)
    //    where T : IParsable<T>
    //{
    //    return T.Parse(input, formatProvider);
    //}
}
