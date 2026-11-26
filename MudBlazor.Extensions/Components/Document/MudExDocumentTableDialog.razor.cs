using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>The Insert Table dialog of <see cref="MudExDocumentEditor"/>: number of columns and rows. Closes with a <c>(int Rows, int Columns)</c> tuple.</summary>
public partial class MudExDocumentTableDialog
{
    internal const int MaxColumns = 63;
    internal const int MaxRows = 500;

    private int _columns = 5;
    private int _rows = 2;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    private void Ok() => MudDialog.Close(DialogResult.Ok((Math.Clamp(_rows, 1, MaxRows), Math.Clamp(_columns, 1, MaxColumns))));

    private void Cancel() => MudDialog.Cancel();
}
