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

    public MudExSize(T value, CssUnit sizeUnit = CssUnit.Pixels)
    {
        Value = value;
        SizeUnit = sizeUnit;
    }

    public static implicit operator T(MudExSize<T> size) => size.Value;
    public static implicit operator MudExSize<T>(T s) => new(s);
}
