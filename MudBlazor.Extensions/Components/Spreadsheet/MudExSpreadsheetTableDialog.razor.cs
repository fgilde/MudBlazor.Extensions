using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The Create Table dialog of <see cref="MudExSpreadsheetEditor"/>: asks for the range and whether it has a header row and closes with a
/// <see cref="CreateTableCommand"/> as result data.
/// </summary>
public partial class MudExSpreadsheetTableDialog
{
    private string _range = "";
    private bool _headers;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The sheet that gets the table.</summary>
    [Parameter] public Worksheet Sheet { get; set; }

    /// <summary>The proposed range.</summary>
    [Parameter] public CellRange Range { get; set; }

    /// <summary>Whether the first row of the range is proposed as header row.</summary>
    [Parameter] public bool HasHeaders { get; set; } = true;

    /// <summary>The table style, for example TableStyleMedium2.</summary>
    [Parameter] public string StyleName { get; set; } = "TableStyleMedium2";

    private bool Valid => CellRange.TryParse(_range.Replace("$", "").Trim(), out _);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _range = Range.Normalized.ToA1();
        _headers = HasHeaders;
    }

    private void Ok()
    {
        if (CellRange.TryParse(_range.Replace("$", "").Trim(), out var range))
            MudDialog.Close(DialogResult.Ok<IWorkbookCommand>(new CreateTableCommand(Sheet, range.Normalized, _headers, StyleName)));
    }

    private void Cancel() => MudDialog.Cancel();
}
