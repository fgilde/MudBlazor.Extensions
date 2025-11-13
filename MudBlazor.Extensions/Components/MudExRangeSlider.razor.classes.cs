
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
