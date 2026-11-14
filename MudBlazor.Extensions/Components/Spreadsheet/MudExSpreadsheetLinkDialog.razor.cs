using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The Insert Hyperlink dialog of <see cref="MudExSpreadsheetEditor"/>: a web or mail address or a place in the workbook, the text to display and
/// a screen tip. Closes with the new <see cref="CellLink"/> as result data.
/// </summary>
public partial class MudExSpreadsheetLinkDialog
{
    private string _display;
    private string _address;
    private string _sheet;
    private string _reference;
    private string _tooltip;
    private bool _internal;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The workbook, for the list of sheets.</summary>
    [Parameter] public Workbook Workbook { get; set; }

    /// <summary>The sheet the link is set on.</summary>
    [Parameter] public Worksheet Sheet { get; set; }

    /// <summary>The cell the link is set on.</summary>
    [Parameter] public CellRef Cell { get; set; }

    /// <summary>The link to edit, or null for a new one.</summary>
    [Parameter] public CellLink Link { get; set; }

    /// <summary>The text the cell shows now.</summary>
    [Parameter] public string DisplayText { get; set; }

    private bool Unsafe => !string.IsNullOrWhiteSpace(_address) && !new CellLink(Cell, _address.Trim(), null, null, null).IsSafe;

    private CellLink Result
    {
        get
        {
            var tooltip = string.IsNullOrWhiteSpace(_tooltip) ? null : _tooltip;
            var display = string.IsNullOrEmpty(_display) ? null : _display;
            if (!_internal)
                return string.IsNullOrWhiteSpace(_address) ? null : new CellLink(Cell, _address.Trim(), null, display, tooltip);
            if (Workbook?.FindSheet(_sheet ?? "") is not { } sheet || !CellRange.TryParse(_reference?.Trim() ?? "", out var range)) return null;
            return new CellLink(Cell, null, Workbook.QuoteSheetName(sheet.Name) + "!" + range.ToA1(), display, tooltip);
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _display = Link?.Display ?? DisplayText;
        _tooltip = Link?.Tooltip;
        _internal = Link != null && string.IsNullOrEmpty(Link.Target);
        _address = Link?.Target;
        _sheet = Sheet?.Name;
        _reference = "A1";
        var location = Link?.Location ?? "";
        var bang = location.LastIndexOf('!');
        if (!_internal || bang <= 0) return;
        _sheet = location[..bang].Trim('\'').Replace("''", "'");
        _reference = location[(bang + 1)..];
    }

    private void Ok()
    {
        if (Result is { } link)
            MudDialog.Close(DialogResult.Ok(link));
    }

    private void Cancel() => MudDialog.Cancel();
}
