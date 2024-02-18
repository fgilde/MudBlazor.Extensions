using MudBlazor.Interop;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Size response for the Splitter
/// </summary>
public class SizeResponse
{
    /// <summary>
    /// Previous element size
    /// </summary>
    public ElementSize Prev { get; set; }
    
    /// <summary>
    /// Next element size
    /// </summary>
    public ElementSize Next { get; set; }
}

/// <summary>
/// Element size
/// </summary>
public class ElementSize
{
    /// <summary>
    /// Width of the element
    /// </summary>
    public string Width { get; set; }
    
    /// <summary>
    /// Height of the element
    /// </summary>
    public string Height { get; set; }
    
    /// <summary>
    /// Bounding client rect of the element
    /// </summary>
    public BoundingClientRect Bounds { get; set; }
}
