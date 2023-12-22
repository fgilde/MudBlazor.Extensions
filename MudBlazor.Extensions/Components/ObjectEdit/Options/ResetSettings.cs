namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

public class PropertyResetSettings
{
    public bool AllowReset { get; set; } = true;
    public string ResetIcon { get; set; } = Icons.Material.Outlined.Restore;
    public string ResetText { get; set; } = "Reset {0}";
    public bool ShowResetText { get; set; } = false;
}

public class GlobalResetSettings: PropertyResetSettings
{
    public GlobalResetSettings()
    {
        RequiresConfirmation = true;
        ResetIcon = Icons.Material.Filled.Undo;
        ResetText = "Reset";
    }

    public bool RequiresConfirmation { get; set; }
}