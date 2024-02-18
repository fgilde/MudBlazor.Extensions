namespace MudBlazor.Extensions.Components;

/// <summary>
/// Render behavior of the child fragment inside the code view
/// </summary>
public enum CodeViewModeWithRenderFragment
{
    /// <summary>
    /// Code is expandable and collapsible
    /// </summary>
    ExpansionPanel,
    
    /// <summary>
    /// Code is docked to the left
    /// </summary>
    CodeDockedLeft,
    
    /// <summary>
    /// Code is docked to the right
    /// </summary>
    CodeDockedRight,
    
    /// <summary>
    /// Code is docked to the top
    /// </summary>
    CodeDockedTop,
    
    /// <summary>
    /// Cpde is docked to the bottom
    /// </summary>
    CodeDockedBottom
}