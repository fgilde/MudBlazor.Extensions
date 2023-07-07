using MudBlazor.Extensions.Helper.Internal;
using System.Globalization;
using MudBlazor.Extensions.Attribute;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// MudExSize is a readonly struct that provides a convenient and type-safe way to deal with size values in the context of MudBlazor components.
/// </summary>
[HasDocumentation("MudExSize.md")]
public readonly struct MudExSize<T>
{
    /// <summary>
    /// The value of the size.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// The unit of the size.
    /// </summary>
    public CssUnit SizeUnit { get; }

    /// <summary>
    /// Creates a new size by parsing an string like 3px or 10% etc
    /// </summary>
    public MudExSize(string value)
    {
        var res = SizeParser.ParseMudExSize<T>(value);
        Value = res.Value;
        SizeUnit = res.Unit;
    }

    /// <summary>
    /// Creates a new size
    /// </summary>
    public MudExSize(T value, CssUnit sizeUnit = CssUnit.Pixels)
    {
        Value = value;
        SizeUnit = sizeUnit;
    }

    /// <summary>
    /// Returns the string representation that is fully compatible with all css sizes
    /// </summary>
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

    /// <summary>
    /// Implicit conversion from MudExSize type to Generic Type T.
    /// </summary>
    public static implicit operator T(MudExSize<T> size) => size.Value;

    /// <summary>
    /// Implicit conversion from Generic Type T to MudExSize.
    /// </summary>
    public static implicit operator MudExSize<T>(T s) => new(s);

    /// <summary>
    /// Implicit conversion from MudExSize type to String.
    /// </summary>
    public static implicit operator string(MudExSize<T> size) => size.ToString();

    /// <summary>
    /// Implicit conversion from String to MudExSize type.
    /// </summary>
    public static implicit operator MudExSize<T>(string s) => new(s);

}