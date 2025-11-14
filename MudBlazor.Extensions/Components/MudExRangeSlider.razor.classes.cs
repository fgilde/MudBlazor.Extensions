
using Nextended.Core.Contracts;
using System.ComponentModel;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Orientation for the <see cref="MudExRangeSlider{T}"/> component.
/// </summary>
public enum SliderOrientation
{
    /// <summary>
    /// Default horizontal orientation.
    /// </summary>
    [Description("mud-ex-horizontal")]
    Horizontal,

    /// <summary>
    /// Specifies that the layout or orientation is vertical.
    /// </summary>
    [Description("mud-ex-vertical")]
    Vertical
}

internal enum DragMode { None, StartThumb, EndThumb, WholeRange }

/// <summary>
/// Thumb identifier for the <see cref="MudExRangeSlider{T}"/> component.
/// </summary>
public enum Thumb
{
    /// <summary>
    /// The start thumb.
    /// </summary>
    Start,

    /// <summary>
    /// The end thumb.
    /// </summary>
    End
}

//TODO: check BoundingClientRect
internal record struct DomRect(double Left, double Top, double Width, double Height);


/// <summary>
/// Template Context
/// </summary>
public abstract record MudExRangeContextBase<T> where T : IComparable<T>
{
    /// <summary>
    /// Creates a new instance of <see cref="MudExRangeContextBase{T}"/>
    /// </summary>
    protected MudExRangeContextBase(MudExRangeSlider<T> component)
    {
        Value = component.Value;
        SizeRange = component.SizeRange;
        Math = component.M;
        Size = component.Size;
        Disabled = component.Disabled;
        ReadOnly = component.ReadOnly;
    }

    public bool Disabled { get; init; }
    public bool ReadOnly { get; init; }
    public Size Size { get; init; }
    public IRangeMath<T> Math { get; init; }
    public IRange<T> Value { get; init; }
    public IRange<T> SizeRange { get; init; }


}

/// <summary>
/// Template Context
/// </summary>
public record MudExRangeSliderContext<T> : MudExRangeContextBase<T> where T : IComparable<T>
{
    /// <inheritdoc />
    public MudExRangeSliderContext(MudExRangeSlider<T> component) : base(component)
    {}

    public double StartPercent { get; init; }
    public double EndPercent { get; init; }

}

public record MudExRangeSliderThumbContext<T> : MudExRangeContextBase<T> where T : IComparable<T>
{
    public MudExRangeSliderThumbContext(MudExRangeSlider<T> component) : base(component)
    {
    }

    /// <summary>
    /// The value on this thumb
    /// </summary>
    public T ThumbValue { get; set; }

    /// <summary>
    /// Type of thumb
    /// </summary>
    public Thumb Thumb { get; init; }

    /// <summary>
    /// Gets the percentage value represented by this property.
    /// </summary>
    public double Percent { get; init; }
}