using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// MudExPromptDialog Component
/// </summary>
public partial class MudExPromptDialog
{
    /// <summary>
    /// Cascading parameter of the MudDialogInstance object, which is responsible for the dialog instance.
    /// </summary>
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; }

    /// <summary>
    /// Boolean value indicating whether prompt should be submitted immediately.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool Immediate { get; set; } = true;

    /// <summary>
    /// Boolean value indicating whether prompt should be submitted when Enter key is pressed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool SubmitOnEnter { get; set; } = true;

    /// <summary>
    /// The text of the "Ok" button.
    /// </summary>
    [Parameter, SafeCategory("Content")]
    public string OkText { get; set; } = "Ok";

    /// <summary>
    /// Color for icon
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExColor OkColor { get; set; } = Color.Error;

    /// <summary>
    /// The text of the "Cancel" button.
    /// </summary>
    [Parameter, SafeCategory("Content")]
    public string CancelText { get; set; } = "Cancel";

    /// <summary>
    /// The initial value of the input field.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public string Value { get; set; }

    /// <summary>
    /// The Helper text
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public string HelperText { get; set; }

    /// <summary>
    /// The message to be displayed in the component.
    /// </summary>
    [Parameter, SafeCategory("Content")]
    public string Message { get; set; }

    /// <summary>
    /// The icon of the component.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string Icon { get; set; }

    /// <summary>
    /// Color for icon
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExColor IconColor { get; set; } = Color.Secondary;

    /// <summary>
    /// The list of strings with additional details of the component.
    /// </summary>
    [Parameter, SafeCategory("Content")]
    public IEnumerable<string> Details { get; set; }

    /// <summary>
    /// The function that determines whether the prompt window can be confirmed or not.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public Func<string, bool> CanConfirm { get; set; } = s => true;


    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Class ??= MudExCss.Classes.Dialog._Initial;
    }

    /// <summary>
    /// Method for submitting the prompt window.
    /// </summary>
    void Submit()
    {
        MudDialog.Close(DialogResult.Ok(Value));
    }

    /// <summary>
    /// Method for cancelling the prompt window.
    /// </summary>
    void Cancel() => MudDialog.Cancel();

    /// <summary>
    /// Method that determines if the input value is a valid entry.
    /// </summary>
    private bool IsValid() => CanConfirm(Value);

    private void OnKeyUp(KeyboardEventArgs arg)
    {
        if (arg.Key == "Enter" && SubmitOnEnter && IsValid())
        {
            Submit();
        }
    }
}