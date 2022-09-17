using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public partial class MudExObjectEditDialog<T>
{
    protected override bool OverwriteActionBar => true;
    
    [CascadingParameter] MudDialogInstance MudDialog { get; set; }
    [Parameter] public bool DisableSidePadding { get; set; }
    [Parameter] public string ClassDialog { get; set; } = "dialog-content-full-height overflow-hidden mud-ex-object-edit-dialog";
    [Parameter] public string ClassContent { get; set; } = "full-height flex-column";
    [Parameter] public string ClassDialogForm { get; set; } = "mud-ex-object-edit-dialog-form";
    [Parameter] public string ClassActions{ get; set; }
    [Parameter] public string ClassTitle{ get; set; }
    [Parameter] public string ContentStyle { get; set; }
    [Parameter] public string DialogStyle { get; set; }
    [Parameter] public DefaultFocus DefaultFocus { get; set; }
    [Parameter] public string DialogIcon { get; set; }
    [Parameter] public Color DialogIconColor { get; set; }

    protected override Task OnInitializedAsync()
    {
        StickyActionBar = true;
        return base.OnInitializedAsync();
    }

    protected override async Task OnSubmit(EditContext ctx)
    {
        await base.OnSubmit(ctx);
        MudDialog.Close(DialogResult.Ok(Value));
    }

    protected override async Task Cancel()
    {
        await base.Cancel();
        MudDialog.Cancel();
    }
}