using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// MudExPromptDialog Component
/// </summary>
public partial class MudExPromptDialog
{
    /// <summary>
    /// Cascading parameter of the MudDialogInstance object, which is responsible for the dialog instance.
    /// </summary>
    [CascadingParameter] MudDialogInstance MudDialog { get; set; }

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
    /// The CSS class of the component root element.
    /// </summary>
    [Parameter, SafeCategory("Common")]
    public string Class { get; set; } = "mud-ex-dialog-initial";

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
    /// The list of strings with additional details of the component.
    /// </summary>
    [Parameter, SafeCategory("Content")]
    public IEnumerable<string> Details { get; set; }

    /// <summary>
    /// The function that determines whether the prompt window can be confirmed or not.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public Func<string, bool> CanConfirm { get; set; } = s => true;


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