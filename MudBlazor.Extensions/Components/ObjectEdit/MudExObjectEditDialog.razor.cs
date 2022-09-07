using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public partial class MudExObjectEditDialog<T>
{
    protected override bool OverwriteActionBar => true;
    protected override bool UseFormSubmit => false;
    [CascadingParameter] MudDialogInstance MudDialog { get; set; }
    [Parameter] public string DialogIcon { get; set; }
    [Parameter] public Color DialogIconColor { get; set; }

    protected override async Task Submit()
    {
        await base.Submit();
        MudDialog.Close(DialogResult.Ok(Value));
    }

    protected override async Task Cancel()
    {
        await base.Cancel();
        MudDialog.Cancel();
    }
}