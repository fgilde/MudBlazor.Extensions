using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The Go To dialog of <see cref="MudExSpreadsheetEditor"/>: lists the defined names and takes a name or reference such as <c>B7</c> or
/// <c>Sheet2!A1:C3</c>. Closes with the entered text as result data.
/// </summary>
public partial class MudExSpreadsheetGoToDialog
{
    private string _reference = "";

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The defined names with their references.</summary>
    [Parameter] public IReadOnlyDictionary<string, string> Names { get; set; } = new Dictionary<string, string>();

    private void Ok()
    {
        if (!string.IsNullOrWhiteSpace(_reference))
            MudDialog.Close(DialogResult.Ok(_reference.Trim()));
    }

    private void Cancel() => MudDialog.Cancel();
}
