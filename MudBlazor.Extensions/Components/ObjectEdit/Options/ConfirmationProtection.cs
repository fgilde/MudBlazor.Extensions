using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Provides a set of static factory options to protect a component with a confirmation.
/// </summary>
public static class ConfirmationProtection
{
    public static SwitchConfirmationProtection Switch() => Switch(null, box => { box.UncheckedColor = Color.Default; box.Color = Color.Primary; });
    public static SwitchConfirmationProtection Switch(string tooltip) => Switch(tooltip, box => { box.UncheckedColor = Color.Default; box.Color = Color.Primary; });
    public static SwitchConfirmationProtection Switch(string tooltip, Action<MudSwitch<bool>> configure) => new(tooltip, configure);

    public static CheckBoxConfirmationProtection CheckBox() => CheckBox(null, box => { box.UncheckedColor = Color.Default; box.Color = Color.Primary;});
    public static CheckBoxConfirmationProtection CheckBox(string tooltip) => CheckBox(tooltip, box => { box.UncheckedColor = Color.Default; box.Color = Color.Primary;});
    public static CheckBoxConfirmationProtection CheckBox(string tooltip, Action<MudCheckBox<bool>> configure) => new (tooltip, configure);

    public static ToggleButtonConfirmationProtection ToggleButton() => ToggleButton(null, null);
    public static ToggleButtonConfirmationProtection ToggleButton(string tooltip) => ToggleButton(tooltip, null);
    public static ToggleButtonConfirmationProtection ToggleButton(string tooltip, Action<MudToggleIconButton> configure) => new(tooltip, configure);


    public static DialogConfirmationProtection ConfirmDialog(IDialogService service) => ConfirmDialog(service, "Confirmation", "Press Unlock to confirm edit", null, null);
    public static DialogConfirmationProtection ConfirmDialog(IDialogService service, string title, string message, 
        Action<MudToggleIconButton> configure = null,
        Action<MudExMessageDialog> dialogConfigure = null) 
        => new(service, title, message, configure, dialogConfigure);

    public static DialogConfirmationProtection ConfirmDialog(IDialogService service, string title, string message,
        DialogOptionsEx dialogOptions,
        Action<MudToggleIconButton> configure = null,
        Action<MudExMessageDialog> dialogConfigure = null)
        => new(service, title, message, configure, dialogConfigure) { DialogOptions = dialogOptions };

    public static DialogConfirmationProtection ConfirmDialog(IDialogService service, string title, string message, string okText, string cancelText,
        DialogOptionsEx dialogOptions,
        Action<MudToggleIconButton> configure = null,
        Action<MudExMessageDialog> dialogConfigure = null)
        => new(service, title, message, configure, dialogConfigure) { DialogOptions = dialogOptions, OkText = okText, CancelText = cancelText };



    public static DialogConfirmationProtection PromptDialog(IDialogService service) => PromptDialog(service, "Confirmation", "confirm", "Please confirm to unlock edit", "Please type {0} to confirm", null, null);

    public static DialogConfirmationProtection PromptDialog(IDialogService service, string title, string confirmationWord, string message, string helperText,
        Action<MudToggleIconButton> configure = null,
        Action<MudExPromptDialog> dialogConfigure = null)
        => new(service, title, confirmationWord, message, helperText, configure, dialogConfigure);

    public static DialogConfirmationProtection PromptDialog(IDialogService service, string title, string confirmationWord, string message, string helperText,
        DialogOptionsEx dialogOptions,
        Action<MudToggleIconButton> configure = null,
        Action<MudExPromptDialog> dialogConfigure = null)
        => new(service, title, confirmationWord, message, helperText, configure, dialogConfigure) { DialogOptions = dialogOptions };

    public static DialogConfirmationProtection PromptDialog(IDialogService service, string title, string confirmationWord, string message, string helperText, string okText, string cancelText,
        DialogOptionsEx dialogOptions,
        Action<MudToggleIconButton> configure = null,
        Action<MudExPromptDialog> dialogConfigure = null)
        => new(service, title, confirmationWord, message, helperText, configure, dialogConfigure) { DialogOptions = dialogOptions, OkText = okText, CancelText = cancelText };

}