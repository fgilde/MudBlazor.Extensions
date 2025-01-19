namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <inheritdoc />
public class CheckBoxConfirmationProtection: BooleanInputProtectionBase<MudCheckBox<bool>>
{
    public CheckBoxConfirmationProtection(string tooltip = null, Action<MudCheckBox<bool>> configure = null) : base(tooltip, configure)
    {}
}