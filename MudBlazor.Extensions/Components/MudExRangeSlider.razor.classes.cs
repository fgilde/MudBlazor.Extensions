
using Nextended.Core.Contracts;
using System.ComponentModel;

namespace MudBlazor.Extensions.Components;

public enum SliderOrientation
{
    [Description("mud-ex-horizontal")]
    Horizontal,

    [Description("mud-ex-vertical")]
    Vertical
}

internal enum DragMode { None, StartThumb, EndThumb, WholeRange }

public enum Thumb { Start, End }

//TODO: check BoundingClientRect
internal record struct DomRect(double Left, double Top, double Width, double Height);



public class MudExRangeContextBase<T> where T : IComparable<T> { }

// Context classes
public class MudExRangeSliderContext<T> where T : struct, IComparable<T>
{
    public IRange<T> Value { get; init; } = default!;
    public IRange<T> SizeRange { get; init; } = default!;
    public double StartPercent { get; init; }
    public double EndPercent { get; init; }
    public Size Size { get; init; }
    public bool Disabled { get; init; }
    public bool ReadOnly { get; init; }
}

public class MudExRangeSliderThumbContext<T> where T : struct, IComparable<T>
{
    public T Value { get; init; } = default!;
    public Thumb Thumb { get; init; }
    public double Percent { get; init; }
    public Size Size { get; init; }
    public bool Disabled { get; init; }
    public bool ReadOnly { get; init; }
}