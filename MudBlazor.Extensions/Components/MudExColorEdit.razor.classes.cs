using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;


class ColorItem
{
    public ColorItem(MudExColor color, string name, string group)
    {
        Color = color;
        Name = name;
        Group = group;
    }
    public MudExColor Color { get; set; }
    public string Name { get; set; }
    public string Group { get; set; }
    public override string ToString()
    {
        return Name;
    }
}

/// <summary>
/// Mode for the preview of the color in MudExColorEdit
/// </summary>
public enum ColorPreviewMode
{
    /// <summary>
    /// No preview
    /// </summary>
    None,
    
    /// <summary>
    /// Preview is applied to the text
    /// </summary>
    Text,
    
    /// <summary>
    /// Preview is applied to the icon
    /// </summary>
    Icon,
    
    /// <summary>
    /// Preview is applied to text and icon
    /// </summary>
    Both
}

/// <summary>
/// Behavior for the auto close of the dropdown in MudExColorEdit
/// </summary>
public enum AutoCloseBehaviour
{
    /// <summary>
    /// Never close the dropdown
    /// </summary>
    Never,
    
    /// <summary>
    /// Always close the dropdown after a selection
    /// </summary>
    Always,
    
    /// <summary>
    /// Close the dropdown after a selection of a defined color
    /// </summary>
    OnDefinedSelect,
    
    /// <summary>
    /// Close the dropdown after a selection of a custom color
    /// </summary>
    OnCustomSelect
}