using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Core;

using System;
using System.ComponentModel;
using System.Numerics;

public enum CssUnit
{
    [Description("%")]
    Percentage,
    [Description("px")]
    Pixels,
    [Description("em")]
    Em,
    [Description("rem")]
    Rem,
    [Description("vw")]
    ViewportWidth,
    [Description("vh")]
    ViewportHeight,
    [Description("vmin")]
    ViewportMinimum,
    [Description("vmax")]
    ViewportMaximum,
    [Description("cm")]
    Centimeters,
    [Description("mm")]
    Millimeters,
    [Description("in")]
    Inches,
    [Description("pt")]
    Points,
    [Description("pc")]
    Picas
}

public class MudExSize<T>
{
    public T Value { get; set; }
    public CssUnit SizeUnit { get; set; }

    public MudExSize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));

        var regex = new Regex(@"^(\d*\.?\d+)([a-zA-Z%]+)$");
        var match = regex.Match(value);

        if (!match.Success)
            throw new ArgumentException("Invalid value and unit format.", nameof(value));

        var size = match.Groups[1].Value;
        var unit = match.Groups[2].Value;

        Value = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(size);

        var matchingUnit = Enum.GetValues(typeof(CssUnit))
            .Cast<CssUnit>()
            .FirstOrDefault(u => u.ToDescriptionString().Equals(unit, StringComparison.OrdinalIgnoreCase));

        if (Enum.IsDefined(typeof(CssUnit), matchingUnit))
        {
            SizeUnit = matchingUnit;
        }
        else
        {
            throw new ArgumentException("Invalid unit", nameof(value));
        }
    }

    public MudExSize(T value, CssUnit sizeUnit = CssUnit.Pixels)
    {
        Value = value;
        SizeUnit = sizeUnit;
    }

    public override string ToString() => $"{Value}{SizeUnit.ToDescriptionString()}";

    public static implicit operator T(MudExSize<T> size) => size.Value;
    public static implicit operator MudExSize<T>(T s) => new(s);

    public static implicit operator string(MudExSize<T> size) => size.ToString();
    public static implicit operator MudExSize<T>(string s) => new(s);
}
