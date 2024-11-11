using MudBlazor.Extensions.Helper.Internal;

namespace MudBlazor.Extensions.Core;


/// <summary>
/// Holds two position one for left and one for top and their units
/// </summary>
public struct MudExPosition
{
    /// <summary>
    /// Creates a new instance of <see cref="MudExDimension"/>
    /// </summary>
    /// <param name="leftAndTop"></param>
    public MudExPosition(MudExSize<double> leftAndTop) : this(leftAndTop, leftAndTop) { }

    /// <summary>
    /// Creates a new instance of <see cref="MudExDimension"/>
    /// </summary>
    /// <param name="left"></param>
    /// <param name="top"></param>
    public MudExPosition(MudExSize<double> left, MudExSize<double> top)
    {
        Left = left;
        Top = top;
    }

    /// <summary>
    /// Creates a new position by parsing a string like 3px or 10% etc
    /// </summary>
    public MudExPosition(string value)
    {
        var parts = value.Split("x");
        var width = new MudExSize<double>(parts.FirstOrDefault());
        var height = new MudExSize<double>(parts.LastOrDefault());
        Left = width;
        Top = height;
    }

    /// <summary>
    /// Left
    /// </summary>
    public MudExSize<double> Left { get; set; }

    /// <summary>
    /// Top
    /// </summary>
    public MudExSize<double> Top { get; set; }

    /// <summary>
    /// Implicit conversion from double to <see cref="MudExDimension"/>
    /// </summary>
    /// <param name="s"></param>
    public static implicit operator MudExPosition(double s) => new(s);

    /// <summary>
    /// Implicit conversion from String to MudExDimension type.
    /// </summary>
    public static implicit operator MudExPosition(string s) => new(s);

    /// <inheritdoc />
    public override string ToString() => $"{Left}x{Top}";

    public MudExPosition ToAbsolute(MudExDimension reference)
    {
        return new MudExPosition(Left.ToAbsolute(reference.Width), Top.ToAbsolute(reference.Height));
    }

    public MudExPosition ToRelative(MudExDimension reference)
    {
        return new MudExPosition(Left.ToRelative(reference.Width), Top.ToRelative(reference.Height));
    }
}