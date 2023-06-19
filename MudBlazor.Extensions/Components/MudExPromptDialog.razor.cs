using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace MudBlazor.Extensions.Components;

public partial class MudExPromptDialog
{
    [Parameter]
    public IStringLocalizer Localizer { get; set; }

    [CascadingParameter] MudDialogInstance MudDialog { get; set; }

    [Parameter] public bool Immediate { get; set; } = true;
    [Parameter] public bool SubmitOnEnter { get; set; } = true;
    [Parameter] public string OkText { get; set; } = "Ok";
    [Parameter] public string CancelText { get; set; } = "Cancel";
    [Parameter] public string Value { get; set; }
    [Parameter] public string Class { get; set; } = "mud-ex-dialog-initial";
    [Parameter] public string Message { get; set; }
    [Parameter] public string Icon { get; set; }
    [Parameter] public IEnumerable<string> Details { get; set; }
    [Parameter] public Func<string, bool> CanConfirm { get; set; } = s => true;

    void Submit()
    {
        MudDialog.Close(DialogResult.Ok(Value));
    }

    void Cancel() => MudDialog.Cancel();

    private bool IsValid() => CanConfirm(Value);

    private void OnKeyUp(KeyboardEventArgs arg)
    {
        if (arg.Key == "Enter" && SubmitOnEnter && IsValid())
        {
            Submit();
        }
    }
}