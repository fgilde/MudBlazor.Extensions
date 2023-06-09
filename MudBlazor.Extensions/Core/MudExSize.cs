using MudBlazor.Extensions.Helper.Internal;
using OneOf;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Core;

public readonly struct MudExSize<T>
{
    public T Value { get; }
    public CssUnit SizeUnit { get; }

    public MudExSize(string value)
    {
        var res = SizeParser.ParseMudExSize<T>(value);
        Value = res.Value;
        SizeUnit = res.Unit;
    }

    public MudExSize(T value, CssUnit sizeUnit = CssUnit.Pixels)
    {
        Value = value;
        SizeUnit = sizeUnit;
    }

    public override string ToString()
    {
        var stringValue = Value switch
        {
            float floatValue => floatValue.ToString(CultureInfo.InvariantCulture),
            double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
            _ => Value.ToString()
        };

        return $"{stringValue}{Nextended.Core.Helper.EnumExtensions.ToDescriptionString(SizeUnit)}";
    }

    public static implicit operator T(MudExSize<T> size) => size.Value;
    public static implicit operator MudExSize<T>(T s) => new(s);
    public static implicit operator string(MudExSize<T> size) => size.ToString();
    public static implicit operator MudExSize<T>(string s) => new(s);
}