using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The Sort dialog of <see cref="MudExSpreadsheetEditor"/> with several sort levels; closes with a <see cref="SortCommand"/> as result data.
/// </summary>
public partial class MudExSpreadsheetSortDialog
{
    private readonly List<SortKey> _levels = [];
    private bool _header;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The sheet to sort.</summary>
    [Parameter] public Worksheet Sheet { get; set; }

    /// <summary>The range to sort, including a header row if <see cref="HasHeader"/> is set.</summary>
    [Parameter] public CellRange Range { get; set; }

    /// <summary>Whether the first row of the range is proposed as header that stays in place.</summary>
    [Parameter] public bool HasHeader { get; set; }

    /// <summary>The zero-based sheet column of the first sort level.</summary>
    [Parameter] public int Column { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _header = HasHeader;
        _levels.Add(new SortKey(Math.Clamp(Column, Range.Left, Range.Right)));
    }

    private string ColumnName(int column)
    {
        var text = _header && Sheet?[new CellRef(Range.Top, column)] is { HasContent: true } cell ? cell.Value.ToString() : null;
        return string.IsNullOrWhiteSpace(text) ? TryLocalize("Column {0}", CellRef.ColumnName(column)) : text;
    }

    private void AddLevel() => _levels.Add(new SortKey(_levels.Count < Range.ColumnCount ? Range.Left + _levels.Count : Range.Left));

    private void Ok() => MudDialog.Close(DialogResult.Ok<IWorkbookCommand>(new SortCommand(Sheet, Range, _levels.ToList(), _header)));

    private void Cancel() => MudDialog.Cancel();
}
