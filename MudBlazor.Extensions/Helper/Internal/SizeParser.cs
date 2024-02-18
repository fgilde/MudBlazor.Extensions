using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using MudBlazor.Extensions.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Helper.Internal;

internal static class SizeParser
{

    //public static (T Value, CssUnit Unit) ParseMudExSize<T>(string value)
    //{
    //    if (string.IsNullOrWhiteSpace(value))
    //        return (default, CssUnit.Pixels);
    //    if (double.TryParse(value, out var doubleValue))
    //        return (doubleValue.MapTo<T>(), CssUnit.Pixels); // TODO: use static T Parse with net7

    //    var regex = new Regex(@"^(\d*\.?\d+)([a-zA-Z%]+)$");
    //    var match = regex.Match(value);

    //    if (!match.Success)
    //        throw new ArgumentException("Invalid value and unit format.", nameof(value));

    //    var size = match.Groups[1].Value;
    //    var unit = match.Groups[2].Value;
    //    var sizeValue = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(size);

    //    var matchingUnit = Enum.GetValues(typeof(CssUnit))
    //        .Cast<CssUnit>()
    //        .FirstOrDefault(u => u.ToDescriptionString().Equals(unit, StringComparison.OrdinalIgnoreCase));

    //    return Enum.IsDefined(typeof(CssUnit), matchingUnit) 
    //        ? (sizeValue, matchingUnit) 
    //        : (sizeValue, CssUnit.Pixels);
    //}

    /// <summary>
    /// Parses a string to a tuple of value and unit.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static (T Value, CssUnit Unit) ParseMudExSize<T>(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (default, CssUnit.Pixels);

        // Try parsing with invariant culture
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue)
            || double.TryParse(value, NumberStyles.Float, new CultureInfo("de-DE"), out doubleValue))  // if invariant fails, try with German culture
            return (doubleValue.MapTo<T>(), CssUnit.Pixels); // TODO: use static T Parse with net7

        var regex = new Regex(@"^(\d*\.?\d+|\d*,?\d*)([a-zA-Z%]+)$");
        var match = regex.Match(value);

        if (!match.Success)
            throw new ArgumentException("Invalid value and unit format.", nameof(value));

        var size = match.Groups[1].Value;
        var unit = match.Groups[2].Value;

        // parse size value with the appropriate CultureInfo
        var parsed = double.TryParse(size, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDouble)
                     || double.TryParse(size, NumberStyles.Float, new CultureInfo("de-DE"), out parsedDouble)
                     || double.TryParse(size, NumberStyles.Float, new CultureInfo("en-US"), out parsedDouble);

        if (!parsed)        
            throw new ArgumentException("Invalid size value");
                
        // convert to T
        var sizeValue = (T)Convert.ChangeType(parsedDouble, typeof(T));

        var matchingUnit = Enum.GetValues(typeof(CssUnit))
            .Cast<CssUnit>()
            .FirstOrDefault(u => u.GetDescription().Equals(unit, StringComparison.OrdinalIgnoreCase));

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
