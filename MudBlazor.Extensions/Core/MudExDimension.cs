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
}