using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Core;

using System;
using System.ComponentModel;
using System.Globalization;
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

public class MudExDimension
{
    public MudExDimension(MudExSize<double> widthAndHeight): this(widthAndHeight, widthAndHeight){}

    public MudExDimension(MudExSize<double> width, MudExSize<double> height)
    {
        Width = width;
        Height = height;
    }

    public MudExSize<double> Width { get; set; }
    public MudExSize<double> Height { get; set; }

    
    public static implicit operator MudExDimension(double s) => new(s);

}


