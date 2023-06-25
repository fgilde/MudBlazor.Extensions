using System.ComponentModel;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// Enum for all css units
/// </summary>
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

/// <summary>
/// Holds two sizes one for height and one for width and their units
/// </summary>
public readonly struct MudExDimension
{
    public MudExDimension(MudExSize<double> widthAndHeight): this(widthAndHeight, widthAndHeight){}

    public MudExDimension(MudExSize<double> width, MudExSize<double> height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Width
    /// </summary>
    public MudExSize<double> Width { get; }

    /// <summary>
    /// Height
    /// </summary>
    public MudExSize<double> Height { get; }

    
    public static implicit operator MudExDimension(double s) => new(s);

}


