namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Reset settings for a property.
/// </summary>
public class PropertyResetSettings
{
    /// <summary>
    /// Reset is allowed for this property.
    /// </summary>
    public bool AllowReset { get; set; } = true;
    
    /// <summary>
    /// Icon to use for the reset button.
    /// </summary>
    public string ResetIcon { get; set; } = Icons.Material.Outlined.Restore;
    
    /// <summary>
    /// Tooltip text to display when hovering over the reset button.
    /// </summary>
    public string ResetText { get; set; } = "Reset {0}";
    
    /// <summary>
    /// Show the reset text.
    /// </summary>
    public bool ShowResetText { get; set; } = false;
}

/// <summary>
/// Reset settings for the whole object.
/// </summary>
public class GlobalResetSettings: PropertyResetSettings
{
    /// <summary>
    /// Create a new instance of GlobalResetSettings.
    /// </summary>
    public GlobalResetSettings()
    {
        RequiresConfirmation = true;
        ResetIcon = Icons.Material.Filled.Undo;
        ResetText = "Reset";
    }

    /// <summary>
    /// Requires confirmation before resetting the object.
    /// </summary>
    public bool RequiresConfirmation { get; set; }
}