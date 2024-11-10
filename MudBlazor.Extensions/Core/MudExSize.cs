using MudBlazor.Extensions.Helper.Internal;
using System.Globalization;
using MudBlazor.Extensions.Attribute;
using System.Numerics;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// MudExSize is a readonly struct that provides a convenient and type-safe way to deal with size values in the context of MudBlazor components.
/// </summary>
[HasDocumentation("MudExSize.md")]
public readonly struct MudExSize<T> where T : INumber<T>
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
    /// String representation of the size that is fully compatible with all css sizes.
    /// </summary>
    public string CssValue => ToString();

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
    /// Returns true if the size is zero
    /// </summary>
    /// <returns></returns>
    public bool IsZero()
    {
        return T.IsZero(Value);
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

    public MudExSize<double> ToAbsolute(double parentSize = 0, double fontSize = 16,    
        double viewportWidth = 0,   
        double viewportHeight = 0,
        double dpi = 96)
    {
        var doubleValue = Value is double value ? value : 0;
        return MudExSize<double>.ToAbsolute(new MudExSize<double>(doubleValue, SizeUnit), parentSize, fontSize, viewportWidth, viewportHeight, dpi);
    }

    public static MudExSize<double> ToAbsolute(MudExSize<double> size,
        double parentSize = 0,      // Für Prozentangaben
        double fontSize = 16,       // Für em und rem, Standard-Schriftgröße ist meist 16px
        double viewportWidth = 0,   // Für vw
        double viewportHeight = 0,  // Für vh
        double dpi = 96             // Für physische Maßeinheiten (cm, mm, in, pt, pc)
    )
    {
        const double inchInMillimeters = 25.4;

        return size.SizeUnit switch
        {
            CssUnit.Pixels => size.Value,

            CssUnit.Percentage when parentSize > 0 =>
                (parentSize * size.Value) / 100,

            CssUnit.Em => size.Value * fontSize,

            CssUnit.Rem => size.Value * fontSize,

            CssUnit.ViewportWidth when viewportWidth > 0 =>
                (viewportWidth * size.Value) / 100,

            CssUnit.ViewportHeight when viewportHeight > 0 =>
                (viewportHeight * size.Value) / 100,

            CssUnit.ViewportMinimum when viewportWidth > 0 && viewportHeight > 0 =>
                (Math.Min(viewportWidth, viewportHeight) * size.Value) / 100,

            CssUnit.ViewportMaximum when viewportWidth > 0 && viewportHeight > 0 =>
                (Math.Max(viewportWidth, viewportHeight) * size.Value) / 100,

            CssUnit.Centimeters => (size.Value / inchInMillimeters) * dpi,

            CssUnit.Millimeters => (size.Value / inchInMillimeters) * dpi,

            CssUnit.Inches => size.Value * dpi,

            CssUnit.Points => (size.Value * dpi) / 72,  // 1pt = 1/72 inch

            CssUnit.Picas => (size.Value * dpi) / 6,    // 1pc = 12pt = 1/6 inch

            _ => throw new NotSupportedException($"Konvertierung für Einheit {size.SizeUnit} wird nicht unterstützt oder es fehlen Kontextinformationen.")
        };
    }


}