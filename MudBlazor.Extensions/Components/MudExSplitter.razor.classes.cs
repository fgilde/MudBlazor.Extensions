using MudBlazor.Interop;

namespace MudBlazor.Extensions.Components;

public class SizeResponse
{
    public ElementSize Prev { get; set; }
    public ElementSize Next { get; set; }
}

public class ElementSize
{
    public string Width { get; set; }
    public string Height { get; set; }
    public BoundingClientRect Bounds { get; set; }
}
