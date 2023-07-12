namespace MudBlazor.Extensions.Components;

[Flags]
public enum MudExCardHoverMode 
{
    /// <summary>
    /// A LightBulb follows the mouse
    /// </summary>
    LightBulb = 1,
    
    /// <summary>
    /// Simple just change background
    /// </summary>
    Simple = 2,

    /// <summary>
    /// Card zooms in
    /// </summary>
    Zoom = 4,
    
    /// <summary>
    /// Card has a 3D effect
    /// </summary>
    CardEffect3d = 8,
}