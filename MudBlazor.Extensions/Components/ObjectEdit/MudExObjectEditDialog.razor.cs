using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public partial class MudExObjectEditDialog<T>
{
    protected override bool OverwriteActionBar => true;

    [CascadingParameter] public MudDialogInstance MudDialog { get; set; }
    [Parameter] public bool DisableSidePadding { get; set; }
    [Parameter] public string ClassDialog { get; set; } = "dialog-content-full-height overflow-hidden mud-ex-object-edit-dialog";
    [Parameter] public string ClassContent { get; set; } = "full-height flex-column";
    [Parameter] public string ClassDialogForm { get; set; } = "mud-ex-object-edit-dialog-form";
    [Parameter] public string ClassActions { get; set; }
    [Parameter] public string ClassTitle { get; set; }
    [Parameter] public string ContentStyle { get; set; }
    [Parameter] public string DialogStyle { get; set; }
    [Parameter] public DefaultFocus DefaultFocus { get; set; }
    [Parameter] public string DialogIcon { get; set; }
    [Parameter] public AnimationType ErrorAnimation { get; set; } = AnimationType.Pulse;
    [Parameter] public Color DialogIconColor { get; set; }

    /// <summary>
    /// Can be used as custom submit function. Dialog will only closed if result of this function is empty or null otherwise result will displayed as error message.
    /// This is usefull to keep dialog open until server save is succeeded
    /// </summary>
    [Parameter] public Func<T, MudExObjectEditDialog<T>, Task<string>> CustomSubmit { get; set; }

    private string _errorMessage;
    
    protected override Task OnPropertyChange(ObjectEditPropertyMeta property)
    {
        _errorMessage = null;
        return base.OnPropertyChange(property);
    }

    protected override async Task OnSubmit(EditContext ctx)
    {
        try
        {
            IsLoading = true;
            await base.OnSubmit(ctx);
            if (CustomSubmit == null || string.IsNullOrWhiteSpace(_errorMessage = await CustomSubmit.Invoke(Value, this)))
                MudDialog.Close(DialogResult.Ok(Value));
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    protected override async Task Cancel()
    {
        await base.Cancel();
        MudDialog.Cancel();
    }
}