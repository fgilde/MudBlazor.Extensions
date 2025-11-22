using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components.ObjectEdit;

/// <summary>
/// Dialog for editing objects with the MudExObjectEditForm
/// </summary>
/// <typeparam name="T">Type of object being edited</typeparam>
public partial class MudExObjectEditDialog<T>
{
    /// <summary>
    /// If set to true, overwrite the standard action bar of the dialog with custom content.
    /// </summary>
    protected override bool OverwriteActionBar => true;

    /// <summary>
    /// The MudDialog instance that represents the current dialog instance that is open.
    /// </summary>
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; }

    /// <summary>
    /// If set to true, remove the side padding of the dialog content.
    /// </summary>
    [Parameter]
    public bool Gutters { get; set; }

    /// <summary>
    /// The CSS class to apply to the dialog.
    /// </summary>
    [Parameter]
    public string DialogClass { get; set; } = $"{MudExCss.Classes.Dialog.FullHeightContent} overflow-hidden {MudExCss.Classes.Dialog.ObjectEdit}";

    /// <summary>
    /// The CSS class to apply to the content area of the dialog.
    /// </summary>
    [Parameter]
    public string ContentClass { get; set; } = "flex-column";

    /// <summary>
    /// The CSS class to apply to the form element of the dialog.
    /// </summary>
    [Parameter]
    public string DialogFormClass { get; set; } = MudExCss.Classes.Dialog.ObjectEditForm;

    /// <summary>
    /// The CSS class to apply to the action buttons of the dialog.
    /// </summary>
    [Parameter]
    public string ActionsClass { get; set; }

    /// <summary>
    /// The CSS class to apply to the title of the dialog.
    /// </summary>
    [Parameter]
    public string TitleClass { get; set; }

    /// <summary>
    /// The CSS styles to apply to the content area of the dialog.
    /// </summary>
    [Parameter]
    public string ContentStyle { get; set; }

    /// <summary>
    /// The CSS styles to apply to the dialog.
    /// </summary>
    [Parameter]
    public string DialogStyle { get; set; }

    /// <summary>
    /// The default focus location for the dialog.
    /// </summary>
    [Parameter]
    public DefaultFocus DefaultFocus { get; set; }

    /// <summary>
    /// The name of the icon to display in the dialog header area.
    /// </summary>
    [Parameter]
    public string DialogIcon { get; set; }

    /// <summary>
    /// The type of animation to display when an error occurs.
    /// </summary>
    [Parameter]
    public AnimationType ErrorAnimation { get; set; } = AnimationType.Pulse;

    /// <summary>
    /// The color of the dialog icon for when the dialog is used in relation to a particular color theme.
    /// </summary>
    [Parameter]
    public MudExColor DialogIconColor { get; set; }

    /// <summary>
    /// An optional custom submit function.
    /// If a string is returned that string will be treated as an error message and displayed in place of the default success message.
    /// Dialog will only closed if result of this function is empty or null otherwise result will displayed as error message.
    /// This can be useful to keep dialog open until server save is succeeded and not failed
    /// </summary>
    [Parameter]
    public Func<T, MudExObjectEditDialog<T>, Task<string>> CustomSubmit { get; set; }

    private string _errorMessage;

    /// <summary>
    /// Customize what happens when a property changes.
    /// </summary>
    /// <param name="property">The property that has been changed.</param>
    protected override Task OnPropertyChange(ObjectEditPropertyMeta property)
    {
        _errorMessage = null;
        return base.OnPropertyChange(property);
    }

    /// <summary>
    /// Called when the form is submitted.
    /// </summary>
    /// <param name="ctx">The edit context that represents the current state of the form's data.</param>
    protected override async Task OnSubmit(EditContext ctx)
    {
        try
        {
            IsLoading = true;
            await base.OnSubmit(ctx);
            if (CustomSubmit == null || string.IsNullOrWhiteSpace(_errorMessage = await CustomSubmit.Invoke(Value, this)))
            {
                MudDialog.CloseAnimatedIf(DialogResult.Ok(Value), JsRuntime);
            }
        }
        finally
        {
            IsLoading = false;
            CallStateHasChanged();
        }
    }

    /// <summary>
    /// Called when the Cancel button is clicked.
    /// </summary>
    protected override async Task Cancel()
    {
        await base.Cancel();
        MudDialog.CancelAnimatedIf(JsRuntime);
    }
}