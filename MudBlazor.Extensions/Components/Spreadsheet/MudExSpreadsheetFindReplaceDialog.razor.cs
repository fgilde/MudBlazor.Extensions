using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The Find and Replace dialog of <see cref="MudExSpreadsheetEditor"/>. Searches the sheet or the workbook by rows, in formulas or shown
/// values, optionally case-sensitive or for the entire cell. Find Next selects the cell, switching sheets if needed; Replace All is one undo
/// step and reports the count. While the auto filter hides rows, only visible cells are searched and replaced.
/// </summary>
public partial class MudExSpreadsheetFindReplaceDialog
{
    private int _tab;
    private string _what = "";
    private string _with = "";
    private bool _workbookScope;
    private bool _formulas = true;
    private bool _matchCase;
    private bool _entireCell;
    private string _status;
    private Worksheet _sheet;
    private CellRef _at;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The workbook to search.</summary>
    [Parameter] public Workbook Workbook { get; set; }

    /// <summary>The sheet where the search starts.</summary>
    [Parameter] public Worksheet Sheet { get; set; }

    /// <summary>The cell after which the search starts.</summary>
    [Parameter] public CellRef Start { get; set; }

    /// <summary>Whether the dialog opens on the Replace tab.</summary>
    [Parameter] public bool Replace { get; set; }

    /// <summary>Whether replacing is allowed; false shows only the Find part.</summary>
    [Parameter] public bool CanReplace { get; set; } = true;

    /// <summary>Selects a found cell, switching to its sheet.</summary>
    [Parameter] public Func<Worksheet, CellRef, Task> Select { get; set; }

    private bool Replacing => _tab == 1;

    private bool CanSearch => !string.IsNullOrEmpty(_what);

    private StringComparison Comparison => _matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _tab = Replace && CanReplace ? 1 : 0;
        _sheet = Sheet;
        _at = Start;
    }

    private async Task FindNextAsync()
    {
        _status = null;
        if (Next() is not { } hit)
        {
            _status = TryLocalize("Cannot find \"{0}\".", _what);
            return;
        }

        (_sheet, _at) = hit;
        if (Select != null) await Select(_sheet, _at);
    }

    private async Task ReplaceAsync()
    {
        if (_sheet?[_at] is { } cell && Matches(_sheet, _at, cell) && !TryWrite(() => Write(_sheet, _at, Replaced(Text(_sheet, _at, cell))), "Replace"))
            return;
        await FindNextAsync();
    }

    private void ReplaceAll()
    {
        var count = 0;
        if (TryWrite(() =>
            {
                foreach (var sheet in Sheets())
                foreach (var (at, cell) in sheet.UsedCells().Where(u => Matches(sheet, u.Ref, u.Cell)).ToList())
                {
                    Write(sheet, at, Replaced(Text(sheet, at, cell)));
                    count++;
                }
            }, "Replace All"))
            _status = TryLocalize("All done. We made {0} replacements.", count);
    }

    private bool TryWrite(Action write, string description)
    {
        try
        {
            using (Workbook.Undo.BeginTransaction(description))
                write();
            return true;
        }
        catch (Exception e)
        {
            _status = e.Message;
            return false;
        }
    }

    private IEnumerable<Worksheet> Sheets()
    {
        var sheets = Workbook.Sheets.ToList();
        var index = Math.Max(0, sheets.IndexOf(_sheet));
        if (!_workbookScope) return [sheets[index]];
        return sheets.Skip(index).Concat(sheets.Take(index)).Where(s => !s.Hidden || s == _sheet);
    }

    private (Worksheet Sheet, CellRef Cell)? Next()
    {
        static long Key(CellRef c) => (long)c.Row * CellRef.MaxColumns + c.Column;
        var after = Key(_at);
        var sheets = Sheets().ToList();
        if (First(sheets[0], k => k > after) is { } here) return (sheets[0], here);
        foreach (var sheet in sheets.Skip(1))
            if (First(sheet, _ => true) is { } there) return (sheet, there);
        return First(sheets[0], k => k <= after) is { } wrapped ? (sheets[0], wrapped) : null;

        CellRef? First(Worksheet sheet, Func<long, bool> where)
        {
            CellRef? best = null;
            foreach (var (at, cell) in sheet.UsedCells())
                if (where(Key(at)) && (best == null || Key(at) < Key(best.Value)) && Matches(sheet, at, cell))
                    best = at;
            return best;
        }
    }

    private bool Matches(Worksheet sheet, CellRef at, Cell cell)
    {
        if (cell?.HasContent != true || (sheet.FilteredRows.Count > 0 && sheet.IsRowHidden(at.Row))) return false;
        var text = Text(sheet, at, cell);
        return _entireCell ? string.Equals(text, _what, Comparison) : text.Contains(_what, Comparison);
    }

    private string Text(Worksheet sheet, CellRef at, Cell cell)
    {
        if (!_formulas && !Replacing)
            return NumberFormat.Format(cell.Value, Workbook.Styles[sheet.EffectiveStyleId(at)].NumberFormat, Culture, out _);
        if (cell.Formula != null) return "=" + cell.Formula;
        return cell.Value.Kind switch
        {
            CellValueKind.Number => cell.Value.NumberValue.ToString("G15", Culture),
            CellValueKind.Boolean => NumberFormat.BooleanText(cell.Value.BooleanValue, Culture),
            _ => cell.Value.ToString()
        };
    }

    private string Replaced(string text) => _entireCell ? _with ?? "" : text.Replace(_what, _with ?? "", Comparison);

    private void Write(Worksheet sheet, CellRef at, string text)
    {
        if (text.Length > 1 && text[0] == '=')
            Workbook.Execute(new SetFormulaCommand(sheet, at, text));
        else if (text.Length == 0)
            Workbook.Execute(new ClearCommand(sheet, new CellRange(at, at)));
        else
            Workbook.Execute(new SetValueCommand(sheet, at, Workbook.Styles[sheet.EffectiveStyleId(at)].NumberFormat == "@"
                ? CellValue.Text(text)
                : CellInputParser.Parse(text, Culture).Value));
    }

    private void Close() => MudDialog.Close();
}
