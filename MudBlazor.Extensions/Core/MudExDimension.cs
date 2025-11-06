using MudBlazor.Extensions.Helper.Internal;

namespace MudBlazor.Extensions.Core;


/// <summary>
/// Holds two sizes one for height and one for width and their units
/// </summary>
public struct MudExDimension
{
    /// <summary>
    /// Creates a new instance of <see cref="MudExDimension"/>
    /// </summary>
    /// <param name="widthAndHeight"></param>
    public MudExDimension(MudExSize<double> widthAndHeight) : this(widthAndHeight, widthAndHeight) { }

    /// <summary>
    /// Creates a new instance of <see cref="MudExDimension"/>
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public MudExDimension(MudExSize<double> width, MudExSize<double> height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Creates a new size dimension by parsing an string like 3px or 10% etc
    /// </summary>
    public MudExDimension(string value)
    {
        value = value.ToLower().Replace("px", "__tmp__");
        var parts = value.Split("x");
        var width = new MudExSize<double>(parts.FirstOrDefault()?.Replace("__tmp__", "px"));
        var height = new MudExSize<double>(parts.LastOrDefault()?.Replace("__tmp__", "px"));
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Width
    /// </summary>
    public MudExSize<double> Width { get; set; }

    /// <summary>
    /// Height
    /// </summary>
    public MudExSize<double> Height { get; set; }

    /// <summary>
    /// Implicit conversion from double to <see cref="MudExDimension"/>
    /// </summary>
    /// <param name="s"></param>
    public static implicit operator MudExDimension(double s) => new(s);

    /// <summary>
    /// Implicit conversion from String to MudExDimension type.
    /// </summary>
    public static implicit operator MudExDimension(string s) => new(s);

    /// <inheritdoc />
    public override string ToString() => $"{Width}x{Height}";

    public MudExDimension ToAbsolute(MudExDimension reference)
    {
        return new MudExDimension(Width.ToAbsolute(reference.Width), Height.ToAbsolute(reference.Height));
    }

    public MudExDimension ToRelative(MudExDimension reference)
    {
        return new MudExDimension(Width.ToRelative(reference.Width), Height.ToRelative(reference.Height));
    }
}