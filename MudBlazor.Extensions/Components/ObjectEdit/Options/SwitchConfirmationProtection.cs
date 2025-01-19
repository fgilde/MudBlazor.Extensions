namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <inheritdoc />
public class SwitchConfirmationProtection : BooleanInputProtectionBase<MudSwitch<bool>>
{
    public SwitchConfirmationProtection(string tooltip = null, Action<MudSwitch<bool>> configure = null) : base(tooltip, configure)
    { }
}